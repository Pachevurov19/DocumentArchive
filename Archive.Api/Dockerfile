FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Archive.Api/Archive.Api.csproj", "Archive.Api/"]
RUN dotnet restore "Archive.Api/Archive.Api.csproj"

COPY . .
WORKDIR /src/Archive.Api

RUN dotnet publish "Archive.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=build /app/publish .

# Persistent data directory for SQLite and uploaded files.
RUN mkdir -p /app/data/storage \
    && chown -R $APP_UID:$APP_UID /app/data

USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__ArchiveDatabase="Data Source=/app/data/archive.db"

ENTRYPOINT ["dotnet", "Archive.Api.dll"]
