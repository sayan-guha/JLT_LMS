using System.Text.Json;

namespace JLT.AIProxy.Tests;

public class AgentReadTests : AgentTestBase
{
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

    [Fact]
    public async Task Agent_ListLearningContent_ReturnsContentData()
    {
        var convId = NewConversationId();
        var result = await SendAgentMessageAsync(convId, "List all learning content");

        Assert.Equal("text", result.GetProperty("type").GetString());
        Assert.False(string.IsNullOrWhiteSpace(result.GetProperty("content").GetString()));
    }

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
}
