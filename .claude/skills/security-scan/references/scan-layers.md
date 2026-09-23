# Security Scan — Layer Reference

Detailed checks used by the `security-scan` skill.

Use these as investigation prompts, not as automatic vulnerability declarations.

# Layer 1: Dependencies and Supply Chain

OWASP:

```text
A03:2025 Software Supply Chain Failures
```

## .NET

When supported by the installed SDK:

```bash
dotnet package list --vulnerable --include-transitive
```

Review:

- vulnerable direct dependencies;
- vulnerable transitive dependencies;
- severity/advisory information;
- whether the vulnerable component is actually used in the affected application;
- whether a patched compatible version exists;
- unexpected or untrusted NuGet sources.

Do not automatically upgrade a package during a scan unless asked.

Do not rate the application finding solely from CVSS. Consider reachability and
exposure.

## Angular / npm

When an npm frontend exists, inspect:

```bash
npm audit
```

Distinguish:

- production dependency;
- development/build dependency;
- directly used package;
- transitive package.

A development-only vulnerability can still matter to the software supply chain,
but its runtime impact differs from a browser-shipped dependency.

Review lockfiles when package integrity or unexpected dependency changes are
relevant.

# Layer 2: Secrets and Configuration

OWASP:

```text
A02:2025 Security Misconfiguration
A04:2025 Cryptographic Failures
```

Search source-controlled files for real credentials and secret material.

Relevant file types include:

```text
.cs
.ts
.json
.yml
.yaml
.xml
.config
.env*
Dockerfile
Compose files
CI workflow files
scripts
```

Potential high-confidence indicators:

```text
private keys
real access/refresh tokens
cloud access keys
database passwords
API secrets
service-account credentials
production connection strings containing credentials
```

Potential indicators requiring context:

```text
ApiKey = "..."
Secret = "..."
Token = "..."
Password = "..."
long opaque strings
connection strings
certificate material
```

Do not automatically ignore `appsettings.Development.json`.

A real committed secret remains a secret regardless of the filename.

Obviously fake values such as:

```text
changeme
example-only
test-password
not-a-real-key
```

may be ignored or reported as Info when context makes their non-sensitive nature
clear.

`UserSecretsId` itself is not a secret.

Preferred pattern:

```csharp
var connectionString =
    builder.Configuration.GetConnectionString("Default");
```

with the real secret supplied through the project's chosen local/deployment
secret mechanism.

Also review:

- production debug/development flags;
- certificate-validation bypass;
- insecure URLs carrying credentials;
- overly verbose production error configuration;
- accidentally committed `.env` files.

# Layer 3: Injection and Dangerous Code Patterns

OWASP:

```text
A04 Cryptographic Failures
A05 Injection
A08 Software or Data Integrity Failures
```

## SQL Injection

Look for SQL constructed from untrusted input:

```csharp
var sql =
    $"SELECT * FROM Orders WHERE Name = '{search}'";
```

or interpolated/concatenated data passed to APIs that do not parameterize it.

Prefer:

```text
EF Core LINQ
parameterized SQL
FromSqlInterpolated where appropriate
ADO.NET/Dapper parameters
```

Do not report normal EF Core LINQ as SQL injection merely because user input
participates in a predicate.

## Command Injection

Inspect:

```text
Process.Start
shell execution
PowerShell/bash invocation
command arguments built from user input
```

Prefer APIs that separate executable and arguments.

Validate or constrain external input when command execution is genuinely needed.

## Path Traversal

Review user-controlled:

```text
file names
paths
archive extraction
download paths
upload destinations
```

Look for:

```text
../
absolute-path injection
path escaping from intended root
unsafe archive extraction
```

Normalize and verify paths remain within the intended storage boundary.

## SSRF

Inspect outbound requests where users can influence:

```text
scheme
host
port
full URL
redirect destination
```

Pay particular attention when the server can access internal networks,
metadata endpoints, or privileged services.

Do not report every user-supplied query parameter in an outbound request as
SSRF; host/control-plane influence matters.

## XSS / Unsafe HTML

For Angular, investigate deliberate sanitizer bypasses such as:

```text
bypassSecurityTrustHtml
bypassSecurityTrustScript
bypassSecurityTrustResourceUrl
direct DOM manipulation with untrusted content
```

Ordinary Angular binding benefits from framework escaping/sanitization and is not
automatically XSS.

Review deliberate raw HTML handling in any server-rendered UI as well.

## Unsafe Deserialization

Investigate dangerous polymorphic/type-driven deserialization mechanisms.

Legacy examples include:

```text
BinaryFormatter
unsafe Newtonsoft TypeNameHandling configurations
```

Do not deserialize untrusted payloads into arbitrary runtime types.

## Cryptography

Flag security-sensitive uses of:

```text
MD5
SHA1
ECB mode
hardcoded encryption keys
custom password hashing
home-grown encryption protocols
```

Do not flag MD5/SHA1 solely when used as a non-security checksum and collision
resistance is irrelevant.

For passwords, use established password-hashing facilities such as ASP.NET Core
Identity/password hashers rather than reversible encryption.

Do not recommend custom cryptography.

# Layer 4: Authentication and Access Control

OWASP:

```text
A01:2025 Broken Access Control
A07:2025 Authentication Failures
```

## Effective Authorization

Determine effective access policy.

Inspect:

```text
fallback/global authorization policy
controller [Authorize]
action [Authorize]
[AllowAnonymous]
policy names
role/claim requirements
resource-based authorization
```

An endpoint without an explicit `[Authorize]` is not automatically vulnerable if
a secure fallback/global policy protects it.

Likewise, bare `[Authorize]` is not automatically insufficient when simple
authentication is the intended requirement.

Identify the actual authorization behavior.

## Anonymous Access

Review `[AllowAnonymous]` and other deliberately public surfaces.

Confirm public access is intentional.

Do not report every public endpoint as a vulnerability.

## Resource Authorization / IDOR

Look for operations such as:

```text
GET /orders/{id}
PUT /users/{id}
DELETE /documents/{id}
```

where possession of an identifier could be enough to access another user's or
tenant's data.

Verify ownership, tenant, permission, or resource policy where required.

Client-side filtering is not authorization.

## Authentication Configuration

When Bearer/OIDC authentication is used, inspect whether the configured
authentication mechanism appropriately validates:

```text
issuer
audience where applicable
signature/signing authority
lifetime
```

Do not require manually setting every `TokenValidationParameters` property when
the selected framework/provider already establishes secure validation through
its authority/metadata configuration.

Do not impose an arbitrary universal `ClockSkew` value as a vulnerability rule.

Flag intentionally disabled validation when it weakens the actual authentication
contract without a valid reason.

## Passwords and Accounts

When the application owns local accounts, review:

```text
password hashing
password reset behavior
lockout/brute-force controls where appropriate
account-enumeration behavior
credential storage
```

Do not recommend custom password hashing.

## Angular

Route guards and hidden/disabled buttons are UX mechanisms.

Verify that sensitive operations are protected by the backend.

# Layer 5: Browser and HTTP/API Security

OWASP:

```text
A01 Broken Access Control
A02 Security Misconfiguration
```

## CORS

CORS is a browser-origin policy, not an authorization mechanism.

Review whether the configured origins match the actual browser-client model.

Potential problems include:

```text
unnecessarily broad trusted origins
credentialed requests from untrusted origins
environment-specific origins accidentally exposed in Production
```

`AllowAnyOrigin()` can be valid for a genuinely public non-credentialed API.

Do not report wildcard origin alone as a vulnerability without considering the
API's intended exposure.

Credentialed cross-origin access requires appropriately restricted origins.

## CSRF

When browser credentials are sent automatically, especially cookies, review CSRF
protection for state-changing operations.

Bearer tokens explicitly attached through the Authorization header have different
CSRF characteristics.

Do not assume Angular itself provides server-side CSRF protection for every
authentication model.

## TLS

Review:

```text
disabled certificate validation
accept-all certificate callbacks
plain HTTP for sensitive external communication
```

Account for TLS termination at reverse proxies, ingress, gateways, or hosting
platforms.

Absence of HTTPS redirection in application code is not automatically a
vulnerability when the hosting topology correctly enforces TLS.

## File Uploads

When uploads exist, inspect:

```text
size limits
storage path safety
file-name handling
content/type assumptions
execution/public-serving risks
authorization
```

Do not trust the client-provided MIME type as authoritative.

## Rate Limiting

Consider rate limiting where abuse could realistically matter, such as:

```text
login/password reset
expensive anonymous endpoints
resource-intensive operations
```

Do not require rate limiting on every endpoint.

Follow `http-api` for `429` semantics.

# Layer 6: Sensitive Data, Logging, and Error Exposure

OWASP:

```text
A04 Cryptographic Failures
A09 Security Logging & Alerting Failures
```

## Logging

Never log:

```text
passwords
access/refresh tokens
private keys
authentication secrets
full payment credentials
```

Review PII according to project requirements.

Prefer technical identifiers over identity data where practical.

Do not consider Debug/Trace level a security boundary.

Review duplicate or missing security-relevant logging in context; do not demand
logs for every successful request.

## Error Responses

Review public errors for:

```text
stack traces
SQL
connection strings
internal filesystem paths
secret values
internal exception details
```

Use the project's centralized error-handling policy.

## Response Data

Look for:

```text
database/domain entities returned directly
password hashes
internal security fields
unnecessary personal information
secret metadata
```

Prefer explicit response contracts where needed.

Do not assume returning a Domain entity is automatically vulnerable; inspect the
actual serialized fields.

## Data at Rest

Do not prescribe ASP.NET Core Data Protection as a universal database-encryption
solution.

Choose protection according to the requirement:

```text
password -> one-way password hashing
verification-only token -> often hash rather than reversible encryption
retrievable application secret -> appropriate protected/encrypted storage
database/storage encryption -> platform/database mechanism when suitable
temporary application payload -> ASP.NET Core Data Protection may be suitable
```

Do not roll custom cryptography.

# Layer 7: Exceptional Conditions and Failure Handling

OWASP:

```text
A06:2025 Insecure Design
A10:2025 Mishandling of Exceptional Conditions
```

Review exceptional paths where failure could weaken security or correctness.

## Fail-Open Behavior

Investigate code that catches an exception and then allows a privileged operation
to continue.

Examples:

```text
authorization service failed -> allow
token validation failed -> continue as anonymous but perform privileged action
permission lookup failed -> default to permitted
```

Security-sensitive checks should normally fail safely.

## Swallowed Exceptions

Review:

```csharp
catch (Exception)
{
}
```

and broad catch blocks that continue without a deliberate fallback.

Not every swallowed exception is a vulnerability, but security-relevant failures
must not silently convert into success.

## Retry and Idempotency

Review retries of state-changing operations.

A lost response followed by retry can create duplicate effects.

Follow the `resilience` and `httpclient-factory` skills.

Do not classify caller cancellation as a transient failure requiring retry.

## Timeouts and Resource Exhaustion

Review external operations or public inputs that can consume unbounded:

```text
time
memory
database rows
file size
request body
retry attempts
parallel work
```

A theoretical lack of limit is not automatically High severity; evaluate whether
an attacker can actually exploit it.

## Startup and Configuration Failure

Review security-sensitive startup configuration for dangerous fallback.

Examples:

```text
invalid auth configuration -> authentication silently disabled
missing encryption key -> plaintext mode
failed authorization provider -> allow-all fallback
```

Prefer visible failure over insecure fallback when a required security control
cannot initialize.

## Unexpected Error Exposure

Confirm unexpected exceptions follow the project's centralized error-handling
policy and do not reveal sensitive information.

# Cross-Cutting Design Review

Static pattern checks do not fully cover:

```text
A06 Insecure Design
tenant isolation
business workflow abuse
privilege escalation through legitimate operations
financial/idempotency abuse
multi-step authorization problems
```

When the application has meaningful security-sensitive workflows, identify them
as candidates for threat modeling or dedicated manual review rather than claiming
the static scan proves them secure.

# Severity Guidance

Do not mechanically map:

```text
pattern -> fixed severity
CVSS -> application severity
OWASP category -> severity
```

Consider actual reachability and impact.

Examples:

```text
real production private key committed publicly
→ potentially Critical

raw SQL string containing only developer-controlled constant text
→ not SQL injection

missing [Authorize] while secure fallback policy exists
→ not a finding

PII logged in a restricted internal diagnostic environment
→ contextual finding, not automatically High
```

# Report Format

For each finding:

```markdown
### [HIGH] src/.../File.cs:42 — Resource authorization missing

OWASP: A01:2025 Broken Access Control

Evidence:
The endpoint loads an order solely by route ID and does not constrain it to the
authenticated customer.

Impact:
An authenticated user who learns another order ID may access that customer's
order.

Remediation:
Apply resource/ownership authorization before returning the order.
```

When evidence is incomplete:

```markdown
### [NEEDS VERIFICATION] ...

The static code suggests ..., but effective behavior depends on ...
```

Do not invent exploitability that the code does not establish.