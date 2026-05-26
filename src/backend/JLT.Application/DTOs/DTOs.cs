namespace JLT.Application.DTOs;

public record UserDto(
    Guid Id,
    Guid TenantId,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string? AvatarUrl,
    string? Department,
    string? JobTitle,
    string? Location,
    Guid? ManagerId,
    string? Attributes,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<string> Roles
);

public record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    string? Domain,
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string PlanType,
    int? MaxUsers,
    int? MaxStorageGb,
    bool IsActive,
    DateTime CreatedAt
);

public record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    bool IsActive,
    IReadOnlyList<string> Permissions
);

public record UserGroupDto(
    Guid Id,
    string Name,
    string? Description,
    string Type,
    string? Rules,
    int MemberCount,
    DateTime CreatedAt
);

public record DynamicFieldDto(
    Guid Id,
    string FieldKey,
    string DisplayName,
    string FieldType,
    bool IsRequired,
    string? Options,
    string? DefaultValue,
    int SortOrder,
    bool IsActive
);

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);

public record PermissionDto(
    Guid Id,
    string Key,
    string Name,
    string Category,
    string? Description
);
