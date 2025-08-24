# Railway Dockerfile with EF Migration support
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy csproj and restore
COPY *.csproj ./
RUN dotnet restore

# Copy everything and publish
COPY . ./
RUN dotnet publish -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app

# Install ICU and Chrome for Railway
RUN apk add --no-cache \
    icu-libs \
    chromium \
    chromium-chromedriver \
    xvfb \
    && ln -s /usr/bin/chromium-browser /usr/bin/google-chrome \
    && ln -s /usr/bin/chromedriver /usr/local/bin/chromedriver

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Copy published app
COPY --from=build /app .

# Railway environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:$PORT

EXPOSE $PORT

# Start app (migrations will run in Program.cs)
ENTRYPOINT ["dotnet", "ExcelSheetsApp.dll"]