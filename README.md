# Wasit Kheir - Lost & Found Platform

> A production-ready RESTful API for managing lost and found reports, built with .NET 9 and Clean Architecture.

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Database Setup](#database-setup)
- [Running the Application](#running-the-application)
- [Deployment](#deployment)
- [API Reference](#api-reference)
- [Authentication](#authentication)
- [Real-Time Communication](#real-time-communication)
- [Roles & Permissions](#roles--permissions)
- [Health Checks](#health-checks)
- [Known Limitations](#known-limitations)
- [Contributing](#contributing)

---

## Overview

**Wasit Kheir** is a backend API that powers a lost-and-found platform where users can report lost or found items and people, communicate via real-time chat, receive intelligent match suggestions, and get notified about relevant activity. It is designed to be consumed by a Flutter mobile application or any HTTP client.

### Key Features

- **User Management** -- Registration, email verification, login, Google Sign-In, password reset, account deletion
- **Report System** -- Create, update, and search lost/found reports with images, geolocation, and categories
- **Moderation Workflow** -- Admin-controlled lifecycle (Pending, Approved, Rejected, Matched, Closed, Archived, Flagged)
- **Smart Matching** -- Automatic and manual matching based on subcategory, proximity, and keyword similarity
- **Real-Time Chat** -- 1-to-1 messaging via SignalR with read receipts
- **Notifications** -- In-app + push notifications (Firebase FCM) for matches, messages, status changes, and location alerts
- **Admin Panel Support** -- Report moderation, user verification, and content management endpoints

---

## Architecture

The solution follows **Clean Architecture** principles with strict dependency inversion:

```
LostAndFound.sln
|
|-- LostAndFound.Domain          Entities, enums, core domain models
|-- LostAndFound.Application     DTOs, services, interfaces, validators, MediatR handlers
|-- LostAndFound.Infrastructure  EF Core, repositories, Identity, persistence config
|-- LostAndFound.Api             Controllers, middleware, SignalR hubs, DI setup
```

**Dependency flow:** `Api -> Application <- Infrastructure -> Domain`

---

## Tech Stack

| Layer              | Technology                                                    |
|--------------------|---------------------------------------------------------------|
| Runtime            | .NET 9.0, C#                                                 |
| Web Framework      | ASP.NET Core Web API                                         |
| Database           | SQL Server + Entity Framework Core 9 (Code-First)            |
| Authentication     | ASP.NET Core Identity, JWT Bearer, Refresh Tokens, Google OAuth |
| Real-Time          | SignalR (WebSockets / Long Polling)                          |
| Push Notifications | Firebase Admin SDK (FCM) with graceful fallback              |
| Validation         | FluentValidation + MediatR pipeline behavior                 |
| Mapping            | AutoMapper                                                   |
| Logging            | Serilog (Console + Rolling File sinks)                       |
| API Documentation  | Swagger / OpenAPI (Swashbuckle) with XML comments            |
| Rate Limiting      | ASP.NET Core Rate Limiting (per-policy: auth, upload, api)   |

---

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server (Developer, Express, or LocalDB)
- (Optional) Firebase project for push notifications
- (Optional) Gmail SMTP credentials for email services

### Clone & Restore

```bash
git clone <repo-url>
cd LostAndFound
dotnet restore
```

---

## Configuration

All settings are managed through `LostAndFound.Api/appsettings.json`. For local development, use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) for sensitive values.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=LostAndFoundDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "SecretKey": "",
    "Issuer": "LostAndFound.Api",
    "Audience": "LostAndFound.Client",
    "ExpiryMinutes": 60
  },
  "Google": { "ClientId": "" },
  "Firebase": { "ProjectId": "", "ClientEmail": "", "PrivateKey": "", "VapidKey": "" },
  "EmailSettings": { "From": "", "Password": "", "Host": "smtp.gmail.com", "Port": 587, "EnableSSL": true },
  "Cors": { "AllowedOrigins": [] }
}
```

> **Note:** In production, set `JwtSettings:SecretKey` to at least 32 characters. The application will fail to start if the key is missing or too short.

For Firebase push notification setup, see [`docs/FIREBASE_PUSH_SETUP.md`](docs/FIREBASE_PUSH_SETUP.md). If Firebase is not configured, the API falls back to a no-op stub -- all other functionality remains unaffected.

---

## Database Setup

### Option A -- EF Core Migrations (Recommended)

```bash
dotnet ef database update \
  --project LostAndFound.Infrastructure \
  --startup-project LostAndFound.Api
```

### Option B -- Database Migrator Tool

A dedicated `LostAndFound.DatabaseMigrator` project can apply migrations and copy data from a source database:

```powershell
.\Run-DatabaseMigrator.ps1
```

---

## Running the Application

```bash
cd LostAndFound.Api
dotnet run
```

The API will start on the default Kestrel ports. In non-Production environments:

- **Swagger UI** is available at `/swagger`
- **Static files** (uploaded images) are served from `/uploads/...`

---

## Deployment

### Production Environment Variables

Set via environment variables, Azure Key Vault, or a production `appsettings.json`:

| Variable                            | Required | Notes                                          |
|-------------------------------------|----------|-------------------------------------------------|
| `ConnectionStrings__DefaultConnection` | Yes   | SQL Server connection string                   |
| `JwtSettings__SecretKey`            | Yes      | Minimum 32 characters                          |
| `JwtSettings__Issuer`               | Yes      | Token issuer identifier                        |
| `JwtSettings__Audience`             | Yes      | Token audience identifier                      |
| `Google__ClientId`                  | No       | Required for Google Sign-In                    |
| `EmailSettings__From`               | No       | SMTP sender address                            |
| `EmailSettings__Password`           | No       | SMTP password / app password                   |
| `Firebase__ProjectId`               | No       | Required for push notifications                |
| `Cors__AllowedOrigins`              | Yes      | Comma-separated list of allowed origins        |

### Publish & Deploy

```bash
dotnet publish LostAndFound.Api -c Release -o ./out
```

Deploy the `out` directory to your hosting environment (IIS, Azure App Service, Docker, etc.), ensure SQL Server is reachable, and verify via the [health check endpoints](#health-checks).

---

## API Reference

Full endpoint documentation with request/response examples is maintained in:

> **[`API_DOCUMENTATION_FULL.md`](API_DOCUMENTATION_FULL.md)**

### Response Envelope

All endpoints return a standardized `BaseResponse<T>` envelope:

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { },
  "errors": []
}
```

### Endpoint Groups

| Group           | Base Path            | Description                                |
|-----------------|----------------------|--------------------------------------------|
| Authentication  | `/api/auth`          | Signup, login, Google OAuth, token refresh, password management |
| Reports         | `/api/reports`       | CRUD for lost/found reports with images and geolocation |
| Categories      | `/api/categories`    | Category and subcategory management        |
| Subcategories   | `/api/subcategories` | Subcategory listings and filtering         |
| Users           | `/api/users`         | Profile management, user lookup            |
| Chat            | `/api/chat`          | 1-to-1 messaging sessions                 |
| Notifications   | `/api/notifications` | In-app notifications, device registration  |
| Matching        | `/api/matching`      | Automatic and manual report matching       |
| Dashboard       | `/api/home`          | Home feed and statistics                   |
| Admin           | `/api/admin`         | Report moderation and user verification    |

---

## Authentication

The API uses a **JWT + Refresh Token** authentication model with support for multi-device sessions.

| Step | Endpoint | Description |
|------|----------|-------------|
| 1    | `POST /api/auth/signup` | Register with email/password |
| 2    | `GET /api/auth/verify-account` | Verify email with OTP code |
| 3    | `POST /api/auth/login` | Obtain access + refresh tokens |
| 4    | `POST /api/auth/refresh-token` | Rotate tokens (old token is revoked) |
| 5    | `POST /api/auth/logout` | Invalidate refresh token |

**Token usage:** Include the access token in the `Authorization` header as `Bearer <token>`.

**Google Sign-In:** Supported via `POST /api/auth/google` with a Google ID token.

---

## Real-Time Communication

### SignalR Hubs

| Hub                  | URL                             | Purpose                    |
|----------------------|---------------------------------|----------------------------|
| Chat Hub             | `{BASE_URL}/chatHub`           | Real-time messaging        |
| Notification Hub     | `{BASE_URL}/notificationHub`   | Live notification delivery |

**Authentication:** Pass the JWT as a query parameter:

```
/chatHub?access_token=<JWT>
/notificationHub?access_token=<JWT>
```

### Push Notifications (Firebase)

1. Retrieve the VAPID key: `GET /api/notifications/vapid-public-key`
2. Register the device token: `POST /api/notifications/register-device`
3. Notifications are sent automatically on matches, messages, status updates, and location alerts

---

## Roles & Permissions

| Role      | Access Level                                                                       |
|-----------|------------------------------------------------------------------------------------|
| Anonymous | View approved/matched/closed reports, dashboard, categories                        |
| User      | All anonymous access + manage own reports, chat, notifications, matching, profile  |
| Admin     | All user access + approve/reject/flag/archive reports, delete reports, verify users |

> Anonymous users only see reports with lifecycle status: **Approved**, **Matched**, or **Closed**. Report owners can always see their own reports regardless of status.

---

## Health Checks

| Endpoint        | Purpose                              |
|-----------------|--------------------------------------|
| `/health`       | Overall application health           |
| `/health/ready` | Readiness probe (database + dependencies) |
| `/health/live`  | Liveness probe                       |

---

## Rate Limiting

The API enforces per-policy rate limits to prevent abuse:

| Policy   | Limit              | Applied To                     |
|----------|--------------------|---------------------------------|
| `auth`   | 5 requests/min     | Authentication endpoints       |
| `refresh`| 20 requests/min    | Token refresh endpoint         |
| `upload` | 10 requests/min    | File upload endpoints          |
| `api`    | 100 requests/min   | All other endpoints (default)  |

---

## Known Limitations

The following items are acknowledged as areas for future improvement:

- **Image validation** relies on file extension only; content-type verification is not enforced
- **Nearby reports** query loads candidates into memory before filtering by distance
- **Saved reports** queries may exhibit N+1 patterns under high volume
- **Path traversal** protection on image uploads is basic

These do not affect core functionality or demo stability.

---

## Contributing

1. Follow the existing Clean Architecture patterns and conventions
2. Use `BaseResponse<T>` for all API responses
3. Place business logic in Application services, not controllers
4. Use FluentValidation for request validation
5. Update [`API_DOCUMENTATION_FULL.md`](API_DOCUMENTATION_FULL.md) when adding or modifying endpoints
6. Use clear commit messages: `feat:`, `fix:`, `refactor:`, `docs:`

---

## Project Structure

```
LostAndFound/
  LostAndFound.Domain/
    Entities/           Domain entities (AppUser, Report, ChatSession, etc.)
    Enums/              Enumerations (ReportType, ReportLifecycleStatus, Gender, etc.)

  LostAndFound.Application/
    DTOs/               Data transfer objects organized by feature
    Interfaces/         Service and repository contracts
    Services/           Business logic implementations
    Validators/         FluentValidation rules
    Features/           MediatR command/query handlers
    Common/             Shared types (BaseResponse, pagination)
    Mapping/            AutoMapper profiles

  LostAndFound.Infrastructure/
    Persistence/
      Config/           EF Core entity configurations
      Repositories/     Repository and Unit of Work implementations
      Migrations/       EF Core migrations
    Services/           Infrastructure services (Email, OTP, Firebase)

  LostAndFound.Api/
    Controllers/        API endpoints
    Hubs/               SignalR hubs (Chat, Notifications)
    Middleware/         Custom middleware (error handling, logging)
    wwwroot/           Static files and uploads
    Program.cs         Application entry point and DI configuration
```

---

*For detailed endpoint documentation, see [`API_DOCUMENTATION_FULL.md`](API_DOCUMENTATION_FULL.md).*
