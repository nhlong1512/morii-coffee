<h1 align="center">☕ Morii Coffee API</h1>

<p align="center">
  A production-ready RESTful backend for a coffee shop platform — built with Clean Architecture, CQRS, and Domain-Driven Design on .NET 10.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/C%23-13.0-239120?logo=csharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/SQL%20Server-2022-CC2927?logo=microsoftsqlserver&logoColor=white" alt="SQL Server" />
  <img src="https://img.shields.io/badge/Redis-7-DC382D?logo=redis&logoColor=white" alt="Redis" />
  <img src="https://img.shields.io/badge/MinIO-Object%20Storage-C72E49?logo=minio&logoColor=white" alt="MinIO" />
  <img src="https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white" alt="Docker" />
</p>

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Running with Docker](#running-with-docker)
  - [Configuration](#configuration)
- [API Reference](#api-reference)
- [Order Lifecycle](#order-lifecycle)
- [Cart System](#cart-system)
- [Background Jobs](#background-jobs)
- [Testing](#testing)
- [CI/CD](#cicd)
- [Project Structure](#project-structure)

---

## Overview

Morii Coffee API powers a full-featured coffee shop platform — from browsing the menu and managing a shopping cart to placing orders and tracking their lifecycle. It handles authentication (JWT + Google OAuth), file uploads, transactional emails, and admin operations, all deployed as a containerised service on AWS.

---

## Features

| Domain | Capabilities |
|--------|-------------|
| **Auth** | Email/password registration & login, Google OAuth, JWT access tokens, refresh tokens, password reset via email |
| **Products** | CRUD for products, product variants (size/type), categories, and promotional banners |
| **Cart** | Redis-backed cart with 24-hour TTL, guest-to-user merge on login |
| **Orders** | Place orders from cart, rank-based status lifecycle, admin status management, customer cancellation |
| **Users** | Profile management, saved delivery profiles |
| **Files** | Presigned upload/download URLs via MinIO (primary) and AWS S3 (fallback) |
| **Email** | Transactional emails via Brevo (password reset, order confirmations) |
| **Admin** | Full order management, product catalog management, user listing |

---

## Architecture

The codebase follows **Clean Architecture** with strict dependency rules — outer layers depend on inner layers, never the reverse.

```
┌─────────────────────────────────────────────────────┐
│                   Presentation                      │
│         ASP.NET Core Controllers · Middleware       │
├─────────────────────────────────────────────────────┤
│                   Infrastructure                    │
│   Hangfire Jobs · Redis Cart · Email · File Storage │
├─────────────────────────────────────────────────────┤
│             Infrastructure.Persistence              │
│      EF Core · Repositories · Migrations · Cache   │
├─────────────────────────────────────────────────────┤
│                    Application                      │
│       CQRS Handlers (MediatR) · DTOs · Validators  │
├─────────────────────────────────────────────────────┤
│                      Domain                         │
│     Aggregates · Entities · Value Objects · Rules  │
├─────────────────────────────────────────────────────┤
│                   Domain.Shared                     │
│          Enums · Constants · Settings               │
└─────────────────────────────────────────────────────┘
```

**Key patterns:**
- **CQRS** — every use case is a separate `ICommand` or `IQuery` handled by a dedicated handler via MediatR
- **Domain aggregates** enforce all business rules (e.g. order status transitions throw `InvalidOperationException` on invalid moves)
- **Repository pattern** with `IUnitOfWork` — persistence concerns are fully isolated from the domain
- **Global exception middleware** maps domain exceptions to appropriate HTTP status codes

---

## Tech Stack

| Category | Technology |
|----------|------------|
| Runtime | .NET 10 / ASP.NET Core 10 |
| ORM | Entity Framework Core 10.0.5 (SQL Server) |
| Mediator | MediatR 14.1.0 |
| Validation | FluentValidation 12.1.1 |
| Mapping | AutoMapper 16.1.1 |
| Caching | Redis 7 via StackExchange.Redis 2.8.x |
| Background jobs | Hangfire 1.8.23 (SQL Server storage) |
| Object storage | MinIO 7.0.0 · AWS S3 SDK 4.0.20.2 |
| Email | Brevo SDK 1.1.2 |
| Authentication | JWT Bearer 10.0.5 · Google OAuth 10.0.5 |
| Logging | Serilog 4.3.1 |
| API docs | Swashbuckle / Swagger 6.7.2 |
| Testing | xUnit · Moq 4.20.72 · FluentAssertions 8.9.0 |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Running with Docker

The development stack (API + SQL Server + Redis + MinIO) is fully Dockerised.

```bash
cd deploy
bash run-docker-development.sh
```

This brings up:

| Service | Port |
|---------|------|
| API | `http://localhost:8002` |
| Swagger UI | `http://localhost:8002/swagger` |
| Hangfire Dashboard | `http://localhost:8002/hangfire` |
| SQL Server | `localhost:1433` |
| Redis | `localhost:6379` |
| MinIO Console | `http://localhost:9001` |

The API seeds an admin account on first run using the `AdminSeed` config values.

### Configuration

Copy `appsettings.json` and fill in the required values. In development, override via `appsettings.Development.json`:

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnectionString": "Server=...;Database=MoriiCoffeeDb;...",
    "CachingConnectionString": "localhost:6379,abortConnect=false"
  },
  "JwtOptions": {
    "Secret": "<256-bit secret>",
    "Issuer": "MoriiCoffee",
    "Audience": "MoriiCoffee",
    "AccessTokenExpiryInMinutes": 480,
    "RefreshTokenExpiryInDays": 7
  },
  "Authentication": {
    "Google": { "ClientId": "...", "ClientSecret": "..." }
  },
  "EmailSettings": {
    "Brevo": { "ApiKey": "..." }
  },
  "MinioSettings": {
    "Endpoint": "localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin"
  },
  "OrderSettings": {
    "AutoCompleteAfterDays": 3,
    "AutoCompleteJobRunHour": 2,
    "AutoCompleteJobRunMinute": 0
  }
}
```

> In production, secrets are injected via AWS SSM Parameter Store and pulled at deploy time.

---

## API Reference

Swagger UI is available at `/swagger` when running in Development mode.

| Controller | Prefix | Auth |
|------------|--------|------|
| `AuthController` | `POST /api/v1/auth/**` | Public |
| `ProductsController` | `/api/v1/products` | Public (read) · Admin (write) |
| `ProductVariantsController` | `/api/v1/product-variants` | Admin |
| `CategoriesController` | `/api/v1/categories` | Public (read) · Admin (write) |
| `BannersController` | `/api/v1/banners` | Public (read) · Admin (write) |
| `CartController` | `/api/v1/cart` | User |
| `OrdersController` | `/api/v1/orders` | User · Admin |
| `UsersController` | `/api/v1/users` | User · Admin |
| `FilesController` | `/api/v1/files` | User |

---

## Order Lifecycle

Orders follow a **rank-based forward-only** state machine. Skipping steps is allowed (e.g. `PENDING → IN_DELIVERY`); going backward is not.

```
PENDING ──► CONFIRMED ──► READY_TO_PICKUP ──► IN_DELIVERY ──► DELIVERED ──► REVIEWED
   │              │
   └──────────────┴──► CANCELLED  (only from PENDING or CONFIRMED)
```

- Customers can cancel from `PENDING` only
- Admins can cancel from `PENDING` or `CONFIRMED`
- `REVIEWED` and `CANCELLED` are terminal states
- `GET /api/v1/orders/{id}/valid-statuses` returns the valid transitions for a given order
- `PATCH /api/v1/orders/{id}/status` returns the updated list of valid next statuses after the transition

---

## Cart System

The cart is stored entirely in **Redis** — there is no cart table in the database.

```
Redis key:   cart:{userId}
Value:       CartDto (JSON)
TTL:         24 hours (reset on each write)
```

**Flow:**
1. `POST /cart/items` — validates product/variant exists in DB, snapshots the price at add-time, writes to Redis
2. `PUT /cart/items` — updates quantity (quantity `0` removes the item)
3. `DELETE /cart/items` — removes a specific product/variant
4. `DELETE /cart` — deletes the Redis key entirely
5. `POST /cart/merge` — merges a guest cart (from `localStorage`) into the user's Redis cart after login; quantities are summed for duplicate items
6. `POST /orders` — reads the cart from Redis, creates an `Order` in the database, then clears the cart

---

## Background Jobs

Hangfire manages recurring jobs with SQL Server as the storage backend. The dashboard is available at `/hangfire` (admin-only in production).

### Order Auto-Complete

Runs daily at `AutoCompleteJobRunHour:AutoCompleteJobRunMinute` UTC. Finds all `IN_DELIVERY` orders whose `CreatedAt` is older than `AutoCompleteAfterDays` days and marks them `DELIVERED`.

```jsonc
"OrderSettings": {
  "AutoCompleteAfterDays": 3,  // orders older than this are auto-completed
  "AutoCompleteJobRunHour": 2,  // 2 AM UTC
  "AutoCompleteJobRunMinute": 0
}
```

The job is decorated with `[DisableConcurrentExecution(600s)]` to prevent overlapping runs. Each order transition is wrapped in a `try/catch` so a single bad order cannot abort the entire batch.

---

## Testing

The project has two test projects covering the domain and application layers.

```bash
# Run all tests
dotnet test source/MoriiCoffee.Domain.Tests
dotnet test source/MoriiCoffee.Application.Tests

# Generate HTML coverage report (Domain + Application)
bash coverage.sh
# → opens coverage-report/index.html
```

| Project | Tests | Focus |
|---------|-------|-------|
| `MoriiCoffee.Domain.Tests` | 68 | Aggregate rules, status transitions, business invariants |
| `MoriiCoffee.Application.Tests` | 225 | Command/query handlers, background jobs |

Test stack: **xUnit** · **Moq** · **FluentAssertions** · **MockQueryable** (for async EF IQueryable)

---

## CI/CD

GitHub Actions pipeline on every push to `main`:

```
push to main
    │
    ├─► unit-test job
    │       dotnet test (Domain + Application)
    │
    └─► build job  (needs: unit-test)
            docker build → push to AWS ECR
                │
                └─► deploy job
                        SSH into EC2
                        pull image from ECR
                        fetch secrets from AWS SSM
                        restart container
```

Secrets (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `EC2_HOST`, `EC2_SSH_KEY`) are stored in GitHub repository secrets.

---

## Project Structure

```
morii-coffee/
├── source/
│   ├── MoriiCoffee.Domain/              # Aggregates, entities, value objects, domain rules
│   ├── MoriiCoffee.Domain.Shared/       # Enums, constants, settings (no dependencies)
│   ├── MoriiCoffee.Application/         # CQRS handlers, DTOs, validators, abstractions
│   ├── MoriiCoffee.Infrastructure/      # Background jobs, cart service, email, file storage
│   ├── MoriiCoffee.Infrastructure.Persistence/ # EF Core DbContext, repositories, migrations
│   ├── MoriiCoffee.Presentation/        # ASP.NET Core host, controllers, middleware, DI setup
│   ├── MoriiCoffee.Domain.Tests/        # Domain layer unit tests
│   └── MoriiCoffee.Application.Tests/   # Application layer unit tests
├── deploy/
│   ├── docker-compose.yml               # Base services (SQL Server, Redis, MinIO)
│   ├── docker-compose.development.yml   # Development overrides (API container)
│   └── run-docker-development.sh        # One-command dev startup
├── docs/
│   └── features/                        # Feature specification documents
├── specs/                               # API / domain specs
├── .github/workflows/deploy.yml         # CI/CD pipeline
├── coverage.sh                          # Coverage report generator
├── global.json                          # .NET SDK version pin (10.0.102)
└── Directory.Build.props                # Solution-wide MSBuild properties
```
