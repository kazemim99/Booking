# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only project files first for better layer caching
COPY ["src/APIGateway/Booksy.Gateway/Booksy.Gateway.csproj", "src/APIGateway/Booksy.Gateway/"]

# Restore with BuildKit cache mount for NuGet packages
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore "./src/APIGateway/Booksy.Gateway/Booksy.Gateway.csproj"

# Copy only the source files needed for this service
COPY ["src/APIGateway/", "src/APIGateway/"]

WORKDIR "/src/src/APIGateway/Booksy.Gateway"

# Build with cache mount
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet build "./Booksy.Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish "./Booksy.Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Booksy.Gateway.dll"]