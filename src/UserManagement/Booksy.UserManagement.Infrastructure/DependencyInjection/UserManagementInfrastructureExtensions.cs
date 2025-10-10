// ========================================
// Booksy.UserManagement.Infrastructure/Persistence/Seeders/UserManagementDatabaseSeeder.cs
// ========================================
using Microsoft.EntityFrameworkCore;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.UserManagement.Application.Services.Interfaces;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.Services;
using Booksy.UserManagement.Infrastructure.Persistence.Seeders;
using Booksy.UserManagement.Infrastructure.Services.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Booksy.UserManagement.Infrastructure.Persistence.Repositories;
using Booksy.UserManagement.Infrastructure.Services.Security;
using Booksy.UserManagement.Infrastructure.Services.Application;
using Booksy.Infrastructure.Core.Persistence.Base;
using Microsoft.Extensions.Logging;
using Booksy.UserManagement.Application.Abstractions.Queries;
using Booksy.UserManagement.Infrastructure.Queries;
using Booksy.Infrastructure.Core.DependencyInjection;
using Booksy.Infrastructure.External.Notifications;

namespace Booksy.UserManagement.Infrastructure.DependencyInjection
{
    public static class UserManagementInfrastructureExtensions
    {
        public static IServiceCollection AddUserManagementInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {


            services.AddInfrastructureCore(configuration);

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
                var logger = provider.GetRequiredService<ILogger<EfCoreUnitOfWork<UserManagementDbContext>>>();

                return new EfCoreUnitOfWork<UserManagementDbContext>(context, logger);
            });

            services.AddScoped<IUnitOfWork, EfCoreUnitOfWork<UserManagementDbContext>>();


            // Register Repositories

            services.AddScoped<ISeeder,UserManagementDatabaseSeeder>();
            

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserQueryRepository, UserQueryRepository>();
            services.AddScoped<IPhoneVerificationRepository, PhoneVerificationRepository>();
            services.AddScoped<IPhoneVerificationService, PhoneVerificationService>();

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

