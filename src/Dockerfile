# Use NVIDIA's CUDA runtime as the base image
FROM nvidia/cuda:12.2.0-runtime-ubuntu22.04 AS base

# Install .NET runtime dependencies
RUN apt-get update && apt-get install -y \
    wget \
    apt-transport-https \
    software-properties-common \
    && wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y dotnet-runtime-8.0 \
    && apt-get clean

# Build your application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy and restore projects
COPY MaIN/*.csproj ./MaIN/
COPY MaIN.Domain/*.csproj ./MaIN.Domain/
COPY MaIN.Infrastructure/*.csproj ./MaIN.Infrastructure/
COPY MaIN.Services/*.csproj ./MaIN.Services/
RUN dotnet restore ./MaIN/MaIN.csproj

# Copy and publish
COPY . ./
RUN dotnet publish ./MaIN/MaIN.csproj -c Release -o /app/out

# Use the CUDA base image with .NET runtime installed
FROM base AS runtime
WORKDIR /app
COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "MaIN.dll"]
