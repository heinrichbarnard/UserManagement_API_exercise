using System.Security.Cryptography;
using System.Text;
using UserManagementAPI.Middleware;
using UserManagementAPI.Models;
using UserManagementAPI.Services;

var builder = WebApplication.CreateBuilder(args);

var configuredToken = builder.Configuration["Authentication:ApiToken"];
if (string.IsNullOrWhiteSpace(configuredToken))
{
    throw new InvalidOperationException(
        "Configure a non-empty Authentication:ApiToken before starting the API.");
}

var expectedTokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(configuredToken));
builder.Services.AddSingleton(new ApiTokenOptions(expectedTokenHash));
builder.Services.AddSingleton<UserStore>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ApiTokenMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapGet("/users", (UserStore store) => Results.Ok(store.GetAll()));

app.MapGet("/users/{id:guid}", (Guid id, UserStore store) =>
{
    var user = store.Get(id);
    return user is null
        ? Results.NotFound(new ErrorResponse("User not found."))
        : Results.Ok(user);
});

app.MapPost("/users", (CreateUserRequest request, UserStore store) =>
{
    var validationError = Validate(request);
    if (validationError is not null)
    {
        return Results.BadRequest(validationError);
    }

    if (!store.TryCreate(request.Name, request.Email, out var user))
    {
        return Results.Conflict(new ErrorResponse("A user with this email already exists."));
    }

    return Results.Created($"/users/{user.Id}", user);
});

app.MapPut("/users/{id:guid}", (Guid id, UpdateUserRequest request, UserStore store) =>
{
    var validationError = Validate(request);
    if (validationError is not null)
    {
        return Results.BadRequest(validationError);
    }

    var result = store.TryUpdate(id, request.Name, request.Email, out var user);
    return result switch
    {
        UserUpdateResult.NotFound => Results.NotFound(new ErrorResponse("User not found.")),
        UserUpdateResult.EmailConflict => Results.Conflict(
            new ErrorResponse("A user with this email already exists.")),
        _ => Results.Ok(user)
    };
});

app.MapDelete("/users/{id:guid}", (Guid id, UserStore store) =>
    store.TryDelete(id)
        ? Results.NoContent()
        : Results.NotFound(new ErrorResponse("User not found.")));

app.Run();

static ErrorResponse? Validate<TRequest>(TRequest request)
{
    var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
    var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(request!);
    var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
        request!,
        validationContext,
        validationResults,
        validateAllProperties: true);

    return isValid
        ? null
        : new ErrorResponse(
            "Validation failed.",
            validationResults
                .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
                    .Select(member => new { Member = member, Message = result.ErrorMessage ?? "Invalid value." }))
                .GroupBy(error => error.Member)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.Message).Distinct().ToArray()));
}

public partial class Program;
