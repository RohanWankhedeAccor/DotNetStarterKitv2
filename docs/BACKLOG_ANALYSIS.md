# Backlog Analysis — Features & Design Patterns
**Date:** 2026-03-16

---

## Part 1: Feature Audit

### Results

| # | Feature | Verdict |
|---|---------|---------|
| 1 | Correlation ID / Request Tracing | **Partial** |
| 2 | Configuration / Options Pattern | **Absent** |
| 3 | Audit Fields | **Present ✅** |
| 4 | Audit Trail (dedicated log table) | **Absent** |
| 5 | Soft Delete | **Present ✅** |
| 6 | Pagination / Filtering / Sorting | **Partial** |
| 7 | Caching Abstraction | **Absent** |
| 8 | Email / Notification Service | **Absent** |
| 9 | File Storage Abstraction | **Absent** |
| 10 | External API Client Wrapper | **Partial** |
| 11 | Background Job Support | **Absent** |
| 12 | Feature Flags | **Absent** |
| 13 | Sensitive Data Masking | **Absent** |

### Detail

**1. Correlation ID / Request Tracing — Partial**
`traceId` is emitted in ProblemDetails error responses via `Activity.Current?.Id ?? context.TraceIdentifier`, and `UseSerilogRequestLogging` logs method/path/status/elapsed. But no `X-Correlation-Id` header is read or written, no Serilog `LogContext.PushProperty("CorrelationId", ...)` enricher exists, and trace IDs only appear on error responses — not on every log line.
Key files: `ExceptionHandlerMiddleware.cs`, `ApplicationBuilderExtensions.cs`

**2. Configuration / Options Pattern — Absent**
Config values are read directly from `IConfiguration` via raw string keys (`configuration["Jwt:SecretKey"]`, etc.). No `*Options.cs` / `*Settings.cs` classes, no `services.Configure<T>()` calls, no `IOptions<T>` injection anywhere.
Key files: `InfrastructureServiceExtensions.cs`, `ServiceCollectionExtensions.cs`

**3. Audit Fields — Present ✅**
`BaseEntity` carries `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`. `ApplicationDbContext.SaveChangesAsync` automatically stamps them via `ICurrentUserService` + `IDateTimeService`. All entity configurations enforce `IsRequired/HasMaxLength` on the string fields.
Key files: `BaseEntity.cs`, `ApplicationDbContext.cs` (lines 99–124)

**4. Audit Trail — Absent**
Only the current state of each record is tracked (via audit fields). No `AuditLogs` table, no EF Core interceptor, no history of what changed from what to what.
Key files: `ApplicationDbContext.cs` (DbSet properties confirm no AuditLog)

**5. Soft Delete — Present ✅**
`BaseEntity.IsDeleted` with `Delete()` / `Restore()` methods. `HasQueryFilter(e => !e.IsDeleted)` applied in every entity configuration. Soft deletes also automatically update `ModifiedAt/ModifiedBy`.
Key files: `BaseEntity.cs` (lines 62–87), all `*Configuration.cs` files

**6. Pagination / Filtering / Sorting — Partial**
`PagedRequest` + `PagedResponse<T>` models are solid. `GetUsersQuery` + handler implement `Skip/Take` with `CountAsync`. However, sort order is hard-coded (`OrderBy(u => u.Email)`) — callers cannot change it. No filtering (`searchTerm`, status, date range) exists anywhere.
Key files: `PagedRequest.cs`, `PagedResponse.cs`, `GetUsersQueryHandler.cs` (line 38)

**7. Caching Abstraction — Absent**
No `IMemoryCache`, `IDistributedCache`, `ICacheService` or any cache provider registered. Not referenced in any `.csproj` or DI extension.

**8. Email / Notification Service — Absent**
No `IEmailService`, no SMTP/SendGrid/MailKit references anywhere. Listed as Phase 2+ in `CLAUDE.md`.

**9. File Storage Abstraction — Absent**
No file upload/download routes, no blob storage packages, no `IFileStorageService` interface.

**10. External API Client Wrapper — Partial**
No `IHttpClientFactory` or typed HTTP clients registered. `AzureAdTokenValidator.cs` instantiates `HttpDocumentRetriever` directly in its constructor to fetch OIDC metadata — a raw point-dependency, not an abstracted wrapper.
Key file: `AzureAdTokenValidator.cs` (lines 47–51)

**11. Background Job Support — Absent**
No Hangfire, Quartz.NET, `IHostedService`, or `BackgroundService`. Listed as Phase 2+ in `CLAUDE.md`.

**12. Feature Flags — Absent**
No `Microsoft.FeatureManagement`, no `FeatureManagement` config section, no `IFeatureManager` usage.

**13. Sensitive Data Masking — Absent**
No Serilog destructuring policies, no `IDestructuringPolicy`, no masking enrichers. `LoggingBehavior` only logs the MediatR type name (not payloads), which avoids accidental PII leakage — but this is omission, not active masking.

---

## Part 2: Design Patterns Audit

### Results

| # | Pattern | Verdict |
|---|---------|---------|
| 1 | Clean Architecture | **Present ✅** |
| 2 | CQRS | **Present ✅** |
| 3 | Dependency Injection | **Present ✅** |
| 4 | Middleware Pattern | **Present ✅** |
| 5 | Repository Pattern | **Absent** (deliberate EF-as-repo — to be replaced per user preference) |
| 6 | Unit of Work | **Partial** (implicit via DbContext — to be made explicit) |
| 7 | Options Pattern | **Absent** |
| 8 | Result Pattern | **Absent** |
| 9 | Strategy Pattern | **Partial** |
| 10 | Factory Pattern | **Partial** |
| 11 | Adapter Pattern | **Present ✅** |
| 12 | Pipeline / Chain of Responsibility | **Present ✅** |

### Detail

**1. Clean Architecture — Present ✅**
Four separate projects with compiler-enforced one-way dependencies: `Domain` → `Application` → `Infrastructure` → `Api`. Domain has zero external packages. Api is the sole composition root.

**2. CQRS — Present ✅**
Commands (write) and Queries (read) in separate namespaces. All query handlers use `AsNoTracking()` + DTO projection via `Select()`. MediatR pipeline: `ValidationBehavior` → `LoggingBehavior` → handler.

**3. Dependency Injection — Present ✅**
`Program.cs` has 4 lines delegating to extension methods — zero inline registrations. All handlers receive interfaces, never concrete types. Auto-discovery via assembly scanning.

**4. Middleware Pattern — Present ✅**
`ExceptionHandlerMiddleware` (domain exceptions → RFC 9457 ProblemDetails) and `SecurityHeadersMiddleware`. Correct pipeline order: exception handler → security headers → CORS → auth.

**5. Repository Pattern — Absent (to be implemented)**
No `IRepository<T>` exists. Handlers access data directly through `IApplicationDbContext`. User preference: implement combined Repository + Unit of Work pattern (Task #12).

**6. Unit of Work — Partial (to be made explicit)**
EF Core `DbContext` acts as the implicit unit of work. `IApplicationDbContext.SaveChangesAsync()` is the commit point. No explicit `IUnitOfWork` interface. User preference: explicit `IUnitOfWork` with typed repositories.

**7. Options Pattern — Absent**
Raw `IConfiguration` string-key reads. No strongly-typed options classes. (Backlog Task #2 — highest priority.)

**8. Result Pattern — Absent**
No `Result<T>` or discriminated union type. Errors signaled exclusively via exceptions. Handlers return raw DTOs.

**9. Strategy Pattern — Partial**
`IPasswordHasher`, `ITokenService`, `IAzureAdTokenValidator` are strategy contracts by design. Each has only one concrete implementation — no runtime strategy selection yet.

**10. Factory Pattern — Partial**
Only `ApplicationDbContextFactory` (EF Core design-time tooling hook) exists. No domain-level entity factories.

**11. Adapter Pattern — Present ✅**
Four adapters: `IApplicationDbContext` (EF Core), `IDateTimeService` (clock), `ICurrentUserService` (ClaimsPrincipal), `IAzureAdTokenValidator` (Microsoft.IdentityModel OIDC).

**12. Pipeline / Chain of Responsibility — Present ✅**
`ValidationBehavior` → `LoggingBehavior` → handler. Validation runs all validators in parallel and short-circuits on failure. Both registered as open generic behaviors via `AddOpenBehavior`.

---

## Part 3: Backlog (Full List)

| Task # | Item | Category | Priority Tier |
|--------|------|----------|---------------|
| #2 | Configuration / Options Pattern | Pattern | **Tier 1** |
| #1 | Correlation ID / Request Tracing | Feature | **Tier 1** |
| #11 | Sensitive Data Masking | Feature | **Tier 1** |
| #12 | Repository + Unit of Work | Pattern | **Tier 2** |
| #13 | Result Pattern | Pattern | **Tier 2** |
| #4 | Pagination Filtering + Sorting | Feature | **Tier 3** |
| #3 | Audit Trail | Feature | **Tier 3** |
| #8 | External API Client Wrapper | Feature | **Tier 3** |
| #5 | Caching Abstraction | Feature | **Tier 3** |
| #15 | Factory Pattern | Pattern | **Tier 4** |
| #14 | Strategy Pattern | Pattern | **Tier 4** |
| #6 | Email / Notification Service | Feature | **Tier 4** |
| #7 | File Storage Abstraction | Feature | **Tier 4** |
| #9 | Background Job Support | Feature | **Tier 4** |
| #10 | Feature Flags | Feature | **Tier 4** |

---

## Part 4: Priority Rationale

### Tier 1 — Quick Wins (Low effort, High value)

| Priority | Task | Effort | Reason |
|----------|------|--------|--------|
| 1st | Options Pattern (#2) | ~2h | Fixes raw string config reads — security smell. Unblocks Strategy Pattern. |
| 2nd | Correlation ID (#1) | ~1h | Single middleware + Serilog enricher. Every log line gets a trace ID. |
| 3rd | Sensitive Data Masking (#11) | ~1h | Single Serilog destructuring policy. Cheap security insurance. |

### Tier 2 — Foundational Refactors (Critical, do early)

| Priority | Task | Effort | Reason |
|----------|------|--------|--------|
| 4th | Repository + Unit of Work (#12) | ~6-8h | User preference. Affects every handler. Harder to retrofit later. |
| 5th | Result Pattern (#13) | ~4-6h | Changes all handler return types. Do before building more endpoints. |

### Tier 3 — Feature Completeness

| Priority | Task | Effort | Reason |
|----------|------|--------|--------|
| 6th | Pagination Filtering + Sorting (#4) | ~2h | Direct API/frontend value. |
| 7th | Audit Trail (#3) | ~3-4h | Compliance — who changed what. |
| 8th | External API Client Wrapper (#8) | ~1-2h | IHttpClientFactory correctness. |
| 9th | Caching Abstraction (#5) | ~2-3h | Performance foundation. |

### Tier 4 — Defer

| Priority | Task | Dependency |
|----------|------|-----------|
| 10th | Factory Pattern (#15) | After Repository + Unit of Work |
| 11th | Strategy Pattern (#14) | After Options Pattern |
| 12th | Email Service (#6) | Phase 2+ |
| 13th | File Storage (#7) | Phase 2+ |
| 14th | Background Jobs (#9) | Phase 2+ |
| 15th | Feature Flags (#10) | Lowest urgency |

### Key Dependencies
```
Options Pattern (#2)
  └── Strategy Pattern (#14)

Repository + Unit of Work (#12)
  └── Factory Pattern (#15)

Result Pattern (#13)
  └── all future feature endpoints
```

---

*Generated by Claude Code — DotNetStarterKitv2 session 2026-03-16*
