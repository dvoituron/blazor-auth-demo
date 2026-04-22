# How to get the AppSettings,json values

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",

  "Authentication": {
    "GitHub": {
      "ClientId": "<GITHUB_CLIENTID>",
      "ClientSecret": "<GITHUB_CLIENT_SECRET>"
    }
  },

  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "<AAD_DOMAIN>",
    "TenantId": "<AAD_TENANTID>",
    "ClientId": "<AAD_CLIENTID>",
    "ClientSecret": "<AAD_CLIENT_SECRET>",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

## 1. GitHub — `Authentication:GitHub`

GitHub uses a classic **OAuth App** (no tenant, no domain — just a client id/secret pair).

### Steps

1. Open **<https://github.com/settings/developers>** → tab **OAuth Apps** → **New OAuth App**.
   (For an org: *Organization → Settings → Developer settings → OAuth Apps*.)
2. Fill the form:

   | Field | Value |
   |---|---|
   | **Application name** | BlazorAuthDemo (free text) |
   | **Homepage URL** | `https://localhost:5001` (check your `Properties/launchSettings.json` for the real port) |
   | **Authorization callback URL** | `https://localhost:5001/signin-github` ← **must match** `CallbackPath` in Program.cs |

3. Click **Register application**. You now land on the app page.
4. Copy the **Client ID** shown at the top → goes into `Authentication:GitHub:ClientId`.
5. Click **Generate a new client secret** → copy it **immediately** (shown only once) → goes into `Authentication:GitHub:ClientSecret`.

### Store the secrets (don't commit them!)

```powershell
cd BlazorAuthDemo
dotnet user-secrets init
dotnet user-secrets set "Authentication:GitHub:ClientId"     "Iv1.abcdef1234567890"
dotnet user-secrets set "Authentication:GitHub:ClientSecret" "ghp_xxxxxxxxxxxxxxxxxxxxxxxx"
```

User-secrets are stored outside the project (in `%APPDATA%\Microsoft\UserSecrets\`) and override appsettings.json at runtime.

### For production

Add a **second callback URL** pointing to your public domain, e.g. `https://myapp.contoso.com/signin-github`, either in the same OAuth App or — better — in a separate one per environment.

---

## 2. Microsoft Entra ID — `AzureAd`

Entra ID (the new name for Azure AD) uses an **App registration**.

### Steps

1. Go to the **Azure portal → Microsoft Entra ID → App registrations → + New registration**.
2. Fill the form:

   | Field | Value |
   |---|---|
   | **Name** | BlazorAuthDemo |
   | **Supported account types** | *Accounts in this organizational directory only* (single tenant) is the simplest |
   | **Redirect URI** | Platform **Web** → `https://localhost:5001/signin-oidc` |

3. Click **Register**. The **Overview** page now shows most values you need.

### Where each value comes from

| appsettings.json key | Where to find it |
|---|---|
| `AzureAd:Instance` | Always `https://login.microsoftonline.com/` (leave as is) |
| `AzureAd:TenantId` | Overview page → **Directory (tenant) ID** |
| `AzureAd:ClientId` | Overview page → **Application (client) ID** |
| `AzureAd:Domain` | Overview → **Publisher domain** (or *Entra ID → Overview → Primary domain*, e.g. `contoso.onmicrosoft.com`) |
| `AzureAd:ClientSecret` | **Certificates & secrets → Client secrets → + New client secret** → copy the **Value** (not the Secret ID!) immediately |
| `AzureAd:CallbackPath` | `/signin-oidc` (default — matches the redirect URI you registered) |
| `AzureAd:SignedOutCallbackPath` | `/signout-callback-oidc` (default) |

### Configure the redirect / logout URLs

In the app registration:

1. **Authentication** blade → **+ Add a platform → Web**.
2. **Redirect URIs**: add `https://localhost:5001/signin-oidc` (and your prod URL).
3. **Front-channel logout URL**: `https://localhost:5001/signout-callback-oidc`.
4. Under **Implicit grant and hybrid flows**, leave both boxes **unchecked** — the Microsoft.Identity.Web library uses the authorization-code flow with PKCE.

### Create the client secret

1. **Certificates & secrets → + New client secret**.
2. Give it a description + expiration (max 24 months).
3. **Copy the `Value` column immediately** — it becomes unreadable after you navigate away.

### Store the secrets

```powershell
dotnet user-secrets set "AzureAd:TenantId"     "11111111-2222-3333-4444-555555555555"
dotnet user-secrets set "AzureAd:ClientId"     "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"
dotnet user-secrets set "AzureAd:ClientSecret" "<the 'Value' column from Azure>"
dotnet user-secrets set "AzureAd:Domain"       "contoso.onmicrosoft.com"
```

---

## 3. Quick verification

After `dotnet run`:

- **GitHub**: `/login/github` should redirect to `github.com/login/oauth/authorize?...`. If you see *"The redirect_uri MUST match the registered callback URL"*, your `CallbackPath` or port doesn't match.
- **Entra ID**: `/login/aad` should redirect to `login.microsoftonline.com/<tenant>/oauth2/v2.0/authorize?...`. Error `AADSTS50011` means the redirect URI in the app registration is wrong.

---

## 4. Security reminders

- **Never commit** client secrets. Use `dotnet user-secrets` in development and **Azure Key Vault** / **GitHub Actions secrets** / **environment variables** in production.
- Treat the GitHub client secret and AAD client secret like passwords — rotate them periodically (especially when a team member leaves).
- In production use **HTTPS only** and register a proper domain (not `localhost`).
- For AAD, prefer **certificate credentials** or **Managed Identity / Workload Identity Federation** over client secrets when the app runs in Azure.