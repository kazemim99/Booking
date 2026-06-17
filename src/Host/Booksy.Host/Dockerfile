# Modular-monolith host image. Build context is the repository root
# (see <DockerfileContext> in Booksy.Host.csproj).

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# nuget.config pins restore to nuget.org (the repo's only feed). Copy it first so the
# restore layer is cached independently of source changes.
COPY nuget.config ./
COPY src/ src/

RUN dotnet restore "src/Host/Booksy.Host/Booksy.Host.csproj"
RUN dotnet publish "src/Host/Booksy.Host/Booksy.Host.csproj" \
    -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Booksy.Host.dll"]
