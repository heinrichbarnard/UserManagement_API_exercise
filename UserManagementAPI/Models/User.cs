namespace UserManagementAPI.Models;

public sealed record User(
    Guid Id,
    string Name,
    string Email,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
