@echo off
setlocal
cd /d "%~dp0"

echo Starting Docker dependencies (SQL Server and MongoDB)...
docker compose up -d sqlserver mongodb

where pnpm >nul 2>&1
if errorlevel 1 (
    echo [ERROR] pnpm was not found. Install pnpm or enable Corepack first.
    pause
    exit /b 1
)

echo Starting Backend API...
start "Nexus Backend" /D "%~dp0backend\doanki3API" cmd /k dotnet run

echo Starting Frontend Svelte App with pnpm...
start "Nexus Frontend" /D "%~dp0frontend" cmd /k "pnpm install && pnpm dev"

echo Project is starting...
endlocal
