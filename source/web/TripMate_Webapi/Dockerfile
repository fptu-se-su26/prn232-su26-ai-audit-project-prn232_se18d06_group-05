# ═══════════════════════════════════════════════════════════════════════════
# TripMate Web API - Dockerfile
# Multi-stage build for optimized production image
# ═══════════════════════════════════════════════════════════════════════════

# ── Stage 1: Build ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies (for better caching)
COPY ["TripMate_Webapi/TripMate_Webapi.csproj", "TripMate_Webapi/"]
RUN dotnet restore "TripMate_Webapi/TripMate_Webapi.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/TripMate_Webapi"
RUN dotnet build "TripMate_Webapi.csproj" -c Release -o /app/build

# ── Stage 2: Publish ───────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish "TripMate_Webapi.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ── Stage 3: Runtime ───────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Create directory for uploads
RUN mkdir -p /app/wwwroot/uploads && \
    chmod 755 /app/wwwroot/uploads

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "TripMate_Webapi.dll"]
