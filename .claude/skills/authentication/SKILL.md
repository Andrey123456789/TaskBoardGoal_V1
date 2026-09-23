---
name: authentication
description: >
  Authentication and authorization guidance for ASP.NET Core Controller APIs.
  Covers ASP.NET Core authentication schemes, JWT bearer/OIDC, ASP.NET Core
  Identity, cookies, policies, claims, roles, resource authorization,
  current-user access, password security, and 401/403 behavior.
  Use when implementing login, identity, endpoint protection, permissions,
  roles, claims, JWT, OIDC, cookies, or authorization.
---

# Authentication and Authorization

## Core Principles

1. Authentication determines who the caller is.
2. Authorization determines what an authenticated caller may do.
3. Use established ASP.NET Core authentication mechanisms rather than custom
   token/password security code.
4. Authorization must be enforced server-side.
5. Prefer policies when authorization logic is more than a trivial role check.
6. Keep ASP.NET Core transport/authentication types out of Domain.
7. Follow the `http-api` skill for `401`, `403`, and deliberate `404` semantics.

## Choosing Authentication

Choose the scheme according to the actual application.

Common options include:

- Bearer/OIDC for an SPA calling an API;
- cookies for server-rendered interactive web applications;
- ASP.NET Core Identity when the application owns local users/passwords;
- an external OpenID Connect/OAuth provider when identity is delegated.

Do not add ASP.NET Core Identity if the application does not own user management.

Do not issue custom JWTs merely because the API needs authentication if an
existing identity provider already owns token issuance.

## Local User Accounts

When the application owns passwords, use established password-management
facilities such as ASP.NET Core Identity.

Do not:

- store plaintext passwords;
- implement custom password hashing;
- invent password reset token cryptography;
- store authentication secrets directly in application source.

Use the framework's password hashing, lockout, reset-token, and related security
features where applicable.

## Bearer Authentication

A Controller API used by an Angular SPA commonly authenticates access tokens
through Bearer authentication.

Configure token validation according to the issuer that actually issues tokens.

Validate relevant properties such as:

- issuer;
- audience;
- signature;
- lifetime.

Do not disable validation merely to make a token work.

Secrets, signing keys, certificates, and credentials must follow the project's
configuration/security rules.

## Authorization

Use `[Authorize]` or project-wide authorization policy as appropriate.

```csharp
[Authorize]
[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
}
```

A secure global/fallback policy may make repeating `[Authorize]` unnecessary.

Anonymous access should be deliberate:

```csharp
[AllowAnonymous]
```

when the project otherwise defaults to authenticated access.

## Policy-Based Authorization

Prefer policies for meaningful capabilities.

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(
        "CanManageOrders",
        policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim("permission", "orders:write"));
```

```csharp
[Authorize(Policy = "CanManageOrders")]
[HttpPost]
public async Task<ActionResult<OrderResponse>> Create(...)
{
    ...
}
```

Use stable constants for policy names when repeated throughout the application.

## Roles

Roles are appropriate when the business authorization model is genuinely
role-oriented.

Avoid scattering compound role strings everywhere.

Prefer a policy when several roles or claims represent one business capability.

## Resource-Based Authorization

Some permissions depend on the actual resource.

Examples:

- user may edit only their own record;
- project member may update only projects they belong to;
- administrators may access any record.

Use ASP.NET Core resource authorization or explicit Application authorization
logic where appropriate.

Do not rely only on Angular route guards or hidden buttons.

Client-side checks are UX; server-side checks are security.

## 401 vs 403

Follow standard HTTP semantics:

```text
not authenticated / authentication failed
→ 401 Unauthorized

authenticated successfully
but not permitted
→ 403 Forbidden
```

Authentication/authorization middleware should normally generate these responses
rather than controllers manually manufacturing them.

A security-sensitive API may deliberately return `404` for some forbidden
resource lookups to avoid disclosing existence. Make that a deliberate,
consistent policy.

Detailed HTTP status semantics belong to the `http-api` skill.

## Current User

Controllers may use `User` when information is purely HTTP/API-specific.

```csharp
[Authorize]
[HttpGet("me")]
public ActionResult<CurrentUserResponse> GetCurrentUser()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    return Ok(new CurrentUserResponse(userId));
}
```

When Application use cases repeatedly require caller identity, define a small
Application abstraction such as:

```csharp
public interface ICurrentUser
{
    string? UserId { get; }
    bool IsAuthenticated { get; }
}
```

Implement the HTTP-specific adapter at an outer boundary.

Do not make Application services depend directly on `HttpContext`,
`ClaimsPrincipal`, or `IHttpContextAccessor`.

Do not create `ICurrentUser` if passing an explicit user identifier is simpler.

## Cookie Authentication and CSRF

When authentication credentials are sent automatically by the browser through
cookies, consider CSRF protection as part of the design.

Do not assume an Angular frontend makes cookie-based API authentication immune
to CSRF.

Bearer tokens explicitly sent through the `Authorization` header have different
CSRF characteristics.

## Token Storage

Do not log access tokens or refresh tokens.

Do not commit signing secrets.

Client-side token storage strategy must be chosen according to the application's
threat model.

Do not state that one browser storage mechanism is universally secure.

## Authentication Errors

Do not expose detailed authentication failure information that helps an attacker
distinguish secrets or account state unnecessarily.

Login endpoints should avoid account-enumeration leaks where relevant.

## Testing

Test authorization behavior for important protected operations.

Useful integration cases include:

- unauthenticated request → `401`;
- authenticated caller without permission → `403`;
- permitted caller → expected success;
- resource-specific ownership rules;
- anonymous endpoint remains accessible when intended.

Do not test only the presence of `[Authorize]`; verify actual observable behavior.

## Anti-Patterns

Avoid:

- custom password hashing;
- disabling token validation;
- hardcoded signing keys;
- authorization only in Angular;
- Application depending on `HttpContext`;
- using `401` for an authenticated-but-forbidden user;
- using `403` for missing authentication;
- logging tokens;
- role/permission magic strings scattered throughout the codebase;
- adding Identity, JWT issuance, or OIDC infrastructure without an actual need.