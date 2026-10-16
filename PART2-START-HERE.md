# Start here — Part 2

This is a working implementation scaffold based on the supplied Part 1 ZIP and endpoint-plan PDF.

1. Open `RaceDay.sln` in Visual Studio 2022.
2. Ensure .NET 8 and SQL Server/LocalDB are installed.
3. Run `docs/raceday.sql` in SSMS (it recreates the database, so back up any existing RaceDay database first).
4. Check the connection string in `src/RaceDay.Api/appsettings.json`.
5. Build the solution and run `RaceDay.Api`.
6. Open Swagger, register an Organiser account and create an event.
7. Register a Participant account in a separate browser profile/session and enrol in a category.
8. Run `dotnet test RaceDay.sln`.
9. Push to GitHub and inspect the Actions results.

Important: this environment does not have the .NET SDK installed, so the project could not be compiled or tests executed here. Treat the first local build as a required verification step and resolve any SDK/database environment differences before submitting. Do not claim the CI build is green until GitHub reports success.
