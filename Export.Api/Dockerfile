FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Export.Api/Export.Api.csproj", "Export.Api/"]
RUN dotnet restore "Export.Api/Export.Api.csproj"

COPY . .
WORKDIR /src/Export.Api

RUN dotnet publish "Export.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=build /app/publish .

USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080
ENV ArchiveApi__BaseUrl=http://archive-api:8080

ENTRYPOINT ["dotnet", "Export.Api.dll"]
