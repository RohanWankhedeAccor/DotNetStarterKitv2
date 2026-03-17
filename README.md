# DotNetStarterKitv2

A **production-grade full-stack starter kit** for building modern web applications with **.NET 9 + React 19 + TypeScript + SQL Server**.

Built on **Clean Architecture**, **CQRS**, and **feature-sliced design** principles for maximum maintainability and scalability.

---

## 🚀 Quick Start

### Prerequisites
- **.NET 9 SDK** — [Download](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- **Node.js 18+** — [Download](https://nodejs.org/)
- **SQL Server Express** — Local instance: `TH-5CD41368G0\SQLEXPRESS`
- Database name: `DotNetStarterKitV2` (create before running)

### Run Locally (5 minutes)

**Terminal 1 — Backend:**
```bash
cd DotNetStarterKitv2
dotnet build
dotnet test --no-build
dotnet run --project src/Api
# API runs at https://localhost:5001
```

**Terminal 2 — Frontend:**
```bash
cd src/Web
npm install
npm run dev
# Frontend runs at http://localhost:5173
```

Visit **http://localhost:5173** to see the app!

---

## 📚 Documentation

| Document | Purpose |
|----------|---------|
| **[docs/architecture.md](./docs/architecture.md)** | Architecture overview & tech stack reference |
| **[docs/BACKLOG_ANALYSIS.md](./docs/BACKLOG_ANALYSIS.md)** | Feature audit and remaining backlog |

---

## 🎯 Architecture Overview

### Folder Structure
```
DotNetStarterKitv2/
├── src/
│   ├── Domain/                    # Business logic (zero dependencies)
│   ├── Application/               # CQRS handlers, validators, DTOs
│   ├── Infrastructure/            # EF Core, persistence, services
│   ├── Api/                       # ASP.NET Core, endpoints, middleware
│   └── Web/                       # React 19 + TypeScript + Vite
├── tests/
│   ├── Unit/                      # Domain + Application tests
│   └── Integration/               # API + Infrastructure tests
├── docs/                          # Project documentation
├── .github/workflows/             # GitHub Actions CI/CD
└── DotNetStarterKitv2.sln
```

### Dependency Flow
```
Domain ← Application ← Infrastructure ← Api
  ↑          ↑              ↑             ↑
  No external dependencies  Implements    Orchestrates
                            interfaces    all layers
```

### Technology Stack

| Layer | Stack |
|-------|-------|
| **API** | ASP.NET Core 9 Minimal APIs |
| **CQRS** | MediatR 12 |
| **ORM** | EF Core 9 with SQL Server |
| **Validation** | FluentValidation |
| **Mapping** | AutoMapper |
| **Logging** | Serilog (with Application Insights support) |
| **Frontend** | React 19 + TypeScript + Vite |
| **State** | Redux Toolkit (auth) + TanStack Query (server) |
| **Forms** | React Hook Form + Zod |
| **UI** | Tailwind CSS + Lucide Icons |
| **Testing BE** | xUnit + NSubstitute + FluentAssertions |
| **Testing FE** | Vitest + React Testing Library + MSW |

---

## 🏗️ Features Included

### ✅ Architecture & Patterns
- **Clean Architecture** — Domain → Application → Infrastructure → Api, compiler-enforced one-way dependencies
- **CQRS** — Commands / Queries via MediatR; one handler file per request
- **Repository + Unit of Work** — `IRepository<T>` / `IUnitOfWork` abstractions over EF Core
- **Result Pattern** — `Result<T>` / `Result` discriminated unions; handlers return typed errors instead of throwing exceptions
- **Options Pattern** — all config sections bound to strongly-typed classes via `IOptions<T>`
- **MediatR Pipeline Behaviors** — `ValidationBehavior` (FluentValidation) + `LoggingBehavior` (request/duration)

### ✅ Domain Layer
- Base entity with audit fields (`CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`) and soft-delete (`IsDeleted`)
- Domain entities: **User**, **Role**, **UserRole**, **Product**, **Project**
- Domain methods on entities (`Activate`, `Deactivate`, `Delete`, `Restore`, `Update`)
- Zero external dependencies

### ✅ Infrastructure & Services
- **EF Core 9** code-first migrations, SQL Server
- **Audit Trail** — `AuditLog` entity captures every insert/update/delete with old/new JSON snapshots
- **Soft-delete** global query filters on all entities
- **Serilog** structured logging with request logging and Correlation ID enrichment on every log line
- **Correlation ID middleware** — reads/generates `X-Correlation-Id`, propagates to response header and all logs
- **Sensitive data masking** — `[Sensitive]` attribute + Serilog destructuring policy redacts PII before log emission
- **ICacheService** — in-memory cache abstraction with prefix-based invalidation
- **IEmailService** — `SmtpEmailService` (MailKit) + `LoggingEmailService` dev stub
- **IFileStorageService** — local-disk implementation with cloud-ready interface (`UploadAsync`, `DownloadAsync`, `GetUrlAsync`)
- **IHttpApiClient** — typed HTTP client with `CorrelationIdDelegatingHandler` and `ExternalApiException` → 502

### ✅ Application Features
- **Products CRUD** — full Clean Architecture stack with create, get, list (filtered/sorted/paged), delete
- **Users CRUD** — create, get by ID, list with filtering/sorting/pagination, assign roles
- **Authentication** — Azure AD / Entra ID with JWT + HttpOnly cookie session
- **RBAC** — fine-grained permissions (`users.view`, `products.create`, `roles.assign`, etc.) embedded in JWT claims
- **Pagination + Filtering + Sorting** — `searchTerm`, `sortBy`, `sortDescending` on all list endpoints

### ✅ API Layer
- 16 REST endpoints (Auth, Users, Products) as ASP.NET Core 9 Minimal APIs
- `ResultExtensions` — maps `Result<T>` to correct HTTP status (200/201/204/401/403/404/409/502)
- Global exception handling → RFC 9457 ProblemDetails
- Security headers middleware (`X-Frame-Options`, `X-Content-Type-Options`, HSTS, etc.)
- CORS pre-configured for `localhost:5173`
- OpenAPI / Swagger

### ✅ React Frontend
- Feature-sliced architecture (`features/users/`, `features/products/`)
- TanStack Query for server state, Redux Toolkit for auth state
- React Hook Form + Zod validation
- Azure AD SSO via MSAL — silent login, HttpOnly cookie session
- Tailwind CSS, responsive design

### ✅ Testing — 118 tests, all green
- **73 unit tests** — NSubstitute mocks, FluentAssertions, IUnitOfWork pattern, Result assertions
- **45 integration tests** — `CustomWebApplicationFactory` with SQLite in-memory, full HTTP round-trips covering 401/403/404/409/201/204 paths
- Frontend: Vitest + React Testing Library + MSW

### ✅ CI/CD
- GitHub Actions — backend (build + test) + frontend (type-check + lint + test) gate jobs
- Branch protection on `main` and `develop` requires passing CI + PR approval

---

## 🔄 Development Workflow

### 1. Create a Feature Branch
```bash
git checkout -b feat/my-feature
```

### 2. Make Changes
Edit files in the appropriate layer:
- **Backend**: `src/[Domain|Application|Infrastructure|Api]/`
- **Frontend**: `src/Web/src/features/[feature-name]/`

### 3. Test Locally
```bash
# Backend
dotnet build
dotnet test
dotnet run --project src/Api

# Frontend (in new terminal)
cd src/Web
npm run lint
npm run type-check
npm run test
npm run dev
```

### 4. Commit & Push
```bash
git add .
git commit -m "feat(users): add user creation endpoint"
git push origin feat/my-feature
```

**Commit message format:** `type(scope): subject`
- **type**: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`
- **scope**: Feature name (users, products, auth)
- **subject**: Imperative mood, no period, max 50 chars

### 5. Open Pull Request
- ✅ Code builds without warnings
- ✅ All tests pass
- ✅ Naming conventions followed
- ✅ No hardcoded secrets
- ✅ Documentation updated

---

## 📊 Database Setup

### SQL Server Express Connection
```
Server: TH-5CD41368G0\SQLEXPRESS
Database: DotNetStarterKitV2
Authentication: Integrated Security (Windows)
```

### Create Database
```bash
sqlcmd -S TH-5CD41368G0\SQLEXPRESS -Q "CREATE DATABASE DotNetStarterKitV2"
```

### Apply Migrations
```bash
dotnet ef database update \
  --project src/Infrastructure \
  --startup-project src/Api
```

### Create New Migration
```bash
dotnet ef migrations add MigrationName \
  --project src/Infrastructure \
  --startup-project src/Api
```

See `docs/` for detailed database instructions.

---

## 🧪 Testing

### Backend Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Unit/Unit.csproj

# Run with coverage
dotnet test /p:CollectCoverage=true
```

**Target: 80%+ coverage** for Domain + Application layers.

### Frontend Tests
```bash
cd src/Web

# Run all tests
npm run test

# Run with coverage
npm run test -- --coverage

# Watch mode
npm run test -- --watch

# UI mode
npm run test:ui
```

**Target: 70%+ coverage** for components + hooks.

---

## 🔐 Security

### Development
- Never commit `.env` files (create `.env.example`)
- Use `dotnet user-secrets` for local secrets
- Validate all input on backend

### Production
- Use Azure Key Vault for secrets
- Enforce HTTPS
- Enable rate limiting
- Configure CORS for production domain

---

## 🚢 Deployment

### Current Status
**Functionally complete for dev/staging. Not yet production-hardened:**
- No background job queue (Hangfire / Quartz)
- No feature flags (`Microsoft.FeatureManagement`)
- No Redis distributed cache (currently in-memory)
- No SignalR real-time updates

### Remaining Phases
- **Next**: Frontend Products feature slice (pages, hooks, tests)
- **Later**: Background Jobs, Feature Flags, Redis, SignalR, Admin dashboard

---

## 💡 Tips & Tricks

### Useful Commands

**Backend:**
```bash
# Clean build
dotnet clean && dotnet build

# Run with debugger
dotnet run --project src/Api

# Check for outdated packages
dotnet outdated

# Check for security vulnerabilities
dotnet list package --vulnerable
```

**Frontend:**
```bash
# Type check only (no build)
npm run type-check

# Lint and fix
npm run lint -- --fix

# Build for production
npm run build

# Preview production build locally
npm run preview
```

### VS Code Extensions (Recommended)
- **C# Dev Kit** — .NET development
- **REST Client** — Test endpoints directly in VS Code
- **Tailwind CSS IntelliSense** — CSS class autocomplete
- **Thunder Client** — API testing (alternative to Postman)

### Common Issues

**"Connection timeout"**
→ Check SQL Server is running: `sqlcmd -S TH-5CD41368G0\SQLEXPRESS -Q "SELECT 1"`

**"Port 5173 already in use"**
→ Kill process: `taskkill /PID <PID> /F` or use different port: `npm run dev -- --port 5174`

**"npm install hangs"**
→ Clear cache: `npm cache clean --force` then `npm install --legacy-peer-deps`

See `docs/` for more.

---

## 📖 Learning Resources

### Architecture
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [Feature-Sliced Design](https://feature-sliced.design/)

### .NET
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [MediatR](https://github.com/jbogard/MediatR)

### React
- [React Documentation](https://react.dev/)
- [TanStack Query](https://tanstack.com/query/latest)
- [Redux Toolkit](https://redux-toolkit.js.org/)
- [React Hook Form](https://react-hook-form.com/)

### Tools
- [Visual Studio 2022](https://visualstudio.microsoft.com/)
- [VS Code](https://code.visualstudio.com/)
- [SQL Server Management Studio (SSMS)](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms)

---

## 🤝 Contributing

- Follow naming conventions and commit message format (`type(scope): subject`)
- All tests must pass before raising a PR
- PRs target `develop`; `main` is release-only

---

## 📝 License

This project is created for educational and development purposes.

---

## 🚀 Next Steps

1. **New to the project?**
   - Review `docs/architecture.md` for architecture overview
   - Run the Quick Start above

2. **Ready to code?**
   - Create a feature branch
   - Follow the patterns in existing code
   - Reference rule files as you work

3. **Want to understand the code?**
   - Start with `src/Domain/Entities/` (business logic)
   - Then `src/Application/Features/` (handlers & validators)
   - Then `src/Api/Endpoints/` (HTTP routes)
   - Finally `src/Web/src/features/` (React components)

---

**Built with ❤️ for clean, maintainable code. Happy coding! 🚀**
