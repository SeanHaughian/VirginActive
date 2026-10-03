# Rock Commitment Tracker

Virgin Active Digital - Integration Service API assessment

A .NET 10 Web API for creating and tracking members' weekly commitments, called Rocks. Members can mark a Rock as completed or missed, and retrieve their Rocks alongside profile data from an external API.

I focused on core API behaviour, validation, error handling and reliable profile integration, and tested the critical paths. Circuit breaker, distributed tracing and deployment automation are left as future work (see below).

> **A Rock can only be created for a member that exists.** Member existence is checked against the external profile API (JSONPlaceholder), which only has members **`1`-`10`**. Creating a Rock for any other member ID returns `404 Not Found` - use an ID in that range when testing.

## Technology

| Technology or package | Use |
| --- | --- |
| .NET 10 / ASP.NET Core | Controller-based Web API |
| FluentValidation | Request validation rules (title, due date, category, member id) |
| Serilog and Serilog.Formatting.Json | Structured JSON logs |
| Microsoft.Extensions.Http.Resilience | Retry and timeout policies for profile requests |
| xUnit | Unit and integration tests |
| Swashbuckle.AspNetCore | OpenAPI documentation and Swagger UI |

There is no database or ORM. `IRockStore` provides in-memory storage. The current implementation also writes a local JSON snapshot (`rocks-store.json`) to retain data between development sessions. This is an extra development convenience, beyond the in-memory storage requested in the brief, and would not be suitable for production storage.

## Project structure

| Folder | Contents |
| --- | --- |
| Controllers | Rock and profile endpoints |
| Models | Rocks, categories and request DTOs |
| Services and Validation | Business rules, storage and request validation |
| Clients/TypiCode | Typed client for the external profile API |
| Middleware | API key checks, correlation IDs, request logging and exception handling |
| Common | Problem Details and HTTP context helpers |
| Startup | Configuration checks and Swagger setup |
| Resources | Error messages in `Messages.resx` |
| RockTracker.Api.Tests | Unit and integration tests |

## Running locally

Install the .NET 10 SDK. From the repository root:

```powershell
cd RockTracker.Api
dotnet restore
dotnet build
```

The application requires an API key hash before it can start. The configuration has this shape:

```json
{
  "Authentication": {
    "ApiKeyHash": "<iterations>:<salt>:<hash>"
  },
  "ExternalApis": {
    "TypiCode": {
      "BaseUrl": "https://jsonplaceholder.typicode.com/",
      "Resilience": {
        "OuterTimeoutSeconds": 15,
        "InnerTimeoutSeconds": 5,
        "MaxRetryAttempts": 3,
        "RetryBaseDelaySeconds": 1,
        "UseJitter": true
      }
    }
  }
}
```

For development, store the hash using user secrets:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Authentication:ApiKeyHash" "<iterations>:<salt>:<hash>"
dotnet run
```

Send the raw key in requests, not the hash. Don't commit real keys or hashes.

> **Required to call the API.** The dev hash is for raw key **`dev-local-api-key`** - send it in the `X-Api-Key` header or you'll get a `401`. Same key the integration tests use. It's a throwaway dev value, not a real secret.

`StartupConfigurationValidator` checks required settings at startup. Swagger UI is at `/swagger` in Development, with an **Authorize** button for the API key. `RockTracker.Api.http` also supports manual testing in Visual Studio.

## Calling the API

Every request requires `X-Api-Key`. You can also supply `X-Correlation-Id`; otherwise one is generated and returned in the response headers.

Examples below use PowerShell with `curl.exe` (not the `curl` alias for `Invoke-WebRequest`). Replace the URL/key/Rock ID placeholders as needed, and check the port printed by `dotnet run` if it differs from the one shown. Member `1` is used so the profile example resolves a JSONPlaceholder user.

### Create a Rock

> Member must exist (JSONPlaceholder IDs `1`–`10` only) or this returns `404 Not Found`.

```powershell
curl.exe --% -X POST "http://localhost:5176/members/1/rocks" -H "X-Api-Key: dev-local-api-key" -H "Content-Type: application/json" -d "{\"title\":\"Apply for five developer roles\",\"category\":\"Career\",\"dueDate\":\"2026-12-15T00:00:00Z\",\"note\":\"Find my next role\"}"
```

Use a future due date when running this example. Revenue Rocks must also fall within the current quarter. Successful creation returns a generated ID and `pending` status.

### List a Member’s Rocks

```powershell
curl.exe --% -X GET "http://localhost:5176/members/1/rocks?status=pending" -H "X-Api-Key: dev-local-api-key"
```

Omit `status` to return all Rocks for the member.

### Retrieve an Enriched Profile

```powershell
curl.exe --% -X GET "http://localhost:5176/members/1/profile" -H "X-Api-Key: dev-local-api-key"
```

### Change a Rock Status

```powershell
curl.exe --% -X PATCH "http://localhost:5176/members/1/rocks/5b7e9f2a-4c1d-4a6e-8f3b-2d5c7a9e1b63" -H "X-Api-Key: dev-local-api-key" -H "Content-Type: application/json" -d "{\"status\":\"completed\"}"
```

The response combines the member's Rocks with external profile data. If enrichment is unavailable, the Rocks are still returned with a flag indicating that the profile could not be fetched.

| Outcome | HTTP status |
| --- | --- |
| Created | 201 |
| Successful read or update | 200 |
| Invalid input | 400 |
| Missing or invalid API key | 401 |
| Resource not found | 404 |
| Invalid state transition | 422 |
| Unexpected failure | 500 |

Errors use Problem Details. Unexpected failures return a generic message without exposing a stack trace.

## Design decisions

Controllers keep routing, validation, business rules and external calls separated. `IRocksService` returns a `RockOperationResult` for expected outcomes (invalid input, missing Rock); the controller maps that to an HTTP response, while middleware handles unexpected exceptions centrally. `IRockStore` keeps storage behind an interface so a real database could slot in later. The profile client uses `AddHttpClient<ITypiCodeClient, TypiCodeClient>()` to centralise HTTP config and allow test substitution. Messages live in `Messages.resx` for easy wording/translation changes. Request validation uses FluentValidation (`CreateRockRequestValidator`, `UpdateRockStatusRequestValidator`) rather than hand-rolled checks - rules read close to plain English and category-specific checks don't leak into the controller.

### Trade-offs and other approaches I considered

- **Controllers over Minimal APIs** - more ceremony for three endpoints, but scales better as things grow.
- **Result objects (`RockOperationResult`) over exceptions** for "not found"/"invalid transition" - these are expected outcomes, not exceptional ones.
- **In-memory store + JSON snapshot** - a dev convenience so data survives a restart, not a real persistence strategy. No concurrent-write safety; a real deployment needs a proper store.
- **Category rules as a class hierarchy, not DI strategies** - quicker to build; the trade-off is adding a category still touches the shared `FromName` lookup (see Requirement 3).
- **Retry/timeout without a circuit breaker** - covers resilience, but a sustained outage means every request still pays the full retry cost before failing.
- **One shared API key, not per-client credentials** - enough to prove auth works, not production-ready (see improvements below).
- **Structured logs + correlation ID, no distributed tracing** - enough to follow a single request, not enough for a system with more dependencies.

## Requirement 3: category validation

The current implementation puts each category's additional rule in a separate class derived from `RockCategory`:

| Category | Additional rule |
| --- | --- |
| Revenue | Due date falls within the current quarter |
| Health | Title contains at least 10 characters |
| Career | A note explaining why the Rock matters is required |
| Other | No additional rule |

The validator calls `rock.Category.IsValid(rock, now)`, so each category owns its own rule rather than a central switch statement. Category objects are shared singletons whose rules depend only on the Rock and the supplied time.

**Limitation:** adding a category still means a new class *and* a change to the `FromName` lookup, so it doesn't fully meet the brief's "no changes to existing code" goal or the suggested DI-registered `IRockValidationStrategy` approach, which would remove the central lookup entirely.

Covered by `RockCategoryValidationStrategyTests` and `RockCategoryValidationTests`.

## Error handling, logging and security

`ExceptionHandlingMiddleware` handles unexpected exceptions centrally; expected errors use the status codes listed above.

`CorrelationIdMiddleware` adds the correlation ID to the Serilog context. `RequestLoggingMiddleware` logs method, path, status and duration - Information for success, Warning otherwise - as structured JSON.

`ApiKeyAuthMiddleware` validates `X-Api-Key` against a salted hash via `ApiKeyValidator`; missing/invalid keys return 401. Swagger UI is Development-only.

The key is hashed (PBKDF2, per-key salt), not encrypted, because we only ever need to *check* it, never recover it - encryption is reversible by design, which is the wrong property here, same reasoning as not storing passwords reversibly.

HTTPS is enforced via `UseHttpsRedirection()`; production would add HSTS and TLS termination at the edge, rejecting plain HTTP outright.

## Requirement 7: resilient profile integration

The typed client calls `https://jsonplaceholder.typicode.com/users/{memberId}`, with resilience settings from configuration.

`MaxRetryAttempts: 3` with exponential backoff and jitter rides out brief blips without piling on a struggling dependency, and jitter avoids synchronized retry storms. A 5-second inner timeout bounds each attempt; a 15-second outer timeout bounds the whole retry sequence so a client never waits indefinitely. Retry attempts, delay and reason are logged.

On failure or timeout, the client returns an unavailable result and logs a warning - the endpoint still returns the member's Rocks with HTTP 200. An upstream 404 is treated as a missing profile and isn't retried.

## Requirement 8: Azure deployment

### Hosting

Azure Container Apps to run the application in a container, using revisions and traffic splitting for controlled releases. Container Apps load-balances across replicas automatically, so scaling out is a rule change (CPU/concurrent requests), not an architecture change.

This assumes shared persistent storage - the assessment's in-memory store/snapshot wouldn't survive multiple replicas or scale-to-zero and would need replacing first. I'd keep at least one warm replica if response-time consistency mattered.

App Service would also work for a plain Web API, but I prefer Container Apps' container/revision workflow. Functions suit event-driven work, not this hosting model; AKS is more operational overhead than this service needs.

### Secure access

API Management would front the API with per-consumer policies, rate limits, versioning and docs, with its subscription keys mapped to backend auth rather than two unrelated key checks. The backend would sit in a private Container Apps environment reachable only through APIM's private connectivity.

Front Door would be added later if global routing, edge WAF or multi-region became a requirement - not needed for a single-region service on day one.

### Secrets

Key Vault would store the API key hash and other secrets, accessed via a managed identity scoped to only what it needs. Secrets stay out of the image and repo, supplied via Key Vault references or the configuration provider at runtime. Rotation would overlap old and new keys briefly to avoid interrupting clients.

### Infrastructure and releases

I would use Bicep to define the Container Apps environment, registry, Key Vault, monitoring and API Management resources, with parameters for each environment.

A GitHub Actions pipeline would:

1. Restore, build, run tests and check dependencies.
2. Build and scan the container image, then push it to Azure Container Registry with the commit SHA as its tag.
3. Authenticate to Azure using OIDC, avoiding a long-lived deployment secret.
4. Deploy infrastructure and a new application revision to a test environment.
5. Run smoke tests before approval for production.
6. Gradually route production traffic to the new revision, monitor errors and latency, and return traffic to the previous revision if needed.

Health probes, smoke tests and Azure Monitor/Log Analytics alerting would be added before production deployment.

The cutover itself would be a red/green release: deploy the new revision alongside the current one at 0% traffic, smoke-test it directly, then shift traffic over gradually while keeping the old revision warm. If anything looks wrong, traffic reverts immediately instead of waiting on a rollback deployment.

## Tests

Run tests from the solution root:

```powershell
dotnet test
```

Unit tests cover category rules and controller behaviour. Integration tests use `WebApplicationFactory` to exercise endpoints and simulate external API responses, retries and failures.

I prioritised status transitions and category validation because they are central to the brief. The profile tests also check that unavailable enrichment does not prevent Rocks from being returned.

## What I would improve next

- **Storage, concurrency and caching:** replace the store with Cosmos DB, partitioned by `memberId` for fast per-member queries, with `_etag` for optimistic concurrency. Add a Redis cache in front of reads (`GET /rocks` is read far more than written).
- **Idempotent creation:** support an idempotency key so a repeated POST doesn't create duplicates.
- **Dependency protection:** add a circuit breaker so a failing profile service fails fast instead of being hammered.
- **Monitoring:** distributed traces plus latency (p50/p95/p99), response codes split by success/4xx/5xx, uptime, CPU/memory, retry counts and circuit-breaker state, via Azure Monitor/App Insights.
- **Alerting:** real alert rules, not just dashboards - error rate, p95 latency, circuit breaker trips, CPU/memory, plus security signals like 401 spikes and repeated validation failures from one source.
- **Authentication:** short term, per-consumer API keys tracked by owner via APIM subscriptions so one can be revoked without affecting others. Longer term, move to OAuth2/JWT via Entra ID - short-lived scoped tokens instead of a long-lived shared secret.
- **Security and code-quality scanning:** required PR checks - dependency/vulnerability scanning, static analysis (`dotnet format`, analyzers), and a container image scan - repeated again just before release.
- **Penetration and end-to-end testing:** a pen test against the deployed environment (auth bypass, injection, OWASP API Top 10) plus true E2E tests against a real deployment rather than in-process `WebApplicationFactory`.
- **Health and load testing:** liveness/readiness probes and expected-traffic load tests; an enrichment outage would be monitored separately since Rocks still return without it.

This section describes the production design only - infrastructure, pipeline and health endpoints aren't implemented in this assessment.
