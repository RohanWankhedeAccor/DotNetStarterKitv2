# Development Scripts

Quick commands to start and stop the full DotNetStarterKitv2 application.

## ⚡ Quick Start
```powershell
# Windows
.\scripts\dev\start-app.bat     # Opens 2 windows with API + Web servers
.\scripts\dev\stop-app.bat      # Or just close the windows

# macOS/Linux
bash scripts/dev/start-app.sh   # Starts both servers in same terminal
bash scripts/dev/stop-app.sh    # Stops both servers
```

Then open your browser:
- **API:** https://localhost:5001
- **Web:** http://localhost:5173

## Start the App

### Windows (Recommended) ⭐
```powershell
.\scripts\dev\start-app.bat
```

This opens **two new command windows**:
1. **[Window 1]** API Server — ASP.NET Core on https://localhost:5001
2. **[Window 2]** Web Server — React Vite on http://localhost:5173

The servers will auto-start and stay running. Don't close the windows until you're done.

**Verification:**
- API: Visit https://localhost:5001/health in browser
- Web: Visit http://localhost:5173 in browser

### macOS / Linux / Git Bash
```bash
bash scripts/dev/start-app.sh
```

Note: On macOS/Linux, both servers run in the same terminal. Use `Ctrl+C` to stop.

## Stop the App

### Windows — Option A (Recommended)
Simply **close both command windows**. The servers will shut down gracefully.

### Windows — Option B (Force Stop)
```powershell
.\scripts\dev\stop-app.bat
```

This kills all `dotnet.exe` and `node.exe` processes. Use if servers don't respond.

### macOS / Linux / Git Bash
```bash
bash scripts/dev/stop-app.sh
```

Or press `Ctrl+C` in the terminal window where `start-app.sh` is running.

---

## What These Scripts Do

### `start-app.bat` / `start-app.sh`
1. ✅ Builds the API project (`dotnet build`)
2. ✅ Starts ASP.NET Core on port 5001
3. ✅ Installs/updates npm dependencies
4. ✅ Starts Vite dev server on port 5173
5. 📍 Shows URLs where the app is accessible

### `stop-app.bat` / `stop-app.sh`
1. ✅ Kills all `dotnet.exe` processes (API)
2. ✅ Kills all `node.exe` processes (Vite)
3. ✅ Reports when complete

---

## Troubleshooting

### Port Already in Use
If you see `Port 5001 in use` or similar:

**Windows:**
```powershell
# Find what's using the port
netstat -ano | findstr :5001

# Kill the process by PID
taskkill /F /PID <PID>
```

**macOS/Linux:**
```bash
# Find what's using the port
lsof -i :5001

# Kill the process
kill -9 <PID>
```

### Dependencies Not Installed
If you see npm errors, run:
```bash
cd src/Web
npm install
```

### API Connection Errors
Ensure the API is running before opening the web UI. Check:
- https://localhost:5001/swagger (should load)
- Browser console for CORS errors
- Vite proxy is configured in `vite.config.ts`

---

## Environment Variables

Create `.env.development` in `src/Web/` for frontend config:
```
VITE_API_BASE_URL=https://localhost:5001
```

(The Vite proxy handles this automatically in dev mode)

---

**Pro tip:** Use `npm run dev` directly in `src/Web/` to run only the frontend, or `dotnet run --project src/Api` for just the API.
