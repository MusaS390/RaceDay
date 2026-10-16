namespace RaceDay.Api.Models;

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = Roles.Participant;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<EventOrganiser> OrganisedEvents { get; set; } = new List<EventOrganiser>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

public class RaceEvent
{
    public int EventId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Location { get; set; } = "";
    public DateTime EventDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<EventOrganiser> EventOrganisers { get; set; } = new List<EventOrganiser>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}

public class EventOrganiser
{
    public int EventId { get; set; }
    public RaceEvent Event { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public RaceEvent Event { get; set; } = null!;
    public string Name { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int ParticipantId { get; set; }
    public User Participant { get; set; } = null!;
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    public string? EmergencyContact { get; set; }
    public Result? Result { get; set; }
}

public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public Enrolment Enrolment { get; set; } = null!;
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
}

public class RefreshToken
{
    public int RefreshTokenId { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Token { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
