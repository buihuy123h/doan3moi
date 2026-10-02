# Nexus Service Marketing System API

The backend is the ASP.NET Core Web API for the Nexus Service Marketing System. It manages customers, service plans, orders, connections, billing, inventory, equipment, employee workflows, chat, and notifications.

## Technology stack

- **.NET 10 / ASP.NET Core Web API**
- **Entity Framework Core with SQL Server** for transactional business data
- **MongoDB** for chat sessions, chat messages, OTP data, and notification history
- **JWT bearer authentication** with role-based authorization
- **SignalR** for real-time notifications and chat events
- **Swagger/OpenAPI** for development-time API exploration
- **MailKit** for email delivery, including OTP messages

## Project structure

```text
backend/
├── doanki3API/
│   ├── Controllers/     HTTP endpoints grouped by business module
│   ├── Services/        Business logic and integrations
│   ├── DTOs/            Request and response contracts
│   ├── Constants/       Shared roles and status values
│   ├── Data/            Entity Framework DbContext and mappings
│   ├── Models/          SQL Server entities
│   ├── Hubs/            SignalR hubs
│   ├── Migrations/      Entity Framework migration files
│   └── Program.cs       Application startup and dependency registration
├── NexusSystem_Database.sql  SQL Server schema and seed data
└── README.md
```

## Prerequisites

Install the following tools before starting the API locally:

- .NET 10 SDK
- SQL Server or SQL Server LocalDB
- MongoDB 6 or later (a local MongoDB installation or Docker container)
- An SMTP account if email OTP delivery is required

Docker users can start SQL Server and MongoDB with the repository-level [Docker Compose file](../docker-compose.yml). The Compose setup exposes SQL Server on `1433`, MongoDB on `27017`, and the API on `5105`.

## Configuration

1. Copy the example configuration to a local, untracked configuration file:

   ```powershell
   Copy-Item doanki3API/appsettings.example.json doanki3API/appsettings.Development.json
   ```

2. Update the values for your environment:

   - `ConnectionStrings:MyAppDbContext` — SQL Server connection string.
   - `Mongo:ConnectionString` and `Mongo:Database` — MongoDB connection settings.
   - `Email:Host`, `Email:Port`, `Email:User`, and `Email:Password` — SMTP settings for OTP emails.
   - `Jwt:Key`, `Jwt:Issuer`, and `Jwt:ExpiresInMinutes` — token settings.

The application also supports environment-variable overrides. For example:

```text
ConnectionStrings__MyAppDbContext=Server=localhost;Database=NexusSystem;Trusted_Connection=True;TrustServerCertificate=True
Mongo__ConnectionString=mongodb://localhost:27017
```

Never commit real passwords, SMTP app passwords, JWT keys, or connection strings. Use `appsettings.example.json`, environment variables, or .NET user secrets for local development.

## Database setup

The SQL seed file creates the `NexusSystem` database, schema, and initial data:

```text
backend/NexusSystem_Database.sql
```

Run it with SQL Server tooling, for example:

```powershell
sqlcmd -S localhost -E -i .\NexusSystem_Database.sql
```

Adjust the server and authentication flags for your SQL Server installation. The API does not automatically create or seed the database at startup, so SQL Server must be available and initialized before the first request.

MongoDB does not require a schema migration for the default collections. The database and collection names are read from configuration.

## Run locally

From the `backend` directory:

```powershell
dotnet restore
dotnet build doanki3API/doanki3API.csproj
dotnet run --project doanki3API/doanki3API.csproj
```

The default launch profiles are:

| Service | URL |
| --- | --- |
| API over HTTP | `http://localhost:5105` |
| API over HTTPS | `https://localhost:7069` |
| Swagger UI | `https://localhost:7069/swagger` |
| SignalR notifications hub | `http://localhost:5105/hubs/notifications` |

The frontend uses `http://localhost:5105` by default. If you run the API on another port, update the frontend `VITE_API_URL` value accordingly.

## Main API areas

The API is organized around the following modules:

- **Authentication:** login, registration, OTP verification, and account recovery.
- **Public self-service:** service plan discovery and public order submission.
- **Administration:** employees, service plans, inventory, vendors, retail shops, purchase orders, system settings, and feedback.
- **Retail operations:** order processing, branch approval, connection information, and payment collection.
- **Technical operations:** feasibility checks, installation workflow, customer connections, equipment, stock, and equipment requests.
- **Billing and accounting:** bill generation, payment records, charges, and tax settings.
- **Customer portal:** profile, connections, bills, payments, service orders, and feedback.
- **Chat and notifications:** MongoDB-backed conversations and REST/SignalR notifications.

Role-based access is enforced for managers, retail staff, field engineers, senior accountants, and customers. Protected requests must include a valid JWT bearer token:

```http
Authorization: Bearer <token>
```

## Real-time events

Connect the SignalR client to:

```text
/hubs/notifications
```

The hub supports role, account, and chat-session groups. The frontend client includes automatic reconnect behavior and listens for notification, chat, and data-change events.

## Verification

There is currently no separate automated test project in the repository. The recommended checks are:

```powershell
dotnet build doanki3API/doanki3API.csproj
dotnet run --project doanki3API/doanki3API.csproj
```

Then open Swagger and verify authentication, database connectivity, and the endpoints required by the feature you are working on.

## Troubleshooting

### Database connection failures

- Confirm that SQL Server is running and that `NexusSystem` has been created from the seed script.
- Check `ConnectionStrings:MyAppDbContext` and the SQL Server authentication mode.
- When using Docker Compose, use the service hostname `sqlserver` from inside the API container rather than `localhost`.

### MongoDB or chat failures

- Confirm that MongoDB is listening on `27017`.
- Check `Mongo:ConnectionString` and the configured database/collection names.

### OTP emails are not delivered

- Configure a valid SMTP host, port, username, and app password.
- For local development, inspect the application logs and the email fallback directory when SMTP delivery is unavailable.

### Swagger is not available

Swagger is enabled in the Development environment. Start the API with the Development launch profile or set `ASPNETCORE_ENVIRONMENT=Development`.

## Related documentation

- [Project specification summary](../SPEC-SUMMARY.md)
- [Frontend README](../frontend/README.md)
- [Docker Compose setup](../docker-compose.yml)
