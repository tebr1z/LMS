# Docker Setup for LMS API

This guide explains how to run the LMS API using Docker and Docker Compose.

## Prerequisites

- Docker Desktop (or Docker Engine + Docker Compose)
- .NET 8 SDK (for local development)

## Quick Start

1. **Clone the repository** (if not already done)
   ```bash
   git clone <repository-url>
   cd LMS
   ```

2. **Create environment file** (optional)
   ```bash
   cp env.example .env
   ```
   Edit `.env` file to customize configuration if needed.

3. **Build and start all services**
   ```bash
   docker-compose up -d
   ```

4. **Check service status**
   ```bash
   docker-compose ps
   ```

5. **View logs**
   ```bash
   # All services
   docker-compose logs -f
   
   # Specific service
   docker-compose logs -f api
   docker-compose logs -f mysql
   docker-compose logs -f redis
   ```

6. **Access the API**
   - API: http://localhost:8080
   - Swagger UI: http://localhost:8080/swagger
   - Health Check: http://localhost:8080/api/health

## Environment Variables

The following environment variables can be configured:

### MySQL Configuration
- `MYSQL_ROOT_PASSWORD` - MySQL root password (default: rootpassword)
- `MYSQL_DATABASE` - Database name (default: LmsDb)
- `MYSQL_USER` - Database user (default: lmsuser)
- `MYSQL_PASSWORD` - Database password (default: lmspassword)
- `MYSQL_PORT` - MySQL port (default: 3306)

### Redis Configuration
- `REDIS_PORT` - Redis port (default: 6379)

### API Configuration
- `API_PORT` - API port (default: 8080)
- `ConnectionStrings__DefaultConnection` - Database connection string
- `Jwt__Key` - JWT signing key
- `Jwt__Issuer` - JWT issuer
- `Jwt__Audience` - JWT audience
- `Jwt__ExpirationMinutes` - JWT expiration in minutes (default: 60)
- `SupportedCultures` - Comma-separated list of supported cultures (default: en,az,tr,ru)
- `ASPNETCORE_ENVIRONMENT` - ASP.NET Core environment (default: Production)

## Services

### API Service
- **Container**: `lms_api`
- **Port**: 8080
- **Health Check**: `/api/health`
- **Dependencies**: MySQL, Redis

### MySQL Service
- **Container**: `lms_mysql`
- **Port**: 3306
- **Volume**: `mysql_data` (persistent storage)
- **Health Check**: MySQL ping

### Redis Service
- **Container**: `lms_redis`
- **Port**: 6379
- **Volume**: `redis_data` (persistent storage)
- **Health Check**: Redis ping

## Database Migration

After starting the containers, you need to run database migrations:

```bash
# Enter the API container
docker exec -it lms_api bash

# Run migrations (if you have EF Core migrations)
dotnet ef database update --project LMS.Infrastructure --startup-project LMS.API
```

Or run migrations from your local machine (if connected to the Docker MySQL):

```bash
dotnet ef database update --project LMS.Infrastructure --startup-project LMS.API --connection "Server=localhost;Port=3306;Database=LmsDb;User Id=lmsuser;Password=lmspassword;"
```

## Stopping Services

```bash
# Stop all services
docker-compose down

# Stop and remove volumes (WARNING: This deletes all data)
docker-compose down -v
```

## Building the Docker Image

To build the Docker image manually:

```bash
docker build -t lms-api:latest -f Dockerfile .
```

## Troubleshooting

### Port Already in Use
If port 8080, 3306, or 6379 is already in use, change the ports in `docker-compose.yml` or `.env` file.

### Database Connection Issues
1. Check if MySQL container is running: `docker-compose ps`
2. Check MySQL logs: `docker-compose logs mysql`
3. Verify connection string in environment variables
4. Ensure MySQL is healthy before API starts (healthcheck configured)

### API Not Starting
1. Check API logs: `docker-compose logs api`
2. Verify all environment variables are set correctly
3. Ensure MySQL and Redis are healthy

### Permission Issues
If you encounter permission issues, ensure Docker has proper permissions to access volumes and networks.

## Development

For local development without Docker:

1. Ensure MySQL and Redis are running locally
2. Update `appsettings.json` or `appsettings.Development.json` with local connection strings
3. Run the API: `dotnet run --project LMS.API`

## Production Considerations

1. **Security**:
   - Change all default passwords
   - Use strong JWT keys
   - Enable HTTPS
   - Restrict network access

2. **Performance**:
   - Configure MySQL connection pooling
   - Use Redis for caching
   - Enable production logging

3. **Monitoring**:
   - Set up health checks
   - Configure logging aggregation
   - Monitor container resources

4. **Backup**:
   - Regularly backup MySQL volumes
   - Backup Redis data if needed

## Volumes

- `mysql_data`: Persistent MySQL data storage
- `redis_data`: Persistent Redis data storage

These volumes persist data even when containers are stopped or removed (unless using `docker-compose down -v`).

