using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using JLT.AIProxy.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace JLT.AIProxy.Services;

public class PendingToolCall
{
    public string ToolCallId { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public string ArgumentsJson { get; set; } = string.Empty;
}

public class OpenAIService
{
    private readonly ToolRegistry _toolRegistry;
    private readonly ILogger<OpenAIService> _logger;
    private readonly ChatClient _chatClient;
    
    // In-memory conversation history: ConversationId -> Chat Messages
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _history = new();
    
    // Pending destructive tool calls: ToolCallId -> PendingToolCall
    private readonly ConcurrentDictionary<string, PendingToolCall> _pendingToolCalls = new();

    public OpenAIService(ToolRegistry toolRegistry, IConfiguration config, ILogger<OpenAIService> logger)
    {
        _toolRegistry = toolRegistry;
        _logger = logger;

        var apiKey = config["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
            ?? throw new InvalidOperationException("OpenAI API Key is not configured. Set 'OpenAI:ApiKey' or 'OPENAI_API_KEY' environment variable.");
        var model = config["OpenAI:Model"] ?? "gpt-4o-mini";
        
        _chatClient = new ChatClient(model: model, apiKey: apiKey);
        _logger.LogInformation("OpenAIService initialized with model {Model}", model);
    }

    private List<ChatMessage> GetOrCreateHistory(string conversationId)
    {
        return _history.GetOrAdd(conversationId, id => new List<ChatMessage>
        {
            new SystemChatMessage(GetSystemPrompt())
        });
    }

    public void ResetConversation(string conversationId)
    {
        _history.TryRemove(conversationId, out _);
    }

    public async Task<ChatResponse> ChatAsync(
        string conversationId, 
        string message, 
        BackendApiClient backendClient, 
        string authToken, 
        string tenantSlug)
    {
        _logger.LogInformation("Processing chat for ConversationId: {ConversationId}", conversationId);
        
        var history = GetOrCreateHistory(conversationId);
        history.Add(new UserChatMessage(message));

        return await ProcessChatLoopAsync(conversationId, history, backendClient, authToken, tenantSlug);
    }

    public async Task<ChatResponse> ExecuteConfirmedActionAsync(
        string conversationId, 
        ConfirmAction confirm, 
        BackendApiClient backendClient, 
        string authToken, 
        string tenantSlug)
    {
        _logger.LogInformation("Processing confirmed action for ToolCallId: {ToolCallId}, Approved: {Approved}", confirm.ToolCallId, confirm.Approved);
        
        if (!_pendingToolCalls.TryRemove(confirm.ToolCallId, out var pending))
        {
            return new ChatResponse("error", "Pending action session not found or already executed.");
        }

        var history = GetOrCreateHistory(conversationId);

        if (!confirm.Approved)
        {
            _logger.LogInformation("Action declined by user");
            // Add a tool response stating the user cancelled the operation
            history.Add(new ToolChatMessage(confirm.ToolCallId, "{\"error\": \"User declined to proceed with this destructive action. Operation aborted.\"}"));
            
            // Resume the completion loop to let the LLM explain or confirm the cancel
            return await ProcessChatLoopAsync(conversationId, history, backendClient, authToken, tenantSlug);
        }

        _logger.LogInformation("Action approved by user. Executing tool {ToolName}...", pending.FunctionName);
        
        var toolDef = _toolRegistry.GetTool(pending.FunctionName);
        if (toolDef == null)
        {
            return new ChatResponse("error", $"Tool configuration for '{pending.FunctionName}' was not found.");
        }

        string resultJson;
        try
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, object>>(pending.ArgumentsJson) ?? new();
            resultJson = await backendClient.CallAsync(toolDef.HttpMethod, toolDef.EndpointTemplate, args, authToken, tenantSlug);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName} after confirmation", pending.FunctionName);
            resultJson = JsonSerializer.Serialize(new { error = ex.Message });
        }

        history.Add(new ToolChatMessage(confirm.ToolCallId, resultJson));

        // Resume loop to summarize result
        return await ProcessChatLoopAsync(conversationId, history, backendClient, authToken, tenantSlug);
    }

    private async Task<ChatResponse> ProcessChatLoopAsync(
        string conversationId, 
        List<ChatMessage> history, 
        BackendApiClient backendClient, 
        string authToken, 
        string tenantSlug)
    {
        const int maxIterations = 5;
        
        var options = new ChatCompletionOptions();
        foreach (var tool in _toolRegistry.GetChatTools())
        {
            options.Tools.Add(tool);
        }

        for (int i = 0; i < maxIterations; i++)
        {
            _logger.LogInformation("Sending request to OpenAI. Iteration {Iteration}", i + 1);
            
            ChatCompletion completion;
            try
            {
                completion = await _chatClient.CompleteChatAsync(history, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI Chat Completion request failed");
                return new ChatResponse("error", $"AI generation error: {ex.Message}");
            }

            if (completion.FinishReason == ChatFinishReason.ToolCalls && completion.ToolCalls.Count > 0)
            {
                _logger.LogInformation("OpenAI requested {Count} tool calls", completion.ToolCalls.Count);
                
                // Add the assistant's message requesting tool calls to history
                history.Add(new AssistantChatMessage(completion.ToolCalls));

                // Process tool calls
                var allReads = true;
                PendingConfirmation? pendingConfirmation = null;

                foreach (var toolCall in completion.ToolCalls)
                {
                    var toolName = toolCall.FunctionName;
                    var toolDef = _toolRegistry.GetTool(toolName);
                    
                    if (toolDef == null)
                    {
                        _logger.LogWarning("OpenAI requested unknown tool: {ToolName}", toolName);
                        history.Add(new ToolChatMessage(toolCall.Id, "{\"error\": \"Tool not found in registry\"}"));
                        continue;
                    }

                    if (toolDef.HttpMethod == "UI")
                    {
                        _logger.LogInformation("Intercepting UI control command: {ToolName}", toolName);
                        var argsJson = toolCall.FunctionArguments.ToString();
                        var parsedArgs = JsonSerializer.Deserialize<object>(argsJson) ?? new object();
                        
                        // Return immediately to frontend with ui_action
                        return new ChatResponse("ui_action", "I've updated the center panel for you.", new { action = toolName, payload = parsedArgs });
                    }

                    if (toolDef.IsDestructive)
                    {
                        _logger.LogInformation("Tool {ToolName} is destructive. Intercepting for confirmation.", toolName);
                        allReads = false;
                        
                        var argsJson = toolCall.FunctionArguments.ToString();
                        
                        var pending = new PendingToolCall
                        {
                            ToolCallId = toolCall.Id,
                            FunctionName = toolName,
                            ArgumentsJson = argsJson
                        };
                        _pendingToolCalls[toolCall.Id] = pending;

                        // Create a human readable summary of what is happening
                        var summary = BuildHumanReadableSummary(toolDef, argsJson);
                        var parsedArgs = JsonSerializer.Deserialize<object>(argsJson) ?? new object();

                        pendingConfirmation = new PendingConfirmation(
                            toolCall.Id,
                            toolName,
                            summary,
                            parsedArgs
                        );
                        break; // Stop loop and request confirmation immediately for first destructive action
                    }
                    else
                    {
                        // It is a read tool, execute immediately
                        _logger.LogInformation("Executing read tool {ToolName} immediately...", toolName);
                        string toolResult;
                        try
                        {
                            var args = JsonSerializer.Deserialize<Dictionary<string, object>>(toolCall.FunctionArguments.ToString()) ?? new();
                            toolResult = await backendClient.CallAsync(toolDef.HttpMethod, toolDef.EndpointTemplate, args, authToken, tenantSlug);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error executing read tool {ToolName}", toolName);
                            toolResult = JsonSerializer.Serialize(new { error = ex.Message });
                        }

                        history.Add(new ToolChatMessage(toolCall.Id, toolResult));
                    }
                }

                if (!allReads && pendingConfirmation != null)
                {
                    // Return confirmation response
                    return new ChatResponse("confirmation", null, null, pendingConfirmation);
                }

                // If all were reads, continue the loop so the model processes the tool results
                continue;
            }

            // Normal text response
            var assistantContent = completion.Content[0].Text;
            _logger.LogInformation("Received assistant response content length: {Length}", assistantContent?.Length ?? 0);
            
            // Add assistant response to history
            history.Add(new AssistantChatMessage(assistantContent));

            if (string.IsNullOrEmpty(assistantContent))
            {
                return new ChatResponse("text", "I'm sorry, I encountered an issue processing that request.");
            }

            // Parse out structured workspace data if present
            var (cleanContent, data) = ParseWorkspaceData(assistantContent);

            return new ChatResponse("text", cleanContent, data);
        }

        return new ChatResponse("error", "Maximum conversational reasoning limit reached.");
    }

    private string BuildHumanReadableSummary(ToolDefinition tool, string argsJson)
    {
        try
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, object>>(argsJson);
            if (args == null) return $"Perform action: {tool.Name}";

            switch (tool.Name)
            {
                case "create_user":
                    return $"Create user account for **{args.GetValueOrDefault("firstName")} {args.GetValueOrDefault("lastName")}** ({args.GetValueOrDefault("email")}) with role **{args.GetValueOrDefault("role")}**.";
                case "update_user":
                    return $"Update user account **{args.GetValueOrDefault("id")}** settings (Name: {args.GetValueOrDefault("firstName")} {args.GetValueOrDefault("lastName")}).";
                case "toggle_user_status":
                    var active = args.GetValueOrDefault("isActive")?.ToString()?.ToLower() == "true";
                    return $"{(active ? "Activate" : "Deactivate")} user account **{args.GetValueOrDefault("id")}**.";
                case "assign_user_roles":
                    return $"Assign roles to user **{args.GetValueOrDefault("id")}**.";
                case "create_user_group":
                    return $"Create group **{args.GetValueOrDefault("name")}** (Type: {args.GetValueOrDefault("type")}).";
                case "update_user_group":
                    return $"Update group **{args.GetValueOrDefault("name")}** attributes.";
                case "delete_user_group":
                    return $"Delete user group **{args.GetValueOrDefault("id")}**.";
                case "add_group_members":
                    return $"Add selected users to group **{args.GetValueOrDefault("id")}**.";
                case "remove_group_members":
                    return $"Remove selected users from group **{args.GetValueOrDefault("id")}**.";
                case "create_role":
                    return $"Create custom role **{args.GetValueOrDefault("name")}** with designated permissions.";
                case "create_learning_content":
                    return $"Create learning content draft **{args.GetValueOrDefault("title")}** (Type: {args.GetValueOrDefault("contentType")}).";
                case "update_learning_content":
                    return $"Update learning content item **{args.GetValueOrDefault("title")}** details.";
                case "delete_learning_content":
                    return $"Delete learning content item **{args.GetValueOrDefault("id")}**.";
                case "update_content_status":
                    return $"Update content item **{args.GetValueOrDefault("id")}** status to **{args.GetValueOrDefault("status")}**.";
                case "create_tenant":
                    return $"Create new tenant **{args.GetValueOrDefault("name")}** (Slug: {args.GetValueOrDefault("slug")}) managed by {args.GetValueOrDefault("adminEmail")}.";
                case "update_tenant":
                    return $"Update tenant details for ID **{args.GetValueOrDefault("id")}**.";
                case "create_dynamic_field":
                    return $"Create metadata field **{args.GetValueOrDefault("fieldName")}** (Label: {args.GetValueOrDefault("label")}, Type: {args.GetValueOrDefault("fieldType")}).";
                default:
                    return $"Perform operation '{tool.Name}' with arguments: {argsJson}";
            }
        }
        catch
        {
            return $"Perform operation '{tool.Name}' with arguments: {argsJson}";
        }
    }

    private (string CleanContent, object? Data) ParseWorkspaceData(string content)
    {
        var regex = new Regex(@"---DATA---\s*(.*?)\s*---END---", RegexOptions.Singleline);
        var match = regex.Match(content);

        if (!match.Success)
        {
            return (content, null);
        }

        var jsonString = match.Groups[1].Value.Trim();
        var cleanContent = regex.Replace(content, "").Trim();

        try
        {
            var dataObject = JsonSerializer.Deserialize<object>(jsonString);
            return (cleanContent, dataObject);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Workspace Data JSON: {JsonString}", jsonString);
            return (content, null); // return raw if error
        }
    }

    private string GetSystemPrompt()
    {
        return @"You are the JLT Learning Management System (LMS) AI Assistant, designed to help system administrators manage the platform through natural language.

You have access to a variety of backend API tools representing actions in the LMS.

IMPORTANT RULES OF OPERATION:
1. Always use the registered tools to query or mutate state. Never fake details or make up IDs.
2. If the user asks for list data (e.g. 'show users', 'list content', 'view roles'), retrieve it via the proper tool.
3. Whenever you display structured list data, table data, single entity details, or key metrics to the user, you MUST ALSO output a structured JSON block at the end of your response, wrapped inside '---DATA---' and '---END---' delimiters. This block is used by the frontend to render the information in a premium visual workspace.
4. Keep the text portion of your response warm, friendly, concise, and focused on helping the administrator.
5. Do not output the JSON delimiters inside standard markdown code blocks, just write them directly.

JSON Data structures to output for the workspace:

For rendering tables (Users, Groups, Courses, etc.):
---DATA---
{
  ""component"": ""data-table"",
  ""title"": ""Active Platform Users"",
  ""columns"": [""id"", ""name"", ""email"", ""role"", ""status""],
  ""rows"": [
    { ""id"": ""uuid-1"", ""name"": ""Sarah Connor"", ""email"": ""sarah@acme.com"", ""role"": ""Learner"", ""status"": ""Active"" }
  ]
}
---END---
(Ensure rows have properties matching columns. If columns are not provided, the frontend will auto-detect from keys).

For rendering stats / metric cards (Dashboard/Overview):
---DATA---
{
  ""component"": ""metrics"",
  ""title"": ""System Performance Metrics"",
  ""items"": [
    { ""label"": ""Total Users"", ""value"": ""1,240"", ""change"": ""+12%"", ""type"": ""success"" },
    { ""label"": ""Active Batches"", ""value"": ""15"", ""type"": ""primary"" }
  ]
}
---END---

For rendering single entity details:
---DATA---
{
  ""component"": ""detail"",
  ""title"": ""User Profile: Sarah Connor"",
  ""data"": {
    ""ID"": ""uuid-1"",
    ""Name"": ""Sarah Connor"",
    ""Email"": ""sarah@acme.com"",
    ""Role"": ""Learner"",
    ""Status"": ""Active"",
    ""Department"": ""Operations"",
    ""Location"": ""Los Angeles""
  }
}
---END---";
    }
}
