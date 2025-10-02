using Booksy.Core.Domain.Infrastructure.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Booksy.API.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Configure API behavior options for pagination
    /// </summary>
    public static IServiceCollection ConfigureApiOptions(this IServiceCollection services, IWebHostEnvironment env)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            // Customize model validation responses to work with pagination
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                var result = new
                {
                    Message = "Validation failed",
                    Errors = errors
                };

                return new BadRequestObjectResult(result);
            };
        });


        services.Configure<ApiResponseOptions>(options =>
        {
            options.ApiPathPrefix = "/api";
            options.WriteIndented = env.IsDevelopment();
            options.ApiVersion = "1.0";
            options.IncludeMetadata = true;
        });
        return services;
    }
}
