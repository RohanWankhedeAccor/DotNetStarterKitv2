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
cd D:\RWANKHEDE\Claude\DotNetStarterKitv2
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

All developer guidance is in `.claude/`:

| Document | Purpose |
|----------|---------|
| **[setup.md](./.claude/setup.md)** | Local development setup (database, migrations, troubleshooting) |
| **[CLAUDE.md](./.claude/CLAUDE.md)** | Architecture overview & tech stack reference |
| **[rules/development.md](./.claude/rules/development.md)** | Coding standards, naming conventions, git workflow, code review |
| **[rules/api.md](./.claude/rules/api.md)** | REST API design, endpoints, error handling, CORS |
| **[rules/frontend.md](./.claude/rules/frontend.md)** | React patterns, state management, component design, testing |
| **[rules/security.md](./.claude/rules/security.md)** | Authentication, secrets, CORS, security headers, compliance |

**Start here:** [setup.md](./.claude/setup.md)

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
├── .claude/                       # AI guidance & documentation
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

### ✅ Phase 1: Architecture
- Clean Architecture with CQRS
- Feature-sliced design patterns
- Dependency injection configuration
- Error handling middleware

### ✅ Phase 2: Domain Layer
- Base entity with audit trails (CreatedAt, CreatedBy, ModifiedAt, ModifiedBy, IsDeleted)
- Domain entities: User, Role, UserRole, Product, Project
- Custom domain exceptions
- Zero external dependencies

### ✅ Phase 3: Infrastructure
- EF Core 9 with code-first migrations
- Entity type configurations with soft-delete
- Design-time DbContext factory
- Audit field population

### ✅ Phase 4: Application Layer
- CQRS handlers (Commands & Queries)
- FluentValidation validators
- AutoMapper profiles
- Pagination support
- MediatR pipeline behaviors

### ✅ Phase 5: API Layer
- 12 REST endpoints (Users, Products)
- Global exception handling → ProblemDetails
- Security headers middleware
- CORS configuration
- OpenAPI/Swagger (disabled pending Phase 2)

### ✅ Phase 6: React Frontend
- Feature-sliced architecture
- TanStack Query for server state
- Redux Toolkit for auth state
- React Hook Form + Zod validation
- Tailwind CSS styling
- Responsive design (mobile, tablet, desktop)

### ✅ Phase 7: Documentation
- Comprehensive setup guide
- Architecture documentation
- Development standards & conventions
- API design guidelines
- Frontend patterns guide
- Security best practices

### ✅ Phase 8: Database
- Initial migration applied
- All tables created (Users, Roles, UserRoles, Products, Projects)
- Audit fields on all entities
- Soft-delete pattern enabled

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

See [rules/development.md](./.claude/rules/development.md) for detailed conventions.

### 5. Open Pull Request
Check the PR checklist in [rules/development.md](./.claude/rules/development.md):
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

See [setup.md](./.claude/setup.md) for detailed database instructions.

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
- Review [rules/security.md](./.claude/rules/security.md)

### Production (Phase 2+)
- Use Azure Key Vault for secrets
- Enforce HTTPS
- Implement authentication (Entra ID)
- Enable rate limiting
- Configure CORS for production domain
- Run security headers

---

## 🚢 Deployment

### Current Status (Phase 1)
**NOT production-ready:**
- Authentication is mocked
- No email service
- No caching (Redis)
- No job queue (Hangfire)

### Future Phases
- **Phase 2**: Entra ID authentication
- **Phase 3**: Email service + Hangfire jobs
- **Phase 4**: Redis caching + SignalR real-time
- **Phase 5**: Admin dashboard
- **Phase 6**: Staging/Production deployment guides

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

See [setup.md](./.claude/setup.md#common-issues--solutions) for more.

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

See [rules/development.md](./.claude/rules/development.md) for:
- Naming conventions
- Code organization
- Commit message format
- Pull request checklist
- Code review standards
- Testing guidelines

---

## 📝 License

This project is created for educational and development purposes.

---

## 🚀 Next Steps

1. **New to the project?**
   - Read [setup.md](./.claude/setup.md) for local setup
   - Review [CLAUDE.md](./.claude/CLAUDE.md) for architecture overview
   - Check [rules/development.md](./.claude/rules/development.md) for conventions

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
