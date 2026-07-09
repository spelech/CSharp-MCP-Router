# Stage 1: Build the C# application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy csproj and restore dependencies
COPY mcp-router.csproj ./
RUN dotnet restore mcp-router.csproj

# Copy source and publish
COPY . ./
RUN dotnet publish mcp-router.csproj -c Release -o /app

# Stage 2: Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install native dependencies for SQLite / SQLCipher bundle if any
RUN apt-get update && apt-get install -y --no-install-recommends \
    libsqlite3-dev \
    && rm -rf /var/lib/apt/lists/*

# Copy the published app
COPY --from=build /app .

# Expose port
EXPOSE 8080

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

# Run the app
ENTRYPOINT ["dotnet", "mcp-router.dll"]
