using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JLT.Application.Common.Models;
using JLT.Application.DTOs;
using JLT.Application.Features.Users;
using JLT.Domain.Entities;
using JLT.Domain.Interfaces;
using NSubstitute;
using Xunit;
using MediatR;

namespace JLT.Tests;

public class UserTests
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDynamicFieldService _dynamicFieldService;
    private readonly IAuditService _auditService;
    private readonly IMediator _mediator;

    public UserTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _roleRepository = Substitute.For<IRoleRepository>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _dynamicFieldService = Substitute.For<IDynamicFieldService>();
        _auditService = Substitute.For<IAuditService>();
        _mediator = Substitute.For<IMediator>();
    }


    [Fact]
    public async Task CreateUser_ShouldCreateUser_WhenEmailUnique()
    {
        // Arrange
        var command = new CreateUserCommand("new@example.com", "SecurePassword123!", "Alice", "Smith", "HR", "Manager", "US", null, null, null);
        _userRepository.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns((User?)null);
        _passwordHasher.Hash(command.Password).Returns("hashed_pwd");

        var handler = new CreateUserHandler(_userRepository, _roleRepository, _passwordHasher, _dynamicFieldService, _auditService, _mediator);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(command.Email, result.Email);
        Assert.Equal(command.FirstName, result.FirstName);
        Assert.Equal(command.LastName, result.LastName);
        Assert.True(result.IsActive);

        await _userRepository.Received(1).AddAsync(Arg.Is<User>(u => u.Email == command.Email && u.PasswordHash == "hashed_pwd"), Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogAsync("user.created", "User", Arg.Any<Guid>(), Arg.Any<object>(), Arg.Any<object>(), JLT.Domain.Enums.AuditSource.User, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateUser_ShouldModifyUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, FirstName = "Alice", LastName = "Smith", IsActive = true };
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var command = new UpdateUserCommand(userId, "Alice New", "Smith New", "HR", "VP", "CA", null, null);
        var handler = new UpdateUserHandler(_userRepository, _roleRepository, _dynamicFieldService, _auditService, _mediator);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Alice New", result.FirstName);
        Assert.Equal("Smith New", result.LastName);
        Assert.Equal("HR", result.Department);
        Assert.Equal("VP", result.JobTitle);

        await _userRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogAsync("user.updated", "User", userId, Arg.Any<object>(), Arg.Any<object>(), JLT.Domain.Enums.AuditSource.User, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleUserStatus_ShouldChangeIsActive()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", IsActive = true };
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var command = new ToggleUserStatusCommand(userId, false);
        var handler = new ToggleUserStatusHandler(_userRepository, _auditService, _mediator);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);

        await _userRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogAsync("user.deactivated", "User", userId, Arg.Any<object>(), Arg.Any<object>(), JLT.Domain.Enums.AuditSource.User, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePassword_ShouldVerifyAndHashNewPassword()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", PasswordHash = "hashed_old" };
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("old_password", "hashed_old").Returns(true);
        _passwordHasher.Hash("new_password").Returns("hashed_new");

        var command = new ChangePasswordCommand(userId, "old_password", "new_password");
        var handler = new ChangePasswordHandler(_userRepository, _passwordHasher, _auditService);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal("hashed_new", user.PasswordHash);

        await _userRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogAsync("user.password_changed", "User", userId, Arg.Any<object>(), Arg.Any<object>(), JLT.Domain.Enums.AuditSource.User, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUsersByAttributes_ShouldReturnMatchedUsers()
    {
        // Arrange
        var attributesJson = "{\"department\":\"Sales\"}";
        var matchedUsers = new List<User> { new User { Id = Guid.NewGuid(), Email = "sales1@example.com", Attributes = attributesJson } };
        _userRepository.GetByAttributesAsync(attributesJson, Arg.Any<CancellationToken>()).Returns(matchedUsers);

        var query = new GetUsersByAttributesQuery(attributesJson);
        var handler = new GetUsersByAttributesHandler(_userRepository);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("sales1@example.com", result[0].Email);
    }
}
