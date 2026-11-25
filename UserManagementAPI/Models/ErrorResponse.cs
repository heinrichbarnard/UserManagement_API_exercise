using System.Text.Json.Serialization;

namespace UserManagementAPI.Models;

public sealed record ErrorResponse(
    string Error,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, string[]>? Details = null);
