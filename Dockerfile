# ── Stage 1: Build Angular ───────────────────────────────────────────────────
FROM node:20-alpine AS frontend-build
WORKDIR /app
COPY infinity-webapp/package*.json ./
RUN npm ci --prefer-offline
COPY infinity-webapp/ ./
RUN npm run build

# ── Stage 2: Build ASP.NET Core ──────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src
COPY *.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /publish --no-restore

# ── Stage 3: Runtime image ───────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=backend-build /publish ./

# Angular's @angular/build:application builder outputs to dist/{project}/browser
COPY --from=frontend-build /app/dist/infinity-webapp/browser ./wwwroot

# ASP.NET Core 8 defaults to port 8080 on HTTP
EXPOSE 8080

ENTRYPOINT ["dotnet", "InfinityCodexWebApp.dll"]
