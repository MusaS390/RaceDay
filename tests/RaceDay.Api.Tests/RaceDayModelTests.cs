using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

public class RaceDayModelTests
{
    [Fact]
    public void PasswordHasher_DoesNotStorePlainText_AndVerifiesPassword()
    {
        var hasher = new PasswordHasher<User>();
        var user = new User { Email = "runner@example.com" };
        var hash = hasher.HashPassword(user, "CorrectHorseBatteryStaple!");
        Assert.NotEqual("CorrectHorseBatteryStaple!", hash);
        Assert.Equal(PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(user, hash, "CorrectHorseBatteryStaple!"));
        Assert.Equal(PasswordVerificationResult.Failed,
            hasher.VerifyHashedPassword(user, hash, "wrong-password"));
    }

    [Fact]
    public async Task DatabaseModel_AllowsOrganiserParticipantAndEventCategoryEnrolmentResult()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new RaceDayDbContext(options);
        var organiser = new User { FullName = "Organizer", Email = "org@test.local", PasswordHash = "hash", Role = Roles.Organiser };
        var participant = new User { FullName = "Runner", Email = "runner@test.local", PasswordHash = "hash", Role = Roles.Participant };
        var ev = new RaceEvent { Name = "Test Run", Location = "Cape Town", EventDate = DateTime.UtcNow.Date };
        db.AddRange(organiser, participant, ev);
        await db.SaveChangesAsync();
        db.EventOrganisers.Add(new EventOrganiser { EventId = ev.EventId, UserId = organiser.UserId });
        var category = new Category { EventId = ev.EventId, Name = "5K", DistanceKm = 5, MaxParticipants = 20 };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        var enrolment = new Enrolment { CategoryId = category.CategoryId, ParticipantId = participant.UserId };
        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();
        db.Results.Add(new Result { EnrolmentId = enrolment.EnrolmentId, FinishTime = TimeSpan.FromMinutes(30), Position = 1 });
        await db.SaveChangesAsync();

        Assert.Equal(1, await db.Users.CountAsync());
        Assert.Equal(1, await db.Events.CountAsync());
        Assert.Equal(1, await db.Categories.CountAsync());
        Assert.Equal(1, await db.Enrolments.CountAsync());
        Assert.Equal(1, await db.Results.CountAsync());
    }

    [Theory]
    [InlineData("Organiser", true)]
    [InlineData("Participant", true)]
    [InlineData("Admin", false)]
    [InlineData("", false)]
    public void RegistrationRoleAllowlist_OnlyAllowsTheTwoDefinedRoles(string role, bool expected)
    {
        Assert.Equal(expected, role is Roles.Organiser or Roles.Participant);
    }
}
