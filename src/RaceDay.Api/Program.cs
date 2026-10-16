using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

//Start Of Project

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("RaceDay")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=RaceDay;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<RaceDayDbContext>(o => o.UseSqlServer(connectionString));//SQL Server
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>

{
    //API Cookie
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddAuthentication("RaceDaySession")
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>("RaceDaySession", _ => { });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Organiser", p => p.RequireAuthenticatedUser().RequireRole(Roles.Organiser));
    options.AddPolicy("Participant", p => p.RequireAuthenticatedUser().RequireRole(Roles.Participant));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    //Start of API Swagger
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "RaceDay API", Version = "v1", Description = "Race event management API. Login first to establish a server-side session." });
    c.AddSecurityDefinition("cookie", new OpenApiSecurityScheme
    {
        Name = ".RaceDay.Session", Type = SecuritySchemeType.ApiKey, In = ParameterLocation.Cookie,
        Description = "Session cookie created by POST /api/auth/login."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "cookie" } }, Array.Empty<string>() }
    });
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseSession(); // Must run before authentication because auth reads the session.
app.UseAuthentication();
app.UseAuthorization();

static int CurrentUserId(ClaimsPrincipal p) => int.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);
static bool IsValidRole(string role) => role is Roles.Organiser or Roles.Participant;
static object PublicUser(User u) => new { u.UserId, u.FullName, u.Email, u.Role, u.CreatedAt };
static async Task<bool> OwnsEvent(RaceDayDbContext db, int eventId, int userId) =>
    await db.EventOrganisers.AnyAsync(x => x.EventId == eventId && x.UserId == userId);

//Authentication
app.MapPost("/api/auth/register", async (RegisterRequest req, RaceDayDbContext db, HttpContext ctx) =>
{
    if (string.IsNullOrWhiteSpace(req.FullName) || string.IsNullOrWhiteSpace(req.Email) || req.Password.Length < 8 || !IsValidRole(req.Role))
        return Results.BadRequest(new { message = "Full name, valid email, password (minimum 8 characters), and role Organiser or Participant are required." });
    var email = req.Email.Trim().ToLowerInvariant();
    if (await db.Users.AnyAsync(x => x.Email == email))
        return Results.Conflict(new { message = "An account with this email already exists." });
    var user = new User { FullName = req.FullName.Trim(), Email = email, Role = req.Role, PasswordHash = new Microsoft.AspNetCore.Identity.PasswordHasher<User>().HashPassword(null!, req.Password) };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    ctx.Session.SetInt32("UserId", user.UserId);
    ctx.Session.SetString("Role", user.Role);
    ctx.Session.SetString("Email", user.Email);
    return Results.Created("/api/users/me", PublicUser(user));
}).WithTags("Authentication").WithSummary("Register a new Organiser or Participant").WithDescription("Creates an account, securely hashes the password, and starts a server-side session. Returns 201 Created or 400/409.");

app.MapPost("/api/auth/login", async (LoginRequest req, RaceDayDbContext db, HttpContext ctx) =>
{
    var email = req.Email.Trim().ToLowerInvariant();
    var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email);
    if (user is null || new Microsoft.AspNetCore.Identity.PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, req.Password) == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
        return Results.Unauthorized();
    ctx.Session.Clear();
    ctx.Session.SetInt32("UserId", user.UserId);
    ctx.Session.SetString("Role", user.Role);
    ctx.Session.SetString("Email", user.Email);
    return Results.Ok(new { user = PublicUser(user), message = "Login successful. Session established." });
}).WithTags("Authentication").WithSummary("Log in and establish a server-side session").WithDescription("Returns 200 on success or 401 for invalid credentials.");

app.MapPost("/api/auth/logout", (HttpContext ctx) =>
{
    ctx.Session.Clear();
    return Results.NoContent();
}).RequireAuthorization().WithTags("Authentication").WithSummary("End the current session");

//Start of Profile 
app.MapGet("/api/users/me", async (ClaimsPrincipal principal, RaceDayDbContext db) =>
{
    var user = await db.Users.FindAsync(CurrentUserId(principal));
    return user is null ? Results.NotFound() : Results.Ok(PublicUser(user));
}).RequireAuthorization().WithTags("User Profile").WithSummary("View the signed-in user's profile");

app.MapPut("/api/users/me", async (UpdateProfileRequest req, ClaimsPrincipal principal, RaceDayDbContext db) =>
{
    var user = await db.Users.FindAsync(CurrentUserId(principal));
    if (user is null) return Results.NotFound();
    var email = req.Email.Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(req.FullName) || string.IsNullOrWhiteSpace(email)) return Results.BadRequest(new { message = "Name and email are required." });
    if (await db.Users.AnyAsync(x => x.Email == email && x.UserId != user.UserId)) return Results.Conflict(new { message = "Email is already in use." });
    user.FullName = req.FullName.Trim(); user.Email = email;
    await db.SaveChangesAsync();
    return Results.Ok(PublicUser(user));
}).RequireAuthorization().WithTags("User Profile").WithSummary("Update the signed-in user's profile");
//End of Profile

// Events: public read, organiser CRUD limited to events assigned to that organiser.
app.MapGet("/api/events", async (RaceDayDbContext db) =>
    Results.Ok(await db.Events.AsNoTracking().OrderBy(x => x.EventDate).Select(x => new { x.EventId, x.Name, x.Description, x.Location, x.EventDate, x.CreatedAt }).ToListAsync()))
    .AllowAnonymous().WithTags("Events").WithSummary("List all events");

app.MapGet("/api/events/{id:int}", async (int id, RaceDayDbContext db) =>
{
    var e = await db.Events.AsNoTracking().Where(x => x.EventId == id).Select(x => new { x.EventId, x.Name, x.Description, x.Location, x.EventDate, x.CreatedAt }).SingleOrDefaultAsync();
    return e is null ? Results.NotFound() : Results.Ok(e);
}).AllowAnonymous().WithTags("Events").WithSummary("Get event details");

app.MapPost("/api/events", async (EventRequest req, ClaimsPrincipal p, RaceDayDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Location)) return Results.BadRequest(new { message = "Name and location are required." });
    var e = new RaceEvent { Name = req.Name.Trim(), Description = req.Description, Location = req.Location.Trim(), EventDate = req.EventDate.Date };
    db.Events.Add(e); await db.SaveChangesAsync();
    db.EventOrganisers.Add(new EventOrganiser { EventId = e.EventId, UserId = CurrentUserId(p) });
    await db.SaveChangesAsync();
    return Results.Created($"/api/events/{e.EventId}", e);
}).RequireAuthorization("Organiser").WithTags("Events").WithSummary("Create an event and assign the current organiser");

app.MapPut("/api/events/{id:int}", async (int id, EventRequest req, ClaimsPrincipal p, RaceDayDbContext db) =>
{
    if (!await OwnsEvent(db, id, CurrentUserId(p))) return Results.NotFound(new { message = "Event not found or not managed by this organiser." });
    var e = await db.Events.FindAsync(id);
    if (e is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Location)) return Results.BadRequest(new { message = "Name and location are required." });
    e.Name = req.Name.Trim(); e.Description = req.Description; e.Location = req.Location.Trim(); e.EventDate = req.EventDate.Date;
    await db.SaveChangesAsync(); return Results.Ok(e);
}).RequireAuthorization("Organiser").WithTags("Events").WithSummary("Update an event managed by the current organiser");

app.MapDelete("/api/events/{id:int}", async (int id, ClaimsPrincipal p, RaceDayDbContext db) =>
{
    if (!await OwnsEvent(db, id, CurrentUserId(p))) return Results.NotFound(new { message = "Event not found or not managed by this organiser." });
    var e = await db.Events.FindAsync(id); if (e is null) return Results.NotFound();
    db.Events.Remove(e); await db.SaveChangesAsync(); return Results.NoContent();
}).RequireAuthorization("Organiser").WithTags("Events").WithSummary("Delete an event managed by the current organiser");


//Categories
app.MapGet("/api/events/{id:int}/categories", async (int id, RaceDayDbContext db) =>
{
    if (!await db.Events.AnyAsync(x => x.EventId == id)) return Results.NotFound();
    return Results.Ok(await db.Categories.AsNoTracking().Where(x => x.EventId == id).OrderBy(x => x.DistanceKm).ToListAsync());
}).AllowAnonymous().WithTags("Categories").WithSummary("List categories for an event");

app.MapPost("/api/events/{id:int}/categories", async (int id, CategoryRequest req, ClaimsPrincipal p, RaceDayDbContext db) =>
{
    if (!await OwnsEvent(db, id, CurrentUserId(p))) return Results.NotFound(new { message = "Event not found or not managed by this organiser." });
    if (string.IsNullOrWhiteSpace(req.Name) || req.DistanceKm <= 0 || req.MaxParticipants <= 0) return Results.BadRequest(new { message = "Name, positive distance, and positive capacity are required." });
    var c = new Category { EventId = id, Name = req.Name.Trim(), DistanceKm = req.DistanceKm, MaxParticipants = req.MaxParticipants };
    db.Categories.Add(c);
    try { await db.SaveChangesAsync(); } catch (DbUpdateException) { return Results.Conflict(new { message = "Category name already exists for this event." }); }
    return Results.Created($"/api/categories/{c.CategoryId}", c);
}).RequireAuthorization("Organiser").WithTags("Categories").WithSummary("Create a category for an event managed by the organiser");

app.MapPut("/api/categories/{id:int}", async (int id, CategoryRequest req, ClaimsPrincipal p, RaceDayDbContext db) =>
{
    var c = await db.Categories.Include(x => x.Event).ThenInclude(x => x.EventOrganisers).SingleOrDefaultAsync(x => x.CategoryId == id);
    if (c is null || !c.Event.EventOrganisers.Any(x => x.UserId == CurrentUserId(p))) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(req.Name) || req.DistanceKm <= 0 || req.MaxParticipants <= 0) return Results.BadRequest(new { message = "Name, positive distance, and positive capacity are required." });
    c.Name = req.Name.Trim(); c.DistanceKm = req.DistanceKm; c.MaxParticipants = req.MaxParticipants;
    try { await db.SaveChangesAsync(); } catch (DbUpdateException) { return Results.Conflict(new { message = "Category name already exists for this event." }); }
    return Results.Ok(c);
}).RequireAuthorization("Organiser").WithTags("Categories").WithSummary("Update a category");

app.MapDelete("/api/categories/{id:int}", async (int id, ClaimsPrincipal p, RaceDayDbContext db) =>
{
    var c = await db.Categories.Include(x => x.Event).ThenInclude(x => x.EventOrganisers).SingleOrDefaultAsync(x => x.CategoryId == id);
    if (c is null || !c.Event.EventOrganisers.Any(x => x.UserId == CurrentUserId(p))) return Results.NotFound();
    db.Categories.Remove(c); await db.SaveChangesAsync(); return Results.NoContent();
}).RequireAuthorization("Organiser").WithTags("Categories").WithSummary("Delete a category");

// Enrolments
app.MapPost("/api/categories/{id:int}/enrolments", async (int id, EnrolmentRequest req, ClaimsPrincipal p, RaceDayDbContext db) =>
{
    var userId = CurrentUserId(p);
    var category = await db.Categories.Include(x => x.Enrolments).SingleOrDefaultAsync(x => x.CategoryId == id);
    if (category is null) return Results.NotFound(new { message = "Category not found." });
    if (category.Enrolments.Any(x => x.ParticipantId == userId)) return Results.Conflict(new { message = "You are already enrolled in this category." });
    if (category.Enrolments.Count >= category.MaxParticipants) return Results.Conflict(new { message = "This category is full." });
    var enrolment = new Enrolment { CategoryId = id, ParticipantId = userId, EmergencyContact = req.EmergencyContact };
    db.Enrolments.Add(enrolment);
    try { await db.SaveChangesAsync(); } catch (DbUpdateException) { return Results.Conflict(new { message = "Could not create enrolment; check duplicate or capacity." }); }
    return Results.Created($"/api/enrolments/{enrolment.EnrolmentId}", new { enrolment.EnrolmentId, enrolment.CategoryId, enrolment.ParticipantId, enrolment.EnrolmentDate, enrolment.EmergencyContact });
}).RequireAuthorization("Participant").WithTags("Event Enrolments").WithSummary("Enrol the current participant in a category");

app.MapGet("/api/users/me/enrolments", async (ClaimsPrincipal p, RaceDayDbContext db) =>
{
    var uid = CurrentUserId(p);
    return Results.Ok(await db.Enrolments.AsNoTracking().Where(x => x.ParticipantId == uid)
        .Select(x => new { x.EnrolmentId, x.EnrolmentDate, x.EmergencyContact, x.CategoryId, CategoryName = x.Category.Name, x.Category.EventId, EventName = x.Category.Event.Name, x.Category.Event.EventDate })
        .ToListAsync());
}).RequireAuthorization("Participant").WithTags("Event Enrolments").WithSummary("List the current participant's enrolments");

app.MapDelete("/api/enrolments/{id:int}", async (int id, ClaimsPrincipal p, RaceDayDbContext db) =>
{
    var uid = CurrentUserId(p);
    var enrolment = await db.Enrolments.SingleOrDefaultAsync(x => x.EnrolmentId == id && x.ParticipantId == uid);
    if (enrolment is null) return Results.NotFound(new { message = "Enrolment not found for this participant." });
    db.Enrolments.Remove(enrolment); await db.SaveChangesAsync(); return Results.NoContent();
}).RequireAuthorization("Participant").WithTags("Event Enrolments").WithSummary("Cancel one of the current participant's enrolments");

app.MapGet("/api/events/{id:int}/enrolments", async (int id, ClaimsPrincipal p, RaceDayDbContext db) =>
{
    if (!await OwnsEvent(db, id, CurrentUserId(p))) return Results.NotFound(new { message = "Event not found or not managed by this organiser." });
    return Results.Ok(await db.Enrolments.AsNoTracking().Where(x => x.Category.EventId == id)
        .Select(x => new { x.EnrolmentId, x.ParticipantId, ParticipantName = x.Participant.FullName, x.Participant.Email, x.CategoryId, CategoryName = x.Category.Name, x.EnrolmentDate, x.EmergencyContact })
        .ToListAsync());
}).RequireAuthorization("Organiser").WithTags("Event Enrolments").WithSummary("View enrolments for an event managed by the organiser");

// Results
app.MapGet("/api/enrolments/{id:int}/result", async (int id, ClaimsPrincipal p, RaceDayDbContext db) =>
{
    var uid = CurrentUserId(p);
    var result = await db.Results.AsNoTracking().Where(x => x.EnrolmentId == id && x.Enrolment.ParticipantId == uid)
        .Select(x => new { x.ResultId, x.EnrolmentId, x.FinishTime, x.Position }).SingleOrDefaultAsync();
    return result is null ? Results.NotFound() : Results.Ok(result);
}).RequireAuthorization("Participant").WithTags("Results").WithSummary("View a result belonging to the current participant");

app.MapPost("/api/enrolments/{id:int}/result", async (int id, ResultRequest req, ClaimsPrincipal p, RaceDayDbContext db) =>
{
    var enrolment = await db.Enrolments.Include(x => x.Category).ThenInclude(x => x.Event).ThenInclude(x => x.EventOrganisers)
        .SingleOrDefaultAsync(x => x.EnrolmentId == id);
    if (enrolment is null || !enrolment.Category.Event.EventOrganisers.Any(x => x.UserId == CurrentUserId(p))) return Results.NotFound(new { message = "Enrolment not found for an event managed by this organiser." });
    if (req.FinishTime <= TimeSpan.Zero || req.Position <= 0) return Results.BadRequest(new { message = "Finish time and position must be positive." });
    if (await db.Results.AnyAsync(x => x.EnrolmentId == id)) return Results.Conflict(new { message = "A result already exists for this enrolment; use PUT to update it." });
    var result = new Result { EnrolmentId = id, FinishTime = req.FinishTime, Position = req.Position };
    db.Results.Add(result); await db.SaveChangesAsync();
    return Results.Created($"/api/enrolments/{id}/result", result);
}).RequireAuthorization("Organiser").WithTags("Results").WithSummary("Record a result for an enrolment in an event managed by the organiser");

app.MapPut("/api/results/{id:int}", async (int id, ResultRequest req, ClaimsPrincipal p, RaceDayDbContext db) =>
{
    var result = await db.Results.Include(x => x.Enrolment).ThenInclude(x => x.Category).ThenInclude(x => x.Event).ThenInclude(x => x.EventOrganisers)
        .SingleOrDefaultAsync(x => x.ResultId == id);
    if (result is null || !result.Enrolment.Category.Event.EventOrganisers.Any(x => x.UserId == CurrentUserId(p))) return Results.NotFound();
    if (req.FinishTime <= TimeSpan.Zero || req.Position <= 0) return Results.BadRequest(new { message = "Finish time and position must be positive." });
    result.FinishTime = req.FinishTime; result.Position = req.Position; await db.SaveChangesAsync();
    return Results.Ok(result);
}).RequireAuthorization("Organiser").WithTags("Results").WithSummary("Update a result for an event managed by the organiser");

app.MapGet("/api/events/{id:int}/results", async (int id, ClaimsPrincipal p, RaceDayDbContext db) =>
{
    if (!await OwnsEvent(db, id, CurrentUserId(p))) return Results.NotFound(new { message = "Event not found or not managed by this organiser." });
    return Results.Ok(await db.Results.AsNoTracking().Where(x => x.Enrolment.Category.EventId == id)
        .OrderBy(x => x.Position)
        .Select(x => new { x.ResultId, x.EnrolmentId, x.FinishTime, x.Position, ParticipantName = x.Enrolment.Participant.FullName, CategoryName = x.Enrolment.Category.Name })
        .ToListAsync());
}).RequireAuthorization("Organiser").WithTags("Results").WithSummary("List results for an event managed by the organiser");

app.MapGet("/", () => Results.Ok(new { service = "RaceDay API", swagger = "/swagger" })).AllowAnonymous();
app.Run();

public partial class Program { }
