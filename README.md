# DatingApp — Enterprise Full-Stack Social Platform

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0.16-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-17.3.8-DD0031?logo=angular&logoColor=white)](https://angular.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.4.5-3178C6?logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![RxJS](https://img.shields.io/badge/RxJS-7.8.1-B7178C?logo=reactivex&logoColor=white)](https://rxjs.dev/)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-8.0.16-512BD4?logo=dotnet)](https://learn.microsoft.com/en-us/ef/core/)
[![SignalR](https://img.shields.io/badge/SignalR-7.0.14-512BD4?logo=dotnet)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)

A production-oriented, full-stack social matching platform engineered with a **decoupled ASP.NET Core Web API** backend and an **Angular 17 standalone-component SPA**. The solution implements clean architectural boundaries—Repository Pattern, Unit of Work, DTO projection, and client-side reactive state—designed for maintainability, security, and real-time user engagement at scale.

---

## Architectural Overview

The system follows a **strict client–server separation** during development, with a unified deployment model for production.

| Layer | Responsibility |
|---|---|
| **Angular SPA** (`client/`) | Presentation, routing, client-side auth enforcement, reactive state, SignalR hub consumers |
| **ASP.NET Core API** (`API/`) | REST endpoints, Identity/JWT issuance, business orchestration, EF Core persistence, SignalR hubs |
| **SQL Server** | Relational persistence via EF Core migrations |

**Backend decoupling.** Controllers depend exclusively on abstractions (`IUnitOfWork`, `ITokenService`, `IPhotoService`) registered through extension-method service modules (`AddApplicationServices`, `AddIdentityServices`). Data access is isolated in repository implementations; transaction boundaries are centralized in `UnitOfWork.Complete()`.

**Frontend decoupling.** The Angular client communicates exclusively over HTTP and WebSocket (SignalR) contracts. Domain services encapsulate API interaction; cross-cutting concerns (JWT attachment, global error handling, loading UX) are implemented as functional `HttpInterceptorFn` providers—no legacy class-based interceptors.

**State management.** The client adopts **Angular Signals** as the primary reactive state primitive (`signal`, `computed`, `update`, `model`) across authentication, pagination, likes, messaging threads, and online presence—complemented by RxJS operators for async HTTP streams, hub event subscriptions, and interceptor pipelines.

---

## Core Backend Highlights

### ASP.NET Core Identity & Stateless JWT Authentication

- `AddIdentityCore<AppUser>` with role support (`AppRole`, `AppUserRole`) backed by a unified `DataContext` extending `IdentityDbContext`.
- Stateless bearer-token authentication via `JwtBearerDefaults.AuthenticationScheme` with HMAC-SHA512 signed tokens (`TokenService`).
- Role claims embedded at token issuance; authorization policies enforced server-side: `RequireAdminRole`, `ModeratePhotoRole`, `MemberRole`.
- SignalR-compatible JWT transport: `OnMessageReceived` extracts `access_token` from query strings on `/hubs/*` endpoints, enabling authenticated WebSocket connections without cookie sessions.

### Entity Framework Core — Schema, Relations & Query Optimization

- **Code-first migrations** (`SqlInitial`) with automatic migration on application startup.
- Explicit fluent configuration for composite keys (`UserLike`), cascade/restrict delete behaviors, and Identity role join entities.
- **Repository-level query optimization:**
  - `ProjectTo<TDto>()` (AutoMapper) for server-side DTO projection—avoiding over-fetching entity graphs.
  - `AsNoTracking()` on read-heavy member discovery queries.
  - Filtered, sortable, paginated member discovery (`UserParams`: gender, age range, order by last active/created).
  - Message inbox/outbox/unread container queries with automatic read-receipt batch updates in thread retrieval.

### SignalR — Real-Time Messaging & Presence

| Hub | Endpoint | Capability |
|---|---|---|
| `PresenceHub` | `/hubs/presence` | Online/offline broadcast, connection-scoped user tracking via singleton `PresenceTracker` |
| `MessageHub` | `/hubs/message` | Per-recipient group threads, live message delivery, read-receipt coordination, offline push notifications via `IHubContext<PresenceHub>` |

Connection groups are persisted in EF (`Group`, `Connection` entities) to correlate active SignalR sessions with message read state.

### Cross-Cutting Infrastructure

- **`ExceptionMiddleware`** — Global exception boundary returning structured `ApiException` JSON payloads; stack traces gated to Development environment.
- **`LogUserActivity`** — `IAsyncActionFilter` applied at `BaseApiController` level; updates `LastActive` timestamp post-action for authenticated users.
- **Pagination contract** — `PagedList<T>` with `Pagination` response headers exposed via CORS (`Access-Control-Expose-Headers`).
- **Cloudinary integration** — `PhotoService` handles secure upload, transformation, and deletion of user media assets.
- **AutoMapper profiles** — Consistent entity ↔ DTO mapping across controllers and hubs.

---

## Core Frontend Highlights

### RxJS Streams & Modern Reactive State

- **Angular Signals** drive UI reactivity for `currentUser`, role derivation (`computed` from JWT payload), paginated results, like IDs, message threads, and online user rosters.
- **RxJS** orchestrates HTTP observables (`map`, `tap`, `catchError`, `finalize`, `take`) and SignalR hub lifecycle events.
- **Client-side caching** — `MembersService` maintains a parameter-keyed `Map` cache to eliminate redundant paginated fetches during navigation.

### Functional Route Guards — Explicit JWT Client-Side Protection

| Guard | Type | Behavior |
|---|---|---|
| `authGuard` | `CanActivateFn` | Blocks unauthenticated access to member, messaging, and list routes |
| `adminGuard` | `CanActivateFn` | Role-gated access (`Admin` / `Moderator`) for admin panel |
| `preventUnsavedChangesGuard` | `CanDeactivateFn` | Dirty-form protection on member profile edit with modal confirmation |

Child routes under authenticated layout use `runGuardsAndResolvers: 'always'` to re-evaluate guard state on every navigation.

### Component Architecture & UX Integration

- **Standalone components** throughout — no NgModule coupling; tree-shakable imports per feature.
- **Reusable reactive form controls** — `TextInputComponent`, `DatePickerComponent` implement `ControlValueAccessor` for composable validation UX.
- **Custom structural directive** — `HasRoleDirective` for declarative role-based template rendering.
- **Route resolver** — `memberDetailedResolver` pre-fetches member data before detail route activation.
- **HTTP interceptor pipeline** — `errorInterceptor` → `jwtInterceptor` → `loadingInterceptor` (global spinner via `BusyService` / `ngx-spinner`).
- **Responsive UI stack** — Bootstrap 5 + Bootswatch United theme, ngx-bootstrap modals/datepicker, ngx-toastr notifications, ng-gallery photo viewer, ng2-file-upload for media management.

---

## System Flow Topology

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Angular 17 SPA (client/)                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐ │
│  │ Route Guards │  │  Interceptors │  │   Services   │  │ SignalR Clients  │ │
│  │ auth/admin   │  │ JWT/Error/   │  │ Account,     │  │ PresenceService  │ │
│  │ deactivate   │  │ Loading      │  │ Members,     │  │ MessageService   │ │
│  └──────┬───────┘  └──────┬───────┘  │ Messages...  │  └────────┬─────────┘ │
│         │                 │          └──────┬───────┘           │           │
│         └─────────────────┴─────────────────┴───────────────────┘           │
│                                    │                                         │
│                          HTTPS REST  │  WSS (Bearer JWT)                     │
└────────────────────────────────────┼─────────────────────────────────────────┘
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                    ASP.NET Core 8 Web API (API/)                            │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │ ExceptionMiddleware → CORS → Authentication → Authorization        │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│         │                              │                    │               │
│         ▼                              ▼                    ▼               │
│  ┌─────────────┐              ┌─────────────────┐   ┌───────────────────┐   │
│  │ Controllers │              │  SignalR Hubs   │   │  Identity + JWT   │   │
│  │ Account     │              │  PresenceHub    │   │  TokenService     │   │
│  │ Users       │              │  MessageHub     │   │  Role Policies    │   │
│  │ Messages    │              │  PresenceTracker│   └───────────────────┘   │
│  │ Likes       │              └────────┬────────┘                           │
│  │ Admin       │                       │                                   │
│  └──────┬──────┘                       │                                   │
│         │                              │                                   │
│         ▼                              ▼                                   │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │              Unit of Work  →  Repositories  →  DataContext           │   │
│  │         (UserRepository, MessageRepository, LikesRepository)         │   │
│  └──────────────────────────────────┬──────────────────────────────────┘   │
└─────────────────────────────────────┼───────────────────────────────────────┘
                                      ▼
                         ┌────────────────────────┐
                         │   SQL Server 2022      │
                         │   (DatingDB)           │
                         │   EF Core Migrations   │
                         └────────────────────────┘
                                      │
                         ┌────────────┴────────────┐
                         │   Cloudinary CDN        │
                         │   (Photo Storage)       │
                         └─────────────────────────┘
```

**Production build path:** `ng build` emits static assets to `API/wwwroot`; `FallbackController` serves the SPA shell for deep-link routing.

---

## Detailed Tech Stack

| Tier | Technology | Version |
|---|---|---|
| **Backend Runtime** | .NET | 8.0 |
| **Backend Framework** | ASP.NET Core Web API | 8.0.16 |
| **Authentication** | ASP.NET Core Identity + JWT Bearer | 8.0.16 |
| **Token Handling** | System.IdentityModel.Tokens.Jwt | 8.12.0 |
| **Object Mapping** | AutoMapper | 14.0.0 |
| **Data Layer — ORM** | Entity Framework Core (SQL Server provider) | 8.0.16 |
| **Data Layer — Database** | Microsoft SQL Server | 2022 |
| **Real-Time / API** | ASP.NET Core SignalR | 8.0.16 |
| **Real-Time / Client** | @microsoft/signalr | 7.0.14 |
| **Media Storage** | Cloudinary .NET SDK | 1.27.6 |
| **Frontend Core** | Angular (standalone, signals) | 17.3.8 |
| **Frontend Language** | TypeScript | 5.4.5 |
| **Frontend Reactivity** | RxJS | 7.8.1 |
| **Frontend Routing** | Angular Router (functional guards/resolvers) | 17.3.8 |
| **Client Utilities — UI** | Bootstrap, Bootswatch, ngx-bootstrap | 5.3.3 / 12.0.0 |
| **Client Utilities — UX** | ngx-toastr, ngx-spinner, ngx-timeago | 18.0.0 / 17.0.0 / 3.0.0 |
| **Client Utilities — Media** | ng-gallery, ng2-file-upload | 11.0.0 / 5.0.0 |
| **CI/CD** | GitHub Actions → Azure Web App | Node 20 / .NET 8 |

---

## Repository Structure

```
DatingApp/
├── API/                          # ASP.NET Core Web API
│   ├── Controllers/              # REST endpoints (Account, Users, Messages, Likes, Admin)
│   ├── Data/                     # DbContext, Repositories, UnitOfWork, Migrations, Seed
│   ├── DTOs/                     # API contract models
│   ├── Entities/                 # Domain & Identity entities
│   ├── Extensions/               # Service registration & HTTP helpers
│   ├── Helpers/                  # Pagination, filters, AutoMapper profiles
│   ├── Interfaces/               # Repository & service abstractions
│   ├── Middleware/               # Global exception handling
│   ├── Services/                 # TokenService, PhotoService
│   └── SignalR/                  # PresenceHub, MessageHub, PresenceTracker
├── client/                       # Angular 17 SPA
│   └── src/app/
│       ├── _guards/              # Functional route guards
│       ├── _interceptors/        # Functional HTTP interceptors
│       ├── _services/            # Domain & hub services (signal-based state)
│       ├── _forms/               # Reusable CVA form components
│       ├── _directives/          # Role-based structural directives
│       ├── _resolvers/           # Route data resolvers
│       ├── members/              # Member discovery & profile features
│       ├── messages/             # Messaging UI
│       ├── admin/                # Role-gated administration
│       └── modals/               # Confirmation & role management dialogs
├── docker-compose.yml            # SQL Server 2022 container
└── DatingApp.sln
```

---

## Getting Started Locally

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/) & npm
- [Angular CLI 17](https://angular.dev/tools/cli): `npm install -g @angular/cli@17`
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for SQL Server container)

### 1. Start the Database

From the repository root:

```bash
docker compose up -d
```

This provisions SQL Server 2022 on `localhost:1433` with credentials matching `API/appsettings.Development.json`.

### 2. Configure Application Secrets

`API/appsettings.json` is excluded from source control. Initialize user secrets from the `API` directory:

```bash
cd API
dotnet user-secrets init
dotnet user-secrets set "TokenKey" "YOUR_SUPER_SECRET_KEY_AT_LEAST_64_CHARACTERS_LONG_FOR_HMAC_SHA512_SIGNING"
dotnet user-secrets set "CloudinarySettings:CloudName" "your_cloud_name"
dotnet user-secrets set "CloudinarySettings:ApiKey" "your_api_key"
dotnet user-secrets set "CloudinarySettings:ApiSecret" "your_api_secret"
```

Alternatively, create `API/appsettings.json`:

```json
{
  "TokenKey": "YOUR_SUPER_SECRET_KEY_AT_LEAST_64_CHARACTERS_LONG_FOR_HMAC_SHA512_SIGNING",
  "CloudinarySettings": {
    "CloudName": "your_cloud_name",
    "ApiKey": "your_api_key",
    "ApiSecret": "your_api_secret"
  }
}
```

> Photo upload endpoints require valid Cloudinary credentials. All other features operate without them.

### 3. Run the Backend API

```bash
cd API
dotnet restore
dotnet run
```

The API starts at:

- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5001`

On startup, EF Core applies pending migrations and seeds development users automatically.

### 4. Run the Angular Client (Development)

In a separate terminal:

```bash
cd client
npm install
ng serve
```

The SPA is available at `http://localhost:4200` and proxies API calls to `https://localhost:5001/api/` per `environment.development.ts`.

### 5. Production Build (Unified Host)

Build the Angular client into the API static host:

```bash
cd client
npm install
ng build

cd ../API
dotnet publish -c Release
```

The published artifact serves both the REST API and the compiled SPA from a single ASP.NET Core process.

---

## API Surface (Selected)

| Method | Route | Auth | Description |
|---|---|---|---|
| `POST` | `/api/account/register` | Public | User registration + JWT issuance |
| `POST` | `/api/account/login` | Public | Credential validation + JWT issuance |
| `GET` | `/api/users` | Bearer | Paginated, filtered member discovery |
| `GET` | `/api/users/{username}` | Bearer | Member profile retrieval |
| `PUT` | `/api/users` | Bearer | Profile update |
| `POST` | `/api/users/add-photo` | Bearer | Cloudinary photo upload |
| `GET` | `/api/messages` | Bearer | Paginated inbox/outbox/unread |
| `GET` | `/api/likes` | Bearer | Paginated likes (sent/received) |
| `POST` | `/api/likes/{targetUserId}` | Bearer | Toggle like |
| `GET` | `/api/admin/users-with-roles` | Admin | User role management |
| WS | `/hubs/presence` | Bearer | Real-time online presence |
| WS | `/hubs/message?user={username}` | Bearer | Real-time direct messaging |

---

## Design Decisions

- **Stateless authentication** — No server-side sessions; JWT claims drive both REST authorization and SignalR hub authentication, enabling horizontal API scaling without sticky sessions.
- **Repository + Unit of Work** — Controllers remain thin; repositories encapsulate EF queries; `Complete()` provides an explicit, testable transaction boundary.
- **DTO projection at the query layer** — `ProjectTo<T>()` minimizes payload size and eliminates accidental lazy-load serialization issues.
- **Signal-first client state** — Angular Signals reduce boilerplate versus NgRx for this domain scope while preserving fine-grained reactivity for presence indicators, pagination, and message threads.
- **Functional cross-cutting** — Guards, interceptors, and resolvers use Angular's functional APIs (`CanActivateFn`, `HttpInterceptorFn`, `ResolveFn`) aligned with modern standalone bootstrap (`app.config.ts`).

---

## License

Private repository. All rights reserved.
