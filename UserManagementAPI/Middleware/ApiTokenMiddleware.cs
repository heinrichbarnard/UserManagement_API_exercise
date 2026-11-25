using System.Security.Cryptography;
using System.Text;
using Microsoft.Net.Http.Headers;
using UserManagementAPI.Models;

namespace UserManagementAPI.Middleware;

public sealed record ApiTokenOptions(byte[] ExpectedTokenHash);

public sealed class ApiTokenMiddleware(
    RequestDelegate next,
    ApiTokenOptions options,
    ILogger<ApiTokenMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var authorization = context.Request.Headers[HeaderNames.Authorization].ToString();
        var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : string.Empty;

        var suppliedTokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        if (token.Length == 0 ||
            !CryptographicOperations.FixedTimeEquals(options.ExpectedTokenHash, suppliedTokenHash))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers[HeaderNames.WWWAuthenticate] = "Bearer";
            await context.Response.WriteAsJsonAsync(new ErrorResponse("Unauthorized."));
            logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode);
            return;
        }

        await next(context);
    }
}
