# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies separately so Docker can reuse the layer when only source changes.
COPY ["MindFlow.Platform/MindFlow.Platform.csproj", "MindFlow.Platform/"]
RUN dotnet restore "MindFlow.Platform/MindFlow.Platform.csproj"

COPY . .
WORKDIR "/src/MindFlow.Platform"
RUN dotnet publish "MindFlow.Platform.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Azure App Service forwards traffic to this container port.
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MindFlow.Platform.dll"]
