param(
    [string] $BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"
$token = $env:Authentication__ApiToken
if ([string]::IsNullOrWhiteSpace($token)) {
    throw "Set Authentication__ApiToken to the same value configured on the API."
}

$headers = @{ Authorization = "Bearer $token" }
$createdId = $null

function Assert-Status {
    param(
        [int] $Expected,
        [scriptblock] $Request
    )

    try {
        $response = & $Request
        $actual = [int] $response.StatusCode
    }
    catch {
        if ($_.Exception.Response) {
            $actual = [int] $_.Exception.Response.StatusCode
        }
        else {
            throw
        }
    }

    if ($actual -ne $Expected) {
        throw "Expected HTTP $Expected but received HTTP $actual."
    }
}

Assert-Status 401 {
    Invoke-WebRequest -Uri "$BaseUrl/users" -Method Get -UseBasicParsing
}
Assert-Status 401 {
    Invoke-WebRequest -Uri "$BaseUrl/users" -Method Get `
        -Headers @{ Authorization = "Bearer invalid-token" } -UseBasicParsing
}

$email = "alex.$([Guid]::NewGuid().ToString('N'))@example.test"
$payload = @{ name = "Alex Example"; email = $email } | ConvertTo-Json
$created = Invoke-RestMethod -Uri "$BaseUrl/users" -Method Post -Headers $headers `
    -ContentType "application/json" -Body $payload
$createdId = $created.id
if ([string]::IsNullOrWhiteSpace($createdId)) {
    throw "POST /users did not return a user ID."
}

Assert-Status 409 {
    Invoke-WebRequest -Uri "$BaseUrl/users" -Method Post -Headers $headers `
        -ContentType "application/json" -Body $payload -UseBasicParsing
}

$listed = Invoke-RestMethod -Uri "$BaseUrl/users" -Method Get -Headers $headers
if (@($listed | Where-Object { $_.id -eq $createdId }).Count -ne 1) {
    throw "GET /users did not return the created user."
}

$fetched = Invoke-RestMethod -Uri "$BaseUrl/users/$createdId" -Method Get -Headers $headers
if ($fetched.email -ne $email) {
    throw "GET /users/{id} returned an unexpected user."
}

$updatedPayload = @{
    name = "Alex Updated"
    email = "alex.updated.$([Guid]::NewGuid().ToString('N'))@example.test"
} | ConvertTo-Json
$updated = Invoke-RestMethod -Uri "$BaseUrl/users/$createdId" -Method Put -Headers $headers `
    -ContentType "application/json" -Body $updatedPayload
if ($updated.name -ne "Alex Updated") {
    throw "PUT /users/{id} did not update the user."
}

Assert-Status 400 {
    Invoke-WebRequest -Uri "$BaseUrl/users" -Method Post -Headers $headers `
        -ContentType "application/json" -Body (@{ name = ""; email = "invalid" } | ConvertTo-Json) `
        -UseBasicParsing
}
Assert-Status 404 {
    Invoke-WebRequest -Uri "$BaseUrl/users/$([Guid]::NewGuid())" -Method Get -Headers $headers -UseBasicParsing
}
Assert-Status 204 {
    Invoke-WebRequest -Uri "$BaseUrl/users/$createdId" -Method Delete -Headers $headers -UseBasicParsing
}
Assert-Status 404 {
    Invoke-WebRequest -Uri "$BaseUrl/users/$createdId" -Method Get -Headers $headers -UseBasicParsing
}

Write-Output "Smoke tests passed: authentication, CRUD, validation, and missing-user responses."
