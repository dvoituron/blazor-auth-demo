# BlazorAuthDemo — Blazor Static Web App with GitHub + AAD login (.NET 10)

A minimal sample that shows how to secure a page in a **Blazor Web App**
(Static Server render mode) running on **.NET 10**, offering the user a
choice between **GitHub** and **Microsoft Entra ID (AAD)** as identity
providers.

## What it demonstrates

- Blazor Web App with Static Server rendering (no WebAssembly, no interactive server).
- Cookie authentication as the primary scheme.
- External sign-in with **GitHub** (`AspNet.Security.OAuth.GitHub`).
- External sign-in with **AAD / Entra ID** (`Microsoft.Identity.Web`).
- A `/login` page letting the user pick a provider.
- A protected `/secure` page using `[Authorize]` + `AuthorizeRouteView`.
- A sign-out endpoint protected against CSRF via `AntiforgeryToken`.

## Project structure

```
BlazorAuthDemo/
├── Program.cs                       # Auth pipeline + login/logout endpoints
├── appsettings.json                 # GitHub + AzureAd configuration
└── Components/
    ├── App.razor
    ├── Routes.razor                 # AuthorizeRouteView + RedirectToLogin
    ├── _Imports.razor
    ├── LoginDisplay.razor           # Nav widget: user name + sign out
    ├── RedirectToLogin.razor        # Redirects unauthorized users
    ├── Layout/
    │   └── MainLayout.razor
    └── Pages/
        ├── Home.razor
        ├── LoginPage.razor          # Choose GitHub or AAD
        └── SecurePage.razor         # [Authorize]-protected page
```

## Configure the providers

### GitHub OAuth App

1. Go to <https://github.com/settings/developers> → **New OAuth App**.
2. **Homepage URL**: `https://localhost:5001` (use the port shown in
   `Properties/launchSettings.json`).
3. **Authorization callback URL**: `https://localhost:5001/signin-github`.
4. Copy the **Client ID** and generate a **Client Secret**.
5. Store the secrets with `dotnet user-secrets` (recommended):

    ```powershell
    dotnet user-secrets init
    dotnet user-secrets set "Authentication:GitHub:ClientId"     "<client-id>"
    dotnet user-secrets set "Authentication:GitHub:ClientSecret" "<client-secret>"
    ```

### Microsoft Entra ID (AAD)

1. In the Azure portal open **Microsoft Entra ID → App registrations → New registration**.
2. **Redirect URI (Web)**: `https://localhost:5001/signin-oidc`.
3. **Front-channel logout URL**: `https://localhost:5001/signout-callback-oidc`.
4. Under **Certificates & secrets**, create a **Client secret**.
5. Store the values with user-secrets:

    ```powershell
    dotnet user-secrets set "AzureAd:TenantId"     "<tenant-id>"
    dotnet user-secrets set "AzureAd:ClientId"     "<client-id>"
    dotnet user-secrets set "AzureAd:ClientSecret" "<client-secret>"
    dotnet user-secrets set "AzureAd:Domain"       "<tenant>.onmicrosoft.com"
    ```

## Run

```powershell
dotnet run
```

Open the app, navigate to `/secure`, and you will be redirected to
`/login` where you can choose GitHub or Microsoft. After signing in, the
`/secure` page displays the current user and their claims.
