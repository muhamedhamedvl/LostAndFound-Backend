# Wasit Kheir – Lost and Found API Documentation

Complete API reference for the **Wasit Kheir** backend (.NET 9, Clean Architecture). Use this with the mobile app or any client.

---

## Table of Contents

1. [Base Response Structure](#base-response-structure)
2. [Authentication](#authentication)
3. [Reports](#reports)
4. [Categories](#categories)
5. [SubCategories](#subcategories)
6. [Notifications](#notifications)
7. [Profile / Users](#profile--users)
8. [Chat](#chat)
9. [Matching](#matching)
10. [Dashboard / Home](#dashboard--home)
11. [Admin](#admin)
12. [Reference Tables](#reference-tables)
13. [Image / File Upload Rules](#image--file-upload-rules)
14. [Error Codes and Conventions](#error-codes-and-conventions)

---

## Base Response Structure

All documented endpoints (except some Admin responses) return a **BaseResponse&lt;T&gt;** envelope.

**Success:**

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { ... },
  "errors": []
}
```

**Failure:**

```json
{
  "success": false,
  "message": "Error description",
  "data": null,
  "errors": ["Error detail 1", "Error detail 2"]
}
```

- **success**: boolean  
- **message**: string  
- **data**: payload or `null`  
- **errors**: array of strings (e.g. validation messages)

---

## Authentication

Base path: **`/api/auth`**

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/auth/signup` | POST | No | Register |
| `/api/auth/login` | POST | No | Login |
| `/api/auth/google` | POST | No | Google Sign-In |
| `/api/auth/verify-account` | GET | No | Verify email with code |
| `/api/auth/refresh-token` | POST | No | Refresh access token |
| `/api/auth/forgot-password` | POST | No | Request reset email |
| `/api/auth/reset-password` | POST | No | Reset password with token |
| `/api/auth/change-password` | POST | Yes | Change password (authenticated) |
| `/api/auth/resend-verification` | POST | No | Resend verification code |
| `/api/auth/logout` | POST | Yes | Logout and invalidate refresh token |
| `/api/auth/me` | GET | Yes | Current user (auth context) |
| `/api/auth/change-email-request` | POST | Yes | Request email change |
| `/api/auth/change-email-confirm` | POST | Yes | Confirm email change with code |
| `/api/auth/delete-account` | DELETE | Yes | Delete account (with password) |

All timestamps in examples use **ISO 8601** (e.g. `2026-02-22T10:00:00Z`). Example user data is consistent across auth examples.

---

### POST /api/auth/signup

**Request body (JSON):**

```json
{
  "firstName": "mohamed",
  "lastName": "hamed",
  "dateOfBirth": "2004-04-06",
  "gender": "Male",
  "email": "mh1191128@gmail.com",
  "phone": "01146784553",
  "password": "SecurePass123"
}
```

**Validation:**  
- `firstName`, `lastName`: required, max 50.  
- `email`: required, valid email.  
- `phone`: required, valid phone.  
- `password`: required, length 6–100.

**Success (200):**

```json
{
  "success": true,
  "message": "Registration successful. Please verify your email.",
  "data": {
    "user": {
      "id": 1,
      "fullName": "mohamed hamed",
      "email": "mh1191128@gmail.com",
      "phone": "01146784553",
      "isVerified": false,
      "roles": ["User"],
      "dateOfBirth": "2004-04-06",
      "gender": "Male",
      "profilePictureUrl": null,
      "createdAt": "2026-02-22T10:00:00Z",
      "updatedAt": null
    },
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
    "expiresAt": "2026-02-22T11:00:00Z"
  },
  "errors": []
}
```

**Failure (400):** e.g. email already exists; `success: false`, `message` and optional `errors`.

---

### POST /api/auth/login

**Request body (JSON):**

```json
{
  "email": "mh1191128@gmail.com",
  "password": "SecurePass123"
}
```

**Success (200):** Same `data` shape as signup (user, accessToken, refreshToken, expiresAt).  
**Failure (401):** Invalid credentials; `success: false`, `message`.

---

### POST /api/auth/google

**Request body (JSON):**

```json
{
  "idToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Success (200):** Same as login.  
**Failure (401):** Invalid or expired Google token.

---

### GET /api/auth/verify-account

**Query parameters:**  
- `code` (string): Verification code from email.  
- `email` (string): User email.

**Example:** `GET /api/auth/verify-account?code=123456&email=mh1191128@gmail.com`

**Success (200):** BaseResponse with success message.  
**Failure (400):** Invalid or expired code.

---

### POST /api/auth/refresh-token

**Request body (JSON):**

```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4..."
}
```

**Success (200):**

```json
{
  "success": true,
  "message": "Token refreshed successfully",
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
      "createdAt": "2026-02-22T10:00:00Z",
      "updatedAt": null
    },
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "new_refresh_token...",
    "expiresAt": "2026-02-22T11:00:00Z"
  },
  "errors": []
}
```

**Failure (401):** Invalid or expired refresh token.

---

### POST /api/auth/forgot-password

**Request body (JSON):**

```json
{
  "email": "mh1191128@gmail.com"
}
```

**Success (200):** BaseResponse with message that reset email was sent.  
**Failure (400):** e.g. user not found.

---

### POST /api/auth/reset-password

**Request body (JSON):**

```json
{
  "email": "mh1191128@gmail.com",
  "resetToken": "123456",
  "newPassword": "NewSecurePass123"
}
```

**Success (200):** BaseResponse.  
**Failure (400):** Invalid or expired reset token.

---

### POST /api/auth/change-password

**Auth:** Bearer token required.

**Request body (JSON):**

```json
{
  "currentPassword": "SecurePass123",
  "newPassword": "NewSecurePass123"
}
```

**Success (200):** BaseResponse.  
**Failure (400):** Wrong current password or validation error.  
**Failure (401):** Not authenticated.

---

### POST /api/auth/resend-verification

**Request body (JSON):**

```json
{
  "email": "mh1191128@gmail.com"
}
```

**Success (200):** BaseResponse.  
**Failure (400):** e.g. already verified or rate limit.

---

### POST /api/auth/logout

**Auth:** Bearer token required.

**Request body (JSON):**

```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4..."
}
```

**Success (200):** BaseResponse.  
**Failure (400/401):** Invalid token or not authenticated.

---

### GET /api/auth/me

**Auth:** Bearer token required.

**Success (200):** BaseResponse with **user** object (same shape as login/signup `data.user`, including `roles`).  
**Failure (401/404):** Not authenticated or user not found.

---

### POST /api/auth/change-email-request

**Auth:** Bearer required.

**Request body (JSON):**

```json
{
  "newEmail": "newemail@example.com"
}
```

**Success (200):** BaseResponse (verification sent to new email).  
**Failure (400):** e.g. email already in use.

---

### POST /api/auth/change-email-confirm

**Auth:** Bearer required.

**Request body (JSON):**

```json
{
  "newEmail": "newemail@example.com",
  "verificationCode": "123456"
}
```

**Success (200):** BaseResponse; sessions may be invalidated.  
**Failure (400):** Invalid or expired code.

---

### DELETE /api/auth/delete-account

**Auth:** Bearer required.

**Request body (JSON):** Typically password confirmation (check backend for exact DTO).

**Success (200):** BaseResponse.  
**Failure (400/401):** Wrong password or not authenticated.

---

## Reports

Base path: **`/api/reports`**

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/reports` | POST | Yes | Create report (multipart/form-data) |
| `/api/reports` | GET | No | List with filters and pagination |
| `/api/reports/{id}` | GET | No | Get one report |
| `/api/reports/my-reports` | GET | Yes | Current user's reports (paginated) |
| `/api/reports/nearby` | GET | No | Nearby for map (lat, lng, radius) |
| `/api/reports/{id}/interested` | POST | Yes | Express interest (notifies owner) |
| `/api/reports/{id}` | PUT | Yes | Update report (multipart/form-data) |
| `/api/reports/{id}/status` | PUT | Yes | Update status (Open/Matched/Closed) |
| `/api/reports/{id}` | DELETE | Yes | Delete report |

Report **types**: `LostItem`, `FoundItem`, `LostPerson`, `FoundPerson`.  
Report **statuses (legacy)**: `Open`, `Matched`, `Closed`.  
Report **lifecycle statuses (moderation)**: `Pending`, `Approved`, `Rejected`, `Matched`, `Closed`, `Archived`, `Flagged`.

---

### POST /api/reports

**Auth:** Bearer required.  
**Content-Type:** `multipart/form-data`.

**Form fields:**

| Field | Type | Required | Rules |
|-------|------|----------|--------|
| title | string | Yes | 3–200 chars |
| description | string | Yes | 10–2000 chars |
| type | string | Yes | One of: LostItem, FoundItem, LostPerson, FoundPerson |
| subCategoryId | int | Yes | From `/api/categories/mapping` |
| locationName | string | No | |
| latitude | double | No | -90 to 90 |
| longitude | double | No | -180 to 180 |
| dateReported | datetime | No | ISO 8601 |
| images | files | No | Multiple; see [Image / File Upload Rules](#image--file-upload-rules) |

**Success (200):**

```json
{
  "success": true,
  "message": "Report created successfully",
  "data": {
    "id": 1,
    "title": "Lost Brown Wallet",
    "description": "Brown leather wallet with ID and credit cards inside. Lost near Central Park.",
    "type": "LostItem",
    "status": "Open",
    "lifecycleStatus": "Pending",
    "locationName": "Central Park, New York",
    "latitude": 40.7829,
    "longitude": -73.9654,
    "matchPercentage": null,
    "subCategoryId": 1,
    "subCategoryName": "Wallets",
    "categoryName": "Item",
    "createdById": 1,
    "createdByName": "mohamed hamed",
    "createdByProfilePictureUrl": "/uploads/profiles/1/avatar.jpg",
    "dateReported": "2026-02-21T14:30:00Z",
    "createdAt": "2026-02-22T10:00:00Z",
    "updatedAt": null,
    "images": [
      {
        "id": 1,
        "imageUrl": "/uploads/reports/1/image1.jpg",
        "reportId": 1
      }
    ]
  },
  "errors": []
}
```

**Validation error (400):**

```json
{
  "success": false,
  "message": "Invalid report type 'mobile'. Valid types are: LostPerson, FoundPerson, LostItem, FoundItem",
  "data": null,
  "errors": []
}
```

---

### GET /api/reports/{id}

**Auth:** Not required.

**Success (200):** Same report object as create response.  

- Owners (the user with `createdById`) and Admins can access reports in **any** lifecycle status.  
- Anonymous users and non-owners can only see reports where `lifecycleStatus` is **Approved**, **Matched**, or **Closed**.  

**Failure (404):** Report not found or not visible to the current caller (e.g. Pending / Rejected / Flagged / Archived for non-owners).

---

### GET /api/reports

**Auth:** Not required.

**Query parameters:**

| Parameter | Type | Default | Description |
|------------|------|---------|-------------|
| type | string | - | LostItem, FoundItem, LostPerson, FoundPerson |
| status | string | - | Filter by lifecycle or legacy status. Accepts `Pending`, `Approved`, `Rejected`, `Matched`, `Closed`, `Archived`, `Flagged` or legacy `Open`, `Matched`, `Closed`. |
| search | string | - | Title, description, location |
| categoryId | int | - | Category ID |
| subCategoryId | int | - | Subcategory ID |
| dateFrom | datetime | - | ISO 8601 |
| dateTo | datetime | - | ISO 8601 |
| page | int | 1 | Page number |
| pageSize | int | 20 | 1–100 |

**Success (200):**

```json
{
  "success": true,
  "message": "Reports retrieved successfully",
  "data": {
    "data": [ { "id": 1, "title": "Lost Brown Wallet", "type": "LostItem", "status": "Open", "lifecycleStatus": "Approved", ... } ],
    "totalCount": 50,
    "page": 1,
    "pageSize": 20,
    "totalPages": 3
  },
  "errors": []
}
```

When no `status` filter is provided, this **public** endpoint only returns reports whose `lifecycleStatus` is **Approved**, **Matched**, or **Closed**. Reports in `Pending`, `Rejected`, `Flagged`, or `Archived` states are hidden from non-owner callers.

---

### GET /api/reports/my-reports

**Auth:** Bearer required.

**Query parameters:** `page` (default 1), `pageSize` (default 20).

**Success (200):** Same paginated shape as GET `/api/reports` (data array, totalCount, page, pageSize, totalPages).

---

### GET /api/reports/nearby

**Auth:** Not required. For map view.

**Query parameters:**

| Parameter | Type | Default | Description |
|------------|------|---------|-------------|
| lat | double | - | Latitude (-90 to 90) |
| lng | double | - | Longitude (-180 to 180) |
| radius | double | 10 | Radius in km |
| type | string | - | "Lost", "Found", or "All" |
| page | int | 1 | |
| pageSize | int | 20 | |

**Success (200):** `data` is an **array** (no totalCount in response):

```json
{
  "success": true,
  "message": "Nearby reports retrieved successfully",
  "data": [
    {
      "id": 1,
      "title": "Lost Brown Wallet",
      "type": "Lost",
      "category": "Item",
      "subCategory": "Wallets",
      "lat": 40.7829,
      "lng": -73.9654,
      "imageUrl": "/uploads/reports/1/image1.jpg"
    }
  ],
  "errors": []
}
```

**Failure (400):** Invalid coordinates.

---

### POST /api/reports/{id}/interested

**Auth:** Bearer required. Notifies report owner.

**Success (200):** BaseResponse with message; optional `data.reportId`.  
**Failure (404):** Report not found.

---

### PUT /api/reports/{id}

**Auth:** Bearer required (owner or admin).  
**Content-Type:** `multipart/form-data`.

**Form fields:** Same as create; all optional. Plus:  
- `imageIdsToRemove`: array of image IDs to delete.  
- `newImages`: new files (same rules as report images).

**Success (200):** Full report object.  
**Failure (403/404):** Not owner or not found.

---

### PUT /api/reports/{id}/status

**Auth:** Bearer required (owner or admin).

**Request body (JSON):**

```json
{
  "status": "Matched"
}
```

**Valid status:** `Open`, `Matched`, `Closed`.

**Success (200):** Updated report object.  
**Failure (400/404):** Invalid status or report not found.

---

### DELETE /api/reports/{id}

**Auth:** Bearer required (owner or admin).

**Success (200):** BaseResponse.  
**Failure (403/404):** Not owner or not found.

---

## Categories

Base path: **`/api/categories`**

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/categories` | GET | Yes | Flat list of categories |
| `/api/categories/tree` | GET | Yes | Tree with subcategories |
| `/api/categories/mapping` | GET | No | Flat mapping for dropdowns |
| `/api/categories/{id}` | GET | Yes | Category by ID |
| `/api/categories/{id}/reports` | GET | Yes | Reports in category |
| POST/PUT/DELETE `/api/categories/...` | - | Admin | Create/update/delete category |

---

### GET /api/categories/mapping

**Auth:** Not required.

**Success (200):**

```json
{
  "success": true,
  "message": "Category mapping retrieved successfully",
  "data": [
    { "category": "Item", "subCategory": "Wallets", "subCategoryId": 1 },
    { "category": "Item", "subCategory": "Bags", "subCategoryId": 2 },
    { "category": "People", "subCategory": "Child", "subCategoryId": 11 }
  ],
  "errors": []
}
```

---

### GET /api/categories/tree

**Auth:** Bearer required.

**Success (200):**

```json
{
  "success": true,
  "message": "Category tree retrieved successfully",
  "data": [
    {
      "id": 1,
      "name": "Item",
      "description": "Lost or found items/objects",
      "subCategories": [
        { "id": 1, "name": "Wallets", "description": "...", "categoryId": 1, "categoryName": "Item", "reportCount": 15 }
      ],
      "createdAt": "2026-02-22T10:00:00Z",
      "updatedAt": null
    }
  ],
  "errors": []
}
```

---

### GET /api/categories and GET /api/categories/{id}

**Auth:** Bearer required.  
**GET /api/categories:** Returns flat list of categories (id, name, description, subCategoryCount, createdAt, updatedAt).  
**GET /api/categories/{id}:** Same shape for one category.  
**GET /api/categories/{id}/reports:** Reports in that category (filter by category logic; check backend for exact payload).

---

## SubCategories

Base path: **`/api/subcategories`**

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/subcategories` | GET | Yes | All subcategories |
| `/api/subcategories/category/{categoryId}` | GET | Yes | By category |
| `/api/subcategories/{id}` | GET | Yes | By ID |
| `/api/subcategories/{id}/reports` | GET | Yes | Reports in subcategory |
| POST/PUT/DELETE | - | Admin | Create/update/delete subcategory |

**Auth:** All GETs require Bearer except where noted. Responses use BaseResponse with `data` as list or single DTO (id, name, description, categoryId, categoryName, reportCount, createdAt, updatedAt for subcategories).

---

## Notifications

Base path: **`/api/notifications`**

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/notifications/vapid-public-key` | GET | No | VAPID key for web push |
| `/api/notifications` | GET | Yes | List with filters and pagination |
| `/api/notifications/unread` | GET | Yes | Unread count |
| `/api/notifications/{id}/read` | PUT | Yes | Mark as read |
| `/api/notifications/mark-all-read` | POST | Yes | Mark all read |
| `/api/notifications/{id}` | DELETE | Yes | Delete one |
| `/api/notifications/register-device` | POST | Yes | Register FCM/APNs token |

---

### GET /api/notifications/vapid-public-key

**Auth:** Not required.

**Success (200):**

```json
{
  "success": true,
  "message": "VAPID public key",
  "data": {
    "vapidPublicKey": "BEl62iUYgUivxIkv69yViEuiBIa-Ib27..."
  },
  "errors": []
}
```

---

### GET /api/notifications

**Auth:** Bearer required.

**Query parameters:**

| Parameter | Type | Default | Description |
|------------|------|---------|-------------|
| page | int | 1 | |
| pageSize | int | 20 | |
| type | string | all | Read status: all, unread, read |
| category | string | all | Category: all, general, matches |

**Success (200):**

```json
{
  "success": true,
  "message": "Notifications retrieved successfully",
  "data": {
    "notifications": [
      {
        "id": 1,
        "userId": 1,
        "title": "Item Match Found!",
        "message": "A matching report was found with 85% similarity",
        "notificationType": "match",
        "actorName": "Jane Smith",
        "actorProfilePictureUrl": "/uploads/profiles/2/avatar.jpg",
        "relatedReportId": 5,
        "isRead": false,
        "createdAt": "2026-02-22T10:00:00Z",
        "updatedAt": null
      }
    ],
    "totalCount": 5,
    "page": 1,
    "pageSize": 20,
    "totalPages": 1
  },
  "errors": []
}
```

---

### GET /api/notifications/unread

**Success (200):** `data`: `{ "unreadCount": 3 }`.

---

### PUT /api/notifications/{id}/read

**Success (200):** BaseResponse; `data` may be null.

---

### POST /api/notifications/mark-all-read

**Success (200):** `data`: `{ "updatedCount": 3 }`.

---

### DELETE /api/notifications/{id}

**Success (200):** BaseResponse; `data` null.

---

### POST /api/notifications/register-device

**Auth:** Bearer required.

**Request body (JSON):**

```json
{
  "token": "fcm_or_apns_device_token_string",
  "platform": "android"
}
```

**Validation:** `platform` must be one of: `android`, `ios`, `web`.

**Success (200):** BaseResponse with message; `data` null.  
**Failure (400):** Validation error (e.g. invalid platform).

---

## Profile / Users

Base path: **`/api/users`**

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/users/me` | GET | Yes | Current user profile (no roles) |
| `/api/users/me` | PUT | Yes | Update profile (multipart/form-data) |
| `/api/users/me/profile-picture` | POST | Yes | Upload profile picture only |
| `/api/users/{id}` | GET | Yes | User by ID (self or Admin) |
| `/api/users/{id}/reports` | GET | Yes | Reports by user (paginated) |
| GET /api/users | GET | Admin | All users (paginated) |
| POST /api/users/admin, PUT /api/users/{id}/verify | - | Admin | Create user, verify user |

---

### GET /api/users/me

**Auth:** Bearer required.  
Returns **profile** (SafeUserDto); no `roles` field. Use **GET /api/auth/me** for auth context including roles.

**Success (200):**

```json
{
  "success": true,
  "message": "User retrieved successfully",
  "data": {
    "id": 1,
    "fullName": "mohamed hamed",
    "email": "mh1191128@gmail.com",
    "phone": "01146784553",
    "isVerified": false,
    "dateOfBirth": "2004-04-06",
    "gender": "Male",
    "profilePictureUrl": null,
    "createdAt": "2026-02-22T10:00:00Z",
    "updatedAt": null
  },
  "errors": []
}
```

---

### PUT /api/users/me

**Auth:** Bearer required.  
**Content-Type:** `multipart/form-data`.

**Form fields:** fullName, phone, dateOfBirth, gender, profilePicture (optional). Profile picture: max **5MB**; see [Image / File Upload Rules](#image--file-upload-rules).

**Success (200):** Same shape as GET /api/users/me.

---

### POST /api/users/me/profile-picture

**Auth:** Bearer required.  
**Content-Type:** `multipart/form-data`. Single file (e.g. `profilePicture`). Max 5MB.

**Success (200):** Same profile object with updated `profilePictureUrl`.

---

### GET /api/users/{id}/reports

**Auth:** Bearer required.

**Query parameters:** `page` (default 1), `pageSize` (default 20, max 100).

**Success (200):**

```json
{
  "success": true,
  "message": "User reports retrieved successfully",
  "data": {
    "reports": [ { "id": 1, "title": "...", ... } ],
    "totalCount": 5,
    "page": 1,
    "pageSize": 20,
    "totalPages": 1
  },
  "errors": []
}
```

---

## Chat

Base path: **`/api/chat`**

**Auth:** All endpoints require Bearer.

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/chat/sessions` | GET | List sessions (with last message and unread) |
| `/api/chat/sessions/{otherUserId}` | POST | Open or create session |
| `/api/chat/sessions/{sessionId}` | GET | Session details |
| `/api/chat/sessions/{sessionId}/messages` | GET | Messages in session |
| `/api/chat/sessions/{sessionId}/messages` | POST | Send message |
| `/api/chat/messages/{messageId}/read` | PUT | Mark message read |

---

### GET /api/chat/sessions

**Success (200):**

```json
{
  "success": true,
  "message": "Chat sessions retrieved successfully.",
  "data": [
    {
      "id": 1,
      "otherUser": {
        "id": 2,
        "fullName": "Jane Smith",
        "email": "jane@example.com",
        "phone": null,
        "isVerified": true,
        "roles": [],
        "dateOfBirth": null,
        "gender": null,
        "profilePictureUrl": "/uploads/profiles/2/avatar.jpg",
        "createdAt": "2026-02-22T09:00:00Z",
        "updatedAt": null
      },
      "lastMessage": {
        "id": 10,
        "chatSessionId": 1,
        "senderId": 2,
        "receiverId": 1,
        "text": "Hi, I found your wallet!",
        "sentAt": "2026-02-22T10:00:00Z",
        "isRead": false,
        "createdAt": "2026-02-22T10:00:00Z",
        "updatedAt": null
      },
      "createdAt": "2026-02-22T09:00:00Z",
      "lastMessageTime": "2026-02-22T10:00:00Z",
      "hasUnreadMessages": true
    }
  ],
  "errors": []
}
```

---

### POST /api/chat/sessions/{otherUserId}

**Success (200):** `data` is a single **ChatSessionDetailsDto** (id, user1Id, user2Id, user1, user2, createdAt, lastMessageTime).

---

### GET /api/chat/sessions/{sessionId}/messages

**Success (200):** `data` is an **array** of **ChatMessageDto** (id, chatSessionId, senderId, receiverId, sender, receiver, text, sentAt, isRead, createdAt, updatedAt).

---

### POST /api/chat/sessions/{sessionId}/messages

**Request body (JSON):**

```json
{
  "text": "Thank you so much! Where can I pick it up?"
}
```

**Success (200):** `data` is a **single** sent message object (ChatMessageDto), not an array:

```json
{
  "success": true,
  "message": "Message sent successfully.",
  "data": {
    "id": 2,
    "chatSessionId": 1,
    "senderId": 1,
    "receiverId": 2,
    "sender": { "id": 1, "fullName": "mohamed hamed", ... },
    "receiver": { "id": 2, "fullName": "Jane Smith", ... },
    "text": "Thank you so much! Where can I pick it up?",
    "sentAt": "2026-02-22T10:05:00Z",
    "isRead": false,
    "createdAt": "2026-02-22T10:05:00Z",
    "updatedAt": null
  },
  "errors": []
}
```

---

### PUT /api/chat/messages/{messageId}/read

**Success (200):** `data` is the updated ChatMessageDto (isRead: true).

---

## Matching

Base path: **`/api/matching`**

**Auth:** Bearer required.

> **Note:** The backend also runs matching automatically in the background after a report is created.  
> Use these endpoints when you want to manually re-run matching for a specific report or implement custom flows.

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/matching/run/{reportId}` | POST | Run matching algorithm |
| `/api/matching/{reportId}` | GET | Get existing matches |

---

### POST /api/matching/run/{reportId}

**Success (200):** Payload is an **object** with `reportId`, `matchesFound`, and `matches` array:

```json
{
  "success": true,
  "message": "Matching completed successfully",
  "data": {
    "reportId": 1,
    "matchesFound": 2,
    "matches": [
      {
        "id": 1,
        "reportId": 1,
        "matchedReportId": 5,
        "matchedReportTitle": "Found Brown Wallet",
        "similarityScore": 85.5,
        "createdAt": "2026-02-22T10:00:00Z"
      }
    ]
  },
  "errors": []
}
```

**Failure (404):** Report not found.

---

### GET /api/matching/{reportId}

**Success (200):**

```json
{
  "success": true,
  "message": "Matches retrieved successfully",
  "data": {
    "reportId": 1,
    "totalMatches": 2,
    "matches": [
      {
        "id": 1,
        "reportId": 1,
        "matchedReportId": 5,
        "matchedReportTitle": "Found Brown Wallet",
        "similarityScore": 85.5,
        "createdAt": "2026-02-22T10:00:00Z"
      }
    ]
  },
  "errors": []
}
```

---

## Dashboard / Home

### GET /api/home/dashboard

**Auth:** Not required. If authenticated, `myReportsCount` is included.

**Success (200):**

```json
{
  "success": true,
  "message": "Dashboard retrieved successfully",
  "data": {
    "recentReports": [ { "id": 1, "title": "Lost Brown Wallet", "type": "LostItem", ... } ],
    "totalReportsCount": 150,
    "categoriesCount": 2,
    "myReportsCount": 5
  },
  "errors": []
}
```

`recentReports` uses the same report DTO shape as GET report by id and follows the same public visibility rules: only reports with `lifecycleStatus` **Approved**, **Matched**, or **Closed** are included. When not logged in, `myReportsCount` may be omitted or null.

---

## Admin

Base path: **`/api/admin`**

**Auth:** Bearer with **Admin** role required.

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/admin/reports` | GET | All reports (filter + pagination) |
| `/api/admin/reports/{id}/approve` | PUT | Approve/change report status |
| `/api/admin/reports/{id}` | DELETE | Delete any report |
| `/api/admin/users/{id}/verify` | PUT | Verify user |

**Note:** Admin endpoints may **not** use the standard BaseResponse envelope (e.g. GET reports returns `{ data, totalCount, page, pageSize, totalPages }`; PUT approve returns report object; DELETE returns 204 No Content; verify returns `{ message }`). Mobile app typically does not use these unless building an admin panel.

---

## Reference Tables

### Report Types

| Value | Description |
|-------|-------------|
| LostItem | Lost item |
| FoundItem | Found item |
| LostPerson | Missing person |
| FoundPerson | Found person |

### Report Statuses

| Value | Description |
|-------|-------------|
| Open | Active |
| Matched | Potential match found |
| Closed | Resolved/closed |

### Report Lifecycle Statuses

| Value | Description |
|-------|-------------|
| Pending | Newly created, awaiting moderation/approval |
| Approved | Approved and visible in public listings |
| Rejected | Rejected by admin; hidden from public |
| Matched | A high-confidence match was found |
| Closed | Resolved/closed and no longer active |
| Archived | Archived for historical purposes |
| Flagged | Flagged for review (e.g. abuse reports) |

### Notification Types (notificationType / category)

| Type | Typical use |
|------|-------------|
| match | Match found for a report |
| interested | Someone expressed interest |
| new_message | New chat message |
| status_update | Report status changed |
| location_alert | Nearby report alert |

Filter **category**: `all` | `general` (non-match) | `matches` (type = match).

---

## Image / File Upload Rules

- **Allowed formats:** `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`.  
- **Profile picture (PUT /api/users/me, POST /api/users/me/profile-picture):** Max **5MB** per file.  
- **Report images (POST/PUT /api/reports):** Max **10MB** per image; multiple images supported (no fixed limit in backend; recommend a reasonable cap e.g. 10 for UX).  
- **Example URLs:** Relative, e.g. `/uploads/profiles/1/avatar.jpg`, `/uploads/reports/1/image1.jpg`. Base URL is the API host.

---

## Error Codes and Conventions

- **200:** Success.  
- **400:** Bad request (validation, invalid type/status, etc.).  
- **401:** Unauthorized (missing or invalid token).  
- **403:** Forbidden (e.g. not owner, or not Admin where required).  
- **404:** Not found (report, user, notification, etc.).  
- **500:** Server error; `message` and optionally `errors` in BaseResponse.

Validation errors: `success: false`, `message` summary, `errors` array of field/message strings when returned.

---

