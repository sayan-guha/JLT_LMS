using JLT.AIProxy.Models;
using JLT.AIProxy.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Register Core Services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Backend API HttpClient registration
builder.Services.AddHttpClient("BackendApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BackendApi:BaseUrl"] ?? "http://localhost:5126");
});

// Register AI orchestration services
builder.Services.AddSingleton<ToolRegistry>();
builder.Services.AddSingleton<DocumentStore>();
builder.Services.AddSingleton<OpenAIService>();
builder.Services.AddScoped<BackendApiClient>();

var app = builder.Build();

app.UseCors("AllowAll");

// Health Check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Chat endpoint
app.MapPost("/api/agent/chat", async (
    [FromBody] ChatRequest request,
    [FromServices] OpenAIService openAiService,
    [FromServices] BackendApiClient backendClient,
    HttpContext httpContext) =>
{
    if (string.IsNullOrWhiteSpace(request.ConversationId))
    {
        return Results.BadRequest(new { error = "ConversationId is required." });
    }

    // Extract Bearer JWT token from Authorization header
    var authHeader = httpContext.Request.Headers.Authorization.ToString();
    var authToken = string.Empty;
    if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        authToken = authHeader.Substring(7).Trim();
    }

    // Extract X-Tenant-Slug header (defaults to "demo")
    var tenantSlug = httpContext.Request.Headers["X-Tenant-Slug"].ToString();
    if (string.IsNullOrWhiteSpace(tenantSlug))
    {
        tenantSlug = "demo";
    }

    try
    {
        if (request.ConfirmAction != null)
        {
            var response = await openAiService.ExecuteConfirmedActionAsync(
                request.ConversationId, 
                request.ConfirmAction, 
                backendClient, 
                authToken, 
                tenantSlug
            );
            return Results.Ok(response);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.BadRequest(new { error = "Message is required when confirmAction is null." });
            }

            var response = await openAiService.ChatAsync(
                request.ConversationId, 
                request.Message, 
                backendClient, 
                authToken, 
                tenantSlug
            );
            return Results.Ok(response);
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Uncaught error during agent conversation");
        return Results.InternalServerError(new { error = ex.Message });
    }
});

// Session Reset endpoint
app.MapPost("/api/agent/chat/reset/{conversationId}", (
    string conversationId,
    [FromServices] OpenAIService openAiService) =>
{
    if (string.IsNullOrWhiteSpace(conversationId))
    {
        return Results.BadRequest(new { error = "ConversationId is required." });
    }

    openAiService.ResetConversation(conversationId);
    return Results.Ok(new { message = "Conversation history cleared successfully." });
});

// File Upload endpoint
app.MapPost("/api/agent/upload", async (IFormFile file, [FromServices] DocumentStore store) =>
{
    if (file == null || file.Length == 0)
    {
        return Results.BadRequest(new { error = "No file uploaded." });
    }

    using var reader = new StreamReader(file.OpenReadStream());
    var content = await reader.ReadToEndAsync();
    
    var id = store.AddDocument(content);
    return Results.Ok(new { documentId = id, filename = file.FileName, success = true });
}).DisableAntiforgery();

app.Run();
