@echo off
REM Stop DotNetStarterKitv2 - kill both API and Web servers
REM Usage: .\scripts\dev\stop-app.bat

echo.
echo [92m+---------------------------------------------+[0m
echo [92m^| Stopping DotNetStarterKitv2[0m
echo [92m+---------------------------------------------+[0m
echo.

echo [94m[1/2][0m Stopping API servers...
taskkill /F /IM dotnet.exe 2>nul
timeout /t 1 /nobreak > nul
echo [92m✓ API stopped[0m

echo [94m[2/2][0m Stopping Vite dev server...
taskkill /F /IM node.exe 2>nul
timeout /t 1 /nobreak > nul
echo [92m✓ Web server stopped[0m

echo.
echo [92m✓ All servers stopped[0m
echo.
pause
