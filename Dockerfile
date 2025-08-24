# Railway için optimize edilmiş Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything and build
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Runtime stage - Railway için optimize
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Install Chrome dependencies for Railway
RUN apt-get update && apt-get install -y \
    wget \
    gnupg \
    unzip \
    chromium \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# Create necessary directories
RUN mkdir -p /app/wwwroot/uploads
RUN mkdir -p /app/data
RUN chmod 755 /app/wwwroot/uploads
RUN chmod 755 /app/data

# Set environment variables for Railway
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_URLS=http://+:$PORT

# Railway uses $PORT environment variable
EXPOSE $PORT

ENTRYPOINT ["dotnet", "ExcelSheetsApp.dll"]
