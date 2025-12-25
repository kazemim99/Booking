# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only project files first for better layer caching
COPY ["src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Booksy.ServiceCatalog.Api.csproj", "src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/"]
COPY ["src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Booksy.ServiceCatalog.Application.csproj", "src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/"]
COPY ["src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Booksy.ServiceCatalog.Domain.csproj", "src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/"]
COPY ["src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Booksy.ServiceCatalog.Infrastructure.csproj", "src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/"]
COPY ["src/Infrastructure/Booksy.API/Booksy.API.csproj", "src/Infrastructure/Booksy.API/"]
COPY ["src/Infrastructure/Booksy.Infrastructure.Security/Booksy.Infrastructure.Security.csproj", "src/Infrastructure/Booksy.Infrastructure.Security/"]
COPY ["src/Infrastructure/Booksy.Infrastructure.Core/Booksy.Infrastructure.Core.csproj", "src/Infrastructure/Booksy.Infrastructure.Core/"]
COPY ["src/Infrastructure/Booksy.Infrastructure.External/Booksy.Infrastructure.External.csproj", "src/Infrastructure/Booksy.Infrastructure.External/"]
COPY ["src/Core/Booksy.Core.Application/Booksy.Core.Application.csproj", "src/Core/Booksy.Core.Application/"]
COPY ["src/Core/Booksy.Core.Domain/Booksy.Core.Domain.csproj", "src/Core/Booksy.Core.Domain/"]

# Restore packages (this layer is cached when .csproj files don't change)
RUN dotnet restore "./src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Booksy.ServiceCatalog.Api.csproj"

# Copy only the source files needed for this service
COPY ["src/BoundedContexts/ServiceCatalog/", "src/BoundedContexts/ServiceCatalog/"]
COPY ["src/Infrastructure/", "src/Infrastructure/"]
COPY ["src/Core/", "src/Core/"]

WORKDIR "/src/src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api"

# Build (uses cached restore from previous layer)
RUN dotnet build "./Booksy.ServiceCatalog.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Booksy.ServiceCatalog.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Booksy.ServiceCatalog.Api.dll"]