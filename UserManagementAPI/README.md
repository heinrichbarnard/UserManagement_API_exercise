# User Management API

This is a small API I made while learning ASP.NET Core. It lets you add, view, change, and delete users. The user data is kept in memory, so it goes away when the API stops.

## Running the API

You need the .NET 10 SDK. The API also needs a token before it will start. In PowerShell, set one and run the project:

```powershell
$env:Authentication__ApiToken = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet run --project .\UserManagementAPI.csproj --urls http://localhost:5000
```

Keep that PowerShell window open. In a second window, set the same token and run the smoke test:

```powershell
$env:Authentication__ApiToken = "<the token you used above>"
.\tests\smoke-test.ps1
```

You can also try the endpoints in Postman. Add this header to each request:

```text
Authorization: Bearer <your token>
```

The API returns `401 Unauthorized` if the token is missing or incorrect. Don't put a real token in the source files or commit it to the repository.

## Endpoints

| Method | Path | What it does |
| --- | --- | --- |
| GET | `/users` | Gets all users |
| GET | `/users/{id}` | Gets one user |
| POST | `/users` | Adds a user |
| PUT | `/users/{id}` | Changes a user's name and email |
| DELETE | `/users/{id}` | Deletes a user |

For example, a new user needs a name and a valid email address. Names can be up to 100 characters and email addresses up to 254 characters. Two users can't have the same email address, even if the letter casing is different. If the request isn't valid, the API returns `400 Bad Request`.

The ID is a GUID. Looking up or changing an ID that isn't in the list returns `404 Not Found`. Trying to add an email address that's already being used returns `409 Conflict`.

## A few things I learned

- I used minimal API routes in `Program.cs` for the different HTTP methods.
- `UserStore` holds the user list and handles adding, finding, changing, and removing users.
- I added middleware for the token check, request logging, and unexpected errors.
- The smoke test checks the main endpoints, including a few invalid requests.

This is just a learning project. Since the users are only stored in memory, they aren't saved when the API restarts. The shared token is also a simple way to protect a demo API, not a full login system. A real application would need a database and a proper identity provider.
