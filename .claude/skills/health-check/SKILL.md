---
name: health-check
description: >
  ASP.NET Core application health-check guidance. Covers AddHealthChecks,
  MapHealthChecks, liveness/readiness endpoints, dependency checks, status
  mapping, safe response payloads, orchestration probes, and health-check
  testing. Use when implementing or reviewing application health endpoints.
---

# Health Checks

## Core Principles

1. Health checks answer whether the running application is alive and/or ready
   to serve traffic.
2. Keep health checks cheap and deterministic.
3. Distinguish liveness from readiness when deployment infrastructure benefits
   from that distinction.
4. Do not expose secrets or internal diagnostics through public health responses.
5. Do not turn health checks into full business workflows.
6. Do not make liveness depend on every external dependency.

## Basic Setup

```csharp
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");
```

Use more specific readiness/liveness endpoints when appropriate.

## Liveness

Liveness answers approximately:

> Is this application process alive enough that restarting it is not currently
> required?

A liveness probe should usually avoid dependencies such as databases and remote
APIs.

Example:

```csharp
app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false
    });
```

The exact probe configuration may vary by hosting environment.

Do not make the application restart merely because an external dependency is
temporarily unavailable.

## Readiness

Readiness answers approximately:

> Can this instance currently serve the traffic for which it is responsible?

Register readiness checks with tags:

```csharp
builder.Services
    .AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(
        "database",
        tags: ["ready"]);
```

```csharp
app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });
```

Include only dependencies required for meaningful readiness.

An optional external service should not necessarily make the whole application
unready.

## Database Health

A database health check should perform a lightweight connectivity/availability
check.

Do not run expensive business queries or large table scans.

Do not modify database data from a health check.

Use provider/framework health-check integration where suitable.

## External Dependencies

Check an external dependency only when its availability is important to the
meaning of readiness.

Avoid building a dependency cascade where:

```text
service A readiness
→ calls service B readiness
→ calls service C readiness
→ ...
```

without understanding the operational consequences.

Prefer a lightweight check of the capability the application genuinely needs.

## Status Codes

Health endpoints normally return success when considered healthy and a
non-success status when unhealthy according to the configured probe policy.

ASP.NET Core allows status-code mapping through `HealthCheckOptions`.

Do not reuse normal business API status-code semantics mechanically for
orchestrator probes.

## Response Body

Keep probe responses small.

Detailed internal health information should not be exposed publicly by default.

If diagnostic details are useful for operators, protect that endpoint using
network or authorization controls appropriate to the environment.

Do not expose:

- connection strings;
- credentials;
- raw exception details;
- sensitive infrastructure topology.

## Authentication and Exposure

Health endpoints often need to be reachable by infrastructure without ordinary
user authentication.

Choose exposure according to deployment topology.

Possible approaches include:

- private/internal network access;
- dedicated management port;
- host restrictions;
- authorization where the probe infrastructure supports it.

Do not blindly put public unauthenticated diagnostic details on the internet.

## Startup Readiness

If application initialization is asynchronous or takes meaningful time,
readiness should remain unhealthy until required startup work has completed.

Do not misuse liveness for this purpose; doing so can cause restart loops.

## Performance

Health checks may be called frequently.

Avoid:

- expensive queries;
- long external timeouts;
- large object allocation;
- full business workflows;
- writing logs at Error level for every normal failed probe.

## Testing

Test meaningful health configuration when deployment relies on it.

Useful cases include:

- live endpoint responds when the process is healthy;
- readiness changes according to a required dependency;
- response does not leak sensitive details;
- optional dependency behavior matches intended readiness semantics.

Do not create elaborate tests for framework behavior that the application does
not customize.

## Anti-Patterns

Avoid:

- using health checks as a code-quality grading system;
- database writes from probes;
- business transactions from probes;
- every dependency on liveness;
- expensive queries;
- secrets in response payloads;
- remote dependency chains with no operational justification;
- returning application stack traces from health endpoints.