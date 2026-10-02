@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

rem Start Docker dependencies when Docker Desktop is available.
docker info >nul 2>&1
if not errorlevel 1 (
    echo [1/3] Starting SQL Server and MongoDB in Docker...
    docker compose up -d sqlserver mongodb
    if errorlevel 1 (
        echo [ERROR] Docker dependencies could not be started.
        pause
        exit /b 1
    )
    echo Initializing NexusSystem database...
    docker compose up sqlserver-init
    if errorlevel 1 (
        echo [ERROR] Database initialization failed.
        pause
        exit /b 1
    )
    set "ConnectionStrings__MyAppDbContext=Server=localhost,1433;Database=NexusSystem;User Id=sa;Password=Nexus@12345;TrustServerCertificate=True"
    set "Mongo__ConnectionString=mongodb://localhost:27017"
) else (
    echo [1/3] Docker Desktop is not running; using local SQL Server and MongoDB.
    set "ConnectionStrings__MyAppDbContext=Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=NexusSystem;Integrated Security=True;TrustServerCertificate=True"
    set "Mongo__ConnectionString=mongodb://localhost:27017"
)

set "ASPNETCORE_ENVIRONMENT=Development"
set "ASPNETCORE_URLS=http://localhost:5105"
if not defined NEXUS_JWT_KEY set "NEXUS_JWT_KEY=local-development-only-change-this-key"
if not defined NEXUS_JWT_ISSUER set "NEXUS_JWT_ISSUER=doanki3API"
set "Jwt__Key=%NEXUS_JWT_KEY%"
set "Jwt__Issuer=%NEXUS_JWT_ISSUER%"

where pnpm >nul 2>&1
if errorlevel 1 (
    echo [ERROR] pnpm was not found. Install pnpm or enable Corepack first.
    pause
    exit /b 1
)

rem Start the local backend and frontend once; Compose API/web are not started here.
echo [2/3] Starting Backend API on port 5105...
start "Nexus Backend API [Port 5105]" /D "%~dp0backend\doanki3API" cmd /k "dotnet run --no-launch-profile"
echo [3/3] Starting Frontend Svelte UI on port 3000...
start "Nexus Frontend UI [Port 3000]" /D "%~dp0frontend" cmd /k "pnpm install --frozen-lockfile && pnpm dev"

endlocal
