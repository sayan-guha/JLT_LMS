using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace JLT.Tests;

public class LearningContentIntegrationTests
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://localhost:5126";

    public LearningContentIntegrationTests()
    {
        _client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        _client.DefaultRequestHeaders.Add("X-Tenant-Slug", "demo");
    }

    private async Task<Guid> GetDemoTenantIdAsync()
    {
        var response = await _client.GetAsync("/api/tenants?searchTerm=demo");
        Assert.True(response.IsSuccessStatusCode, $"Failed to get tenants. Status: {response.StatusCode}");
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        var data = root.GetProperty("data");
        var items = data.GetProperty("items");
        foreach (var item in items.EnumerateArray())
        {
            if (item.GetProperty("slug").GetString() == "demo")
            {
                return item.GetProperty("id").GetGuid();
            }
        }
        throw new Exception("Demo tenant not found.");
    }

    private async Task<string> GetTokenAsync()
    {
        var tenantId = await GetDemoTenantIdAsync();
        var loginModel = new
        {
            email = "admin@demo.com",
            password = "Admin@123!",
            tenantId = tenantId
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginModel);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        using var loginDoc = JsonDocument.Parse(loginContent);
        return loginDoc.RootElement.GetProperty("data").GetProperty("accessToken").GetString() ?? throw new Exception("Token is null.");
    }

    private async Task<Guid> GetCurrentUserIdAsync(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        Assert.True(response.IsSuccessStatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("data").GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task LearningContent_Lifecycle_Flow_ShouldSucceed()
    {
        // 0. Setup authentication
        var token = await GetTokenAsync();
        var userId = await GetCurrentUserIdAsync(token);

        // 1. POST /api/learning-content (Create content as Draft)
        var createModel = new
        {
            title = $"Integration Test Doc {Guid.NewGuid()}",
            contentType = "Document",
            createdBy = userId,
            description = "This is a document created during integration testing",
            category = "Safety",
            language = "en"
        };

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/learning-content")
        {
            Content = JsonContent.Create(createModel)
        };
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var createResponse = await _client.SendAsync(createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        Assert.Equal(System.Net.HttpStatusCode.Created, createResponse.StatusCode);

        using var createDoc = JsonDocument.Parse(createContent);
        var contentData = createDoc.RootElement.GetProperty("data");
        var contentId = contentData.GetProperty("id").GetGuid();
        Assert.Equal(createModel.title, contentData.GetProperty("title").GetString());
        Assert.Equal("Draft", contentData.GetProperty("status").GetString());

        // 2. GET /api/learning-content/{id} (Retrieve and verify)
        var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/learning-content/{contentId}");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getResponse = await _client.SendAsync(getRequest);
        var getContent = await getResponse.Content.ReadAsStringAsync();
        Assert.True(getResponse.IsSuccessStatusCode);
        using var getDoc = JsonDocument.Parse(getContent);
        Assert.Equal(createModel.title, getDoc.RootElement.GetProperty("data").GetProperty("title").GetString());

        // 3. GET /api/learning-content (List and filter)
        var listRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/learning-content?contentType=Document&category=Safety");
        listRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var listResponse = await _client.SendAsync(listRequest);
        var listContent = await listResponse.Content.ReadAsStringAsync();
        Assert.True(listResponse.IsSuccessStatusCode);
        using var listDoc = JsonDocument.Parse(listContent);
        var items = listDoc.RootElement.GetProperty("data").GetProperty("items");
        Assert.NotEmpty(items.EnumerateArray());

        // 4. PUT /api/learning-content/{id} (Update details)
        var updateModel = new
        {
            id = contentId,
            title = $"Updated Integration Test Doc {Guid.NewGuid()}",
            category = "Safety Compliance"
        };
        var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/learning-content/{contentId}")
        {
            Content = JsonContent.Create(updateModel)
        };
        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var updateResponse = await _client.SendAsync(updateRequest);
        Assert.True(updateResponse.IsSuccessStatusCode);
        var updateContent = await updateResponse.Content.ReadAsStringAsync();
        using var updateDoc = JsonDocument.Parse(updateContent);
        Assert.Equal(updateModel.title, updateDoc.RootElement.GetProperty("data").GetProperty("title").GetString());
        Assert.Equal(updateModel.category, updateDoc.RootElement.GetProperty("data").GetProperty("category").GetString());

        // 5. PATCH /api/learning-content/{id}/status (Transition status Draft -> InReview)
        var statusInReviewModel = new { id = contentId, newStatus = "InReview" };
        var statusInReviewRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/learning-content/{contentId}/status")
        {
            Content = JsonContent.Create(statusInReviewModel)
        };
        statusInReviewRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var statusInReviewResponse = await _client.SendAsync(statusInReviewRequest);
        Assert.True(statusInReviewResponse.IsSuccessStatusCode);
        var statusInReviewContent = await statusInReviewResponse.Content.ReadAsStringAsync();
        using var statusInReviewDoc = JsonDocument.Parse(statusInReviewContent);
        Assert.Equal("InReview", statusInReviewDoc.RootElement.GetProperty("data").GetProperty("status").GetString());

        // 6. PATCH /api/learning-content/{id}/status (Transition status InReview -> Published)
        var statusPublishedModel = new { id = contentId, newStatus = "Published" };
        var statusPublishedRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/learning-content/{contentId}/status")
        {
            Content = JsonContent.Create(statusPublishedModel)
        };
        statusPublishedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var statusPublishedResponse = await _client.SendAsync(statusPublishedRequest);
        Assert.True(statusPublishedResponse.IsSuccessStatusCode);
        var statusPublishedContent = await statusPublishedResponse.Content.ReadAsStringAsync();
        using var statusPublishedDoc = JsonDocument.Parse(statusPublishedContent);
        var publishedData = statusPublishedDoc.RootElement.GetProperty("data");
        Assert.Equal("Published", publishedData.GetProperty("status").GetString());
        Assert.NotNull(publishedData.GetProperty("publishedAt").GetString());

        // 7. PUT /api/content-progress/{contentId} (Upsert Progress)
        var progressModel = new { progressPercent = 75.5m, bookmarkData = "{\"page\":12}", timeSpentSeconds = 300 };
        var progressRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/content-progress/{contentId}")
        {
            Content = JsonContent.Create(progressModel)
        };
        progressRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var progressResponse = await _client.SendAsync(progressRequest);
        Assert.True(progressResponse.IsSuccessStatusCode);
        var progressContent = await progressResponse.Content.ReadAsStringAsync();
        using var progressDoc = JsonDocument.Parse(progressContent);
        var progressData = progressDoc.RootElement.GetProperty("data");
        Assert.Equal(75.5m, progressData.GetProperty("progressPercent").GetDecimal());
        Assert.Equal("InProgress", progressData.GetProperty("status").GetString());

        // 8. GET /api/content-progress/{contentId} (Get Progress)
        var getProgressRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/content-progress/{contentId}");
        getProgressRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getProgressResponse = await _client.SendAsync(getProgressRequest);
        Assert.True(getProgressResponse.IsSuccessStatusCode);
        var getProgressContent = await getProgressResponse.Content.ReadAsStringAsync();
        using var getProgressDoc = JsonDocument.Parse(getProgressContent);
        Assert.Equal(75.5m, getProgressDoc.RootElement.GetProperty("data").GetProperty("progressPercent").GetDecimal());

        // 9. DELETE /api/learning-content/{id} (Fails because it is Published)
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/learning-content/{contentId}");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, deleteResponse.StatusCode);

        // 10. POST /api/learning-content (Create another draft to delete)
        var draftToDestroyModel = new
        {
            title = $"Draft to delete {Guid.NewGuid()}",
            contentType = "Document",
            createdBy = userId
        };
        var draftToDestroyRequest = new HttpRequestMessage(HttpMethod.Post, "/api/learning-content")
        {
            Content = JsonContent.Create(draftToDestroyModel)
        };
        draftToDestroyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var draftToDestroyResponse = await _client.SendAsync(draftToDestroyRequest);
        Assert.Equal(System.Net.HttpStatusCode.Created, draftToDestroyResponse.StatusCode);
        using var draftToDestroyDoc = JsonDocument.Parse(await draftToDestroyResponse.Content.ReadAsStringAsync());
        var draftToDestroyId = draftToDestroyDoc.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        // 11. DELETE /api/learning-content/{id} (Succeeds because it is Draft)
        var deleteDraftRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/learning-content/{draftToDestroyId}");
        deleteDraftRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var deleteDraftResponse = await _client.SendAsync(deleteDraftRequest);
        Assert.True(deleteDraftResponse.IsSuccessStatusCode);
    }

    [Fact]
    public async Task XApiStatements_Storage_ShouldSucceed()
    {
        // Arrange
        var token = await GetTokenAsync();
        var statementModel = new
        {
            actorJson = "{\"mbox\":\"mailto:testuser@demo.com\",\"name\":\"Test User\"}",
            verbId = "http://adlnet.gov/expapi/verbs/completed",
            objectJson = "{\"id\":\"http://example.com/activities/learning-content-1\",\"definition\":{\"name\":{\"en-US\":\"Integration Test Activity\"}}}"
        };

        var storeRequest = new HttpRequestMessage(HttpMethod.Post, "/api/xapi/statements")
        {
            Content = JsonContent.Create(statementModel)
        };
        storeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var storeResponse = await _client.SendAsync(storeRequest);
        Assert.Equal(System.Net.HttpStatusCode.Created, storeResponse.StatusCode);
        var storeContent = await storeResponse.Content.ReadAsStringAsync();
        using var storeDoc = JsonDocument.Parse(storeContent);
        var statementData = storeDoc.RootElement.GetProperty("data");
        Assert.Equal(statementModel.verbId, statementData.GetProperty("verbId").GetString());

        // Get xAPI statements
        var getStatementsRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/xapi/statements?verbId={Uri.EscapeDataString(statementModel.verbId)}");
        getStatementsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getStatementsResponse = await _client.SendAsync(getStatementsRequest);
        Assert.True(getStatementsResponse.IsSuccessStatusCode);
        var getStatementsContent = await getStatementsResponse.Content.ReadAsStringAsync();
        using var getStatementsDoc = JsonDocument.Parse(getStatementsContent);
        var items = getStatementsDoc.RootElement.GetProperty("data").GetProperty("items");
        Assert.NotEmpty(items.EnumerateArray());
    }
}
