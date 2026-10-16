# RaceDay – RESTful API Development

## 1. Project Overview

RaceDay is a race event management system developed using **ASP.NET Core Web API, C#, Entity Framework Core and Microsoft SQL Server**.

The purpose of the system is to allow event organisers to create and manage race events, define event categories, manage participant enrolments and record race results. Participants can browse events, register for categories, manage their profiles and view their results.

This project continues from Part 1 of the RaceDay assignment, which includes the Entity Relationship Diagram (ERD), API endpoint plan and SQL database script.

## 2. Technologies Used

- C#
- ASP.NET Core Web API
- .NET 8
- Entity Framework Core
- Microsoft SQL Server
- SQL Server Management Studio (SSMS)
- Swagger / OpenAPI
- xUnit for unit testing
- Git and GitHub
- GitHub Actions for continuous integration (CI/CD)
- Visual Studio 2022

## 3. User Roles

### Organiser

An Organiser can:

- Register and log in to the system.
- View and update their own profile.
- Create, view, update and delete events they manage.
- Create, update and delete categories for their events.
- View enrolments for their events.
- Record and update participants' finish times and finishing positions.
- View results for events they manage.

### Participant

A Participant can:

- Register and log in to the system.
- View and update their own profile.
- Browse available events and categories.
- Enrol in an event category.
- View their own enrolments.
- Cancel their own enrolments.
- View their own race results.

### Public Users

Unauthenticated users can register, log in, browse events and view event categories. Protected operations require authentication and the appropriate role.

## 4. Project Structure

The main project folders are organised as follows:

```text
RaceDay-Part1-Corrected-Submission/
│
├── .github/
│   └── workflows/
│       ├── validate-docs.yml
│       └── raceday-api-ci.yml
│
├── docs/
│   ├── erd.png
│   ├── raceday.dbml
│   ├── raceday.sql
│   ├── RaceDay_API_Endpoint_Plan.pdf
│   └── api-endpoints.md
│
├── src/
│   └── RaceDay.Api/
│       ├── Controllers/
│       ├── Data/
│       ├── Dtos/
│       ├── Models/
│       ├── Program.cs
│       ├── appsettings.json
│       └── RaceDay.Api.csproj
│
├── tests/
│   └── RaceDay.Api.Tests/
│       ├── RaceDayModelTests.cs
│       └── RaceDay.Api.Tests.csproj
│
├── RaceDay.sln
└── README.md
```

The API uses a minimal API endpoint structure in `Program.cs`. The `Models` folder contains the database entities, `Data` contains the Entity Framework Core database context, and `Dtos` contains request models.

The test project contains automated tests for selected database relationships, password hashing and role validation.

## 5. Prerequisites

Before running the application, install the following:

1. Visual Studio 2022 with the ASP.NET and web development workload.
2. .NET 8 SDK.
3. Microsoft SQL Server or SQL Server LocalDB.
4. SQL Server Management Studio (SSMS), recommended.
5. Git, if you intend to work with the repository from the command line.

## 6. Database Setup

The database is created using Microsoft SQL Server.

### Step 1: Open SQL Server Management Studio

Connect to your SQL Server or LocalDB instance.

### Step 2: Execute the SQL script

Open the following file:

`docs/raceday.sql`

Review the script before executing it. If it drops and recreates the `RaceDay` database, back up any existing data first.

Execute the script in SSMS and check the Messages window for errors.

### Step 3: Verify the database

Confirm that the database contains the tables defined in the schema, including:

- Users
- Events
- EventOrganisers
- Categories
- Enrolments
- Results
- RefreshTokens

### Step 4: Configure the connection string

Open:

`src/RaceDay.Api/appsettings.json`

The default connection string uses SQL Server LocalDB:

```json
{
  "ConnectionStrings": {
    "RaceDay": "Server=(localdb)\\MSSQLLocalDB;Database=RaceDay;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

If you use another SQL Server instance, update the connection string accordingly. Do not commit database passwords or other secrets to GitHub.

## 7. Running the Application

### Option A: Visual Studio

1. Open `RaceDay.sln` in Visual Studio 2022.
2. Restore the NuGet packages.
3. Set `RaceDay.Api` as the startup project.
4. Confirm the SQL Server connection string is correct.
5. Build the solution.
6. Press `F5` to run the API.

### Option B: Command Line

Open a terminal in the repository root and execute:

```bash
dotnet restore RaceDay.sln
dotnet build RaceDay.sln
dotnet run --project src/RaceDay.Api/RaceDay.Api.csproj
```

When the API starts successfully, open the Swagger URL shown in the terminal. The configured development HTTPS address is normally:

`https://localhost:7194/swagger`

The exact address may vary depending on your launch settings.

## 8. Authentication and Session Management

RaceDay uses server-side ASP.NET Core session management to maintain the authenticated user's identity and role.

### Registration

Use `POST /api/auth/register` to create an account.

Example request:

```json
{
  "fullName": "Test Organiser",
  "email": "organiser@example.com",
  "password": "TestPass123!",
  "role": "Organiser"
}
```

The role must be either `Organiser` or `Participant`. Passwords are hashed before being stored in the database.

### Login

Use `POST /api/auth/login` with the registered email and password.

A successful login establishes a session using the `.RaceDay.Session` cookie.

### Logout

Use `POST /api/auth/logout` to clear the current session.

### Role-Based Authorisation

Protected endpoints require an authenticated session. Organiser endpoints enforce the Organiser role, while participant enrolment and personal result endpoints enforce the Participant role.

Users must not be able to access another user's private profile, enrolments or results through these endpoints.

## 9. API Endpoints

The complete endpoint specification is available in `docs/api-endpoints.md` and the Part 1 endpoint plan PDF.

| Method | Endpoint | Purpose |
|---|---|---|
| POST | `/api/auth/register` | Register a user |
| POST | `/api/auth/login` | Log in |
| POST | `/api/auth/logout` | Log out |
| GET | `/api/users/me` | View own profile |
| PUT | `/api/users/me` | Update own profile |
| GET | `/api/events` | List events |
| GET | `/api/events/{id}` | View event details |
| POST | `/api/events` | Create an event |
| PUT | `/api/events/{id}` | Update an event |
| DELETE | `/api/events/{id}` | Delete an event |
| GET | `/api/events/{id}/categories` | List event categories |
| POST | `/api/events/{id}/categories` | Create a category |
| PUT | `/api/categories/{id}` | Update a category |
| DELETE | `/api/categories/{id}` | Delete a category |
| POST | `/api/categories/{id}/enrolments` | Enrol in a category |
| GET | `/api/users/me/enrolments` | View own enrolments |
| DELETE | `/api/enrolments/{id}` | Cancel own enrolment |
| GET | `/api/events/{id}/enrolments` | View event enrolments |
| GET | `/api/enrolments/{id}/result` | View own result |
| POST | `/api/enrolments/{id}/result` | Record a result |
| PUT | `/api/results/{id}` | Update a result |
| GET | `/api/events/{id}/results` | View event results |

### Swagger Documentation

Swagger provides an interactive interface for viewing and testing API endpoints.

Each endpoint should be checked for its request format, response status codes, authentication requirements and expected behaviour.

## 10. Unit Testing

RaceDay includes a test project using xUnit.

Run the tests with:

```bash
dotnet test RaceDay.sln
```

The initial test suite covers selected areas such as:

- Password hashing and verification.
- Database entity relationships.
- Validation of allowed user roles.

Additional tests should verify registration and login success and failure scenarios, unauthenticated access, role restrictions, event management permissions and participant enrolments.

All tests must pass locally and in GitHub Actions before submission.

## 11. GitHub Actions and CI/CD

The repository contains GitHub Actions workflows for documentation validation and API build and test automation.

The API workflow runs when changes are pushed or pull requests are opened. It restores dependencies, builds the solution and executes the automated tests.

To verify the workflow:

1. Push the latest changes to GitHub.
2. Open the repository's **Actions** tab.
3. Select the latest workflow run.
4. Check the logs for failed steps.
5. Confirm that the workflow completes successfully.

### Successful CI/CD Build

Insert a genuine screenshot of a successful green GitHub Actions build here.

![GitHub Actions successful build](docs/github-actions-success.png)

The screenshot should show the actual workflow run for this repository.

## 12. Security Considerations

- Passwords are hashed rather than stored in plain text.
- Session cookies are configured as HTTP-only.
- Protected operations require authentication.
- Role-based authorisation restricts Organiser and Participant functionality.
- Organisers are restricted to events assigned to them.
- Participants can access their own enrolments and results.
- Database constraints help prevent duplicate enrolments and duplicate results.

Before production deployment, additional measures should be implemented or verified, including secure cookie settings, CSRF protection, rate limiting, persistent session storage where required, and proper secret management.

## 13. Known Limitations and Verification

This project must be built and tested in the target development environment before it is considered complete.

The following checks are required:

- Confirm the database schema matches the Part 1 ERD.
- Confirm the SQL script executes successfully on a clean database.
- Confirm the API connects to the intended SQL Server instance.
- Confirm all planned endpoints work through Swagger.
- Confirm authentication and role restrictions work correctly.
- Confirm the full required unit test suite passes.
- Confirm GitHub Actions completes successfully.

Only claim that a test passed or a workflow succeeded after verifying the actual result.

## 14. YouTube Demonstration

**Unlisted YouTube video:**  
https://youtu.be/ieJFVvsU6z8?si=1Cfv-LQnvmoVzoZz

The demonstration should explain the database design, API structure, authentication and role-based access, Swagger endpoints, enrolment and result workflows, unit tests, and GitHub Actions.

## 15. GitHub Repository and Submission

**GitHub repository:**  
https://github.com/MusaS390/RaceDay

The repository should contain the Part 1 planning documents, the Part 2 API source code, unit tests, GitHub Actions workflows and setup instructions.

A minimum of 20 meaningful commits is required for Part 2. Commits should document genuine development progress.

---

**Project:** RaceDay RESTful API  
**Framework:** ASP.NET Core Web API  
**Language:** C#  
**Database:** Microsoft SQL Server  
**Testing:** xUnit  
**CI/CD:** GitHub Actions
