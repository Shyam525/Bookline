#!/usr/bin/env bash
echo "Resetting Bookline Environment..."
docker compose down -v
docker compose up -d postgres redis mailpit
sleep 3
dotnet build backend/Bookline.slnx
echo "Environment Reset Complete."
