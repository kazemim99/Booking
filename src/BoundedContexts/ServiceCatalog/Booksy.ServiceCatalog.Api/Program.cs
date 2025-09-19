// ========================================
// Program.cs
// ========================================
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
using Booksy.ServiceCatalog.Infrastructure.DependencyInjection;
using Booksy.ServiceCatalog.Application.DependencyInjection;
using Booksy.Infrastructure.Security.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add Configuration

// Add Core Infrastructure
builder.Services.AddInfrastructureCore(builder.Configuration);

// Add ServiceCatalog layers
builder.Services.AddServiceCatalogApplication();
builder.Services.AddServiceCatalogInfrastructure(builder.Configuration);

// Add Authentication & Authorization
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddPolicyAuthorization();

// Add API Services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add API-specific services

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Service Catalog API V1");
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "Service Catalog API V2");
    });
}

app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapControllers();

app.Run();