Stop any running API process, rebuild the .NET solution, and restart the API server.

Steps:
1. Find and kill any process currently listening on port 5031 (the API port)
2. Run `dotnet build src/Api` from the project root `D:\RWANKHEDE\Claude\DotNetStarterKitv2` and show the result
3. If the build succeeds, run `dotnet run --project src/Api --no-build` in the background
4. Wait a few seconds, then verify the API is healthy by calling `curl -s http://localhost:5031/health`
5. Report the final status clearly — either "API is running ✅" or the build/startup error

Project root: D:\RWANKHEDE\Claude\DotNetStarterKitv2
API port: 5031
Health endpoint: http://localhost:5031/health
