#!/bin/bash

echo "Starting Docker containers (SQL Server and MongoDB)..."
docker-compose up -d

echo "Applying Database Patches..."
sqlcmd -S "(localdb)\MSSQLLocalDB" -d NexusSystem -i "scripts/sql/2026-09-21_retail-workflow.sql" || echo "Note: sqlcmd might not be available or localdb is not running on this platform. Please run the script manually if needed."

echo "Starting Backend API..."
(cd backend/doanki3API && dotnet run) &

echo "Starting Frontend Svelte App..."
(cd frontend && pnpm run dev) &

echo "Project is starting... Press Ctrl+C to stop."
wait
