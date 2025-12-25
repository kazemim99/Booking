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
COPY ["src/UserManagement/Booksy.UserManagement.API/Booksy.UserManagement.API.csproj", "src/UserManagement/Booksy.UserManagement.API/"]
COPY ["src/UserManagement/Booksy.UserManagement.Application/Booksy.UserManagement.Application.csproj", "src/UserManagement/Booksy.UserManagement.Application/"]
COPY ["src/UserManagement/Booksy.UserManagement.Domain/Booksy.UserManagement.Domain.csproj", "src/UserManagement/Booksy.UserManagement.Domain/"]
COPY ["src/UserManagement/Booksy.UserManagement.Infrastructure/Booksy.UserManagement.Infrastructure.csproj", "src/UserManagement/Booksy.UserManagement.Infrastructure/"]
COPY ["src/Infrastructure/Booksy.API/Booksy.API.csproj", "src/Infrastructure/Booksy.API/"]
COPY ["src/Infrastructure/Booksy.Infrastructure.Security/Booksy.Infrastructure.Security.csproj", "src/Infrastructure/Booksy.Infrastructure.Security/"]
COPY ["src/Infrastructure/Booksy.Infrastructure.Core/Booksy.Infrastructure.Core.csproj", "src/Infrastructure/Booksy.Infrastructure.Core/"]
COPY ["src/Infrastructure/Booksy.Infrastructure.External/Booksy.Infrastructure.External.csproj", "src/Infrastructure/Booksy.Infrastructure.External/"]
COPY ["src/Core/Booksy.Core.Application/Booksy.Core.Application.csproj", "src/Core/Booksy.Core.Application/"]
COPY ["src/Core/Booksy.Core.Domain/Booksy.Core.Domain.csproj", "src/Core/Booksy.Core.Domain/"]

# Restore packages (this layer is cached when .csproj files don't change)
RUN dotnet restore "./src/UserManagement/Booksy.UserManagement.API/Booksy.UserManagement.API.csproj"

# Copy only the source files needed for this service
COPY ["src/UserManagement/", "src/UserManagement/"]
COPY ["src/Infrastructure/", "src/Infrastructure/"]
COPY ["src/Core/", "src/Core/"]

WORKDIR "/src/src/UserManagement/Booksy.UserManagement.API"

# Build (uses cached restore from previous layer)
RUN dotnet build "./Booksy.UserManagement.API.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Booksy.UserManagement.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Booksy.UserManagement.API.dll"]