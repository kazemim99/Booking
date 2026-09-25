// ========================================
// Extensions/SwaggerExtensions.cs
// ========================================
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AsanRezerve.ServiceCatalog.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            // Enable better support for nullable reference types and modern C# features
            options.SupportNonNullableReferenceTypes();

            // Use full type name for schema IDs to avoid naming conflicts
            options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "AsanRezerve Service Catalog API",
                Version = "v1",
                Description = @"API for managing service providers, bookings, and services in the AsanRezerve platform.

## People, salons and memberships
A **Provider** is a salon. A **Person** may own at most one salon and hold any number of
**memberships** in others; working somewhere never creates a Provider of your own, so one
person can work at several salons while remaining a single identity.

A membership carries org-scoped roles (Owner, Manager, StaffProvider, Receptionist) and,
for anyone who provides services, a staff profile with their working week and the services
they perform at that salon.

### Key features:
- Register a salon
- Invite people to join it, including people who have no app account yet
- Manage members, their roles, schedules and service assignments
- Book a specific member, or the salon itself",
                Contact = new OpenApiContact
                {
                    Name = "AsanRezerve Team",
                    Email = "support@asanrezerve.com"
                }
            });

            options.SwaggerDoc("v2", new OpenApiInfo
            {
                Title = "AsanRezerve Service Category API",
                Version = "v2",
                Description = "API v2 with enhanced features"
            });

            // Add JWT Authentication
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                     new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
                    Array.Empty<string>()
                }
            });

            // Add XML comments
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Custom operation filter for API versioning
            options.OperationFilter<ApiVersionOperationFilter>();

            // Custom schema filters
            options.SchemaFilter<EnumSchemaFilter>();
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerConfiguration(
        this IApplicationBuilder app,
        IApiVersionDescriptionProvider provider)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    description.GroupName.ToUpperInvariant());
            }

            options.RoutePrefix = string.Empty;
            options.DocumentTitle = "AsanRezerve Service Category API";
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            options.EnableDeepLinking();
            options.ShowExtensions();
        });

        return app;
    }
}



public class ApiVersionOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiVersion = context.ApiDescription
            .ActionDescriptor.Properties
            .Where(p => p.Key.Equals("ApiVersion"))
            .Select(p => p.Value?.ToString())
            .FirstOrDefault();

        if (apiVersion != null)
        {
            operation.Parameters ??= new List<OpenApiParameter>();
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "api-version",
                In = ParameterLocation.Query,
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Default = new Microsoft.OpenApi.Any.OpenApiString(apiVersion)
                }
            });
        }
    }
}


public class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            schema.Enum.Clear();
            Enum.GetNames(context.Type).ToList().ForEach(name =>
            {
                schema.Enum.Add(new Microsoft.OpenApi.Any.OpenApiString(name));
            });
        }
    }
}
