// ============================================================================
// AsanRezerve.Host — Modular Monolith entry point
// ----------------------------------------------------------------------------
// Single ASP.NET Core host that composes every bounded context (UserManagement,
// ServiceCatalog) in one process. Replaces the per-service hosts + Ocelot gateway.
// Controllers are served from the referenced *.Api assemblies; cross-context
// integration events run in-process over CAP's in-memory transport.
// ============================================================================
using AspNetCoreRateLimit;
using AsanRezerve.API.Extensions;
using AsanRezerve.API.Middleware;
using AsanRezerve.API.RateLimiting;
using AsanRezerve.Core.Domain.Infrastructure.Middleware;
using AsanRezerve.Host.Composition;
using AsanRezerve.Infrastructure.Core.DependencyInjection;
using AsanRezerve.Infrastructure.Security;
using AsanRezerve.Infrastructure.Security.Authorization;
using AsanRezerve.ServiceCatalog.Application.DependencyInjection;
using AsanRezerve.ServiceCatalog.Infrastructure.DependencyInjection;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using AsanRezerve.UserManagement.API.Extensions;
using AsanRezerve.UserManagement.Application.DependencyInjection;
using AsanRezerve.UserManagement.Application.EventHandlers.IntegrationEventHandlers;
using AsanRezerve.UserManagement.Application.Services.Interfaces;
using AsanRezerve.UserManagement.Infrastructure.DependencyInjection;
using AsanRezerve.UserManagement.Infrastructure.Persistence.Context;
using AsanRezerve.UserManagement.Infrastructure.Persistence.Seeders;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
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
        .WriteTo.File("logs/asanrezerve-host-.txt", rollingInterval: RollingInterval.Day));

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
    .AddApplicationPart(typeof(AsanRezerve.UserManagement.API.Extensions.SwaggerExtensions).Assembly)
    .AddApplicationPart(typeof(AsanRezerve.ServiceCatalog.Api.Extensions.SwaggerExtensions).Assembly)
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
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();

        if (builder.Environment.IsDevelopment())
        {
            // Flutter's web dev server (and Vite, if its default ports are
            // taken) picks a random port per run, so a static allowlist
            // constantly falls behind. Locally, trust any localhost/127.0.0.1
            // origin on any port instead of chasing ports one at a time.
            policy.SetIsOriginAllowed(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                (uri.Host == "localhost" || uri.Host == "127.0.0.1"));
        }
        else
        {
            policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>());
        }
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

// The named policies the [EnableRateLimiting] attributes refer to. Until this existed they
// resolved to nothing, so OTP, password reset and registration had only the blanket client rule.
builder.Services.AddAsanRezerveRateLimiting(builder.Configuration);

// ---------------------------------------------------------------------------
// Shared infrastructure + bounded contexts
// ---------------------------------------------------------------------------
builder.Services.AddInfrastructureCore(builder.Configuration);

// UserManagement context
builder.Services.AddTransient<ProviderRegisteredEventSubscriber>();
builder.Services.AddUserManagementApplication(builder.Configuration);
builder.Services.AddUserManagementInfrastructure(builder.Configuration);

// ServiceCatalog context
builder.Services.AddServiceCatalogApplication();
builder.Services.AddServiceCatalogInfrastructureWithCache(builder.Configuration);

// Cross-context composition: serve UserManagement's provider lookup in-process rather
// than over a loopback HTTP call that the host's own auth fallback policy rejects.
// Registered AFTER AddUserManagementInfrastructure so this replaces the HTTP adapter it
// registers (last registration wins for a single-service resolve), and after
// AddServiceCatalogApplication so the query handler it dispatches to is available.
// See InProcessProviderInfoService for why the adapter belongs in the Host.
builder.Services.AddScoped<IProviderInfoService, InProcessProviderInfoService>();

// The reverse direction: ServiceCatalog needs UserManagement to mint a token carrying provider
// claims. Registered here for the same reasons and with the same ordering constraint — it must come
// after AddServiceCatalogInfrastructure, which registers the HTTP TokenService this replaces.
// See InProcessTokenService for the two loopback callers it retires.
builder.Services
    .AddScoped<AsanRezerve.ServiceCatalog.Application.Services.Interfaces.ITokenService, InProcessTokenService>();

// Third seam in the same direction: the invitation register-and-accept flow creates and
// (on failure) compensates by deleting a UserManagement account. Not an "override" like the
// two above — IPersonAccountProvisioningService has no prior HTTP-based registration to
// replace, this is its only registration — but it needs the same ordering: after
// AddUserManagementInfrastructure (IPersonProvisioningService, IUserRepository) and after
// AddServiceCatalogInfrastructure (InvitationRegistrationService, which now depends on it).
// See InProcessPersonAccountProvisioningService for the two loopback callers it retires.
builder.Services
    .AddScoped<AsanRezerve.ServiceCatalog.Application.Services.Interfaces.IPersonAccountProvisioningService,
        InProcessPersonAccountProvisioningService>();

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
// The web root holds only the git-ignored uploads folder, so on a fresh checkout it does not exist at startup and
// ASP.NET would serve no static files at all — it is created here (a no-op in the image, which creates it).
// Every response varies by Origin: photos are cached "public" for 30 days and all *.nahalkmi.ir apps share one
// browser cache, so a copy fetched by a plain <img> (no Origin, so no CORS header) could answer the Flutter apps'
// CORS fetch of the same URL, which then failed. CORS adds "Vary: Origin" itself when the request carries an Origin
// (openspec/changes/_inline/salon-images-load, G7).
var webRoot = Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "wwwroot")).FullName;
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(webRoot),
    OnPrepareResponse = context =>
    {
        if (!context.Context.Request.Headers.ContainsKey(HeaderNames.Origin))
            context.Context.Response.Headers.Append(HeaderNames.Vary, HeaderNames.Origin);
    },
});

app.UseRouting();

// Rate limiting (before auth so it applies to all requests)
app.UseMiddleware<CustomRateLimitMiddleware>();
app.UseClientRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

// After authentication on purpose: the per-policy partition prefers the authenticated user id, so
// it has to run once the principal exists. Endpoint-scoped policies apply at endpoint execution.
app.UseRateLimiter();

// Health probes must stay anonymous — the global fallback policy (C1) would otherwise
// 401 Docker/K8s/curl liveness+readiness probes and mark the container unhealthy.
app.MapHealthChecks("/health").AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();

app.MapControllers();
app.MapHub<AsanRezerve.ServiceCatalog.Infrastructure.Hubs.NotificationHub>("/hubs/notifications");

// ---------------------------------------------------------------------------
// Database initialization (both contexts) — migrate, then seed in dev/test
// ---------------------------------------------------------------------------
// Seeding is opt-out by configuration, not by the environment's NAME. `EnvironmentName.Contains("Test")`
// meant every WebApplicationFactory-based test class — which runs as "Test" — seeded the full development
// data set (providers, staff, services, notification templates, provinces, payments, payouts, reviews)
// into its own throwaway database before its first test ran. Development still seeds by default; the test
// factories set Database:SeedOnStartup=false in appsettings.Testing.json.
var seed = builder.Configuration.GetValue("Database:SeedOnStartup", app.Environment.IsDevelopment());

// Reference data (Iran's province/city hierarchy, notification templates) is a separate switch that
// defaults to ON everywhere, production included. It used to ride on SeedOnStartup above, which is
// off outside Development — so production shipped with an empty ProvinceCities table and the provider
// onboarding city picker could never find a city. Test hosts set it false purely for startup speed.
var seedReferenceData = builder.Configuration.GetValue("Database:SeedReferenceData", true);

await app.MigrateAndSeedDatabaseAsync<UserManagementDbContext, UserManagementDatabaseSeeder>(seedData: seed);

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.InitializeDatabaseAsync(seedDemoData: seed, seedReferenceData: seedReferenceData);
}

// The demo salon's reviews, by named demo customers (openspec/changes/_inline/customer-reviews-and-nahal-seed). Its own
// switch, defaulting to SeedOnStartup — so Development gets them and production never does unless someone decides so.
// Idempotent, and logged rather than rethrown: a demo review is never worth failing startup on a shared box.
if (builder.Configuration.GetValue("Database:SeedDemoReviews", seed))
{
    using var scope = app.Services.CreateScope();
    try
    {
        await DemoSalonReviews.SeedAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        scope.ServiceProvider.GetRequiredService<ILogger<Program>>()
            .LogError(ex, "Seeding the demo salon's reviews failed");
    }
}

app.Run();

// Exposed for WebApplicationFactory-based integration tests once they are retargeted.
public partial class Program { }
