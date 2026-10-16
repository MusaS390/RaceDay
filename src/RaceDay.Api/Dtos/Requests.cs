namespace RaceDay.Api.Dtos;

public record RegisterRequest(string FullName, string Email, string Password, string Role);
public record LoginRequest(string Email, string Password);
public record UpdateProfileRequest(string FullName, string Email);
public record EventRequest(string Name, string? Description, string Location, DateTime EventDate);
public record CategoryRequest(string Name, decimal DistanceKm, int MaxParticipants);
public record EnrolmentRequest(string? EmergencyContact);
public record ResultRequest(TimeSpan FinishTime, int Position);
