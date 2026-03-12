# DotNetStarterKitv2 - Project Overview

A **production-grade full-stack starter kit** combining:
- **.NET Core 9** with Clean Architecture & CQRS
- **React 19** with feature-sliced architecture
- **SQL Server** with EF Core 9 migrations
- **TypeScript** throughout the entire stack

---

## 🎯 Core Priorities

1. **SPEED** — Performance in dev and production
2. **MODERN PATTERNS** — DDD, CQRS, feature-sliced architecture
3. **TYPE SAFETY** — End-to-end TypeScript/C# with strict modes
4. **MINIMAL DEPENDENCIES** — Only proven, essential libraries

---

## 📁 Architecture Overview

```
DotNetStarterKitv2/
├── .claude/                    # This folder - AI assistant guidance
├── .github/workflows/          # GitHub Actions CI/CD
├── src/
│   ├── Domain/                 # Clean Architecture core
│   │   ├── Entities/          # User, Role, Product, Project
│   │   ├── Enums/             # Status enums
│   │   ├── Exceptions/        # Domain exceptions
│   │   └── Common/            # BaseEntity, interfaces
│   │
│   ├── Application/            # CQRS layer
│   │   ├── Features/          # Commands, Queries, Validators
│   │   │   ├── Users/         # Create, Get, List operations
│   │   │   └── Products/      # Create, Get, List operations
│   │   └── Common/            # Behaviors, Pagination, Mappings
│   │
│   ├── Infrastructure/         # EF Core, Services, DI
│   │   ├── Persistence/       # DbContext, Configurations
│   │   ├── Identity/          # Auth services
│   │   └── Services/          # DateTime, etc.
│   │
│   ├── Api/                    # Minimal APIs
│   │   ├── Endpoints/         # /api/v1/ routes
│   │   ├── Middleware/        # Exception handler, security
│   │   └── Extensions/        # DI registration
│   │
│   └── Web/                    # React Vite app
│       ├── src/
│       │   ├── features/      # Users, Products (feature-sliced)
│       │   ├── lib/           # Redux, React Query, API client
│       │   ├── layouts/       # Navigation layout
│       │   ├── styles/        # Tailwind globals
│       │   ├── App.tsx        # Routing
│       │   └── main.tsx       # Entry point
│       └── package.json
│
└── tests/
    ├── Unit/                   # Domain + Application tests
    └── Integration/            # API + Infrastructure tests
```

---

## 🔑 Key Technologies

### Backend
| Layer | Technology |
|-------|-----------|
| **API** | ASP.NET Core 9 Minimal APIs |
| **CQRS** | MediatR 12 |
| **ORM** | EF Core 9, SQL Server |
| **Validation** | FluentValidation |
| **Mapping** | AutoMapper |
| **Logging** | Serilog |
| **Testing** | xUnit, NSubstitute |

### Frontend
| Category | Technology |
|----------|-----------|
| **Framework** | React 19 + TypeScript |
| **Build** | Vite 5 |
| **Routing** | React Router v7 |
| **Server State** | TanStack Query v5 |
| **Client State** | Redux Toolkit |
| **Forms** | React Hook Form + Zod |
| **UI** | Tailwind CSS + Lucide Icons |
| **Notifications** | Sonner (toast) |
| **HTTP** | Axios |
| **Testing** | Vitest + React Testing Library |

### Database
- **SQL Server** (local: `TH-5CD41368G0\SQLEXPRESS`)
- **Database Name**: `DotNetStarterKitV2`
- **Migrations**: EF Core code-first
- **Pattern**: Soft-delete with `IsDeleted` flag

---

## 🏛️ Architecture Rules

### Dependency Rule
**Domain ← Application ← Infrastructure ← API**

- Domain has ZERO external dependencies
- Application depends only on Domain & interfaces
- Infrastructure implements interfaces from Application
- API orchestrates all layers

### Clean Architecture Constraints
- ✅ DTOs at all boundaries (never expose entities)
- ✅ Soft-delete via global query filters
- ✅ Audit trails (CreatedAt, CreatedBy, ModifiedAt, ModifiedBy)
- ✅ DateTimeOffset everywhere (UTC)
- ✅ Async/await throughout
- ✅ `AsNoTracking()` for reads
- ✅ Projection via `Select()` to DTOs

### CQRS Pattern
- **Commands**: Mutations (create, update, delete)
  - Handler per file
  - Validator per command
  - MediatR IRequest<T>
- **Queries**: Reads (get, list, search)
  - Handler per file
  - No side effects
  - MediatR IRequest<T>
- **Handlers**: Pure functions with dependencies injected

### Feature-Sliced Architecture (Frontend)
```
features/
├── users/
│   ├── types.ts          # Domain types
│   ├── hooks/            # useUsers, useCreateUser
│   ├── components/       # UsersList, CreateUserForm
│   ├── pages/            # UsersPage
│   └── __tests__/        # Feature tests
```

---

## 🚀 Getting Started

See `setup.md` for detailed local setup instructions.

### Quick Start (Backend)
```bash
cd D:\RWANKHEDE\Claude\DotNetStarterKitv2
dotnet build
dotnet test
dotnet run --project src/Api
# API runs at https://localhost:5001
```

### Quick Start (Frontend)
```bash
cd src/Web
npm install
npm run dev
# Frontend runs at http://localhost:5173
```

---

## 📝 Development Rules

See the `rules/` folder for detailed guidelines:
- `api.md` — HTTP conventions, endpoint patterns
- `frontend.md` — React patterns, naming conventions
- `development.md` — General workflow & best practices
- `security.md` — Authentication, CORS, secrets

---

## 🔐 Authentication (Phase 2)

Currently using mock authentication. Phase 2 will integrate:
- **Entra ID** (Azure AD) with MSAL
- **JWT tokens** with refresh logic
- **Role-based access control** (RBAC)

See `.claude/AUTH_PLAN.md` for details.

---

## 📊 Database Schema

Tables auto-created by EF Core migrations:
- **Users** (unique email, soft-delete)
- **Roles** (unique name, soft-delete)
- **UserRoles** (junction table)
- **Products** (price decimal(18,2), soft-delete)
- **Projects** (owner FK, soft-delete)

All tables include audit fields: `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`.

---

## 🧪 Testing

### Backend
```bash
dotnet test --no-build --verbosity normal
```
- xUnit framework
- NSubstitute for mocks
- FluentAssertions for readability
- Integration tests use real DbContext

### Frontend
```bash
cd src/Web && npm run test
```
- Vitest (Vite-native test runner)
- React Testing Library
- MSW (Mock Service Worker) for API mocking

---

## 🔄 CI/CD

GitHub Actions workflow (`.github/workflows/build.yml`):
1. Build backend
2. Run backend tests
3. Build frontend
4. Run frontend tests
5. (Future) Deploy to staging

---

## 📖 Key Files to Review

- **api.md** — How API endpoints are structured
- **frontend.md** — React component patterns
- **development.md** — Coding standards & workflows
- **setup.md** — Local development setup

---

## ✨ What's Included

✅ **8 Implementation Phases** (all complete):
1. Project structure & projects
2. Domain layer with DDD
3. Infrastructure with EF Core
4. Application with CQRS
5. API with Minimal APIs
6. React frontend
7. Documentation (this)
8. Database migrations

✅ **Production-Ready Features**:
- Global exception handling → ProblemDetails
- Security headers middleware
- CORS pre-configured for localhost:5173
- Type-safe API client
- Form validation with Zod
- Pagination models
- Audit trails on all entities
- Soft-delete pattern

❌ **Not Included (Phase 2+)**:
- Authentication / Authorization
- Email service
- Caching (Redis)
- Job queue (Hangfire)
- Real-time updates (SignalR)
- Admin dashboard

---

## 🤝 Contributing

See `development.md` for pull request checklist, commit message style, and code review process.

---

**Created by Claude Code** | Based on clean architecture principles from Robert C. Martin
