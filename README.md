# Wasit Kheir — Lost & Found Platform

> RESTful API for managing lost and found reports — built with **.NET 9** and **Clean Architecture**.

## Features

- User authentication (Email/Password, Google Sign-In, JWT + Refresh Tokens)
- Lost/found report management with images, geolocation, and categories
- Admin moderation workflow (Pending → Approved / Rejected / Flagged / Archived)
- Smart matching engine (subcategory, proximity, keyword similarity)
- Real-time chat via SignalR
- Push notifications via Firebase FCM
- Rate limiting, health checks, and Swagger documentation

## Architecture

```
LostAndFound.Domain           Entities, enums, domain models
LostAndFound.Application      DTOs, services, interfaces, validators
LostAndFound.Infrastructure   EF Core, repositories, Identity, persistence
LostAndFound.Api              Controllers, SignalR hubs, middleware, DI
```

## Tech Stack

| Component        | Technology                                            |
|------------------|-------------------------------------------------------|
| Runtime          | .NET 9, C#                                            |
| Database         | SQL Server + EF Core 9 (Code-First)                   |
| Auth             | ASP.NET Core Identity, JWT, Google OAuth              |
| Real-Time        | SignalR                                               |
| Push             | Firebase Admin SDK (graceful fallback if unconfigured)|
| Validation       | FluentValidation + MediatR pipeline                   |
| Docs             | Swagger / OpenAPI with XML comments                   |
| Logging          | Serilog                                               |

## Quick Start

```bash
git clone <repo-url>
cd LostAndFound
dotnet restore
```

Configure `LostAndFound.Api/appsettings.json` (or use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) for sensitive values):

```json
{
  "ConnectionStrings": { "DefaultConnection": "Server=.;Database=LostAndFoundDb;Trusted_Connection=True;TrustServerCertificate=True;" },
  "JwtSettings": { "SecretKey": "", "Issuer": "LostAndFound.Api", "Audience": "LostAndFound.Client", "ExpiryMinutes": 60 },
  "Google": { "ClientId": "" },
  "Firebase": { "ProjectId": "", "ClientEmail": "", "PrivateKey": "", "VapidKey": "" },
  "EmailSettings": { "From": "", "Password": "", "Host": "smtp.gmail.com", "Port": 587, "EnableSSL": true },
  "Cors": { "AllowedOrigins": [] }
}
```

Apply migrations and run:

```bash
dotnet ef database update --project LostAndFound.Infrastructure --startup-project LostAndFound.Api
cd LostAndFound.Api
dotnet run
```

Swagger UI is available at `/swagger` in non-Production environments.

## API Overview

All endpoints return a standard envelope:

```json
{ "success": true, "message": "...", "data": { }, "errors": [] }
```

| Group          | Base Path            | Description                          |
|----------------|----------------------|--------------------------------------|
| Auth           | `/api/auth`          | Signup, login, Google OAuth, tokens  |
| Reports        | `/api/reports`       | CRUD with images and geolocation     |
| Categories     | `/api/categories`    | Category and subcategory management  |
| Users          | `/api/users`         | Profile management                   |
| Chat           | `/api/chat`          | Real-time 1-to-1 messaging          |
| Notifications  | `/api/notifications` | In-app + push notifications          |
| Matching       | `/api/matching`      | Automatic and manual report matching |
| Home           | `/api/home`          | Dashboard feed and statistics        |
| Admin          | `/api/admin`         | Report moderation, user verification |

> Full endpoint documentation: **[`API_DOCUMENTATION_FULL.md`](API_DOCUMENTATION_FULL.md)**

## Auth Flow

1. `POST /api/auth/signup` — Register
2. `GET /api/auth/verify-account` — Verify email with OTP
3. `POST /api/auth/login` — Get access + refresh tokens
4. `POST /api/auth/refresh-token` — Rotate tokens
5. `POST /api/auth/logout` — Invalidate session

Include the access token as `Authorization: Bearer <token>`.

## Real-Time

| Hub              | URL                    | Auth                              |
|------------------|------------------------|-----------------------------------|
| Chat             | `/chatHub`             | `?access_token=<JWT>`             |
| Notifications    | `/notificationHub`     | `?access_token=<JWT>`             |

## Roles

| Role      | Access                                                            |
|-----------|-------------------------------------------------------------------|
| Anonymous | View approved reports, dashboard, categories                      |
| User      | + Own reports, chat, notifications, matching, profile             |
| Admin     | + Approve/reject/flag reports, delete reports, verify users       |

## Health Checks

`/health` · `/health/ready` · `/health/live`

## Deployment

```bash
dotnet publish LostAndFound.Api -c Release -o ./out
```

Set required environment variables: `ConnectionStrings__DefaultConnection`, `JwtSettings__SecretKey` (min 32 chars), `Cors__AllowedOrigins`.

Firebase setup: [`docs/FIREBASE_PUSH_SETUP.md`](docs/FIREBASE_PUSH_SETUP.md)
