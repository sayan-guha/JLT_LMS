using FluentValidation;
using JLT.Application.DTOs;
using JLT.Domain.Interfaces;
using MediatR;

namespace JLT.Application.Features.Auth;

// --- Login Command ---
public record LoginCommand(string Email, string Password, Guid TenantId, string? IpAddress = null, string? UserAgent = null) : IRequest<AuthResponseDto>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public LoginCommandHandler(IUserRepository userRepository, IRoleRepository roleRepository,
        IPasswordHasher passwordHasher, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var roles = await _roleRepository.GetUserRolesAsync(user.Id, cancellationToken);
        var roleNames = roles.Select(r => r.Name).ToList();
        var permissions = await _roleRepository.GetUserPermissionsAsync(user.Id, cancellationToken);

        var accessToken = _jwtService.GenerateAccessToken(user, request.TenantId, roleNames, permissions);
        var refreshToken = _jwtService.GenerateRefreshToken(user.Id, request.IpAddress, request.UserAgent);

        // Save refresh token to database
        await _userRepository.AddRefreshTokenAsync(refreshToken, cancellationToken);

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        var userDto = new UserDto(user.Id, user.TenantId, user.Email, user.FirstName, user.LastName,
            user.FullName, user.AvatarUrl, user.Department, user.JobTitle, user.Location,
            user.ManagerId, user.Attributes, user.IsActive, user.LastLoginAt, user.CreatedAt,
            user.UpdatedAt, roleNames);

        return new AuthResponseDto(accessToken, refreshToken.Token, refreshToken.ExpiresAt, userDto);
    }
}

// --- Register Command ---
public record RegisterCommand(
    string Email, string Password, string FirstName, string LastName,
    string? Department, string? JobTitle, string? Location,
    Guid? ManagerId, string? Attributes) : IRequest<UserDto>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[\W_]").WithMessage("Password must contain at least one special character.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDynamicFieldService _dynamicFieldService;
    private readonly IAuditService _auditService;
    private readonly IMediator _mediator;

    public RegisterCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher,
        IDynamicFieldService dynamicFieldService, IAuditService auditService, IMediator mediator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _dynamicFieldService = dynamicFieldService;
        _auditService = auditService;
        _mediator = mediator;
    }

    public async Task<UserDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check for duplicate email
        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException("A user with this email already exists.");

        // Validate dynamic fields if provided
        if (!string.IsNullOrEmpty(request.Attributes))
            await _dynamicFieldService.ValidateAttributesAsync(request.Attributes, cancellationToken);

        var user = new Domain.Entities.User
        {
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Department = request.Department,
            JobTitle = request.JobTitle,
            Location = request.Location,
            ManagerId = request.ManagerId,
            Attributes = request.Attributes
        };

        await _userRepository.AddAsync(user, cancellationToken);

        await _auditService.LogAsync("user.created", "User", user.Id,
            newValues: new { user.Email, user.FirstName, user.LastName },
            cancellationToken: cancellationToken);

        // Publish event for dynamic group evaluations
        await _mediator.Publish(new JLT.Domain.Events.UserCreatedEvent(user.Id, user.TenantId), cancellationToken);

        return new UserDto(user.Id, user.TenantId, user.Email, user.FirstName, user.LastName,
            user.FullName, user.AvatarUrl, user.Department, user.JobTitle, user.Location,
            user.ManagerId, user.Attributes, user.IsActive, user.LastLoginAt, user.CreatedAt,
            user.UpdatedAt, new List<string>());
    }
}


// --- Refresh Token Command ---
public record RefreshTokenCommand(string RefreshToken, string? IpAddress = null, string? UserAgent = null) : IRequest<AuthResponseDto>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IJwtService _jwtService;

    public RefreshTokenCommandHandler(IUserRepository userRepository, IRoleRepository roleRepository, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _jwtService = jwtService;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var storedToken = await _userRepository.GetRefreshTokenWithUserAsync(request.RefreshToken, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (storedToken.IsRevoked)
        {
            // Replay attack detection: revoke all active tokens for this user
            var user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);
            if (user != null)
            {
                // Normally we'd load user with active tokens, but we can query them directly or load from user
                foreach (var token in user.RefreshTokens.Where(t => t.IsActive))
                {
                    token.RevokedAt = DateTime.UtcNow;
                    token.ReplacedByToken = "REPLAY_DETECTION";
                    await _userRepository.UpdateRefreshTokenAsync(token, cancellationToken);
                }
            }
            throw new UnauthorizedAccessException("Token has been revoked. Potential replay attack detected.");
        }

        if (storedToken.IsExpired)
            throw new UnauthorizedAccessException("Refresh token has expired.");

        var currentUser = storedToken.User ?? throw new InvalidOperationException("User not found for refresh token.");

        if (!currentUser.IsActive)
            throw new UnauthorizedAccessException("User account is deactivated.");

        var roles = await _roleRepository.GetUserRolesAsync(currentUser.Id, cancellationToken);
        var roleNames = roles.Select(r => r.Name).ToList();
        var permissions = await _roleRepository.GetUserPermissionsAsync(currentUser.Id, cancellationToken);

        var newAccessToken = _jwtService.GenerateAccessToken(currentUser, currentUser.TenantId, roleNames, permissions);
        var newRefreshToken = _jwtService.GenerateRefreshToken(currentUser.Id, request.IpAddress, request.UserAgent);

        // Revoke current token and link to new one
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByToken = newRefreshToken.Token;

        await _userRepository.UpdateRefreshTokenAsync(storedToken, cancellationToken);
        await _userRepository.AddRefreshTokenAsync(newRefreshToken, cancellationToken);

        var userDto = new UserDto(currentUser.Id, currentUser.TenantId, currentUser.Email, currentUser.FirstName, currentUser.LastName,
            currentUser.FullName, currentUser.AvatarUrl, currentUser.Department, currentUser.JobTitle, currentUser.Location,
            currentUser.ManagerId, currentUser.Attributes, currentUser.IsActive, currentUser.LastLoginAt, currentUser.CreatedAt,
            currentUser.UpdatedAt, roleNames);

        return new AuthResponseDto(newAccessToken, newRefreshToken.Token, newRefreshToken.ExpiresAt, userDto);
    }
}

// --- Revoke Token Command ---
public record RevokeTokenCommand(string RefreshToken, string? IpAddress = null) : IRequest<bool>;

public class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, bool>
{
    private readonly IUserRepository _userRepository;

    public RevokeTokenCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<bool> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var storedToken = await _userRepository.GetRefreshTokenWithUserAsync(request.RefreshToken, cancellationToken);
        if (storedToken == null || !storedToken.IsActive)
            return false;

        storedToken.RevokedAt = DateTime.UtcNow;
        await _userRepository.UpdateRefreshTokenAsync(storedToken, cancellationToken);
        return true;
    }
}
