// ========================================
// Program.cs
// ========================================
using AsanRezerve.API.Extensions;
using AsanRezerve.API.Middleware;
using AsanRezerve.API.RateLimiting;
using AsanRezerve.Core.Domain.Infrastructure.Middleware;
using AsanRezerve.Infrastructure.Core.DependencyInjection;
using AsanRezerve.Infrastructure.Core.Persistence.Base;
using AsanRezerve.Infrastructure.Security;
using AsanRezerve.Infrastructure.Security.Authorization;
using AsanRezerve.UserManagement.API.Extensions;
using AsanRezerve.UserManagement.Application.DependencyInjection;
using AsanRezerve.UserManagement.Application.EventHandlers.IntegrationEventHandlers;
using AsanRezerve.UserManagement.Infrastructure.DependencyInjection;
using AsanRezerve.UserManagement.Infrastructure.Persistence.Context;
using AsanRezerve.UserManagement.Infrastructure.Persistence.Seeders;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;
var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .WriteTo.Console()
        .WriteTo.File("logs/asanrezerve-usermanagement-.txt", rollingInterval: RollingInterval.Day));

builder.Services.AddTransient<ProviderRegisteredEventSubscriber>();

//builder.Services.Scan(scan => scan
// .FromAssemblyOf<ProviderRegisteredEventSubscriber>()
// .AddClasses(classes => classes.AssignableTo<ICapSubscribe>())
// .AsSelfWithInterfaces()
// .WithTransientLifetime());


// Configure JSON serialization options
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});


// Add services to the container
builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
}).AddJsonOptions(options =>
{
    // Handle camelCase property names from client
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

    // Handle enums as strings
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

    // Allow trailing commas in JSON
    options.JsonSerializerOptions.AllowTrailingCommas = true;

    // Handle comments in JSON (for development)
    options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
});
;


builder.Services.ConfigureApiOptions(builder.Configuration, builder.Environment);


// API Versioning
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

// Add Swagger
builder.Services.AddSwaggerConfiguration(builder.Configuration);

// Add Authentication & Authorization
builder.Services.AddPolicyAuthorization();



// Add Rate Limiting
builder.Services.AddSecurity(builder.Configuration);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        policy =>
        {
            policy.WithOrigins(builder.Configuration
                .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "*" })
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// Add Application Services
builder.Services.AddUserManagementApplication(builder.Configuration);

// The named policies referenced by [EnableRateLimiting] on the auth and user endpoints.
builder.Services.AddAsanRezerveRateLimiting(builder.Configuration);
builder.Services.AddUserManagementInfrastructure(builder.Configuration);

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<UserManagementDbContext>()
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? string.Empty);


// Add Response Compression
builder.Services.AddResponseCompression();

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Add API Behaviors
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Add Swagger UI
app.UseSwaggerConfiguration(app.Services.GetRequiredService<IApiVersionDescriptionProvider>());

// Response Compression (must be before ApiResponseMiddleware to avoid encoding conflicts)
app.UseResponseCompression();

// API Response Wrapper (must be before exception handler to avoid stream conflicts)
app.UseMiddleware<ApiResponseMiddleware>();

// Global Exception Handler
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Request Logging
app.UseMiddleware<RequestLoggingMiddleware>();

// HTTPS Redirection
app.UseHttpsRedirection();

// CORS
app.UseCors("AllowSpecificOrigins");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// After authentication: the per-policy partition prefers the authenticated user id.
app.UseRateLimiter();

// Health Checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Map Controllers
app.MapControllers();

// Initialize Database (migrations + seeding for dev/test)
// Seeding is opt-out by configuration, not by the environment's NAME — see the same change in
// AsanRezerve.Host/Program.cs. Development still seeds by default; the test factories set
// Database:SeedOnStartup=false in appsettings.Testing.json.
await app.MigrateAndSeedDatabaseAsync<UserManagementDbContext, UserManagementDatabaseSeeder>(
    seedData: builder.Configuration.GetValue("Database:SeedOnStartup", app.Environment.IsDevelopment()));

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
