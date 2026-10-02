# Nexus Service Marketing System Frontend

This directory contains the web client for the Nexus Service Marketing System. It provides the public service-registration experience, customer portal, and role-based workspaces for administration, retail operations, technical operations, and accounting.

## Technology stack

- **Svelte 5** with TypeScript
- **Vite 7** for development and production builds
- **Tailwind CSS** for styling
- **SignalR client** for real-time notifications and chat events
- **jsPDF and PDF-Lib** for invoice/document generation
- **pnpm 12.6.0** as the required package manager

## Project structure

```text
frontend/
├── src/
│   ├── components/   Reusable UI, layout, chat, notification, and auth components
│   ├── context/      Shared authentication, application, language, theme, and notification state
│   ├── lib/          REST clients, SignalR client, routing, validation, and utilities
│   ├── locales/      English and Vietnamese translations
│   ├── pages/        Landing, authentication, and role dashboard pages
│   ├── types/        Shared TypeScript domain types
│   ├── App.svelte    Application shell and client-side route mapping
│   └── main.ts       Application entry point
├── .env.example      Environment variable template
├── Dockerfile        Production image build
├── nginx.conf        SPA fallback and API/SignalR reverse proxy
└── package.json       Scripts and dependency definitions
```

## Prerequisites

- Node.js 20 or later
- pnpm 12.6.0
- A running Nexus backend for authenticated features

If pnpm is not installed, enable Corepack and activate the repository version:

```powershell
corepack enable
corepack prepare pnpm@12.6.0 --activate
```

## Installation and configuration

From the `frontend` directory:

```powershell
pnpm install
Copy-Item .env.example .env
```

Set the backend base URL in `.env`:

```dotenv
VITE_API_URL=http://localhost:5105
```

`VITE_API_URL` is injected by Vite at build time. If it is not set, the application falls back to `http://localhost:5105`.

## Development commands

```powershell
# Start the Vite development server
pnpm dev

# Run Svelte and TypeScript checks
pnpm check

# Create a production build
pnpm build

# Preview the production build locally
pnpm preview
```

The development server listens on `http://localhost:3000` by default. Start the backend first so API calls, authentication, billing, and real-time features can work.

There is currently no automated frontend test runner configured. Use `pnpm check`, `pnpm build`, and manual browser verification for the current project.

## Application routes

The client uses a lightweight history-based router. The main routes are:

| Route | Purpose |
| --- | --- |
| `/` or `/home` | Public landing page, plan browsing, branch search, language/theme settings, and public chat |
| `/login` | Employee and customer login |
| `/register` | Customer/business service registration and order creation |
| `/admin` | Administration workspace |
| `/retail` | Retail and branch operations workspace |
| `/technical` or `/tech` | Technical/NOC workspace |
| `/accounts` or `/billing` | Accounting and billing workspace |
| `/user`, `/subscriber`, or `/customer` | Customer portal |

Protected routes redirect unauthenticated users to `/login` and prevent users from opening a workspace for a different role.

## Role-based features

- **Administration:** employees, service plans, inventory, vendors, purchases, retail shops, feedback, and live chat.
- **Retail operations:** new orders, branch approval, order tracking, connections, payment records, and branch settings.
- **Technical operations:** feasibility checks, connections, equipment, equipment requests, network tests, and technical settings.
- **Accounting:** customer connections, bill generation, payment tracking, charges, taxes, and accounting settings.
- **Customer portal:** service overview, new orders, billing, balance payments, live chat, feedback, and profile settings.

The public registration flow supports personal and business customers, plan and branch selection, address validation, multiple connections, and applicable bulk discounts.

## Backend integration

The frontend communicates with the ASP.NET Core API through REST clients in `src/lib/`. Protected requests read the JWT from browser storage and send it as a bearer token.

The default backend endpoints are grouped under:

```text
/api/auth
/api/admin
/api/Technical
/api/billing
/api/retail
/api/customer
/api/chat
/api/notifications
/api/public
```

Real-time notifications and chat events use SignalR:

```text
${VITE_API_URL}/hubs/notifications
```

The client automatically reconnects and joins the role, account, and chat-session groups needed by the signed-in user.

## Docker and production serving

The frontend Dockerfile builds the Vite application and serves the generated `dist` directory with NGINX. NGINX provides:

- SPA fallback to `index.html` for client-side routes.
- `/api/` proxying to the backend API container.
- `/hubs/` proxying with WebSocket upgrade headers for SignalR.

To run the complete stack, use the repository-level Compose configuration:

```powershell
cd ..
docker compose up -d --build
```

The web application is then available at `http://localhost:3000`; the API is available at `http://localhost:5105`.

> **Compose database note:** the current Compose file refers to `./frontend/NexusSystem_Database.sql`, while the seed file is stored at `backend/NexusSystem_Database.sql`. If the SQL initialization service cannot find the seed file, correct that mount path before starting the full stack.

## Troubleshooting

### API requests fail in development

- Confirm the backend is running on `http://localhost:5105`.
- Check `VITE_API_URL` in `.env` and restart Vite after changing it.
- Inspect the browser Network panel for the failing endpoint and response status.

### Authentication does not persist

- Sign in again after changing the backend URL.
- Clear stale `nexus_jwt_token` data from browser storage.
- Verify that the backend JWT key and issuer are configured consistently.

### SignalR does not connect

- Confirm the backend exposes `/hubs/notifications`.
- Check that the API URL is reachable from the browser.
- When using NGINX, confirm that WebSocket upgrade headers are enabled for `/hubs/`.

## Related documentation

- [Backend README](../backend/README.md)
- [Docker Compose setup](../docker-compose.yml)
- [Role workflow reference](./CHI_TIET_HOAT_DONG_TUNG_ROLE.md)
