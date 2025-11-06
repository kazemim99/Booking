// ========================================
// Program.cs
// ========================================
using Booksy.UserManagement.API.Extensions;
using Booksy.UserManagement.Application.DependencyInjection;
using Booksy.UserManagement.Infrastructure.DependencyInjection;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Booksy.UserManagement.Infrastructure.Persistence.Seeders;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Serilog;
using Booksy.Infrastructure.Security.Authorization;
using Booksy.Infrastructure.Security;
using System.Text.Json.Serialization;
using System.Text.Json;
using Booksy.Infrastructure.Core.DependencyInjection;
using Booksy.API.Middleware;
using Booksy.API.Extensions;
using Booksy.Core.Domain.Infrastructure.Middleware;
using Booksy.UserManagement.Application.EventHandlers.IntegrationEventHandlers;
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
        .WriteTo.File("logs/booksy-usermanagement-.txt", rollingInterval: RollingInterval.Day));

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
builder.Services.AddUserManagementApplication();
builder.Services.AddUserManagementInfrastructure(builder.Configuration);



AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);


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
app.UseMiddleware<ApiResponseMiddleware>();



// Response Compression
app.UseResponseCompression();

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

// Seed Database (Development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.InitializeDatabaseAsync();
}

app.Run();
