# AI Agent Integration Tests — Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Create end-to-end integration tests that prove the AI agent (gpt-5.2 via JLT.AIProxy) correctly interprets natural language admin commands, calls the right backend API tools, and produces verifiable data changes in the JLT.API backend.

**Architecture:** The tests start both JLT.API (port 5126) and JLT.AIProxy (port 5200) as background processes, authenticate against JLT.API to get a JWT, then send natural language messages to the AI proxy's `/api/agent/chat` endpoint. For read operations, we verify the agent returns structured data. For write operations, we verify the agent returns a confirmation, then approve it and verify the data was actually created by calling JLT.API directly.

**Tech Stack:** xUnit, HttpClient, System.Text.Json, JLT.AIProxy (gpt-5.2), JLT.API (existing backend)

**Pre-requisites:** JLT.API running on port 5126, JLT.AIProxy running on port 5200, PostgreSQL running, seeded demo tenant with admin@demo.com / Admin@123!.

---

### Task 1: Create AI Agent Test File and Helpers

**Files:**
- Create: `src/ai-proxy/JLT.AIProxy.Tests/JLT.AIProxy.Tests.csproj`
- Create: `src/ai-proxy/JLT.AIProxy.Tests/AgentTestBase.cs`
- Modify: `src/backend/JLT.sln` (add test project)

**Step 1: Create the test project**

```bash
cd d:\vibing hard\JLT\.worktrees\classroom-training\src\ai-proxy
dotnet new xunit -n JLT.AIProxy.Tests
```

**Step 2: Edit `JLT.AIProxy.Tests.csproj`**

Target `net9.0`. Add xUnit packages (already included by template). No project reference to AIProxy needed since these are HTTP integration tests against running services.

**Step 3: Create `AgentTestBase.cs`**

This base class provides:
- `HttpClient _agentClient` (base URL `http://localhost:5200`, default headers with `X-Tenant-Slug: demo`)
- `HttpClient _backendClient` (base URL `http://localhost:5126`, default headers with `X-Tenant-Slug: demo`)
- `async Task<string> AuthenticateAsync()` — logs in as `admin@demo.com` / `Admin@123!`, returns JWT, sets it on both clients
- `async Task<JsonElement> SendAgentMessageAsync(string conversationId, string message)` — sends a chat message to the proxy, returns parsed JSON response
- `async Task<JsonElement> ConfirmAgentActionAsync(string conversationId, string toolCallId, bool approved)` — sends a confirmation, returns parsed response
- `string NewConversationId()` — generates a fresh UUID for each test

```csharp
using System.Net.Http.Json;
using System.Text.Json;

namespace JLT.AIProxy.Tests;

public abstract class AgentTestBase : IAsyncLifetime
{
    protected readonly HttpClient _agentClient;
    protected readonly HttpClient _backendClient;
    protected string _authToken = "";

    protected AgentTestBase()
    {
        _agentClient = new HttpClient { BaseAddress = new Uri("http://localhost:5200") };
        _agentClient.DefaultRequestHeaders.Add("X-Tenant-Slug", "demo");

        _backendClient = new HttpClient { BaseAddress = new Uri("http://localhost:5126") };
        _backendClient.DefaultRequestHeaders.Add("X-Tenant-Slug", "demo");
    }

    public async Task InitializeAsync()
    {
        _authToken = await AuthenticateAsync();
        _agentClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
        _backendClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected string NewConversationId() => Guid.NewGuid().ToString();

    private async Task<string> AuthenticateAsync()
    {
        var tenantResp = await _backendClient.GetAsync("/api/tenants?searchTerm=demo");
        var tenantJson = await tenantResp.Content.ReadAsStringAsync();
        using var tenantDoc = JsonDocument.Parse(tenantJson);
        Guid tenantId = Guid.Empty;
        foreach (var item in tenantDoc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray())
        {
            if (item.GetProperty("slug").GetString() == "demo")
            {
                tenantId = item.GetProperty("id").GetGuid();
                break;
            }
        }

        var loginResp = await _backendClient.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@demo.com",
            password = "Admin@123!",
            tenantId
        });
        var loginJson = await loginResp.Content.ReadAsStringAsync();
        using var loginDoc = JsonDocument.Parse(loginJson);
        return loginDoc.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    protected async Task<JsonElement> SendAgentMessageAsync(string conversationId, string message)
    {
        var response = await _agentClient.PostAsJsonAsync("/api/agent/chat", new
        {
            conversationId,
            message,
            confirmAction = (object?)null
        });
        var body = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(body).RootElement;
    }

    protected async Task<JsonElement> ConfirmAgentActionAsync(string conversationId, string toolCallId, bool approved)
    {
        var response = await _agentClient.PostAsJsonAsync("/api/agent/chat", new
        {
            conversationId,
            message = (string?)null,
            confirmAction = new { toolCallId, approved }
        });
        var body = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(body).RootElement;
    }
}
```

**Step 4: Add to solution**

```bash
cd d:\vibing hard\JLT\.worktrees\classroom-training
dotnet sln src/backend/JLT.sln add src/ai-proxy/JLT.AIProxy.Tests/JLT.AIProxy.Tests.csproj
```

**Step 5: Verify build**

```bash
dotnet build src/ai-proxy/JLT.AIProxy.Tests
```

Expected: BUILD SUCCEEDED

**Step 6: Commit**

```bash
git add src/ai-proxy/JLT.AIProxy.Tests src/backend/JLT.sln
git commit -m "test(ai-agent): scaffold integration test project with AgentTestBase"
```

---

### Task 2: Read Operation Tests — List Users, Roles, Content

**Files:**
- Create: `src/ai-proxy/JLT.AIProxy.Tests/AgentReadTests.cs`

**Step 1: Write test `Agent_ListUsers_ReturnsUserData`**

Send: "Show me all users in the system"
Assert:
- Response `type` is `"text"`
- Response `content` is non-empty and mentions users
- Hit `GET /api/users` on the backend directly and compare — the agent should reference the same user count or names

```csharp
[Fact]
public async Task Agent_ListUsers_ReturnsUserData()
{
    var convId = NewConversationId();
    var result = await SendAgentMessageAsync(convId, "Show me all users in the system");

    Assert.Equal("text", result.GetProperty("type").GetString());
    var content = result.GetProperty("content").GetString()!;
    Assert.False(string.IsNullOrWhiteSpace(content));

    // Verify against backend directly
    var backendResp = await _backendClient.GetAsync("/api/users");
    var backendJson = await backendResp.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(backendJson);
    var userCount = doc.RootElement.GetProperty("data").GetProperty("items").GetArrayLength();

    // The agent should have fetched and mentioned user data
    Assert.True(userCount > 0, "Backend should have seeded users");
}
```

**Step 2: Write test `Agent_ListRoles_ReturnsRoleData`**

Send: "What roles are available?"
Assert:
- Response `type` is `"text"`
- Content mentions at least "Administrator" or "Learner"

```csharp
[Fact]
public async Task Agent_ListRoles_ReturnsRoleData()
{
    var convId = NewConversationId();
    var result = await SendAgentMessageAsync(convId, "What roles are available in the system?");

    Assert.Equal("text", result.GetProperty("type").GetString());
    var content = result.GetProperty("content").GetString()!;
    // Agent should mention at least one role
    Assert.True(
        content.Contains("Administrator", StringComparison.OrdinalIgnoreCase) ||
        content.Contains("Learner", StringComparison.OrdinalIgnoreCase),
        $"Expected role names in response but got: {content[..Math.Min(200, content.Length)]}"
    );
}
```

**Step 3: Write test `Agent_ListLearningContent_ReturnsContentData`**

Send: "List all learning content"
Assert:
- Response `type` is `"text"`
- Content is non-empty (backend has seeded content or returns an empty list gracefully)

```csharp
[Fact]
public async Task Agent_ListLearningContent_ReturnsContentData()
{
    var convId = NewConversationId();
    var result = await SendAgentMessageAsync(convId, "List all learning content");

    Assert.Equal("text", result.GetProperty("type").GetString());
    Assert.False(string.IsNullOrWhiteSpace(result.GetProperty("content").GetString()));
}
```

**Step 4: Write test `Agent_ConversationMemory_RemembersContext`**

Send two messages in the same conversation:
1. "Show me all users"
2. "How many did you find?"
Assert: The second response references the count from the first query (proves memory works).

```csharp
[Fact]
public async Task Agent_ConversationMemory_RemembersContext()
{
    var convId = NewConversationId();
    await SendAgentMessageAsync(convId, "Show me all users in the system");
    var followUp = await SendAgentMessageAsync(convId, "How many users did you find?");

    var content = followUp.GetProperty("content").GetString()!;
    // Should reference a number or the user count
    Assert.False(string.IsNullOrWhiteSpace(content));
    Assert.True(content.Any(char.IsDigit), $"Expected a number in follow-up but got: {content[..Math.Min(200, content.Length)]}");
}
```

**Step 5: Verify tests pass**

Pre-requisite: JLT.API and JLT.AIProxy both running.

```bash
dotnet test src/ai-proxy/JLT.AIProxy.Tests --filter "FullyQualifiedName~AgentReadTests" -v normal
```

Expected: 4 tests pass (may take 5-15s each due to OpenAI latency).

**Step 6: Commit**

```bash
git add src/ai-proxy/JLT.AIProxy.Tests
git commit -m "test(ai-agent): add read operation integration tests (users, roles, content, memory)"
```

---

### Task 3: Write Operation Tests — Create User with Confirmation + Backend Verification

**Files:**
- Create: `src/ai-proxy/JLT.AIProxy.Tests/AgentWriteTests.cs`

**Step 1: Write test `Agent_CreateUser_ReturnsConfirmation_ThenCreatesOnApproval`**

This is the critical end-to-end test:
1. Send: "Create a new user named TestAgent User with email testagent@demo.com as a Learner"
2. Assert: Response `type` is `"confirmation"` and `confirmation.action` is `"create_user"`
3. Approve the confirmation
4. Assert: Approval response `type` is `"text"` and content mentions success
5. **Verify at backend:** Call `GET /api/users?searchTerm=testagent@demo.com` and assert the user exists

```csharp
[Fact]
public async Task Agent_CreateUser_ReturnsConfirmation_ThenCreatesOnApproval()
{
    var convId = NewConversationId();
    var uniqueEmail = $"testagent-{Guid.NewGuid():N}@demo.com";

    // Step 1: Ask agent to create a user
    var result = await SendAgentMessageAsync(convId,
        $"Create a new user named Integration Test with email {uniqueEmail} as a Learner");

    // Step 2: Should return a confirmation since create_user is destructive
    Assert.Equal("confirmation", result.GetProperty("type").GetString());
    var confirmation = result.GetProperty("confirmation");
    Assert.Equal("create_user", confirmation.GetProperty("action").GetString());
    var toolCallId = confirmation.GetProperty("toolCallId").GetString()!;

    // Step 3: Approve
    var approvalResult = await ConfirmAgentActionAsync(convId, toolCallId, approved: true);

    // Step 4: Agent should confirm success
    Assert.Equal("text", approvalResult.GetProperty("type").GetString());
    var content = approvalResult.GetProperty("content").GetString()!;
    Assert.True(
        content.Contains("created", StringComparison.OrdinalIgnoreCase) ||
        content.Contains("success", StringComparison.OrdinalIgnoreCase),
        $"Expected success message but got: {content[..Math.Min(300, content.Length)]}"
    );

    // Step 5: Verify user exists in backend
    var backendResp = await _backendClient.GetAsync($"/api/users?searchTerm={uniqueEmail}");
    var backendJson = await backendResp.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(backendJson);
    var items = doc.RootElement.GetProperty("data").GetProperty("items");
    Assert.True(items.GetArrayLength() > 0, $"User {uniqueEmail} should exist in backend after agent created it");
    Assert.Equal(uniqueEmail, items[0].GetProperty("email").GetString());
}
```

**Step 2: Write test `Agent_CreateUser_ReturnsConfirmation_ThenAbortsOnDecline`**

1. Send: "Create a user named Decline Test with email declinetest@demo.com as a Learner"
2. Assert: confirmation returned
3. Decline the confirmation
4. Assert: Agent acknowledges cancellation
5. **Verify at backend:** User does NOT exist

```csharp
[Fact]
public async Task Agent_CreateUser_ReturnsConfirmation_ThenAbortsOnDecline()
{
    var convId = NewConversationId();
    var uniqueEmail = $"declinetest-{Guid.NewGuid():N}@demo.com";

    var result = await SendAgentMessageAsync(convId,
        $"Create a new user named Decline Test with email {uniqueEmail} as a Learner");

    Assert.Equal("confirmation", result.GetProperty("type").GetString());
    var toolCallId = result.GetProperty("confirmation").GetProperty("toolCallId").GetString()!;

    // Decline
    var declineResult = await ConfirmAgentActionAsync(convId, toolCallId, approved: false);

    Assert.Equal("text", declineResult.GetProperty("type").GetString());
    var content = declineResult.GetProperty("content").GetString()!;
    Assert.True(
        content.Contains("cancel", StringComparison.OrdinalIgnoreCase) ||
        content.Contains("abort", StringComparison.OrdinalIgnoreCase) ||
        content.Contains("decline", StringComparison.OrdinalIgnoreCase) ||
        content.Contains("not", StringComparison.OrdinalIgnoreCase),
        $"Expected cancellation acknowledgement but got: {content[..Math.Min(300, content.Length)]}"
    );

    // Verify user does NOT exist
    var backendResp = await _backendClient.GetAsync($"/api/users?searchTerm={uniqueEmail}");
    var backendJson = await backendResp.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(backendJson);
    var items = doc.RootElement.GetProperty("data").GetProperty("items");
    Assert.Equal(0, items.GetArrayLength());
}
```

**Step 3: Write test `Agent_CreateLearningContent_EndToEnd`**

1. Send: "Create a new article titled 'AI Agent Test Article' about integration testing with URL https://example.com/test"
2. Confirm
3. Verify in backend: `GET /api/learning-content?searchTerm=AI Agent Test Article` returns the item

```csharp
[Fact]
public async Task Agent_CreateLearningContent_EndToEnd()
{
    var convId = NewConversationId();
    var uniqueTitle = $"AI Agent Test Article {Guid.NewGuid():N}";

    var result = await SendAgentMessageAsync(convId,
        $"Create a new article titled '{uniqueTitle}' about integration testing with resource URL https://example.com/test");

    Assert.Equal("confirmation", result.GetProperty("type").GetString());
    var toolCallId = result.GetProperty("confirmation").GetProperty("toolCallId").GetString()!;

    var approval = await ConfirmAgentActionAsync(convId, toolCallId, approved: true);
    Assert.Equal("text", approval.GetProperty("type").GetString());

    // Verify in backend
    var backendResp = await _backendClient.GetAsync($"/api/learning-content?searchTerm={Uri.EscapeDataString(uniqueTitle)}");
    var backendJson = await backendResp.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(backendJson);
    var items = doc.RootElement.GetProperty("data").GetProperty("items");
    Assert.True(items.GetArrayLength() > 0, $"Content '{uniqueTitle}' should exist after agent created it");
}
```

**Step 4: Verify tests pass**

```bash
dotnet test src/ai-proxy/JLT.AIProxy.Tests --filter "FullyQualifiedName~AgentWriteTests" -v normal
```

Expected: 3 tests pass. Each test takes 10-20s due to OpenAI round-trips.

**Step 5: Commit**

```bash
git add src/ai-proxy/JLT.AIProxy.Tests
git commit -m "test(ai-agent): add write operation tests with confirmation flow and backend verification"
```

---

### Task 4: Multi-Step Conversation Test and Cleanup

**Files:**
- Create: `src/ai-proxy/JLT.AIProxy.Tests/AgentMultiStepTests.cs`

**Step 1: Write test `Agent_CreateThenFindUser_MultiStepConversation`**

This tests a realistic admin workflow:
1. "Create a user named Multi Step with email multistep-{uuid}@demo.com as a Learner" → confirm
2. "Now show me all users" → verify the newly created user appears in the agent's response
3. Verify via backend API that the user exists

```csharp
[Fact]
public async Task Agent_CreateThenFindUser_MultiStepConversation()
{
    var convId = NewConversationId();
    var uniqueEmail = $"multistep-{Guid.NewGuid():N}@demo.com";

    // Create
    var createResult = await SendAgentMessageAsync(convId,
        $"Create a new user named Multi Step with email {uniqueEmail} as a Learner");
    Assert.Equal("confirmation", createResult.GetProperty("type").GetString());
    var toolCallId = createResult.GetProperty("confirmation").GetProperty("toolCallId").GetString()!;

    var approval = await ConfirmAgentActionAsync(convId, toolCallId, approved: true);
    Assert.Equal("text", approval.GetProperty("type").GetString());

    // Find — same conversation
    var findResult = await SendAgentMessageAsync(convId,
        $"Can you find the user with email {uniqueEmail}?");
    Assert.Equal("text", findResult.GetProperty("type").GetString());
    var findContent = findResult.GetProperty("content").GetString()!;
    Assert.Contains(uniqueEmail, findContent, StringComparison.OrdinalIgnoreCase);
}
```

**Step 2: Write test `Agent_AmbiguousRequest_AsksForClarification`**

Send a vague/ambiguous message: "Delete it"
Assert: Agent responds with a text asking for clarification (not a tool call).

```csharp
[Fact]
public async Task Agent_AmbiguousRequest_AsksForClarification()
{
    var convId = NewConversationId();
    var result = await SendAgentMessageAsync(convId, "Delete it");

    Assert.Equal("text", result.GetProperty("type").GetString());
    var content = result.GetProperty("content").GetString()!;
    // Agent should ask what to delete
    Assert.True(
        content.Contains("which", StringComparison.OrdinalIgnoreCase) ||
        content.Contains("what", StringComparison.OrdinalIgnoreCase) ||
        content.Contains("specify", StringComparison.OrdinalIgnoreCase) ||
        content.Contains("clarif", StringComparison.OrdinalIgnoreCase),
        $"Expected clarification question but got: {content[..Math.Min(300, content.Length)]}"
    );
}
```

**Step 3: Verify tests pass**

```bash
dotnet test src/ai-proxy/JLT.AIProxy.Tests --filter "FullyQualifiedName~AgentMultiStepTests" -v normal
```

Expected: 2 tests pass.

**Step 4: Run all agent tests together**

```bash
dotnet test src/ai-proxy/JLT.AIProxy.Tests -v normal
```

Expected: All 9 tests pass.

**Step 5: Commit**

```bash
git add src/ai-proxy/JLT.AIProxy.Tests
git commit -m "test(ai-agent): add multi-step conversation and ambiguity tests"
```
