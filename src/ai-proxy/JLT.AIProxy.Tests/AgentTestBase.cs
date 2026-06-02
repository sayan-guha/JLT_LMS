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
