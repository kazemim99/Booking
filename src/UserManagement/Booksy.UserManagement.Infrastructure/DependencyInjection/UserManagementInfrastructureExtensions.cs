// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/Seeders/UserManagementDatabaseSeeder.cs
// ========================================
using Microsoft.EntityFrameworkCore;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.Services;
using Booksy.UserManagement.Infrastructure.Persistence.Seeders;
using Booksy.UserManagement.Infrastructure.Services.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Booksy.UserManagement.Infrastructure.Persistence.Repositories;
using Booksy.UserManagement.Infrastructure.Services.Security;
using Booksy.UserManagement.Infrastructure.Services.External;
using Booksy.Infrastructure.Core.Persistence.Base;
using Microsoft.Extensions.Logging;
using Booksy.UserManagement.Application.Abstractions.Queries;
using Booksy.UserManagement.Infrastructure.Queries;
using Booksy.Infrastructure.Core.DependencyInjection;
using Booksy.Infrastructure.Core.EventBus;
using Booksy.Infrastructure.External.Notifications;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.UserManagement.Application.Services.Interfaces;
using Booksy.UserManagement.Infrastructure.Services.Application;

namespace Booksy.UserManagement.Infrastructure.DependencyInjection
{
    public static class UserManagementInfrastructureExtensions
    {
        public static IServiceCollection AddUserManagementInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {

            // Add DbContext
            services.AddDbContext<UserManagementDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("UserManagement")
                    ?? configuration.GetConnectionString("DefaultConnection");

                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(UserManagementDbContext).Assembly.FullName);
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "user_management");
                    npgsqlOptions.CommandTimeout(30);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                });

                // Enable logging in development
                if (configuration.GetValue<bool>("DatabaseSettings:EnableSensitiveDataLogging"))
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                    options.LogTo(Console.WriteLine, LogLevel.Information);

                }
            });

            services.AddScoped<DbContext>(provider => provider.GetRequiredService<UserManagementDbContext>());


            // Register Unit of Work
            //services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<UserManagementDbContext>());
            services.AddScoped<IUnitOfWork>(provider =>
            {
                var context = provider.GetRequiredService<UserManagementDbContext>();
                var eventDispatcher = provider.GetRequiredService<IDomainEventDispatcher>();
                var logger = provider.GetRequiredService<ILogger<EfCoreUnitOfWork<UserManagementDbContext>>>();

                return new EfCoreUnitOfWork<UserManagementDbContext>(context, logger, eventDispatcher);
            });

            services.AddScoped<IUnitOfWork, EfCoreUnitOfWork<UserManagementDbContext>>();


            // Register Repositories

            services.AddScoped<ISeeder,UserManagementDatabaseSeeder>();
            

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserQueryRepository, UserQueryRepository>();
            services.AddScoped<IPhoneVerificationRepository, PhoneVerificationRepository>();

            services.AddExternalServices(configuration);
            // Register context-specific infrastructure
            //services.AddScoped<IUnitOfWork, UserManagementUnitOfWork>();
            //services.AddScoped(typeof(IOutboxProcessor<>), typeof(UserManagementOutboxProcessor));



            // Add cached repository decorator if caching is enabled
            if (configuration.GetValue<bool>("CacheSettings:Enabled"))
            {
                services.AddScoped<IUserRepository, CachedUserRepository>();
            }

            // Register Security Services
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<TwoFactorAuthenticationService>();


            // Register Domain Services
            services.AddScoped<IUserValidationService, UserValidationService>();
            services.AddScoped<IReferralService, ReferralService>();
            services.AddScoped<IPasswordPolicy, PasswordPolicyService>();
            services.AddScoped<IReferralDomainService, ReferralDomainService>();

            // Register Application Services
            services.AddScoped<IAuditUserService, AuditUserService>();
            services.AddScoped<IUserPreferencesService, UserPreferencesService>();
            services.AddScoped<IUserRegistrationService, UserManagement.Application.Services.Implementations.UserRegistrationService>();

            // Register Database Seeder
            services.AddScoped<UserManagementDatabaseSeeder>();

            // HTTP Client for ServiceCatalog API
            services.AddHttpClient("ServiceCatalogAPI", client =>
            {
                var baseUrl = configuration["Services:ServiceCatalog:BaseUrl"]
                    ?? "https://localhost:7002/api";
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                var apiKey = configuration["Services:ServiceCatalog:ApiKey"];
                if (!string.IsNullOrEmpty(apiKey))
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                }
            });

            // Register External Services
            services.AddScoped<IProviderInfoService, ProviderInfoService>();

            // Add distributed caching
            var cacheProvider = configuration.GetValue<string>("CacheSettings:Provider");
            switch (cacheProvider?.ToLower())
            {
                case "redis":
                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = configuration.GetConnectionString("Redis");
                        options.InstanceName = "Booksy:UserManagement:";
                    });
                    break;
                default:
                    services.AddDistributedMemoryCache();
                    break;
            }

            // Add memory cache for local caching
            services.AddMemoryCache();

            // CAP Event Bus with Outbox Pattern
            services.AddCapEventBus<UserManagementDbContext>(configuration, "UserManagement");

            return services;
        }

        /// <summary>
        /// Applies pending migrations and seeds the database
        /// </summary>
        public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
            var seeder = scope.ServiceProvider.GetRequiredService<ISeeder>();

            // Apply migrations
            await context.Database.MigrateAsync();

            // Seed data
            await seeder.SeedAsync();
        }
    }
}

