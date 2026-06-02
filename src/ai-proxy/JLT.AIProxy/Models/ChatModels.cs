namespace JLT.AIProxy.Models;

public record ChatRequest(
    string ConversationId, 
    string? Message, 
    ConfirmAction? ConfirmAction
);

public record ConfirmAction(
    string ToolCallId, 
    bool Approved
);

public record ChatResponse(
    string Type, 
    string? Content, 
    object? Data = null, 
    PendingConfirmation? Confirmation = null
);

public record PendingConfirmation(
    string ToolCallId, 
    string Action, 
    string Summary, 
    object Parameters
);
