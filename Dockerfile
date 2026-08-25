# ============================================================================
# Multi-stage Dockerfile for Quadspace - .NET 10 Blazor WebAssembly App
# Production-optimized with security best practices
# ============================================================================

# Stage 1: Build
# Use the full SDK image with all build tools
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

# Copy solution and project files
COPY ["quadspace.sln", "."]
COPY ["src/Quadspace.Core/Quadspace.Core.csproj", "src/Quadspace.Core/"]
COPY ["src/Quadspace.Client/Quadspace.Client.csproj", "src/Quadspace.Client/"]
COPY ["src/Quadspace.Host/Quadspace.Host.csproj", "src/Quadspace.Host/"]

# Restore dependencies
# This layer is cached until project files change
RUN dotnet restore "src/Quadspace.Host/Quadspace.Host.csproj"

# Copy all source code
COPY . .

# Build the application
# PublishTrimmed reduces size by removing unused IL
# PublishReadyToRun improves startup time
RUN dotnet build "src/Quadspace.Host/Quadspace.Host.csproj" \
    --configuration Release \
    --no-restore \
    -p:DebugType=none \
    -p:DebugSymbols=false

# Publish the application
RUN dotnet publish "src/Quadspace.Host/Quadspace.Host.csproj" \
    --configuration Release \
    --no-build \
    --output /app/publish \
    -p:PublishTrimmed=false \
    -p:PublishReadyToRun=true \
    -p:PublishSingleFile=false

# ============================================================================
# Stage 2: Runtime (Slim)
# Use minimal runtime image for production deployment
# ============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

# Set metadata
LABEL maintainer="Strove Labs"
LABEL description="Quadspace - Retro Arcade Game"
LABEL version="1.0.0"

# Install required dependencies for Alpine
RUN apk add --no-cache \
    ca-certificates \
    tzdata \
    curl

# Create non-root user for security
# Running as root in containers is a security risk
RUN addgroup -g 1001 -S appuser && \
    adduser -u 1001 -S appuser -G appuser

WORKDIR /app

# Copy published application from build stage
COPY --from=build /app/publish .

# Create directory for score data with proper permissions
RUN mkdir -p /app/scores && \
    chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV DOTNET_URLS=http://+:8080 \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check for container orchestration
# Checks if the application is responding to HTTP requests
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/api/scores/top?count=1 || exit 1

# Start the application
ENTRYPOINT ["dotnet", "Quadspace.Host.dll"]
