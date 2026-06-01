using System.Text;
using System.Text.Json;
using System.Web;

namespace JLT.AIProxy.Services;

public class BackendApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BackendApiClient> _logger;

    public BackendApiClient(IHttpClientFactory httpClientFactory, ILogger<BackendApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> CallAsync(
        string method, 
        string endpointTemplate, 
        Dictionary<string, object>? args, 
        string authToken, 
        string tenantSlug)
    {
        var client = _httpClientFactory.CreateClient("BackendApi");
        
        if (!string.IsNullOrWhiteSpace(authToken))
        {
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
        }
        
        if (!string.IsNullOrWhiteSpace(tenantSlug))
        {
            client.DefaultRequestHeaders.Add("X-Tenant-Slug", tenantSlug);
        }

        args ??= new Dictionary<string, object>();
        var url = endpointTemplate;

        // 1. Interpolate Route Parameters (e.g. {id})
        var routeParams = new List<string>();
        foreach (var key in args.Keys.ToList())
        {
            var placeholder = $"{{{key}}}";
            if (url.Contains(placeholder))
            {
                var val = args[key]?.ToString() ?? "";
                url = url.Replace(placeholder, HttpUtility.UrlEncode(val));
                routeParams.Add(key);
            }
        }
        
        // Remove route parameters from the args dictionary so they don't go to query/body
        foreach (var param in routeParams)
        {
            args.Remove(param);
        }

        HttpContent? content = null;
        var methodUpper = method.ToUpperInvariant();

        if (methodUpper == "GET" || methodUpper == "DELETE")
        {
            // 2. Append Query Parameters for GET/DELETE
            if (args.Count > 0)
            {
                var query = HttpUtility.ParseQueryString(string.Empty);
                foreach (var kvp in args)
                {
                    if (kvp.Value is JsonElement element)
                    {
                        query[kvp.Key] = element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText();
                    }
                    else
                    {
                        query[kvp.Key] = kvp.Value?.ToString();
                    }
                }
                url += "?" + query.ToString();
            }
        }
        else
        {
            // 3. Setup Request Body for POST/PUT/PATCH
            object bodyObject;
            
            // Special cases where the endpoint expects an array/list directly in the body (e.g. userIds)
            if (args.Count == 1 && args.ContainsKey("userIds"))
            {
                bodyObject = args["userIds"];
            }
            else if (args.Count == 1 && args.ContainsKey("roleNames"))
            {
                bodyObject = args["roleNames"];
            }
            else
            {
                bodyObject = args;
            }

            var jsonBody = JsonSerializer.Serialize(bodyObject, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            
            content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            _logger.LogInformation("Backend API Request Body: {Body}", jsonBody);
        }

        _logger.LogInformation("Sending {Method} request to JLT.API at {Url}", methodUpper, url);

        HttpResponseMessage response;
        if (methodUpper == "GET") response = await client.GetAsync(url);
        else if (methodUpper == "POST") response = await client.PostAsync(url, content);
        else if (methodUpper == "PUT") response = await client.PutAsync(url, content);
        else if (methodUpper == "PATCH") response = await client.PatchAsync(url, content);
        else if (methodUpper == "DELETE") response = await client.DeleteAsync(url);
        else throw new NotSupportedException($"HTTP method '{method}' is not supported.");

        var responseBody = await response.Content.ReadAsStringAsync();
        
        _logger.LogInformation("JLT.API responded with status {StatusCode}. Body length: {Length}", response.StatusCode, responseBody.Length);
        
        // Return response directly. If it fails, JLT.API glob exception handler wraps it, or we handle it in OpenAIService.
        return responseBody;
    }
}
