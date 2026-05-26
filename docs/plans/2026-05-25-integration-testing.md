# Integration Testing Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Build integration tests verifying the core API endpoints of the user management module (Auth, Users, and UserGroups) using HttpClient against the running API.

**Architecture:** Run the API locally as a background task. Write a new C# test file `IntegrationTests.cs` inside `JLT.Tests` containing asynchronous HTTP integration tests. Use standard `HttpClient` with configured headers (X-Tenant-Slug, Authorization) to interact with the API.

**Tech Stack:** .NET 9, xUnit, HttpClient, System.Text.Json.

---

### Task 1: Create Integration Tests class

**Files:**
- Create: `src/backend/JLT.Tests/IntegrationTests.cs`

**Step 1: Write integration tests hitting the running API**

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace JLT.Tests;

public class IntegrationTests
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://localhost:5126";

    public IntegrationTests()
    {
        _client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }

    [Fact]
    public async Task Auth_Login_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        var loginModel = new
        {
            email = "admin@demo.com",
            password = "Admin@123!",
            tenantSlug = "demo"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginModel);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.NotNull(data.GetProperty("accessToken").GetString());
        Assert.NotNull(data.GetProperty("refreshToken").GetString());
    }

    [Fact]
    public async Task Users_GetMe_WithValidJwt_ShouldReturnUserProfile()
    {
        // Arrange - first login to get token
        var loginModel = new
        {
            email = "admin@demo.com",
            password = "Admin@123!",
            tenantSlug = "demo"
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginModel);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        using var loginDoc = JsonDocument.Parse(loginContent);
        var token = loginDoc.RootElement.GetProperty("data").GetProperty("accessToken").GetString();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Headers.Add("X-Tenant-Slug", "demo");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("admin@demo.com", data.GetProperty("email").GetString());
    }
}
```

**Step 2: Start API in the background**

Run: `dotnet run --project src/backend/JLT.API` as a background task.

**Step 3: Run integration tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~IntegrationTests"`
Expected: PASS

**Step 4: Stop the API background task**

Stop: Clean up/terminate the API background task.
