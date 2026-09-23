---
name: angular
description: >
  Angular frontend architecture and implementation guidance. Covers standalone
  components, feature-based structure, routing, signals and RxJS, HttpClient,
  functional interceptors, API data-access services, forms, guards, error
  handling, lazy loading, shared UI, and frontend testing boundaries.
  Use when creating, modifying, or reviewing Angular TypeScript/templates/routes
  or frontend architecture.
---

# Angular

## Core Principles

1. Use modern standalone Angular APIs for new code.
2. Organize application code primarily by feature.
3. Keep pages/components focused on presentation and UI orchestration.
4. Keep backend communication in explicit data-access/services rather than
   scattering `HttpClient` calls through components.
5. Use Signals for synchronous reactive UI state where they fit naturally.
6. Use RxJS where asynchronous streams, HTTP composition, cancellation, or
   event pipelines make it the clearer model.
7. Do not duplicate server-side authorization/business rules in the frontend and
   treat them as security.
8. Lazy-load meaningful route-level features.
9. Prefer simple local state before introducing a state-management library.

## Suggested Structure

```text
src/app/
  core/
    auth/
    http/
    layout/

  shared/
    components/
    directives/
    pipes/

  features/
    books/
      pages/
      components/
      data-access/
      models/
      books.routes.ts

    account/
      pages/
      data-access/
      account.routes.ts

  app.config.ts
  app.routes.ts
```

Create directories only when they contain meaningful code.

Do not create empty architecture folders solely to satisfy the template.

## Standalone Components

Use standalone components for new Angular code.

```ts
@Component({
  selector: 'app-book-list',
  imports: [],
  templateUrl: './book-list.component.html',
})
export class BookListComponent {}
```

Do not introduce NgModules for ordinary new feature development unless an
integration or legacy dependency genuinely requires them.

## Feature Organization

A feature should own the UI and frontend-specific data-access logic required for
that feature.

Avoid a global structure dominated by technical buckets such as:

```text
components/
services/
models/
```

for the entire application when that spreads one feature across many unrelated
directories.

`core` and `shared` should remain relatively small.

Do not move feature-specific code into `shared` merely because it is used by two
components inside the same feature.

## Pages and Components

Route-level/page components coordinate UI behavior.

Reusable presentation components should receive data through explicit inputs and
emit meaningful UI events.

Do not move trivial presentation logic into services solely to make components
smaller.

Do not put HTTP calls directly into many unrelated components.

## Signals

Use Signals for state that is naturally synchronous and local to Angular UI:

```ts
readonly loading = signal(false);
readonly selectedBookId = signal<string | null>(null);

readonly hasSelection = computed(
  () => this.selectedBookId() !== null
);
```

Use `computed` for derived state.

Avoid unnecessary `effect()` when `computed`, template binding, or explicit
event handling expresses the dependency more clearly.

Do not convert every Observable into a Signal mechanically.

## RxJS

Use RxJS when its stream model provides real value, for example:

- `HttpClient`;
- debounced search;
- cancellation/switching;
- combining asynchronous sources;
- event streams.

Avoid manual nested subscriptions.

Prefer composition operators.

When manually subscribing from framework-managed code, ensure subscription
lifetime is tied to the component/service lifecycle using the project's current
Angular facilities.

## HTTP

Keep API communication in feature data-access services or another explicit HTTP
boundary.

```ts
@Injectable({ providedIn: 'root' })
export class BooksApi {
  private readonly http = inject(HttpClient);

  getBooks(): Observable<Book[]> {
    return this.http.get<Book[]>('/api/books');
  }
}
```

Do not expose raw HTTP details throughout presentation components.

Use frontend API models that match the published HTTP contract.

Do not import backend Domain entities into the Angular project.

## HttpClient Configuration

Use modern provider-based configuration when configuration is required:

```ts
provideHttpClient(
  withInterceptors([
    authInterceptor,
    errorInterceptor,
  ]),
);
```

Prefer functional interceptors for new code.

Do not introduce class-based interceptors without a reason.

Keep interceptor responsibilities cross-cutting.

Do not put feature business logic in an HTTP interceptor.

## Authentication

A frontend may:

- attach an access token;
- display authenticated state;
- hide actions the user cannot normally perform;
- protect navigation with route guards.

These are UI/UX concerns.

Backend authorization remains authoritative.

Do not treat a route guard or hidden button as a security boundary.

Follow the backend `authentication` skill for server-side authentication and
authorization semantics.

## HTTP Errors

Frontend code should understand the API's documented error contract.

When backend errors use `ProblemDetails`, centralize reusable parsing/mapping
where practical.

Do not infer business behavior by parsing arbitrary human-readable exception
messages.

Follow `http-api` for backend HTTP semantics.

## Forms

Use reactive forms for non-trivial forms where explicit state, validation, and
testability are useful.

Keep transport/request mapping explicit.

Frontend validation improves user experience but does not replace backend
validation.

Do not duplicate complex Domain rules in TypeScript unless the UI genuinely
needs an equivalent user-facing rule.

## Routing

Use route-level lazy loading for meaningful features:

```ts
export const routes: Routes = [
  {
    path: 'books',
    loadChildren: () =>
      import('./features/books/books.routes')
        .then(m => m.BOOK_ROUTES),
  },
];
```

For individual standalone route components, `loadComponent` is also appropriate.

Do not lazy-load every tiny component.

## State Management

Start with:

```text
component state
signals
feature service/store
RxJS composition
```

Introduce a dedicated state-management library only when application complexity
demonstrates a need.

Do not add NgRx or another state framework by default.

## Shared Components

`shared` is appropriate for reusable UI primitives or behavior that genuinely
crosses features.

Examples:

```text
confirmation dialog
generic loading indicator
reusable table primitive
formatting pipe
```

Do not use `shared` as a dumping ground.

## Angular Material

When the project has chosen Angular Material, use its components consistently
for common UI primitives where they satisfy the requirement.

Do not add Angular Material automatically to projects that have not chosen it.

Do not wrap every Material component in a custom abstraction without a concrete
reason.

## API Client Generation

When OpenAPI client generation is adopted:

- treat generated code as generated;
- keep it separate from handwritten feature logic;
- regenerate it when the API contract changes;
- do not manually patch generated files unless the generation workflow requires it.

Follow the `openapi` skill.

## Testing

Test frontend behavior at the appropriate level.

Useful targets include:

- meaningful component behavior;
- validation;
- data-access request mapping;
- important route/auth behavior;
- complex reactive state.

Do not test Angular framework internals.

End-to-end tests should focus on a small number of important user workflows
unless the project explicitly requires broader E2E coverage.

Do not duplicate the same behavior unnecessarily at every test level.

## Performance

Do not prematurely optimize normal Angular code.

Use route lazy loading and sensible rendering/data access first.

Introduce specialized memoization, virtual scrolling, custom change-detection
work, or other optimizations after identifying a real need.

## Anti-Patterns

Avoid:

- new NgModules without a reason;
- HTTP calls scattered throughout components;
- global service folders containing unrelated feature services;
- NgRx/state frameworks by default;
- manual subscriptions without lifecycle handling;
- complex business logic in interceptors;
- frontend authorization as the only authorization;
- parsing human-readable backend error strings;
- giant `shared` directories;
- converting every Observable to a Signal;
- using Signals or RxJS merely because one is newer than the other.