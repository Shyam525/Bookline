#!/usr/bin/env bash
echo "Starting Bookline Infrastructure (PostgreSQL, Redis, Mailpit)..."
docker-compose up -d

echo "Starting Bookline Web API..."
dotnet run -c Release --project src/Bookline.Api
