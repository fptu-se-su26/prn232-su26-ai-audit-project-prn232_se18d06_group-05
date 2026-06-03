# Docker Deployment Guide - TripMate Web API

## Overview

This guide explains how to build and deploy the TripMate Web API using Docker and Docker Compose.

---

## Prerequisites

### Required Software

1. **Docker Desktop** (or Docker Engine)
   - Windows: [Docker Desktop for Windows](https://docs.docker.com/desktop/install/windows-install/)
   - Mac: [Docker Desktop for Mac](https://docs.docker.com/desktop/install/mac-install/)
   - Linux: [Docker Engine](https://docs.docker.com/engine/install/)

2. **Docker Compose**
   - Included with Docker Desktop
   - Linux: Install separately if needed

### Verify Installation

```bash
docker --version
# Expected: Docker version 20.10.x or higher

docker-compose --version
# Expected: Docker Compose version 2.x.x or higher
```

---

## Quick Start

### 1. Setup Environment Variables

```bash
# Navigate to project directory
cd source/web/TripMate_Webapi

# Copy the environment template
cp .env.example .env

# Edit .env with your actual values
# Use your favorite text editor (notepad, vim, nano, code)
notepad .env  # Windows
nano .env     # Linux/Mac
```

**Required values in `.env`:**
```env
SUPABASE_URL=https://your-project.supabase.co
SUPABASE_ANON_KEY=your-anon-key
SUPABASE_JWKS_URI=https://your-project.supabase.co/auth/v1/jwks
SUPABASE_ISSUER=https://your-project.supabase.co/auth/v1
```

### 2. Build and Run with Docker Compose

```bash
# Build and start the container
docker-compose up -d

# Check if container is running
docker-compose ps

# View logs
docker-compose logs -f
```

### 3. Access the API

- **API Base URL:** http://localhost:5000
- **Swagger UI:** http://localhost:5000/swagger
- **Health Check:** http://localhost:5000/health

### 4. Stop the Container

```bash
# Stop the container
docker-compose down

# Stop and remove volumes (careful: deletes uploaded files)
docker-compose down -v
```

---

## Manual Docker Commands

### Build Image

```bash
# Build the Docker image
docker build -t tripmate-webapi:latest .

# View built image
docker images | grep tripmate
```

### Run Container

```bash
# Run container with environment variables
docker run -d \
  --name tripmate-api \
  -p 5000:5000 \
  -e Supabase__Url=https://your-project.supabase.co \
  -e Supabase__AnonKey=your-anon-key \
  -e Supabase__JwksUri=https://your-project.supabase.co/auth/v1/jwks \
  -e Supabase__Issuer=https://your-project.supabase.co/auth/v1 \
  tripmate-webapi:latest

# Check container status
docker ps

# View logs
docker logs -f tripmate-api

# Stop container
docker stop tripmate-api

# Remove container
docker rm tripmate-api
```

---

## Production Deployment

### 1. Azure Container Instances (ACI)

#### Build and Push to Azure Container Registry

```bash
# Login to Azure
az login

# Create resource group
az group create --name tripmate-rg --location eastus

# Create container registry
az acr create \
  --name tripmateregistry \
  --resource-group tripmate-rg \
  --sku Basic

# Login to ACR
az acr login --name tripmateregistry

# Tag image
docker tag tripmate-webapi:latest \
  tripmateregistry.azurecr.io/tripmate-webapi:latest

# Push to ACR
docker push tripmateregistry.azurecr.io/tripmate-webapi:latest

# Deploy to ACI
az container create \
  --resource-group tripmate-rg \
  --name tripmate-api \
  --image tripmateregistry.azurecr.io/tripmate-webapi:latest \
  --cpu 1 \
  --memory 1 \
  --registry-login-server tripmateregistry.azurecr.io \
  --registry-username $(az acr credential show --name tripmateregistry --query username -o tsv) \
  --registry-password $(az acr credential show --name tripmateregistry --query passwords[0].value -o tsv) \
  --dns-name-label tripmate-api \
  --ports 5000 \
  --environment-variables \
    ASPNETCORE_ENVIRONMENT=Production \
    Supabase__Url=https://your-project.supabase.co \
    Supabase__AnonKey=your-anon-key \
    Supabase__JwksUri=https://your-project.supabase.co/auth/v1/jwks \
    Supabase__Issuer=https://your-project.supabase.co/auth/v1

# Get public IP
az container show \
  --resource-group tripmate-rg \
  --name tripmate-api \
  --query ipAddress.fqdn \
  --output tsv
```

### 2. AWS Elastic Container Service (ECS)

#### Push to Amazon ECR

```bash
# Login to AWS
aws configure

# Create ECR repository
aws ecr create-repository --repository-name tripmate-webapi

# Get login command
aws ecr get-login-password --region us-east-1 | \
  docker login --username AWS --password-stdin \
  123456789012.dkr.ecr.us-east-1.amazonaws.com

# Tag image
docker tag tripmate-webapi:latest \
  123456789012.dkr.ecr.us-east-1.amazonaws.com/tripmate-webapi:latest

# Push to ECR
docker push 123456789012.dkr.ecr.us-east-1.amazonaws.com/tripmate-webapi:latest
```

#### Deploy with ECS Fargate

Use AWS Console or CLI to:
1. Create ECS Cluster
2. Create Task Definition with image
3. Create Service
4. Configure Load Balancer

### 3. Docker Hub

```bash
# Login to Docker Hub
docker login

# Tag image
docker tag tripmate-webapi:latest yourusername/tripmate-webapi:latest

# Push to Docker Hub
docker push yourusername/tripmate-webapi:latest

# Pull and run on any server
docker pull yourusername/tripmate-webapi:latest
docker run -d -p 5000:5000 \
  -e Supabase__Url=... \
  yourusername/tripmate-webapi:latest
```

### 4. Google Cloud Run

```bash
# Build and push to Google Container Registry
gcloud builds submit --tag gcr.io/your-project-id/tripmate-webapi

# Deploy to Cloud Run
gcloud run deploy tripmate-api \
  --image gcr.io/your-project-id/tripmate-webapi \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated \
  --set-env-vars "Supabase__Url=https://your-project.supabase.co,Supabase__AnonKey=your-key"
```

---

## Environment Variables

### Required

| Variable | Description | Example |
|----------|-------------|---------|
| `Supabase__Url` | Supabase project URL | `https://xyz.supabase.co` |
| `Supabase__AnonKey` | Supabase anonymous key | `eyJhbG...` |
| `Supabase__JwksUri` | JWKS endpoint for JWT validation | `https://xyz.supabase.co/auth/v1/jwks` |
| `Supabase__Issuer` | JWT issuer | `https://xyz.supabase.co/auth/v1` |

### Optional

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production` |
| `SerpApi__Key` | SerpAPI key for location search | (none) |

---

## Volume Management

### Persist Uploaded Files

```bash
# Create named volume
docker volume create tripmate-uploads

# Run with volume mount
docker run -d \
  -v tripmate-uploads:/app/wwwroot/uploads \
  tripmate-webapi:latest

# Backup uploads
docker run --rm \
  -v tripmate-uploads:/source \
  -v $(pwd):/backup \
  alpine tar czf /backup/uploads-backup.tar.gz -C /source .

# Restore uploads
docker run --rm \
  -v tripmate-uploads:/target \
  -v $(pwd):/backup \
  alpine tar xzf /backup/uploads-backup.tar.gz -C /target
```

---

## Monitoring and Debugging

### View Logs

```bash
# Docker Compose
docker-compose logs -f

# Single container
docker logs -f tripmate-api

# Last 100 lines
docker logs --tail 100 tripmate-api

# With timestamps
docker logs -t tripmate-api
```

### Health Check

```bash
# Check container health
docker inspect --format='{{.State.Health.Status}}' tripmate-api

# Manual health check
curl http://localhost:5000/health
```

### Execute Commands Inside Container

```bash
# Open shell
docker exec -it tripmate-api /bin/bash

# Run dotnet command
docker exec tripmate-api dotnet --version

# Check files
docker exec tripmate-api ls -la /app
```

### Monitor Resources

```bash
# View resource usage
docker stats tripmate-api

# View detailed container info
docker inspect tripmate-api
```

---

## Troubleshooting

### Container Won't Start

```bash
# Check logs for errors
docker logs tripmate-api

# Inspect container
docker inspect tripmate-api

# Check if port is already in use
# Windows
netstat -ano | findstr :5000

# Linux/Mac
lsof -i :5000
```

### Environment Variables Not Working

```bash
# Verify environment variables inside container
docker exec tripmate-api env | grep Supabase

# Use .env file with docker-compose
docker-compose --env-file .env up
```

### Health Check Failing

```bash
# Check if app is responding
docker exec tripmate-api curl -f http://localhost:5000/health

# Check container logs
docker logs tripmate-api
```

### Build Issues

```bash
# Clear Docker cache and rebuild
docker-compose build --no-cache

# Remove all stopped containers
docker container prune

# Remove unused images
docker image prune -a
```

---

## Security Best Practices

### 1. Never Commit Secrets

```bash
# Add to .gitignore
echo ".env" >> .gitignore
echo "*.env" >> .gitignore
```

### 2. Use Secrets Management

**Docker Compose with Secrets:**

```yaml
services:
  tripmate-api:
    secrets:
      - supabase_anon_key
    environment:
      - Supabase__AnonKey=/run/secrets/supabase_anon_key

secrets:
  supabase_anon_key:
    file: ./secrets/supabase_anon_key.txt
```

### 3. Run as Non-Root User

Add to Dockerfile:
```dockerfile
RUN adduser --disabled-password --gecos '' apiuser
USER apiuser
```

### 4. Scan for Vulnerabilities

```bash
# Install Trivy
# https://github.com/aquasecurity/trivy

# Scan image
trivy image tripmate-webapi:latest
```

---

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Build and Deploy Docker Image

on:
  push:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v2
      
      - name: Login to Docker Hub
        uses: docker/login-action@v2
        with:
          username: ${{ secrets.DOCKER_USERNAME }}
          password: ${{ secrets.DOCKER_PASSWORD }}
      
      - name: Build and push
        uses: docker/build-push-action@v4
        with:
          context: ./source/web/TripMate_Webapi
          push: true
          tags: yourusername/tripmate-webapi:latest
```

---

## Performance Optimization

### Multi-Architecture Build

```bash
# Build for multiple platforms
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -t tripmate-webapi:latest \
  --push .
```

### Reduce Image Size

Current optimizations:
- ✅ Multi-stage build
- ✅ .dockerignore file
- ✅ Minimal runtime image (aspnet vs sdk)

Additional optimizations:
```dockerfile
# Use Alpine-based image (smaller)
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine

# Remove unnecessary files
RUN rm -rf /tmp/* /var/tmp/*
```

---

## Support

For issues or questions:
1. Check logs: `docker-compose logs -f`
2. Verify environment variables
3. Test health endpoint: `curl http://localhost:5000/health`
4. Review Swagger documentation: `http://localhost:5000/swagger`

---

**Last Updated:** June 1, 2026  
**Docker Version:** 24.0.x  
**Docker Compose Version:** 2.x.x
