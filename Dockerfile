# ============================================================================
# Multi-stage Dockerfile for Quadspace - .NET 10 Blazor WebAssembly App
# Production-optimized with security best practices
# ============================================================================

# Stage 1: Build
# Use the full SDK image with all build tools
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /src

# Copy the solution and project files first so the restore layer is cached
# until a project file changes. Destinations MUST preserve the real "src/..." layout
# so the later "COPY . ." overlays source onto the same project directories.
COPY ["quadspace.sln", "./"]
COPY ["src/Quadspace.Core/Quadspace.Core.csproj", "src/Quadspace.Core/"]
COPY ["src/Quadspace.Client/Quadspace.Client.csproj", "src/Quadspace.Client/"]
COPY ["src/Quadspace.Host/Quadspace.Host.csproj", "src/Quadspace.Host/"]

# Restore dependencies (cached until a project file changes)
RUN dotnet restore "src/Quadspace.Host/Quadspace.Host.csproj"

# Copy all source code
COPY . .

# Build the host (this also builds the referenced Blazor WebAssembly client).
RUN dotnet build "src/Quadspace.Host/Quadspace.Host.csproj" \
    --configuration Release \
    --no-restore \
    -p:DebugType=none \
    -p:DebugSymbols=false

# Publish the application (bundles the client's published WASM assets into wwwroot).
# NOTE: PublishReadyToRun must stay false — R2R/crossgen cannot be applied to the
# Blazor WebAssembly (Client) assemblies and fails the publish with NETSDK1095.
# DebugType=none prevents the publish step from looking for PDB files that don't exist.
# We must NOT use --no-build here because the _framework folder generation requires
# the full publish pipeline to run, which includes bundling the Blazor client.
RUN dotnet publish "src/Quadspace.Host/Quadspace.Host.csproj" \
    --configuration Release \
    --output /app/publish \
    -p:PublishTrimmed=false \
    -p:PublishReadyToRun=false \
    -p:PublishSingleFile=false \
    -p:DebugType=none \
    -p:DebugSymbols=false

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

# Verify wwwroot exists and has Blazor files
RUN ls -la /app/ && \
    ls -la /app/wwwroot/ 2>/dev/null || echo "WARNING: wwwroot folder missing or empty"

# Create directory for score data with proper permissions
RUN mkdir -p /app/scores && \
    chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8090

# Set environment variables
ENV DOTNET_URLS=http://+:8090 \
    ASPNETCORE_URLS=http://+:8090 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

# Health check for container orchestration
# Checks if the application is responding to HTTP requests
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8090/api/scores/top?count=1 || exit 1

# Start the application
ENTRYPOINT ["dotnet", "Quadspace.Host.dll"]
