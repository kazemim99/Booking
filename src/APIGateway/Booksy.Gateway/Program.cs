using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Load Ocelot configuration based on environment
var environment = builder.Environment.EnvironmentName.ToLower();
var ocelotConfigFile = $"Configuration/ocelot.{environment}.json";

Console.WriteLine($"Loading Ocelot configuration from: {ocelotConfigFile}");

builder.Configuration.AddJsonFile(
    ocelotConfigFile,
    optional: false,
    reloadOnChange: true
);

// Add Ocelot services
builder.Services.AddOcelot(builder.Configuration);

// Optional: Add OpenAPI for development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
}

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Use Ocelot middleware - this replaces all other routing
Console.WriteLine("Starting Ocelot API Gateway...");
await app.UseOcelot();

app.Run();
