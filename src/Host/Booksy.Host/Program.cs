// ============================================================================
// Booksy.Host — Modular Monolith entry point
// ----------------------------------------------------------------------------
// Single ASP.NET Core host that composes every bounded context (UserManagement,
// ServiceCatalog) in one process. Replaces the per-service hosts + Ocelot gateway.
// Controllers are served from the referenced *.Api assemblies; cross-context
// integration events run in-process over CAP's in-memory transport.
// ============================================================================
using AspNetCoreRateLimit;
using Booksy.API.Extensions;
using Booksy.API.Middleware;
using Booksy.Core.Domain.Infrastructure.Middleware;
using Booksy.Infrastructure.Core.DependencyInjection;
using Booksy.Infrastructure.Security;
using Booksy.Infrastructure.Security.Authorization;
using Booksy.ServiceCatalog.Application.DependencyInjection;
using Booksy.ServiceCatalog.Infrastructure.DependencyInjection;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.UserManagement.API.Extensions;
using Booksy.UserManagement.Application.DependencyInjection;
using Booksy.UserManagement.Application.EventHandlers.IntegrationEventHandlers;
using Booksy.UserManagement.Infrastructure.DependencyInjection;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Booksy.UserManagement.Infrastructure.Persistence.Seeders;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Serilog
// ---------------------------------------------------------------------------
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .WriteTo.Console()
        .WriteTo.File("logs/booksy-host-.txt", rollingInterval: RollingInterval.Day));

// ---------------------------------------------------------------------------
// JSON + Controllers (one registration; controllers discovered from both
// bounded-context API assemblies via explicit application parts)
// ---------------------------------------------------------------------------
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddControllers(options =>
    {
        options.SuppressAsyncSuffixInActionNames = false;
    })
    .AddApplicationPart(typeof(Booksy.UserManagement.API.Extensions.SwaggerExtensions).Assembly)
    .AddApplicationPart(typeof(Booksy.ServiceCatalog.Api.Extensions.SwaggerExtensions).Assembly)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.JsonSerializerOptions.AllowTrailingCommas = true;
        options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
    });

builder.Services.ConfigureApiOptions(builder.Configuration, builder.Environment);

// ---------------------------------------------------------------------------
// API Versioning + Swagger
// ---------------------------------------------------------------------------
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new HeaderApiVersionReader("X-Api-Version"),
        new QueryStringApiVersionReader("api-version"),
        new UrlSegmentApiVersionReader());
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddSwaggerConfiguration(builder.Configuration);

// ---------------------------------------------------------------------------
// Security (auth + policies) — registered once for the whole host
// ---------------------------------------------------------------------------
builder.Services.AddSecurity(builder.Configuration);
builder.Services.AddPolicyAuthorization();

// ---------------------------------------------------------------------------
// CORS
// ---------------------------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "*" })
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ---------------------------------------------------------------------------
// Client rate limiting (Redis-backed) — carried over from ServiceCatalog
// ---------------------------------------------------------------------------
builder.Services.AddMemoryCache();
builder.Services.Configure<ClientRateLimitOptions>(builder.Configuration.GetSection("ClientRateLimiting"));
builder.Services.Configure<ClientRateLimitPolicies>(builder.Configuration.GetSection("ClientRateLimitPolicies"));
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "RateLimit_";
});
builder.Services.AddDistributedRateLimiting();
builder.Services.AddSingleton<IClientResolveContributor, ClientRateLimitResolver>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// ---------------------------------------------------------------------------
// Shared infrastructure + bounded contexts
// ---------------------------------------------------------------------------
builder.Services.AddInfrastructureCore(builder.Configuration);

// UserManagement context
builder.Services.AddTransient<ProviderRegisteredEventSubscriber>();
builder.Services.AddUserManagementApplication();
builder.Services.AddUserManagementInfrastructure(builder.Configuration);

// ServiceCatalog context
builder.Services.AddServiceCatalogApplication();
builder.Services.AddServiceCatalogInfrastructureWithCache(builder.Configuration);

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// ---------------------------------------------------------------------------
// Cross-cutting services
// ---------------------------------------------------------------------------
builder.Services.AddSignalR();
builder.Services.AddResponseCompression();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// ---------------------------------------------------------------------------
// Health checks — both contexts' DbContexts + Redis
// ---------------------------------------------------------------------------
builder.Services.AddHealthChecks()
    .AddDbContextCheck<UserManagementDbContext>()
    .AddDbContextCheck<ServiceCatalogDbContext>()
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? string.Empty);

var app = builder.Build();

// ---------------------------------------------------------------------------
// HTTP pipeline
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwaggerConfiguration(app.Services.GetRequiredService<IApiVersionDescriptionProvider>());

// Response compression must precede the response-wrapping middleware
app.UseResponseCompression();
app.UseMiddleware<ApiResponseMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigins");

// Static files (ServiceCatalog serves uploaded images from wwwroot)
app.UseStaticFiles();

app.UseRouting();

// Rate limiting (before auth so it applies to all requests)
app.UseMiddleware<CustomRateLimitMiddleware>();
app.UseClientRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapControllers();
app.MapHub<Booksy.ServiceCatalog.Infrastructure.Hubs.NotificationHub>("/hubs/notifications");

// ---------------------------------------------------------------------------
// Database initialization (both contexts) — migrate, then seed in dev/test
// ---------------------------------------------------------------------------
var seed = app.Environment.IsDevelopment() || app.Environment.EnvironmentName.Contains("Test");

await app.MigrateAndSeedDatabaseAsync<UserManagementDbContext, UserManagementDatabaseSeeder>(seedData: seed);

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.InitializeDatabaseAsync(seed);
}

app.Run();

// Exposed for WebApplicationFactory-based integration tests once they are retargeted.
public partial class Program { }
