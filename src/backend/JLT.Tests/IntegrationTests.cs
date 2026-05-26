using System;
using System.Collections.Generic;
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

    [Fact]
    public async Task Auth_Login_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        var tenantId = await GetDemoTenantIdAsync();
        var loginModel = new
        {
            email = "admin@demo.com",
            password = "Admin@123!",
            tenantId = tenantId
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginModel);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Status code: {response.StatusCode}, Content: {content}");
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
        // Arrange
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
        var token = loginDoc.RootElement.GetProperty("data").GetProperty("accessToken").GetString();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Status: {response.StatusCode}");
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("admin@demo.com", data.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Users_GetUsers_WithAdminRole_ShouldReturnUsersList()
    {
        // Arrange
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
        var token = loginDoc.RootElement.GetProperty("data").GetProperty("accessToken").GetString();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        request.Headers.Add("Authorization", $"Bearer {token}");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Status: {response.StatusCode}");
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.NotEmpty(data.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task UserGroups_CreateGroup_ShouldCreateSuccessfully()
    {
        // Arrange
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
        var token = loginDoc.RootElement.GetProperty("data").GetProperty("accessToken").GetString();

        var groupModel = new
        {
            name = $"Test Static Group {Guid.NewGuid()}",
            description = "Created by integration test",
            type = "Static",
            userIds = new List<Guid>()
        };


        var request = new HttpRequestMessage(HttpMethod.Post, "/api/user-groups")
        {
            Content = JsonContent.Create(groupModel)
        };

        request.Headers.Add("Authorization", $"Bearer {token}");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Status: {response.StatusCode}");
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(groupModel.name, data.GetProperty("name").GetString());
    }
}
