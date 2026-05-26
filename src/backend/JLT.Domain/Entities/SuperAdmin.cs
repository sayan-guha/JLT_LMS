using JLT.Domain.Common;

namespace JLT.Domain.Entities;

public class SuperAdmin : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
