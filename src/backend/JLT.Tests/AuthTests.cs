using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JLT.Application.DTOs;
using JLT.Application.Features.Auth;
using JLT.Domain.Entities;
using JLT.Domain.Interfaces;
using NSubstitute;
using Xunit;
using MediatR;

namespace JLT.Tests;

public class AuthTests
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IDynamicFieldService _dynamicFieldService;
    private readonly IAuditService _auditService;
    private readonly IMediator _mediator;

    public AuthTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _roleRepository = Substitute.For<IRoleRepository>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _jwtService = Substitute.For<IJwtService>();
        _dynamicFieldService = Substitute.For<IDynamicFieldService>();
        _auditService = Substitute.For<IAuditService>();
        _mediator = Substitute.For<IMediator>();
    }


    [Fact]
    public async Task Register_ShouldCreateUser_WhenInputIsValid()
    {
        // Arrange
        var command = new RegisterCommand("test@example.com", "Password@123!", "John", "Doe", "Engineering", "Developer", "NY", null, null);
        _userRepository.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns((User?)null);
        _passwordHasher.Hash(command.Password).Returns("hashed_password");
        
        var handler = new RegisterCommandHandler(_userRepository, _passwordHasher, _dynamicFieldService, _auditService, _mediator);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(command.Email, result.Email);
        Assert.Equal(command.FirstName, result.FirstName);
        Assert.Equal(command.LastName, result.LastName);
        
        await _userRepository.Received(1).AddAsync(Arg.Is<User>(u => u.Email == command.Email && u.PasswordHash == "hashed_password"), Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogAsync("user.created", "User", Arg.Any<Guid>(), Arg.Any<object>(), Arg.Any<object>(), Arg.Any<JLT.Domain.Enums.AuditSource>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_ShouldThrowException_WhenEmailDuplicate()
    {
        // Arrange
        var command = new RegisterCommand("test@example.com", "Password@123!", "John", "Doe", null, null, null, null, null);
        _userRepository.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(new User { Email = command.Email });

        var handler = new RegisterCommandHandler(_userRepository, _passwordHasher, _dynamicFieldService, _auditService, _mediator);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Login_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new LoginCommand("test@example.com", "Password@123!", tenantId);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            PasswordHash = "hashed_password",
            IsActive = true,
            TenantId = tenantId
        };

        _userRepository.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(command.Password, user.PasswordHash).Returns(true);
        _roleRepository.GetUserRolesAsync(user.Id, Arg.Any<CancellationToken>()).Returns(new List<Role>());
        _roleRepository.GetUserPermissionsAsync(user.Id, Arg.Any<CancellationToken>()).Returns(new List<string>());
        
        _jwtService.GenerateAccessToken(user, tenantId, Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>()).Returns("access_token");
        
        var generatedRefreshToken = new RefreshToken { Token = "refresh_token", ExpiresAt = DateTime.UtcNow.AddDays(7), UserId = user.Id };
        _jwtService.GenerateRefreshToken(user.Id, Arg.Any<string>(), Arg.Any<string>()).Returns(generatedRefreshToken);

        var handler = new LoginCommandHandler(_userRepository, _roleRepository, _passwordHasher, _jwtService);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("access_token", result.AccessToken);
        Assert.Equal("refresh_token", result.RefreshToken);
        
        await _userRepository.Received(1).AddRefreshTokenAsync(generatedRefreshToken, Arg.Any<CancellationToken>());
        await _userRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshToken_ShouldRotateToken_WhenTokenIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var oldTokenString = "old_refresh_token";
        var user = new User { Id = userId, Email = "test@example.com", IsActive = true, TenantId = tenantId };
        
        var storedToken = new RefreshToken
        {
            Token = oldTokenString,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            User = user
        };

        _userRepository.GetRefreshTokenWithUserAsync(oldTokenString, Arg.Any<CancellationToken>()).Returns(storedToken);
        _roleRepository.GetUserRolesAsync(userId, Arg.Any<CancellationToken>()).Returns(new List<Role>());
        _roleRepository.GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>()).Returns(new List<string>());

        _jwtService.GenerateAccessToken(user, tenantId, Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>()).Returns("new_access_token");
        
        var newRefreshToken = new RefreshToken { Token = "new_refresh_token", ExpiresAt = DateTime.UtcNow.AddDays(7), UserId = userId };
        _jwtService.GenerateRefreshToken(userId, Arg.Any<string>(), Arg.Any<string>()).Returns(newRefreshToken);

        var handler = new RefreshTokenCommandHandler(_userRepository, _roleRepository, _jwtService);
        var command = new RefreshTokenCommand(oldTokenString);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new_access_token", result.AccessToken);
        Assert.Equal("new_refresh_token", result.RefreshToken);
        
        Assert.NotNull(storedToken.RevokedAt);
        Assert.Equal("new_refresh_token", storedToken.ReplacedByToken);
        
        await _userRepository.Received(1).UpdateRefreshTokenAsync(storedToken, Arg.Any<CancellationToken>());
        await _userRepository.Received(1).AddRefreshTokenAsync(newRefreshToken, Arg.Any<CancellationToken>());
    }
}
