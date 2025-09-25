# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# UI build stage (builds the Vue app into ExcelBotCs/wwwroot)
FROM node:20-alpine AS ui-build
WORKDIR /ui
# Copy only package manifest first for better Docker caching
COPY ExcelBotCs/ClientApp/package.json ExcelBotCs/ClientApp/
# Install deps (no lock file required; if you add package-lock.json, prefer `npm ci`)
RUN cd ExcelBotCs/ClientApp && npm install
# Copy the rest of the UI source and build
COPY ExcelBotCs/ClientApp ExcelBotCs/ClientApp
RUN cd ExcelBotCs/ClientApp && npm run build

# Backend build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ExcelBotCs/ExcelBotCs.csproj", "ExcelBotCs/"]
RUN dotnet restore "ExcelBotCs/ExcelBotCs.csproj"
COPY . .
WORKDIR "/src/ExcelBotCs"
RUN dotnet build "./ExcelBotCs.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish (and inject built UI assets into the publish output)
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ExcelBotCs.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
# Copy the built SPA into the publish's wwwroot
COPY --from=ui-build /ui/ExcelBotCs/wwwroot /app/publish/wwwroot

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ExcelBotCs.dll"]
