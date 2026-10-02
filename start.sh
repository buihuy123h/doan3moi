#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR"

if ! command -v docker >/dev/null 2>&1; then
  echo "[ERROR] Docker is required to start SQL Server and MongoDB." >&2
  exit 1
fi
if ! docker compose version >/dev/null 2>&1; then
  echo "[ERROR] Docker Compose v2 is required (use 'docker compose')." >&2
  exit 1
fi
if ! command -v dotnet >/dev/null 2>&1; then
  echo "[ERROR] .NET SDK is required to start the backend." >&2
  exit 1
fi
if ! command -v pnpm >/dev/null 2>&1; then
  echo "[ERROR] pnpm is required to start the frontend." >&2
  exit 1
fi

echo "Starting Docker dependencies (SQL Server and MongoDB)..."
docker compose up -d sqlserver mongodb

echo "Initializing the NexusSystem database..."
docker compose up sqlserver-init

# The API runs locally while its database dependencies run in Docker.
export ASPNETCORE_ENVIRONMENT="Development"
export ASPNETCORE_URLS="http://localhost:5105"
export ConnectionStrings__MyAppDbContext="Server=localhost,1433;Database=NexusSystem;User Id=sa;Password=Nexus@12345;TrustServerCertificate=True"
export Mongo__ConnectionString="mongodb://localhost:27017"
export Jwt__Key="${NEXUS_JWT_KEY:-local-development-only-change-this-key}"
export Jwt__Issuer="${NEXUS_JWT_ISSUER:-doanki3API}"

BACKEND_PID=""
FRONTEND_PID=""

cleanup() {
  [ -z "$BACKEND_PID" ] || kill "$BACKEND_PID" 2>/dev/null || true
  [ -z "$FRONTEND_PID" ] || kill "$FRONTEND_PID" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

echo "Starting Backend API..."
(
  cd "$ROOT_DIR/backend/doanki3API"
  dotnet run --no-launch-profile
) &
BACKEND_PID=$!

echo "Starting Frontend Svelte App..."
(
  cd "$ROOT_DIR/frontend"
  pnpm install --frozen-lockfile && pnpm dev
) &
FRONTEND_PID=$!

echo "Project is starting... Press Ctrl+C to stop."
wait
