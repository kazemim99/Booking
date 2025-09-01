//using Microsoft.Extensions.Options;
//using Microsoft.OpenApi.Models;
//using System.Reflection;

//namespace Booksy.UserManagement.API.Extensions;

///// <summary>
///// OpenAPI/Swagger configuration extensions
///// </summary>
//public static class OpenApiExtensions
//{
//    /// <summary>
//    /// Adds OpenAPI documentation configuration
//    /// </summary>
//    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
//    {
//        services.AddEndpointsApiExplorer();

//        services.AddSwaggerGen(c =>
//        {
//            c.SwaggerDoc("v1", new OpenApiInfo
//            {
//                Title = "Booksy User Management API",
//                Version = "v1.0.0",
//                Description = "User Management API for Booksy booking platform",
//                Contact = new OpenApiContact
//                {
//                    Name = "Booksy Development Team",
//                    Email = "dev@booksy.com"
//                },
//                License = new OpenApiLicense
//                {
//                    Name = "MIT License",
//                    Url = new Uri("https://opensource.org/licenses/MIT")
//                }
//            });

//            //c.ExampleFilters();
//            // Add JWT Authentication
//            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//            {
//                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
//                Name = "Authorization",
//                In = ParameterLocation.Header,
//                Type = SecuritySchemeType.ApiKey,
//                Scheme = "Bearer"
//            });

//            c.AddSecurityRequirement(new OpenApiSecurityRequirement
//            {
//                {
//                    new OpenApiSecurityScheme
//                    {
//                        Reference = new OpenApiReference
//                        {
//                            Type = ReferenceType.SecurityScheme,
//                            Id = "Bearer"
//                        }
//                    },
//                    Array.Empty<string>()
//                }
//            });

//            // Include XML comments
//            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
//            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
//            if (File.Exists(xmlPath))
//            {
//                c.IncludeXmlComments(xmlPath);
//            }

//            // Add examples for better documentation
//            c.EnableAnnotations();

//            // Group by controller
//            c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
//            c.DocInclusionPredicate((name, api) => true);

//            // Add response types
//            c.OperationFilter<ResponseTypesOperationFilter>();
//        });

//        return services;
//    }
//}

///// <summary>
///// Operation filter to add standard response types
///// </summary>
//public class ResponseTypesOperationFilter : Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter
//{
//    public void Apply(OpenApiOperation operation, Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
//    {
//        // Add standard error responses
//        if (!operation.Responses.ContainsKey("400"))
//        {
//            operation.Responses.Add("400", new OpenApiResponse
//            {
//                Description = "Bad Request - Invalid input parameters"
//            });
//        }

//        if (!operation.Responses.ContainsKey("500"))
//        {
//            operation.Responses.Add("500", new OpenApiResponse
//            {
//                Description = "Internal Server Error"
//            });
//        }

//        // Add specific responses for POST operations
//        if (context.MethodInfo.Name.Contains("Post") || context.MethodInfo.Name.Contains("Create"))
//        {
//            if (!operation.Responses.ContainsKey("201"))
//            {
//                operation.Responses.Add("201", new OpenApiResponse
//                {
//                    Description = "Resource created successfully"
//                });
//            }

//            if (!operation.Responses.ContainsKey("409"))
//            {
//                operation.Responses.Add("409", new OpenApiResponse
//                {
//                    Description = "Conflict - Resource already exists"
//                });
//            }
//        }
//    }
//}