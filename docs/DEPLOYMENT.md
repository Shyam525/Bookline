# Docker & Production Deployment Guide

## Production Architecture

Bookline is packaged for Docker containerized environments.

## Deployment Steps

1. Configure `.env`:
   ```bash
   cp .env.example .env
   ```

2. Build and start services:
   ```bash
   docker compose up --build -d
   ```

3. Infrastructure Services:
   - **PostgreSQL 16**: Port `5432`
   - **Redis 7**: Port `6379`
   - **Mailpit**: Port `8026` (Web UI), `1026` (SMTP)
   - **Bookline API**: Port `5168`
