using JLT.Domain.Common;

namespace JLT.Domain.Entities;

public class User : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? Location { get; set; }
    public Guid? ManagerId { get; set; }
    public string? Attributes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    // Computed
    public string FullName => $"{FirstName} {LastName}";

    // Navigation
    public virtual User? Manager { get; set; }
    public virtual ICollection<User> DirectReports { get; set; } = new List<User>();
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<UserGroupMember> GroupMemberships { get; set; } = new List<UserGroupMember>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
