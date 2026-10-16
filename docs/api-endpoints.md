# RaceDay API Endpoint Plan — Part 1 reference for Part 2

All routes are rooted at `/api`. JSON is used for request and response bodies. Session login is maintained with an HTTP-only `.RaceDay.Session` cookie. Public endpoints are marked **Public**; protected routes require an authenticated session and the named role.

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Register account and start session | Public; role must be Organiser or Participant | `fullName, email, password, role` | 201 Created; 400 invalid input; 409 email exists |
| POST | `/api/auth/login` | Verify credentials and establish session | Public | `email, password` | 200 user summary; 401 invalid credentials |
| POST | `/api/auth/logout` | Clear current session | Any authenticated user | None | 204 No Content |
| GET | `/api/users/me` | View own profile | Organiser or Participant | None | 200 profile; 401 unauthenticated; 404 missing user |
| PUT | `/api/users/me` | Update own profile | Organiser or Participant | `fullName, email` | 200 updated profile; 400 invalid input; 409 email used |
| GET | `/api/events` | List events | Public | None | 200 array of events |
| GET | `/api/events/{id}` | View event details | Public | None | 200 event; 404 missing event |
| POST | `/api/events` | Create event and assign organiser | Organiser | `name, description, location, eventDate` | 201 event; 400 invalid input; 401/403 access failure |
| PUT | `/api/events/{id}` | Update own managed event | Organiser assigned to event | `name, description, location, eventDate` | 200 event; 404 missing/unmanaged event |
| DELETE | `/api/events/{id}` | Delete own managed event | Organiser assigned to event | None | 204; 404 missing/unmanaged event |
| GET | `/api/events/{id}/categories` | List event categories | Public | None | 200 categories; 404 missing event |
| POST | `/api/events/{id}/categories` | Create category for managed event | Organiser assigned to event | `name, distanceKm, maxParticipants` | 201 category; 400 invalid; 409 duplicate name |
| PUT | `/api/categories/{id}` | Update category | Organiser who manages category's event | `name, distanceKm, maxParticipants` | 200 category; 400 invalid; 404 missing/unmanaged; 409 duplicate |
| DELETE | `/api/categories/{id}` | Delete category | Organiser who manages category's event | None | 204; 404 missing/unmanaged |
| POST | `/api/categories/{id}/enrolments` | Enrol current participant in category | Participant | `emergencyContact` (optional) | 201 enrolment; 404 category missing; 409 duplicate/full |
| GET | `/api/users/me/enrolments` | List own enrolments | Participant | None | 200 enrolments |
| DELETE | `/api/enrolments/{id}` | Cancel own enrolment | Participant who owns enrolment | None | 204; 404 not owned/missing |
| GET | `/api/events/{id}/enrolments` | View enrolments for managed event | Organiser assigned to event | None | 200 enrolments; 404 missing/unmanaged |
| GET | `/api/enrolments/{id}/result` | View own result | Participant who owns enrolment | None | 200 result; 404 no result/not owned |
| POST | `/api/enrolments/{id}/result` | Record result for enrolment | Organiser assigned to event | `finishTime` (`hh:mm:ss`), `position` | 201 result; 400 invalid; 404 missing/unmanaged; 409 already exists |
| PUT | `/api/results/{id}` | Update result | Organiser assigned to event | `finishTime`, `position` | 200 result; 400 invalid; 404 missing/unmanaged |
| GET | `/api/events/{id}/results` | List event results ordered by position | Organiser assigned to event | None | 200 results; 404 missing/unmanaged |

## Implementation notes
- Passwords are hashed using ASP.NET Core `PasswordHasher<TUser>`.
- The API uses server-side ASP.NET Core session state; login/register establish the session and logout clears it.
- Role checks are enforced by authorization policies. Organisers may only modify events linked to them through `EventOrganisers`.
- Enrolments are unique by `(CategoryId, ParticipantId)` and limited by `MaxParticipants`.
- A result is unique per enrolment.
- This table records the API's planned behavior; check the actual implementation and update the plan only when a deliberate design change is approved.
