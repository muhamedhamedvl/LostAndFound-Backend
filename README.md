## Lost & Found – Backend (.NET 9, Clean Architecture)

Backend API for a **Lost & Found / Wasit Kheir** platform.  
It provides authentication, lost/found reports with images, a home feed, chat, notifications, matching, and admin moderation, designed to be consumed by a Flutter mobile app or any HTTP client.

The full HTTP contract (all endpoints and example payloads) is documented in **`API_DOCUMENTATION_FULL.md`** and should be treated as the source of truth for the frontend team.

---

## 1. Project Overview

### What this project does

- Manages **user accounts** (signup, email verification, login, Google Sign‑In, password reset, delete account).
- Allows users to create **lost or found reports** with:
  - Images
  - Location (lat/lng)
  - Categories/subcategories
  - Moderation lifecycle (Pending → Approved / Rejected / Matched / Closed …).
- Provides a **home dashboard** with:
  - Recent reports feed
  - Total report count
  - Categories count
  - “My reports” count (for logged‑in users).
- Supports **user profiles**:
  - View other users’ profiles and their reports
  - Edit own profile and upload a profile picture.
- Implements **1‑to‑1 chat** between users with real‑time updates via SignalR.
- Sends **notifications**:
  - In‑app listing + unread count
  - Push notifications (Firebase FCM when configured)
  - For matches, new messages, interested users, status updates, location alerts.
- Provides **matching** between reports based on:
  - Subcategory
  - Location proximity
  - Keyword overlap
  - Automatic matching when reports are created, plus manual re‑run endpoint.
- Includes **admin tools**:
  - Approve / reject / flag / archive reports
  - Delete any report
  - View all reports (any lifecycle)
  - Verify users.

For a visual user flow (sign‑up → create report → home → update/delete → chat → notifications → admin moderation), the current backend fully supports the diagram used in the project.

---

## 2. Tech Stack

### Core

- **Language / Runtime:** C#, **.NET 9.0**
- **Web framework:** ASP.NET Core Web API
- **Architecture:**
  - `LostAndFound.Domain` – entities, enums, core domain logic
  - `LostAndFound.Application` – DTOs, services, interfaces, validation, MediatR handlers
  - `LostAndFound.Infrastructure` – EF Core, repositories, persistence config, Identity
  - `LostAndFound.Api` – HTTP endpoints, middleware, SignalR hubs, DI configuration

### Storage

- **Database:** SQL Server
- **ORM:** Entity Framework Core 9 with code‑first migrations
- **Static files / images:**
  - Stored locally under `LostAndFound.Api/wwwroot`
  - Profile pictures: `/uploads/profiles/{userId}/...`
  - Report images: `/uploads/reports/{reportId}/...`

### Authentication & Authorization

- JWT Bearer **access tokens**
- **Refresh tokens**
- Email/password authentication
- **Email verification** (verification codes)
- Password reset / change password
- **Google Sign‑In** (`POST /api/auth/google`) using configured Google Client Id
- ASP.NET Core Identity with custom `AppUser` (int keys)
- Roles:
  - `User`
  - `Admin` (role‑based authorization with `[Authorize(Roles = "Admin")]`)

### Real‑Time & Notifications

- **SignalR hubs:**
  - `/chatHub` – chat sessions & messages
  - `/notificationHub` – realtime in‑app notifications
  - JWT is passed as `access_token` query parameter.
- **Push notifications (optional but supported):**
  - Firebase Admin SDK (`FirebaseAdmin` package)
  - Uses `Firebase` configuration (ProjectId, ClientEmail, PrivateKey, VapidKey)
  - If Firebase is missing or fails, a **stub push service** is used (no‑op) so the API still runs.

### Observability & Hardening

- **Serilog** logging (console + rolling log files)
- Custom middleware for:
  - Error handling
  - Request logging
- Rate limiting (`Microsoft.AspNetCore.RateLimiting`):
  - Separate policies for auth, upload, and general API
- Health checks:
  - `/health`
  - `/health/ready`
  - `/health/live`

---

## 3. Getting Started (Local Setup)

### 3.1 Prerequisites

- **.NET 9 SDK**
- **SQL Server** (Developer / Express / localdb)
- **PowerShell** (for the helper script on Windows)
- Optional:
  - Firebase project (for push notifications)
  - SMTP account (Gmail or another provider) for sending emails

### 3.2 Clone & Restore

```bash
git clone <repo-url>
cd LostAndFound
dotnet restore
```

### 3.3 Configure `appsettings.json` (Local Dev)

File: `LostAndFound.Api/appsettings.json` (simplified):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=LostAndFoundDbfinal;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "SecretKey": "",
    "Issuer": "LostAndFound.Api",
    "Audience": "LostAndFound.Client",
    "ExpiryMinutes": 60
  },
  "Google": {
    "ClientId": ""
  },
  "Firebase": {
    "ProjectId": "",
    "ClientEmail": "",
    "PrivateKey": "",
    "VapidKey": ""
  },
  "EmailSettings": {
    "From": "",
    "Password": "",
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSSL": true
  },
  "Cors": {
    "AllowedOrigins": []
  }
}
```

For **local development** you can:

- Put non‑sensitive values directly here (e.g. local DB, issuer/audience), and  
- Use **User Secrets** (`dotnet user-secrets`) for secrets like:
  - `JwtSettings:SecretKey`
  - `EmailSettings:*`
  - `Firebase:*`
  - `Google:ClientId`

Firebase push notification setup is described in `docs/FIREBASE_PUSH_SETUP.md`.

### 3.4 Database & Migrations

#### Option A – Standard EF Core migrations

From the solution root:

```bash
dotnet ef database update \
  --project LostAndFound.Infrastructure \
  --startup-project LostAndFound.Api
```

This applies all migrations to the database defined in `ConnectionStrings:DefaultConnection`.

#### Option B – DatabaseMigrator (copy + migrate)

The project `LostAndFound.DatabaseMigrator` can:

1. Apply migrations to the target DB (from `LostAndFound.Api/appsettings.json`).
2. Copy data from a **source DB** (User Secrets + appsettings) into the target DB.

From the solution root:

```powershell
.\Run-DatabaseMigrator.ps1
```

This script:

- Ensures it’s run from the proper folder
- Runs `dotnet run` on `LostAndFound.DatabaseMigrator`
- Migrates and copies data in FK‑safe order (roles, users, categories, reports, images, matches, chats, notifications, device tokens).

### 3.5 Run the API Locally

```bash
cd LostAndFound.Api
dotnet run
```

The API will:

- Serve HTTP/HTTPS (Kestrel default ports)
- Expose Swagger UI at `/swagger` (non‑Production)
- Serve static files under `/uploads/...`

---

## 4. Production Setup

> High‑level only; adapt to your hosting (IIS, Azure, Docker, etc.).

### 4.1 Required Environment / Secrets

Set these via environment variables or a secure production `appsettings.json`:

- **Database**
  - `ConnectionStrings__DefaultConnection`

- **JWT**
  - `JwtSettings__SecretKey`
    - Must be **at least 32 characters** in Production.  
      If missing/too short, the API will fail to start.
  - `JwtSettings__Issuer`
  - `JwtSettings__Audience`
  - (Optional) `JwtSettings__ExpiryMinutes`

- **Google Sign‑In**
  - `Google__ClientId`

- **Email (SMTP)**
  - `EmailSettings__From`
  - `EmailSettings__Password`
  - `EmailSettings__Host`
  - `EmailSettings__Port`
  - `EmailSettings__EnableSSL`

- **Firebase (Push Notifications)**
  - `Firebase__ProjectId`
  - `Firebase__ClientEmail`
  - `Firebase__PrivateKey`
  - `Firebase__VapidKey`

If Firebase settings are missing or invalid:

- The backend will log a warning and fall back to a **stub push service** (no push), but the rest of the API will continue to work.

- **CORS**
  - `Cors:AllowedOrigins` in `appsettings.json` as a JSON array, or
  - `Cors__AllowedOrigins` env var (comma‑separated) for allowed origins, e.g.:

    ```text
    Cors__AllowedOrigins=https://app.example.com,https://admin.example.com
    ```

### 4.2 Deploying (Typical Steps)

1. Publish:

   ```bash
   dotnet publish LostAndFound.Api -c Release -o ./out
   ```

2. Deploy the `out` folder to your server / container.
3. Configure environment variables or production `appsettings.json`.
4. Ensure SQL Server is reachable and apply migrations (EF or DatabaseMigrator).
5. Configure HTTPS and reverse proxy (if used).
6. Verify:
   - `/health`, `/health/ready`, `/health/live` return healthy
   - `/swagger` works in non‑Production
   - Frontend origin is allowed via CORS.

---

## 5. API Usage (For Frontend Team)

### 5.1 Base URL

- **BASE_URL** = root of the deployed API  
  Example: `https://api.yourdomain.com`

All endpoints in `API_DOCUMENTATION_FULL.md` are relative to this.

### 5.2 Auth Flow (User)

**Signup → Verify → Login → Refresh flow:**

1. **Signup**

   - `POST /api/auth/signup`
   - Body includes `firstName`, `lastName`, `email`, `phone`, `password`, `dateOfBirth`, `gender`.
   - Returns:
     - `user` object (with `isVerified = false` initially)
     - `accessToken`, `refreshToken`, `expiresAt`.

2. **Verify email**

   - `GET /api/auth/verify-account?code=...&email=...`
   - Marks user as verified; returns base response.

3. **Login**

   - `POST /api/auth/login`
   - Returns:

     ```json
     {
       "success": true,
       "message": "Login successful",
       "data": {
         "user": {
           "id": 1,
           "fullName": "mohamed hamed",
           "email": "mh1191128@gmail.com",
           "phone": "01146784553",
           "isVerified": true,
           "roles": ["User"],
           "dateOfBirth": "2004-04-06",
           "gender": "Male",
           "profilePictureUrl": null,
           "createdAt": "...",
           "updatedAt": null
         },
         "accessToken": "<JWT>",
         "refreshToken": "<refresh>",
         "expiresAt": "..."
       },
       "errors": []
     }
     ```

4. **Refresh token**

   - `POST /api/auth/refresh-token`
   - Body: `{ "refreshToken": "..." }`
   - Returns new access & refresh tokens.

5. **Google Sign‑In (optional)**

   - `POST /api/auth/google`
   - Body: `{ "idToken": "<Google ID token from client>" }`
   - Returns the same structure as login.

### 5.3 Standard Response Shape

All documented endpoints (except a few admin operations) use:

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { ... },
  "errors": []
}
```

- `success`: `true` / `false`
- `message`: human‑readable text
- `data`: main payload or `null`
- `errors`: array of error strings (validation, etc.)

### 5.4 File Upload Notes

- **Content‑Type:** always `multipart/form-data` for file uploads.

- **Profile picture**
  - `PUT /api/users/me` with form fields including `profilePicture`
  - Or `POST /api/users/me/profile-picture`
  - Limit: 5MB

- **Report images**
  - `POST /api/reports`:
    - Fields: `title`, `description`, `type`, `subCategoryId`, `locationName`, `latitude`, `longitude`, `dateReported`, `images[]`
  - `PUT /api/reports/{id}`:
    - Fields: `title?`, `description?`, `type?`, `locationName?`, `latitude?`, `longitude?`, `subCategoryId?`, `dateReported?`, `imageIdsToRemove[]`, `newImages[]`
  - Limit: 10MB per image

### 5.5 Where to Find All Endpoints

- Use **`API_DOCUMENTATION_FULL.md`** in the repo root.  
  It includes:
  - All paths, methods, and query/body parameters
  - Example success and error responses
  - Notes on pagination, filtering, and DTO shapes.

---

## 6. Roles & Permissions

### Anonymous

- Can:
  - View **public** reports:
    - `GET /api/reports`
    - `GET /api/reports/{id}`
    - `GET /api/reports/nearby`
  - View dashboard:
    - `GET /api/home/dashboard`
  - View categories mapping:
    - `GET /api/categories/mapping`
  - Get VAPID public key:
    - `GET /api/notifications/vapid-public-key`
- Visibility rule:
  - Only reports with `lifecycleStatus` in **Approved / Matched / Closed** are returned.

### Authenticated User (`User` role)

- Everything anonymous can do, plus:
  - Manage own account:
    - `GET /api/auth/me`, `GET /api/users/me`
    - Change password, change email, delete account, logout
  - Manage own reports:
    - `POST /api/reports`
    - `GET /api/reports/my-reports`
    - `PUT /api/reports/{id}` (if owner)
    - `PUT /api/reports/{id}/status` (subject to lifecycle rules)
    - `DELETE /api/reports/{id}` (if owner)
  - Access their own reports regardless of lifecycle (including `Pending`, `Rejected`, etc.).
  - View other users’ profiles:
    - `GET /api/users/{id}`
  - View a user’s reports:
    - `GET /api/users/{id}/reports`
  - Save / unsave reports:
    - `POST /api/reports/{id}/save`, `DELETE /api/reports/{id}/save`
  - Report abuse for a report:
    - `POST /api/reports/{id}/report`
  - Chat:
    - `/api/chat/sessions` and `/api/chat/sessions/{sessionId}/messages`
  - Notifications:
    - `/api/notifications`, `/api/notifications/unread`, `/api/notifications/{id}/read`, `/api/notifications/mark-all-read`, `/api/notifications/register-device`
  - Matching:
    - `/api/matching/run/{reportId}` (manual trigger)
    - `/api/matching/{reportId}` (view matches)

### Admin (`Admin` role)

- Access to `/api/admin/**`:
  - `GET /api/admin/reports`
    - All reports (any `lifecycleStatus`)
  - `PUT /api/admin/reports/{id}/approve`
  - `PUT /api/admin/reports/{id}/reject`
  - `PUT /api/admin/reports/{id}/flag`
  - `PUT /api/admin/reports/{id}/archive`
  - `DELETE /api/admin/reports/{id}`
  - `PUT /api/admin/users/{id}/verify`
- Admin is also exempt from lifecycle visibility restrictions on individual reports.

---

## 7. Realtime & Notifications

### 7.1 SignalR Connections

- **Chat hub:** `BASE_URL/chatHub`
- **Notification hub:** `BASE_URL/notificationHub`

**Authentication in SignalR:**

- Pass the JWT **access token** as a query string parameter:

```text
BASE_URL/chatHub?access_token=<JWT>
BASE_URL/notificationHub?access_token=<JWT>
```

The backend is configured to extract tokens from `access_token` for these hub paths.

### 7.2 Notification Flows

See `API_DOCUMENTATION_FULL.md` for full DTO shapes. High‑level types:

- `match` – A matching report was found for a user’s report.
- `interested` – Someone expressed interest in a report (`POST /api/reports/{id}/interested`).
- `new_message` – New chat message in a session.
- `status_update` – Report lifecycle/status changed (e.g. Approved, Closed).
- `location_alert` – A report appears near a saved/interest location.

**Push notifications (Firebase):**

- VAPID public key:
  - `GET /api/notifications/vapid-public-key`
- Register device token:
  - `POST /api/notifications/register-device` with:

    ```json
    {
      "token": "<FCM device token>",
      "platform": "android" // or "ios" or "web"
    }
    ```

When triggers occur (match, new message, status update, etc.), the backend:

- Saves a Notification entity,
- Attempts to send a SignalR message to connected clients,
- Attempts to send a push notification via Firebase if configured.

---

## 8. Matching Flow

### 8.1 How Matching Works (Backend)

The `MatchingService`:

- For a given `reportId`:
  - Loads the source report (with subcategory and location).
  - Finds candidate reports:
    - Same subcategory
    - Different user
    - Not Closed
  - Calculates a **similarity score (0–100)** based on:
    - Same subcategory (base score)
    - Distance (Haversine between lat/lng)
    - Keyword overlap in title + description
  - Stores matches in `ReportMatches` with the score.
  - If score ≥ 80:
    - Optionally sets source `Status` to `Matched`
    - Sets `MatchPercentage`
    - Sends a **match** notification to the owner.

### 8.2 How Matching Is Triggered

There are two ways:

1. **Automatic (background)**
   - After a report is created (`POST /api/reports`), the backend triggers `RunMatchingAsync(report.Id)` in a **fire‑and‑forget background task**.  
     This does *not* block the create API.

2. **Manual API**
   - `POST /api/matching/run/{reportId}`  
     Runs matching immediately and returns matches.
   - `GET /api/matching/{reportId}`  
     Lists previously computed matches for that report.

Frontend can rely on automatic matching, and optionally expose a “refresh matches” button that calls the manual endpoint.

---

## 9. Common Pitfalls / Troubleshooting

### 9.1 CORS Errors

Symptoms: frontend gets  CORS errors in the browser / mobile web.

- Ensure `Cors:AllowedOrigins` or `Cors__AllowedOrigins` includes the **exact** frontend origin(s), e.g.:

  ```json
  "Cors": {
    "AllowedOrigins": [ "https://my-flutter-web.app" ]
  }
  ```

  or:

  ```text
  Cors__AllowedOrigins=https://my-flutter-web.app
  ```

### 9.2 JWT Expired / Unauthorized (401)

- Access tokens have limited lifetime (`JwtSettings:ExpiryMinutes`).
- When receiving 401 due to expiration:
  - Call `POST /api/auth/refresh-token` with the stored refresh token.
  - Update stored `accessToken` and `refreshToken`.
- On logout:
  - Call `POST /api/auth/logout` so backend invalidates the refresh token.

### 9.3 Image Upload Failures

- Check:
  - Content‑Type is `multipart/form-data`.
  - Field names are correct: `images`, `newImages`, `profilePicture`, etc. (see docs).
  - File size:
    - Profile picture ≤ **5MB**
    - Report image ≤ **10MB**
  - Only allowed extensions: `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`.

### 9.4 SignalR Connection Issues

- Ensure:
  - `access_token` query parameter is set with a valid JWT.
  - Base URL uses **HTTPS** in production.
  - CORS allows WebSockets/long polling (same origin config as REST).
- If you see unauthorized errors:
  - Verify that the token is not expired and contains the correct user ID and roles.

### 9.5 Production Config Pitfalls

- Missing or weak `JwtSettings:SecretKey`:
  - In Production, startup will throw if the key is missing/too short.
- Wrong DB connection:
  - Check logs and `/health` endpoints; confirm the DB and migrations are applied.
- Firebase misconfiguration:
  - The app will log a warning and use stub push notifications (no push) instead of crashing.

---

## 10. Contribution & Conventions

### 10.1 General Guidelines

- **Do not** change existing API contracts lightly.  
  - Responses should keep the standard `{ success, message, data, errors }` envelope.
  - Any endpoint additions/changes must be mirrored in `API_DOCUMENTATION_FULL.md`.
- Follow existing patterns:
  - Use DTOs in `LostAndFound.Application.DTOs.*`
  - Put business logic in Application services, not in controllers.
  - Use AutoMapper mappings in `MappingProfile`.
  - Use `BaseResponse<T>` for results.

### 10.2 Branching / Commits

There is no strict enforced branching strategy in this repo, but recommended:

- Create feature branches per change:
  - `feature/<short-description>`
  - `bugfix/<short-description>`
- Use clear, concise commit messages:
  - Focus on *what* and *why*, e.g. `feat: add report lifecycle filtering to public feed`.

### 10.3 Adding New Endpoints

When extending the API:

- Place controllers under `LostAndFound.Api/Controllers`.
- Use proper attributes:
  - `[ApiController]`, `[Route("api/[controller]")]`
  - `[Authorize]` / `[AllowAnonymous]` / `[Authorize(Roles = "Admin")]`
- Reuse the existing validation and response patterns:
  - Use `BaseResponse<T>.SuccessResult(...)` / `.FailureResult(...)`.
  - Validate models using FluentValidation where appropriate.
- Update:
  - `API_DOCUMENTATION_FULL.md`
  - Any relevant diagrams or README sections if behavior changes.

---

This README is intended as a **single entry point** for:

- Frontend developers (Flutter team) integrating with the API.
- DevOps/infra engineers deploying and configuring the backend.
- Contributors who want to extend the system without breaking existing behavior.  

For endpoint‑level details, always refer to **`API_DOCUMENTATION_FULL.md`**. 