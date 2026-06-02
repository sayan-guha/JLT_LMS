using System.Text.Json;
using OpenAI.Chat;

namespace JLT.AIProxy.Services;

public record ToolDefinition(
    string Name,
    string Description,
    bool IsDestructive,
    string HttpMethod,
    string EndpointTemplate,
    string ParameterJsonSchema
);

public class ToolRegistry
{
    private readonly Dictionary<string, ToolDefinition> _tools = new();

    public ToolRegistry()
    {
        RegisterTools();
    }

    public ToolDefinition? GetTool(string name)
    {
        return _tools.TryGetValue(name, out var tool) ? tool : null;
    }

    public IEnumerable<ChatTool> GetChatTools()
    {
        return _tools.Values.Select(t => 
            ChatTool.CreateFunctionTool(
                t.Name, 
                t.Description, 
                BinaryData.FromString(t.ParameterJsonSchema)
            )
        );
    }

    private void RegisterTools()
    {
        // ------------------ USERS ------------------
        Register(new ToolDefinition(
            "list_users",
            "Get a paginated and optionally filtered list of users in the platform.",
            false,
            "GET",
            "/api/users",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""pageNumber"": { ""type"": ""integer"", ""description"": ""The page number (default 1)"" },
                    ""pageSize"": { ""type"": ""integer"", ""description"": ""The page size (default 10)"" },
                    ""searchTerm"": { ""type"": ""string"", ""description"": ""Search filter for name, email, etc."" },
                    ""role"": { ""type"": ""string"", ""description"": ""Filter users by their role (e.g. Administrator, Instructor, Learner)"" },
                    ""isActive"": { ""type"": ""boolean"", ""description"": ""Filter users by active/inactive status"" }
                }
            }"
        ));

        Register(new ToolDefinition(
            "get_user",
            "Retrieve details for a specific user by their unique ID.",
            false,
            "GET",
            "/api/users/{id}",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""string"", ""description"": ""The unique GUID of the user"" }
                },
                ""required"": [""id""]
            }"
        ));

        Register(new ToolDefinition(
            "create_user",
            "Create a new user account in the platform. This is a destructive/write operation.",
            true,
            "POST",
            "/api/users",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""email"": { ""type"": ""string"", ""description"": ""User email address"" },
                    ""password"": { ""type"": ""string"", ""description"": ""Password for the new user (must be at least 8 characters, with uppercase, lowercase, digit, and special char)"" },
                    ""firstName"": { ""type"": ""string"", ""description"": ""User first name"" },
                    ""lastName"": { ""type"": ""string"", ""description"": ""User last name"" },
                    ""role"": { ""type"": ""string"", ""description"": ""Initial system role (e.g. Learner, Instructor)"" },
                    ""department"": { ""type"": ""string"", ""description"": ""User department"" },
                    ""location"": { ""type"": ""string"", ""description"": ""User location"" },
                    ""attributesJson"": { ""type"": ""string"", ""description"": ""JSON string representing additional dynamic attributes"" }
                },
                ""required"": [""email"", ""password"", ""firstName"", ""lastName"", ""role""]
            }"
        ));

        Register(new ToolDefinition(
            "update_user",
            "Update details of an existing user. This is a destructive/write operation.",
            true,
            "PUT",
            "/api/users/{id}",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""string"", ""description"": ""The GUID of the user to update"" },
                    ""firstName"": { ""type"": ""string"", ""description"": ""Updated first name"" },
                    ""lastName"": { ""type"": ""string"", ""description"": ""Updated last name"" },
                    ""department"": { ""type"": ""string"", ""description"": ""Updated department"" },
                    ""location"": { ""type"": ""string"", ""description"": ""Updated location"" }
                },
                ""required"": [""id"", ""firstName"", ""lastName""]
            }"
        ));

        Register(new ToolDefinition(
            "toggle_user_status",
            "Enable or disable a user account. This is a destructive/write operation.",
            true,
            "PATCH",
            "/api/users/{id}/status",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""string"", ""description"": ""The GUID of the user"" },
                    ""isActive"": { ""type"": ""boolean"", ""description"": ""Set to true to activate, false to deactivate"" }
                },
                ""required"": [""id"", ""isActive""]
            }"
        ));

        Register(new ToolDefinition(
            "assign_user_roles",
            "Assign roles to a user. This is a destructive/write operation.",
            true,
            "PUT",
            "/api/users/{id}/roles",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""string"", ""description"": ""The GUID of the user"" },
                    ""roleNames"": { 
                        ""type"": ""array"", 
                        ""items"": { ""type"": ""string"" }, 
                        ""description"": ""List of role names to assign"" 
                    }
                },
                ""required"": [""id"", ""roleNames""]
            }"
        ));

        // ------------------ USER GROUPS ------------------
        Register(new ToolDefinition(
            "list_user_groups",
            "Retrieve a paginated and optionally filtered list of user groups.",
            false,
            "GET",
            "/api/user-groups",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""pageNumber"": { ""type"": ""integer"", ""description"": ""Page number (default 1)"" },
                    ""pageSize"": { ""type"": ""integer"", ""description"": ""Page size (default 10)"" },
                    ""searchTerm"": { ""type"": ""string"", ""description"": ""Search query to filter groups"" },
                    ""type"": { ""type"": ""string"", ""description"": ""Filter by group type (e.g. Static, Dynamic)"" }
                }
            }"
        ));

        Register(new ToolDefinition(
            "get_user_group",
            "Retrieve details and members of a specific user group by its unique ID.",
            false,
            "GET",
            "/api/user-groups/{id}",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""string"", ""description"": ""The unique GUID of the user group"" }
                },
                ""required"": [""id""]
            }"
        ));

        Register(new ToolDefinition(
            "create_user_group",
            "Create a new user group (Static or Dynamic). This is a destructive/write operation.",
            true,
            "POST",
            "/api/user-groups",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""name"": { ""type"": ""string"", ""description"": ""Group name"" },
                    ""description"": { ""type"": ""string"", ""description"": ""Group description"" },
                    ""type"": { ""type"": ""string"", ""description"": ""Group type, either 'Static' or 'Dynamic'"" },
                    ""rules"": { ""type"": ""string"", ""description"": ""JSON rule definition for dynamic membership eligibility"" }
                },
                ""required"": [""name"", ""type""]
            }"
        ));

        Register(new ToolDefinition(
            "update_user_group",
            "Update an existing user group. This is a destructive/write operation.",
            true,
            "PUT",
            "/api/user-groups/{id}",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""string"", ""description"": ""The GUID of the group to update"" },
                    ""name"": { ""type"": ""string"", ""description"": ""New group name"" },
                    ""description"": { ""type"": ""string"", ""description"": ""New description"" },
                    ""rules"": { ""type"": ""string"", ""description"": ""New rules JSON string (for dynamic groups)"" }
                },
                ""required"": [""id"", ""name""]
            }"
        ));

        Register(new ToolDefinition(
            "delete_user_group",
            "Delete a user group. This is a destructive/write operation.",
            true,
            "DELETE",
            "/api/user-groups/{id}",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""string"", ""description"": ""The GUID of the group to delete"" }
                },
                ""required"": [""id""]
            }"
        ));

        Register(new ToolDefinition(
            "add_group_members",
            "Add multiple users to a static user group. This is a destructive/write operation.",
            true,
            "POST",
            "/api/user-groups/{id}/members",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""string"", ""description"": ""The GUID of the group"" },
                    ""userIds"": {
                        ""type"": ""array"",
                        ""items"": { ""type"": ""string"" },
                        ""description"": ""List of user GUIDs to add""
                    }
                },
                ""required"": [""id"", ""userIds""]
            }"
        ));

        Register(new ToolDefinition(
            "remove_group_members",
            "Remove multiple users from a static user group. This is a destructive/write operation.",
            true,
            "DELETE",
            "/api/user-groups/{id}/members",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""string"", ""description"": ""The GUID of the group"" },
                    ""userIds"": {
                        ""type"": ""array"",
                        ""items"": { ""type"": ""string"" },
                        ""description"": ""List of user GUIDs to remove""
                    }
                },
                ""required"": [""id"", ""userIds""]
            }"
        ));

        // ------------------ ROLES ------------------
        Register(new ToolDefinition(
            "list_roles",
            "Get the list of all available system roles.",
            false,
            "GET",
            "/api/roles",
            @"{
                ""type"": ""object"",
                ""properties"": {}
            }"
        ));

        Register(new ToolDefinition(
            "list_permissions",
            "Get all system permissions that can be assigned to roles.",
            false,
            "GET",
            "/api/roles/permissions",
            @"{
                ""type"": ""object"",
                ""properties"": {}
            }"
        ));

        Register(new ToolDefinition(
            "create_role",
            "Create a new custom system role with specified permissions. This is a destructive/write operation.",
            true,
            "POST",
            "/api/roles",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""name"": { ""type"": ""string"", ""description"": ""Role name"" },
                    ""description"": { ""type"": ""string"", ""description"": ""Role description"" },
                    ""permissions"": {
                        ""type"": ""array"",
                        ""items"": { ""type"": ""string"" },
                        ""description"": ""List of permission keys to grant to this role""
                    }
                },
                ""required"": [""name"", ""permissions""]
            }"
        ));

        // ------------------ LEARNING CONTENT ------------------
        Register(new ToolDefinition(
            "list_learning_content",
            "Retrieve a paginated and filtered list of learning content (courses, articles, documents, SCORM).",
            false,
            "GET",
            "/api/learning-content",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""pageNumber"": { ""type"": ""integer"", ""description"": ""Page number (default 1)"" },
                    ""pageSize"": { ""type"": ""integer"", ""description"": ""Page size (default 10)"" },
                    ""contentType"": { ""type"": ""string"", ""description"": ""Filter by content type (e.g. Article, Document, Scorm)"" },
                    ""status"": { ""type"": ""string"", ""description"": ""Filter by status (e.g. Draft, Published, Archived)"" },
                    ""searchTerm"": { ""type"": ""string"", ""description"": ""Search filter for title or description"" }
                }
            }"
        ));

        Register(new ToolDefinition(
            "get_learning_content",
            "Retrieve detailed information for a learning content item by its ID.",
            false,
            "GET",
            "/api/learning-content/{id}",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""string"", ""description"": ""GUID of the content item"" }
                },
                ""required"": [""id""]
            }"
        ));

        Register(new ToolDefinition(
            "create_learning_content",
            "Create a new learning content item (Draft). This is a destructive/write operation.",
            true,
            "POST",
            "/api/learning-content",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""title"": { ""type"": ""string"", ""description"": ""Content title"" },
                    ""description"": { ""type"": ""string"", ""description"": ""Content description"" },
                    ""contentType"": { ""type"": ""string"", ""description"": ""Content type: 'Document', 'Media', 'SCORM', 'xAPI', 'LTI', 'Hyperlink', 'EmbedLink', or 'Image'"" },
                    ""resourceUrl"": { ""type"": ""string"", ""description"": ""URL pointing to the resource file or article link"" },
                    ""durationMinutes"": { ""type"": ""integer"", ""description"": ""Estimated completion time in minutes"" },
                    ""category"": { ""type"": ""string"", ""description"": ""Content category"" },
                    ""language"": { ""type"": ""string"", ""description"": ""Content language (default 'en')"" }
                },
                ""required"": [""title"", ""contentType""]
            }"
        ));

        Register(new ToolDefinition(
            "update_learning_content",
            "Update an existing learning content item. This is a destructive/write operation.",
            true,
            "PUT",
            "/api/learning-content/{id}",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""string"", ""description"": ""GUID of the content to update"" },
                    ""title"": { ""type"": ""string"", ""description"": ""Updated title"" },
                    ""description"": { ""type"": ""string"", ""description"": ""Updated description"" },
                    ""resourceUrl"": { ""type"": ""string"", ""description"": ""Updated resource URL"" },
                    ""durationMinutes"": { ""type"": ""integer"", ""description"": ""Updated duration in minutes"" },
                    ""category"": { ""type"": ""string"", ""description"": ""Updated category"" },
                    ""language"": { ""type"": ""string"", ""description"": ""Updated language"" }
                },
                ""required"": [""id"", ""title""]
            }"
        ));

        Register(new ToolDefinition(
            "delete_learning_content",
            "Delete a learning content item (Only if status is Draft). This is a destructive/write operation.",
            true,
            "DELETE",
            "/api/learning-content/{id}",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""string"", ""description"": ""GUID of the content item"" }
                },
                ""required"": [""id""]
            }"
        ));

        Register(new ToolDefinition(
            "update_content_status",
            "Change the publishing status of a content item. This is a destructive/write operation.",
            true,
            "PATCH",
            "/api/learning-content/{id}/status",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""string"", ""description"": ""GUID of the content item"" },
                    ""status"": { ""type"": ""string"", ""description"": ""New status: 'Draft', 'Published', or 'Archived'"" }
                },
                ""required"": [""id"", ""status""]
            }"
        ));

        // ------------------ TENANTS ------------------
        Register(new ToolDefinition(
            "list_tenants",
            "Retrieve a list of platform tenants.",
            false,
            "GET",
            "/api/tenants",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""pageNumber"": { ""type"": ""integer"", ""description"": ""Page number (default 1)"" },
                    ""pageSize"": { ""type"": ""integer"", ""description"": ""Page size (default 10)"" },
                    ""searchTerm"": { ""type"": ""string"", ""description"": ""Search filter for tenant name or slug"" }
                }
            }"
        ));

        Register(new ToolDefinition(
            "get_tenant",
            "Retrieve details for a single tenant by their ID.",
            false,
            "GET",
            "/api/tenants/{id}",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""string"", ""description"": ""GUID of the tenant"" }
                },
                ""required"": [""id""]
            }"
        ));

        Register(new ToolDefinition(
            "create_tenant",
            "Create a new platform tenant. This is a destructive/write operation.",
            true,
            "POST",
            "/api/tenants",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""name"": { ""type"": ""string"", ""description"": ""Tenant name"" },
                    ""slug"": { ""type"": ""string"", ""description"": ""Unique tenant URL slug"" },
                    ""adminEmail"": { ""type"": ""string"", ""description"": ""Email of the initial tenant administrator"" }
                },
                ""required"": [""name"", ""slug"", ""adminEmail""]
            }"
        ));

        Register(new ToolDefinition(
            "update_tenant",
            "Update details of an existing tenant. This is a destructive/write operation.",
            true,
            "PUT",
            "/api/tenants/{id}",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""string"", ""description"": ""GUID of the tenant to update"" },
                    ""name"": { ""type"": ""string"", ""description"": ""Updated name"" },
                    ""isActive"": { ""type"": ""boolean"", ""description"": ""Enable/disable tenant"" }
                },
                ""required"": [""id"", ""name"", ""isActive""]
            }"
        ));

        // ------------------ DYNAMIC FIELDS ------------------
        Register(new ToolDefinition(
            "list_dynamic_fields",
            "Retrieve all defined user dynamic fields.",
            false,
            "GET",
            "/api/fields",
            @"{
                ""type"": ""object"",
                ""properties"": {}
            }"
        ));

        Register(new ToolDefinition(
            "create_dynamic_field",
            "Define a new user metadata dynamic field. This is a destructive/write operation.",
            true,
            "POST",
            "/api/fields",
            @"{
                ""type"": ""object"",
                ""properties"": {
                    ""fieldName"": { ""type"": ""string"", ""description"": ""Technical field name"" },
                    ""label"": { ""type"": ""string"", ""description"": ""User-facing UI label"" },
                    ""fieldType"": { ""type"": ""string"", ""description"": ""Field type: 'Text', 'Number', 'Date', 'Dropdown'"" },
                    ""configurationJson"": { ""type"": ""string"", ""description"": ""JSON string for config (e.g. dropdown options)"" },
                    ""isRequired"": { ""type"": ""boolean"", ""description"": ""Whether this field is mandatory"" }
                },
                ""required"": [""fieldName"", ""label"", ""fieldType""]
            }"
        ));
    }

    private void Register(ToolDefinition tool)
    {
        _tools[tool.Name] = tool;
    }
}
