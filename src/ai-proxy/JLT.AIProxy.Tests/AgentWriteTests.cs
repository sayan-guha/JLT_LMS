using System.Text.Json;

namespace JLT.AIProxy.Tests;

public class AgentWriteTests : AgentTestBase
{
    [Fact]
    public async Task Agent_CreateUser_ReturnsConfirmation_ThenCreatesOnApproval()
    {
        var convId = NewConversationId();
        var uniqueEmail = $"testagent-{Guid.NewGuid():N}@demo.com";

        // Step 1: Ask agent to create a user
        var result = await SendAgentMessageAsync(convId,
            $"Create a new user named Integration Test with email {uniqueEmail} as a Learner. Use password Password123!");

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

    [Fact]
    public async Task Agent_CreateUser_ReturnsConfirmation_ThenAbortsOnDecline()
    {
        var convId = NewConversationId();
        var uniqueEmail = $"declinetest-{Guid.NewGuid():N}@demo.com";

        var result = await SendAgentMessageAsync(convId,
            $"Create a new user named Decline Test with email {uniqueEmail} as a Learner. Use password Password123!");

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

    [Fact]
    public async Task Agent_CreateLearningContent_EndToEnd()
    {
        var convId = NewConversationId();
        var uniqueTitle = $"AI Agent Test Article {Guid.NewGuid():N}";

        var result = await SendAgentMessageAsync(convId,
            $"Create a new Document titled '{uniqueTitle}' about integration testing with resource URL https://example.com/test");

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
}
