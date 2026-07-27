# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["MegaEaterMMO.API/MegaEaterMMO.API.csproj", "MegaEaterMMO.API/"]
COPY ["MegaEaterMMO.Shared/MegaEaterMMO.Shared.csproj", "MegaEaterMMO.Shared/"]

RUN dotnet restore "MegaEaterMMO.API/MegaEaterMMO.API.csproj"

COPY . .

WORKDIR /src/MegaEaterMMO.API
RUN dotnet build "MegaEaterMMO.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "MegaEaterMMO.API.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "MegaEaterMMO.API.dll"]