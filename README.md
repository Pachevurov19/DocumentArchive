
# Document Archive

A document archive system built with ASP.NET Core 9,
Entity Framework Core, SQLite, and Docker.

## Architecture

The solution contains:

- **Archive.Api** — manages cabinets, documents,
  index values, uploaded files, and document queries.
- **Export.Api** — retrieves documents through
  Archive.Api and exports them into ZIP archives.
- **Archive.Tests** — xUnit tests for upload grouping.

Export.Api communicates with Archive.Api exclusively
through HTTP.

## Technologies

- .NET 9 / ASP.NET Core
- Entity Framework Core 9
- SQLite
- xUnit
- Docker / Docker Compose

## Running locally

Open the solution in Visual Studio 2022.

Configure Archive.Api and Export.Api as startup projects.

Run both applications. Configure the Archive API base URL
in Export.Api/appsettings.json to match the Archive.Api
HTTPS address.

Archive.Api automatically applies EF Core migrations
at startup.

## Running with Docker

From the solution root:

```bash
docker compose up --build
```

Expected endpoints:

- Archive API: http://localhost:8080
- Export API: http://localhost:8082

Archive.Api uses a persistent Docker volume for its
SQLite database and uploaded files.

Note: Docker Compose configuration has not yet been
verified successfully in the development environment.

## API endpoints

### Archive.Api

- GET /api/cabinets
- GET /api/cabinets/{cabinetName}
- POST /api/cabinets/{cabinetName}/documents/upload
- POST /api/cabinets/{cabinetName}/documents/upload-batch
- GET /api/cabinets/{cabinetName}/documents
- GET /api/documents/{id}
- PATCH /api/documents/{id}/indexes
- GET /api/documents/{documentId}/parts/{partId}/download

### Export.Api

- GET /api/export/archive-status
- GET /api/export/{cabinetName}

Export supports optional from/to date filters and
a markerField parameter.

## Tests

Run the tests using Visual Studio Test Explorer,
or execute:

```bash
dotnet test
```

The current test suite covers upload grouping,
nested files, invalid paths, and path separators.

## Known limitations

- The web client is not yet implemented.
- Empty uploaded directories are not yet supported.
- ZIP export currently buffers data in memory.
- Docker Compose has not yet been verified end-to-end.
- Additional integration tests are needed.

See DECISIONS.md for implementation decisions,
trade-offs, and remaining work.
