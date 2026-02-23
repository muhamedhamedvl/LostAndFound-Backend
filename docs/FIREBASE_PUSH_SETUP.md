# LostAndFound – Firebase Push Notifications Setup

## 1. Firebase Admin SDK

- The backend uses **Firebase Admin SDK** (.NET: `FirebaseAdmin` package) to send push notifications via FCM.
- Credentials are loaded from **configuration** (no hardcoded keys). Prefer **User Secrets** for local dev.

## 2. Storing credentials securely

**Do not** put `PrivateKey` or other secrets in `appsettings.json` in source control.

### Option A: User Secrets (recommended for local dev)

From the **API project** directory (`LostAndFound.Api`):

```bash
dotnet user-secrets set "Firebase:ProjectId" "lostandfound-d10e6"
dotnet user-secrets set "Firebase:ClientEmail" "firebase-adminsdk-fbsvc@lostandfound-d10e6.iam.gserviceaccount.com"
dotnet user-secrets set "Firebase:PrivateKey" "-----BEGIN PRIVATE KEY-----\nMIIEvwIBADANBgkqhkiG9w0BAQEFAASCBKkwggSl...\n-----END PRIVATE KEY-----\n"
dotnet user-secrets set "Firebase:VapidKey" "BHT5ZNhHTSookq2-hJ-WqRiJr6HVkifAsECqROvMvZMhm3idoTFyXWzJyFxDe_K2Rtn5V9nGLDdUxUI0hYGsKH8"
```

For `PrivateKey`, use `\n` for newlines in the single string (or paste the key with real newlines; both work).

### Option B: appsettings.Development.json (not in source control)

Add a `Firebase` section with real values only in a file that is gitignored or on the server only.

### Option C: Environment variables / Azure Key Vault (production)

Map configuration so that `Firebase:ProjectId`, `Firebase:ClientEmail`, `Firebase:PrivateKey`, and `Firebase:VapidKey` are provided by your hosting environment or key vault.

## 3. Backend behaviour

- On startup, the API binds the `Firebase` section and checks that `ProjectId`, `ClientEmail`, and `PrivateKey` are set.
- If valid, it creates a **Google credential** from these (no key in code), initializes **FirebaseApp** once, and registers **FirebasePushNotificationService**.
- If not configured or init fails, it falls back to **PushNotificationServiceStub** (no-op) so the app still runs.

## 4. Web Push (VAPID)

- **VAPID public key** is exposed at: **GET /api/notifications/vapid-public-key** (no auth).
- Use this value in the frontend when creating the push subscription (e.g. with the Web Push API or Firebase JS SDK).
- **Do not** expose the VAPID *private* key to the frontend; only the public key is returned by the API.

## 5. Device token registration

- Clients (Web, Android, iOS) obtain an **FCM device token** and send it to:
  **POST /api/notifications/register-device** (auth required).

- Body example:

```json
{
  "token": "<FCM device token>",
  "platform": "android"
}
```

- **platform** must be one of: `android`, `ios`, `web`.

## 6. Sending notifications

- When the backend sends a notification (e.g. match, report update, new message), it resolves the user’s registered device tokens and sends an FCM message to each.
- Notifications are sent for both **foreground and background**; the payload includes `Notification` (title/body) and optional `Data` for custom handling.

## 7. Testing

1. **Backend**: Ensure User Secrets (or env) are set, run the API, and check logs for: `Firebase push notifications enabled.`
2. **Web**: Call **GET /api/notifications/vapid-public-key**, use the key in your web app to subscribe and get an FCM token, then register that token with **POST /api/notifications/register-device** with `"platform": "web"`.
3. **Android/iOS**: Get the FCM token from the Firebase client SDK, then register it with **POST /api/notifications/register-device** with `"platform": "android"` or `"platform": "ios"`.
4. Trigger a flow that sends a notification (e.g. match or new message) and confirm the device receives the push.

## 8. Security

- Keep **PrivateKey** and **VapidKey** (if it’s the full key pair) only on the server.
- Expose only the **VAPID public key** to the frontend.
- Use HTTPS in production.
