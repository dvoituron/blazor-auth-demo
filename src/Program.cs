using AspNet.Security.OAuth.GitHub;
using BlazorAuthDemo.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Razor Components with Static Server rendering (no WebAssembly, no interactive server).
builder.Services.AddRazorComponents();

// Expose AuthenticationState as a cascading value to all components.
builder.Services.AddCascadingAuthenticationState();

// The cookie scheme is the primary scheme: once a user signs in with GitHub
// or AAD, the resulting identity is persisted in the application cookie.
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
    })
    .AddGitHub(GitHubAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"] ?? string.Empty;
        options.CallbackPath = "/signin-github";
        options.SaveTokens = true;
        options.Scope.Add("read:user");
        options.Scope.Add("user:email");
    })
    .AddMicrosoftIdentityWebApp(
        builder.Configuration.GetSection("AzureAd"),
        openIdConnectScheme: OpenIdConnectDefaults.AuthenticationScheme,
        cookieScheme: null);

// Force the pure authorization-code flow (PKCE) so the app registration
// does NOT need "ID tokens" enabled under Implicit grant and hybrid flows.
// Fixes AADSTS700054 ("response_type 'id_token' is not enabled").
builder.Services.Configure<OpenIdConnectOptions>(
    OpenIdConnectDefaults.AuthenticationScheme,
    options =>
    {
        options.ResponseType = OpenIdConnectResponseType.Code;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>();

// --- Authentication endpoints ---------------------------------------------
// Each endpoint triggers an external challenge. When the provider returns,
// the cookie handler creates the application cookie and redirects the user
// back to 'returnUrl'.
app.MapGet("/login/github", (string? returnUrl) =>
{
    var redirectUri = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
    var properties = new AuthenticationProperties { RedirectUri = redirectUri };
    return Results.Challenge(properties, [GitHubAuthenticationDefaults.AuthenticationScheme]);
});

app.MapGet("/login/aad", (string? returnUrl) =>
{
    var redirectUri = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
    var properties = new AuthenticationProperties { RedirectUri = redirectUri };
    return Results.Challenge(properties, [OpenIdConnectDefaults.AuthenticationScheme]);
});

app.MapPost("/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
}).RequireAuthorization();

app.Run();
