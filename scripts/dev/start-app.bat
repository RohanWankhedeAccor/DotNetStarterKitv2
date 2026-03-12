@echo off
REM Start DotNetStarterKitv2 - both API and Web servers
REM Usage: .\scripts\dev\start-app.bat

echo.
echo [92m+---------------------------------------------+[0m
echo [92m^| Starting DotNetStarterKitv2[0m
echo [92m+---------------------------------------------+[0m
echo.

setlocal enabledelayedexpansion

REM Get project root
cd /d "%~dp0..\..\"
set PROJECT_ROOT=%CD%

echo [94m[1/2][0m Starting ASP.NET Core API on https://localhost:5001...
call dotnet build src\Api\Api.csproj > nul 2>&1
start "DotNetStarterKit API" cmd /k "cd /d %PROJECT_ROOT% && dotnet run --project src\Api --no-build"
echo       ^(opening in new window^)
timeout /t 3 /nobreak > nul

echo [94m[2/2][0m Starting React Vite dev server on http://localhost:5173...
cd /d "%PROJECT_ROOT%\src\Web"
start "DotNetStarterKit Web" cmd /k "npm run dev"
echo       ^(opening in new window^)
timeout /t 3 /nobreak > nul

echo.
echo [92m✓ App is starting![0m
echo.
echo 📍 API:  https://localhost:5001
echo 📍 Web:  http://localhost:5173
echo.
echo To stop: Close the command windows or run .\scripts\dev\stop-app.bat
echo.
