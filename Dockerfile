# Multi-stage build für Blazor Server App (ASP.NET Core 8)

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["BelegErfassungApp/BelegErfassungApp.csproj", "BelegErfassungApp/"]
RUN dotnet restore "BelegErfassungApp/BelegErfassungApp.csproj"

# Copy source code
COPY . .
WORKDIR "/src/BelegErfassungApp"
RUN dotnet build "BelegErfassungApp.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "BelegErfassungApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .

# Expose ports (HTTP und HTTPS)
EXPOSE 80 443

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD dotnet --version || exit 1

# Set environment for production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Entry point
ENTRYPOINT ["dotnet", "BelegErfassungApp.dll"]
