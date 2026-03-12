Stop any running Vite dev server and restart the React frontend.

Steps:
1. Find and kill any process currently listening on port 5173 (the Vite dev server port)
2. Change directory to `D:\RWANKHEDE\Claude\DotNetStarterKitv2\src\Web`
3. Run `npm run dev` in the background
4. Wait a few seconds, then verify the frontend is up by calling `curl -s -o /dev/null -w "%{http_code}" http://localhost:5173/`
5. Report the final status clearly — either "React UI is running at http://localhost:5173 ✅" or the startup error

Project web root: D:\RWANKHEDE\Claude\DotNetStarterKitv2\src\Web
Frontend port: 5173
