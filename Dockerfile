# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file and restore dependencies
COPY ["LMS.Backend.sln", "./"]
COPY ["LMS.API/LMS.API.csproj", "./LMS.API/"]
COPY ["LMS.Application/LMS.Application.csproj", "./LMS.Application/"]
COPY ["LMS.Domain/LMS.Domain.csproj", "./LMS.Domain/"]
COPY ["LMS.Infrastructure/LMS.Infrastructure.csproj", "./LMS.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "LMS.Backend.sln"

# Copy all source files
COPY . .

# Build the solution
WORKDIR "/src/LMS.API"
RUN dotnet build "LMS.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "LMS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official .NET 8 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install MySQL client for health checks
RUN apt-get update && \
    apt-get install -y default-mysql-client && \
    rm -rf /var/lib/apt/lists/*

# Create a non-root user
RUN addgroup --system --gid 1001 dotnetgroup && \
    adduser --system --uid 1001 --ingroup dotnetgroup dotnetuser

# Copy published files
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R dotnetuser:dotnetgroup /app

# Switch to non-root user
USER dotnetuser

# Expose the port the app runs on
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Set the entry point (will be overridden by docker-compose if needed)
ENTRYPOINT ["dotnet", "LMS.API.dll"]

