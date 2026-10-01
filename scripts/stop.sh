#!/usr/bin/env bash
echo "Stopping Bookline Web API process..."
pkill -f "Bookline.Api" || true

echo "Stopping Docker containers..."
docker-compose down

echo "Bookline services stopped."
