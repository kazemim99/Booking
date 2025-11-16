using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;
using Booksy.API.Extensions;
using Booksy.API.Middleware;
using Booksy.Infrastructure.Security;
using Booksy.Infrastructure.Security.Authorization;
using Booksy.ServiceCatalog.Application.DependencyInjection;
using Booksy.ServiceCatalog.Infrastructure.DependencyInjection;
using Booksy.ServiceCatalog.Api.Extensions;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.Core.Domain.Infrastructure.Middleware;
using Booksy.API.Observability;
using Booksy.Infrastructure.Core.DependencyInjection;
using System.Configuration;
using AspNetCoreRateLimit;

namespace Booksy.API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        private IWebHostEnvironment Environment {get;}
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {

            services.AddSecurity(Configuration);

            // Configure JSON serialization options
            services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });




            // Controllers
            services.AddControllers(options =>
            {
                options.SuppressAsyncSuffixInActionNames = false;
            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                options.JsonSerializerOptions.AllowTrailingCommas = true;
                options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
            });

            services.ConfigureApiOptions(Configuration,Environment);

            // API Versioning
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new HeaderApiVersionReader("X-Api-Version"),
                    new QueryStringApiVersionReader("api-version"),
                    new UrlSegmentApiVersionReader());
            });

            services.AddVersionedApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            // Swagger
            services.AddSwaggerConfiguration(Configuration);

            // Auth & Security
            services.AddPolicyAuthorization();

            // CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigins", policy =>
                {
                    policy.WithOrigins(Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "*" })
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            // Rate Limiting with Redis
            services.AddMemoryCache();
            services.Configure<ClientRateLimitOptions>(Configuration.GetSection("ClientRateLimiting"));
            services.Configure<ClientRateLimitPolicies>(Configuration.GetSection("ClientRateLimitPolicies"));

            // Use Redis for distributed rate limiting
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = Configuration.GetConnectionString("Redis");
                options.InstanceName = "RateLimit_";
            });
            services.AddDistributedRateLimiting();

            services.AddSingleton<IClientResolveContributor, ClientRateLimitResolver>();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            //SerilogConfiguration.ConfigureSerilog(Configuration, "ServiceCatalog.API");

            // Infrastructure Core (CQRS, Caching, Event Bus, etc.)
            services.AddInfrastructureCore(Configuration);

            // Application Services
            services.AddServiceCatalogApplication();

            // Use cached version for Redis/InMemory caching on read repositories
            services.AddServiceCatalogInfrastructureWithCache(Configuration);

            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            // Other services
            services.AddResponseCompression();
            services.AddHttpContextAccessor();

            // SignalR for real-time notifications
            services.AddSignalR();

            // API Behaviors
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Swagger
            app.UseSwaggerConfiguration(provider);

            // Response Compression (must be before ApiResponseMiddleware to avoid encoding conflicts)
            app.UseResponseCompression();

            // API Response Wrapper (must be before exception handler to avoid stream conflicts)
            app.UseMiddleware<ApiResponseMiddleware>();

            // Global Middlewares
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseMiddleware<RequestLoggingMiddleware>();

            app.UseHttpsRedirection();

            // CORS
            app.UseCors("AllowSpecificOrigins");

            // Static Files - serve uploaded images
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    Path.Combine(env.ContentRootPath, "wwwroot")),
                RequestPath = ""
            });


            app.UseRouting();

            // Custom Rate Limit Response Middleware (must be before ClientRateLimitMiddleware)
            app.UseMiddleware<CustomRateLimitMiddleware>();

            // Client Rate Limiting (before authentication to apply to all requests)
            app.UseClientRateLimiting();

            // Auth
            app.UseAuthentication();
            app.UseAuthorization();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
                endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("ready")
                });
                endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
                {
                    Predicate = _ => false
                });

                endpoints.MapControllers();

                // Map SignalR hubs
                endpoints.MapHub<Booksy.ServiceCatalog.Infrastructure.Hubs.NotificationHub>("/hubs/notifications");
            });

            // Run database seeder in development
            if (env.IsDevelopment() || env.EnvironmentName.Contains("Test"))
            {
                using var scope = app.ApplicationServices.CreateScope();
                scope.ServiceProvider.InitializeDatabaseAsync().GetAwaiter().GetResult();
            }
        }
    }
}
