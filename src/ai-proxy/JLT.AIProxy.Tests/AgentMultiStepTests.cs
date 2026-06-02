using System.Text.Json;

namespace JLT.AIProxy.Tests;

public class AgentMultiStepTests : AgentTestBase
{
    [Fact]
    public async Task Agent_CreateThenFindUser_MultiStepConversation()
    {
        var convId = NewConversationId();
        var uniqueEmail = $"multistep-{Guid.NewGuid():N}@demo.com";

        // Create
        var createResult = await SendAgentMessageAsync(convId,
            $"Create a new user named Multi Step with email {uniqueEmail} as a Learner. Use password Password123!");
        Assert.Equal("confirmation", createResult.GetProperty("type").GetString());
        var toolCallId = createResult.GetProperty("confirmation").GetProperty("toolCallId").GetString()!;

        var approval = await ConfirmAgentActionAsync(convId, toolCallId, approved: true);
        Assert.Equal("text", approval.GetProperty("type").GetString());

        // Find — same conversation
        var findResult = await SendAgentMessageAsync(convId,
            $"Can you find the user with email {uniqueEmail}? Please display their email in your response.");
        Assert.Equal("text", findResult.GetProperty("type").GetString());
        var findContent = findResult.GetProperty("content").GetString()!;
        Assert.Contains(uniqueEmail, findContent, StringComparison.OrdinalIgnoreCase);
    }

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
            content.Contains("clarif", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("delete?", StringComparison.OrdinalIgnoreCase),
            $"Expected clarification question but got: {content[..Math.Min(300, content.Length)]}"
        );
    }
}
