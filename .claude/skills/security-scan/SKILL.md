---
name: security-scan
description: >
  Structured static security review for .NET and Angular applications.
  Covers vulnerable dependencies, secrets and configuration, injection and
  dangerous code patterns, authentication and authorization, browser/API
  security, sensitive-data exposure and logging, and exceptional-condition
  handling. Maps findings to OWASP Top 10:2025 where appropriate.
  Use when asked for a security scan, security audit, vulnerability review,
  OWASP review, secret scan, auth review, CORS review, pre-release security
  check, or penetration-test preparation.
---

# Security Scan

## Purpose

Perform an evidence-based static security review of the repository.

This is not a penetration test.

Static review can identify many code, dependency, configuration, and access-control
problems, but it cannot prove that the application is secure and may not detect:

- runtime-only vulnerabilities;
- infrastructure configuration outside the repository;
- complex business-logic abuse;
- sophisticated authorization bypasses;
- deployment-specific weaknesses;
- vulnerabilities requiring dynamic exploitation.

Report that limitation explicitly.

## Principles

1. Report concrete security risks, not generic security advice.
2. Evaluate findings in the context of actual exposure and application behavior.
3. Distinguish confirmed findings from issues that require verification.
4. Do not inflate severity merely because a suspicious pattern exists.
5. Do not suppress a real secret merely because it appears in a Development file.
6. Do not require security mechanisms that the project does not need.
7. Evaluate effective authorization behavior, including global/fallback policies.
8. Use existing project security, authentication, configuration, logging,
   error-handling, HTTP, and resilience guidance.
9. Do not require a specific MCP server, agent, analyzer, or commercial scanner.

## OWASP Mapping

Use OWASP Top 10:2025 as an awareness taxonomy when it genuinely fits the
finding:

```text
A01 Broken Access Control
A02 Security Misconfiguration
A03 Software Supply Chain Failures
A04 Cryptographic Failures
A05 Injection
A06 Insecure Design
A07 Authentication Failures
A08 Software or Data Integrity Failures
A09 Security Logging & Alerting Failures
A10 Mishandling of Exceptional Conditions
```

Not every issue needs an OWASP category.

Do not force an inaccurate mapping merely to populate the report.

## Scan Scope

Before scanning, inspect the repository and determine which surfaces exist:

```text
.NET backend
Angular frontend
authentication/authorization
database access
external HTTP integrations
file upload/download
Docker/container configuration
CI/CD
production configuration
logging
caching
background processing
```

Skip irrelevant checks rather than reporting them as failures.

## Security Layers

Use the detailed procedures in:

```text
references/scan-layers.md
```

The scan is organized into seven layers:

| # | Layer | Typical OWASP mapping |
|---|---|---|
| 1 | Dependencies and supply chain | A03 |
| 2 | Secrets and configuration | A02, A04 |
| 3 | Injection and dangerous code patterns | A04, A05, A08 |
| 4 | Authentication and access control | A01, A07 |
| 5 | Browser and HTTP/API security | A01, A02 |
| 6 | Sensitive data, logging, and error exposure | A04, A09 |
| 7 | Exceptional conditions and failure handling | A06, A10 |

## Selecting Scope

For a full pre-release or explicitly requested security audit, review all
applicable layers.

For targeted changes, focus first on the affected layers but expand the review
when the change crosses security boundaries.

Examples:

| Change | Priority areas |
|---|---|
| Dependency update | Dependencies / supply chain |
| Authentication change | Authentication / access control |
| New Controller endpoint | Access control, input handling, HTTP security |
| Database/query change | Injection, access control, sensitive data |
| Configuration change | Secrets / configuration |
| Logging change | Sensitive data / logging |
| HttpClient integration | Configuration, SSRF/input handling, failure handling |
| File upload | Input handling, path safety, resource limits |
| Angular auth/UI change | Browser security + server authorization assumptions |

## Tooling

Use available repository search, compiler/tooling output, package-audit commands,
and static analyzers when available.

Tool output is evidence, not authority.

Do not require an MCP tool or external scanner for the skill to work.

When an automated scanner reports a possible issue:

1. inspect the actual code;
2. determine whether the pattern is reachable and relevant;
3. report it only with appropriate confidence.

## Severity

Rate severity from actual risk.

Consider:

```text
exploitability
external exposure
required privileges
affected data or capability
blast radius
whether exploitation is reliable
existing compensating controls
```

### Critical

Use sparingly for immediately exploitable, high-impact conditions such as:

- exposed real production credentials;
- reachable authentication bypass;
- straightforward remote code execution;
- exploitable injection with major data/system impact.

### High

A serious vulnerability that is realistically exploitable or can substantially
compromise data, authorization, or system behavior.

### Medium

A meaningful weakness requiring realistic conditions, limited access, or
additional exploitation steps.

### Low

A genuine but limited security weakness with low practical impact.

### Info

Useful security observation or hardening opportunity that is not itself a
demonstrated vulnerability.

Do not convert advisory CVSS scores directly into application finding severity
without considering whether the affected dependency/code path is relevant.

## False Positives and Context

Do not automatically report these as vulnerabilities:

- endpoint without `[Authorize]` when a secure fallback/global policy protects it;
- bare `[Authorize]` when authentication alone is actually the intended policy;
- `AllowAnyOrigin()` for a genuinely public non-credentialed API;
- MD5/SHA1 used for a non-security checksum;
- data not encrypted at application level when platform/storage encryption
  adequately satisfies the requirement;
- obviously fake test credentials;
- framework defaults that are secure in the actual hosting configuration.

Conversely, do not automatically treat these as safe:

- secrets in `appsettings.Development.json`;
- sensitive values logged only at Debug level;
- client-side Angular route guards;
- hidden UI buttons;
- CORS configuration as a substitute for authorization.

## Cross-Skill Guidance

Use these skills/rules when deeper guidance is needed:

```text
security.md
authentication
configuration
logging
error-handling
http-api
httpclient-factory
resilience
ef-core
angular
```

Do not duplicate their entire implementation guidance inside the security scan.

## Reporting

Every confirmed finding should include:

```text
severity
file and location
title
evidence
why it is a security risk
likely impact
smallest reasonable remediation
OWASP category when applicable
```

If the finding is uncertain, label it clearly:

```text
NEEDS VERIFICATION
```

rather than presenting it as confirmed.

## Report Shape

```markdown
# Security Scan Report

Scope:
- ...

Limitations:
Static analysis only. This does not replace penetration testing,
dynamic testing, threat modeling, or infrastructure review.

## Summary

| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 0 |
| Medium | 0 |
| Low | 0 |
| Info | 0 |

## Findings

### [HIGH] File:Line — Finding title

OWASP: A01:2025 Broken Access Control

Evidence:
...

Impact:
...

Remediation:
...

## Layer Results

| Layer | Status | Findings |
|---|---|---:|
| Dependencies and supply chain | PASS | 0 |
| Secrets and configuration | PASS | 0 |
| Injection and dangerous patterns | PASS | 0 |
| Authentication and access control | PASS | 0 |
| Browser and HTTP/API security | PASS | 0 |
| Sensitive data/logging/error exposure | PASS | 0 |
| Exceptional conditions | PASS | 0 |
```

Use:

```text
PASS
FINDINGS
NOT APPLICABLE
NOT CHECKED
```

rather than pretending an uninspected layer passed.

## Remediation Discipline

Prefer the smallest secure change consistent with project architecture.

Do not recommend:

- introducing a new authentication system unnecessarily;
- custom cryptography;
- blanket encryption of all database fields;
- `[Authorize]` on every action when a global policy already provides the same
  protection;
- security libraries merely to satisfy the scan;
- disabling functionality solely because a static scanner cannot understand it.

## Completion

A security scan is complete when:

- all applicable layers were inspected;
- findings have evidence;
- uncertain findings are identified as such;
- severity reflects actual risk;
- relevant limitations are stated;
- no layer is marked PASS without being checked.