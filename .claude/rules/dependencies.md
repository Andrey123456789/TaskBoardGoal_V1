---
paths:
  - "**/*.csproj"
  - "**/Directory.Packages.props"
  - "**/package.json"
  - "**/package-lock.json"
---

# Dependency Rules

## General Principle

Add a dependency only when it solves a concrete current requirement.

Do not install packages for hypothetical future use.

When the repository defines an approved technology in
`.claude/skills/project-structure/references/technology-stack.md`, use that
technology rather than silently selecting an alternative.

## Version Selection

When adding or updating a NuGet or npm dependency:

1. Determine the latest stable version compatible with the project's target
   framework/runtime and peer dependencies.
2. Verify the current package version from the package registry rather than
   relying on a remembered version.
3. Do not select preview, alpha, beta, RC, dev, nightly, or other prerelease
   versions unless explicitly requested.
4. Check whether the selected package/version is deprecated.
5. Check known vulnerability information available from the relevant package
   ecosystem.
6. Verify the license of the exact selected version before installation.

"Latest" means:

```text
latest stable compatible version
```

not:

```text
latest version of any release channel
```

Do not use floating versions such as `*`.

## License Check

Before installing a third-party dependency, inspect its official registry
metadata and, when necessary, the upstream license for the exact selected
version.

Permissive open-source licenses commonly suitable for normal commercial use,
such as:

```text
MIT
Apache-2.0
BSD-family licenses
```

may normally be accepted.

Do not assume that a package is acceptable merely because it can be downloaded
without payment.

## Paid or Commercial Dependencies

If the selected dependency requires payment or a commercial license, do not
install it automatically.

Present the user with explicit choices:

```text
Package: Example.Package
Version: <selected stable compatible version>
License: Commercial
Reason: <capability for which it was selected>

Options:
1. Install the requested commercial package.
2. Use free alternative A — <license and important trade-off>.
3. Use free alternative B — <license and important trade-off>.
...
```

Provide reasonable free alternatives when they exist.

Do not silently replace the requested package with an alternative.

If no reasonable free alternative exists, say so explicitly.

## Licenses Requiring Review

Do not automatically adopt a dependency when its license has material obligations
or cannot be confidently classified.

Examples include:

```text
strong copyleft licenses
dual licensing that may require a commercial license
source-available/custom licenses
unclear or missing license information
```

Explain the relevant licensing concern and present alternatives when practical.

Do not make legal conclusions beyond what the published license information
supports.

## NuGet

Use NuGet Central Package Management for this template.

Package versions belong in:

```text
Directory.Packages.props
```

Individual project files should normally contain versionless
`PackageReference` entries.

Example:

```xml
<!-- Directory.Packages.props -->
<ItemGroup>
  <PackageVersion Include="FluentValidation" Version="..." />
</ItemGroup>
```

```xml
<!-- Project.csproj -->
<ItemGroup>
  <PackageReference Include="FluentValidation" />
</ItemGroup>
```

Do not duplicate centrally managed versions in individual `.csproj` files.

Keep package references in the narrowest project that actually requires them.

Do not add a package to every project merely because several projects belong to
the same solution.

## npm

Use npm as the default package manager for the Angular workspace unless the
project deliberately adopts another package manager.

Maintain and commit:

```text
package.json
package-lock.json
```

Use the appropriate dependency category:

```text
dependencies
devDependencies
```

Do not put build/test tooling into runtime dependencies without a reason.

When adding packages, preserve a consistent lockfile.

In CI, prefer:

```bash
npm ci
```

for reproducible installation from the committed lockfile.

Do not use `--force` or `--legacy-peer-deps` merely to bypass incompatible peer
dependencies.

Resolve the actual compatibility problem instead.

## Dependency Security

When dependency changes are security-relevant or when verifying the dependency,
use the ecosystem's available vulnerability information.

For modern .NET SDKs, an applicable command is:

```bash
dotnet package list --vulnerable --include-transitive
```

For npm projects:

```bash
npm audit
```

Scanner output requires context.

Do not automatically replace or upgrade unrelated packages merely because a
scanner reports a finding.

## Conditional Dependencies

An approved dependency may still be conditional.

Examples from this template:

```text
Mapster
    -> add when meaningful mapping is required

Microsoft.Extensions.Http.Resilience
    -> add when outbound HTTP needs resilience behavior

Microsoft.Extensions.Resilience
    -> add when a non-HTTP resilience pipeline is required

Testcontainers
    -> add when isolated real-provider testing justifies it

Playwright
    -> add when frontend E2E testing is required
```

"Approved" does not mean "install by default".

## Removal

Remove dependencies that are no longer used when doing so is part of the current
change and safe to verify.

Do not leave dead package references merely because removing them is harmless in
the short term.

Do not perform unrelated dependency cleanup during a narrowly scoped task.