# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything and build
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Install Chrome for Selenium
RUN apt-get update && apt-get install -y \
    wget \
    gnupg \
    unzip \
    && wget -q -O - https://dl.google.com/linux/linux_signing_key.pub | apt-key add - \
    && echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" > /etc/apt/sources.list.d/google-chrome.list \
    && apt-get update \
    && apt-get install -y google-chrome-stable \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# Create uploads directory
RUN mkdir -p /app/wwwroot/uploads

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "ExcelSheetsApp.dll"]
