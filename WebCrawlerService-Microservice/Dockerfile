# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Application/Application.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/Application"
RUN dotnet build "Application.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Application.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Install Chrome for Selenium (if needed)
RUN apt-get update && apt-get install -y \
    wget \
    gnupg2 \
    unzip \
    curl \
    && wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add - \
    && echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google.list \
    && apt-get update \
    && apt-get install -y google-chrome-stable \
    && rm -rf /var/lib/apt/lists/*

# Install Playwright browsers
RUN apt-get update && apt-get install -y \
    libnss3 \
    libatk-bridge2.0-0 \
    libdrm2 \
    libxcomposite1 \
    libxdamage1 \
    libxrandr2 \
    libgbm1 \
    libgtk-3-0 \
    libasound2

WORKDIR /app
COPY --from=publish /app/publish .

# Create non-root user for security
RUN useradd -m -s /bin/bash crawler
RUN chown -R crawler:crawler /app
USER crawler

EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "Application.dll"]