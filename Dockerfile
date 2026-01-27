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

# Set environment for production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:80

# Install Tools
RUN apt-get update && apt-get install -y curl net-tools && rm -rf /var/lib/apt/lists/*

# Entry point
ENTRYPOINT ["dotnet", "BelegErfassungApp.dll"]