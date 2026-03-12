# Local Development Setup

Complete guide to running DotNetStarterKitv2 locally.

---

## Prerequisites

### Required
- **.NET 9 SDK** — [Download](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- **Node.js 18+** — [Download](https://nodejs.org/)
- **SQL Server** — Local instance or remote
  - Local instance: `TH-5CD41368G0\SQLEXPRESS`
  - Database: `DotNetStarterKitV2`

### Optional but Recommended
- **Visual Studio 2022** (Community is free) — [Download](https://visualstudio.microsoft.com/)
  - Or: **VS Code** with C# Dev Kit
- **SQL Server Management Studio (SSMS)** — For database inspection
- **Postman** or **Thunder Client** — For API testing

---

## Step 1: Clone & Restore Dependencies

```bash
# Navigate to project
cd D:\RWANKHEDE\Claude\DotNetStarterKitv2

# Restore NuGet packages (dotnet run does this automatically)
dotnet restore
```

---

## Step 2: Database Setup

### Check Connection String
Edit `src/Api/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=TH-5CD41368G0\SQLEXPRESS;Initial Catalog=DotNetStarterKitV2;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

### Apply Migrations
```bash
cd D:\RWANKHEDE\Claude\DotNetStarterKitv2

# Update database with initial migration
dotnet ef database update \
  --project src/Infrastructure \
  --startup-project src/Api
```

**Output**: Tables created in `DotNetStarterKitV2` database ✅

---

## Step 3: Run Backend (ASP.NET Core)

```bash
cd D:\RWANKHEDE\Claude\DotNetStarterKitv2

# Build all projects
dotnet build

# Run tests (always do this first!)
dotnet test --no-build --verbosity normal

# Start API server
dotnet run --project src/Api
```

**Output**:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
      Now listening on: http://localhost:5000
```

### Test the API
```bash
# Health check
curl https://localhost:5001/health --insecure

# Get users (paginated)
curl https://localhost:5001/api/v1/users --insecure

# Create a user
curl -X POST https://localhost:5001/api/v1/users \
  --insecure \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "fullName": "John Doe",
    "password": "Password123!"
  }'
```

---

## Step 4: Run Frontend (React)

**In a new terminal:**

```bash
cd D:\RWANKHEDE\Claude\DotNetStarterKitv2\src\Web

# Install dependencies (one-time)
npm install

# Start dev server with hot reload
npm run dev
```

**Output**:
```
  VITE v5.0.0  ready in 123 ms

  ➜  Local:   http://localhost:5173/
  ➜  press h to show help
```

### Access the App
- Frontend: http://localhost:5173/
- API: https://localhost:5001/api/v1/
- (Swagger was disabled due to dependency issues—will re-enable in Phase 2)

---

## Running Commands

### Backend Commands

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run a specific test project
dotnet test tests/Unit/Unit.csproj

# Clean build artifacts
dotnet clean

# Create a new migration
dotnet ef migrations add <MigrationName> \
  --project src/Infrastructure \
  --startup-project src/Api

# Update database
dotnet ef database update \
  --project src/Infrastructure \
  --startup-project src/Api
```

### Frontend Commands

```bash
# Install dependencies
npm install

# Dev server (hot reload)
npm run dev

# Build for production
npm run build

# Type checking
npm run type-check

# Linting
npm run lint

# Tests
npm run test
npm run test:ui   # Vitest UI

# Preview production build locally
npm run preview
```

---

## Common Issues & Solutions

### ❌ "Connection timeout"
**Problem**: Can't connect to SQL Server
**Solution**:
1. Check SQL Server is running: `sqlcmd -S TH-5CD41368G0\SQLEXPRESS -Q "SELECT 1"`
2. Verify connection string in `appsettings.Development.json`
3. Ensure database exists: `DotNetStarterKitV2`

### ❌ "DbContext already exists"
**Problem**: EF Core migration error
**Solution**: Delete `appsettings.Development.json` and let it use default, or check the connection string is correct

### ❌ "CORS error in frontend"
**Problem**: React can't call API
**Solution**:
1. Verify API is running on `https://localhost:5001`
2. Check CORS policy in `src/Api/Program.cs` allows `localhost:5173`
3. Browser may need "Allow insecure certificates" (dev only)

### ❌ "npm install hangs"
**Problem**: Dependencies taking forever to install
**Solution**:
```bash
# Clear npm cache
npm cache clean --force

# Install with legacy peer deps
npm install --legacy-peer-deps
```

### ❌ "Port 5173 already in use"
**Problem**: Another process using React dev server port
**Solution**:
```bash
# Find process on port 5173
netstat -ano | findstr :5173

# Kill it (replace PID)
taskkill /PID <PID> /F

# Or just use a different port
npm run dev -- --port 5174
```

---

## Development Workflow

### 1. Make Changes
```bash
# Backend: Edit src/Application/Features/...
# Frontend: Edit src/Web/src/features/...
```

### 2. Test Locally
```bash
# Backend: Both are running with hot reload
# Frontend: Auto-refreshes on file save
```

### 3. Verify Everything Works
```bash
cd D:\RWANKHEDE\Claude\DotNetStarterKitv2

# Backend
dotnet build
dotnet test

# Frontend
cd src/Web
npm run lint
npm run type-check
npm run test
```

### 4. Create a Git Commit
```bash
git add .
git commit -m "feat: add new feature"
# See development.md for commit message conventions
```

---

## Debugging

### Debug Backend (Visual Studio)
1. Open `DotNetStarterKitv2.sln`
2. Set breakpoint in handler
3. Press **F5** to start with debugger
4. Call API endpoint → hit breakpoint

### Debug Backend (VS Code)
Install **C# Dev Kit** extension, then press **F5**

### Debug Frontend (Chrome DevTools)
1. Open http://localhost:5173/
2. Press **F12** to open DevTools
3. **Sources** tab for breakpoints
4. **Network** tab to inspect API calls
5. **Redux DevTools** extension shows state changes

---

## Database Inspection

Open **SQL Server Management Studio (SSMS)**:
1. Connect to `TH-5CD41368G0\SQLEXPRESS`
2. Expand `Databases` → `DotNetStarterKitV2`
3. View tables: `Users`, `Roles`, `UserRoles`, `Products`, `Projects`
4. Check audit columns: `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`, `IsDeleted`

---

## Next Steps

- Review **api.md** — HTTP endpoint conventions
- Review **frontend.md** — React patterns used
- Review **development.md** — Coding standards & PR process
- Check **CLAUDE.md** for architecture overview

---

**Happy coding! 🚀**
