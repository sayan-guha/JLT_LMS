using JLT.Domain.Entities;

namespace JLT.Domain.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user, Guid tenantId, IEnumerable<string> roles, IEnumerable<string> permissions);
    RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress = null, string? userAgent = null);
    bool ValidateAccessToken(string token);
}
