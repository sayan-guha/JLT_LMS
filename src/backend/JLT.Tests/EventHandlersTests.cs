using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JLT.Application.Features.UserGroups;
using JLT.Domain.Entities;
using JLT.Domain.Enums;
using JLT.Domain.Events;
using JLT.Domain.Interfaces;
using NSubstitute;
using Xunit;
using MockQueryable;

namespace JLT.Tests;

public class EventHandlersTests
{
    private readonly IUserRepository _userRepository;
    private readonly IUserGroupRepository _groupRepository;

    public EventHandlersTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _groupRepository = Substitute.For<IUserGroupRepository>();
    }

    [Fact]
    public async Task UserCreatedEventHandler_ShouldAddUserToGroup_WhenUserMatchesRules()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var rules = "{\"department\":\"Sales\"}";

        var group = new UserGroup
        {
            Id = Guid.NewGuid(),
            Type = GroupType.Dynamic,
            Rules = rules,
            Members = new List<UserGroupMember>()
        };

        var dynamicGroups = new List<UserGroup> { group };
        var mock = dynamicGroups.BuildMock();
        _groupRepository.Query().Returns(mock);

        var matchingUsers = new List<User> { new User { Id = userId } };
        _groupRepository.EvaluateDynamicGroupRulesAsync(rules, Arg.Any<CancellationToken>()).Returns(matchingUsers);

        var handler = new UserCreatedEventHandler(_groupRepository);
        var notification = new UserCreatedEvent(userId, tenantId);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.Single(group.Members);
        Assert.Equal(userId, group.Members.First().UserId);
        await _groupRepository.Received(1).UpdateAsync(group, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UserAttributesUpdatedEventHandler_ShouldAddOrRemoveUserCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var rules1 = "{\"department\":\"Sales\"}";
        var rules2 = "{\"department\":\"Marketing\"}";

        // Group 1: User is a member, but no longer matches rules -> should be removed
        var group1 = new UserGroup
        {
            Id = Guid.NewGuid(),
            Type = GroupType.Dynamic,
            Rules = rules1,
            Members = new List<UserGroupMember> { new UserGroupMember { UserId = userId } }
        };

        // Group 2: User is not a member, but now matches rules -> should be added
        var group2 = new UserGroup
        {
            Id = Guid.NewGuid(),
            Type = GroupType.Dynamic,
            Rules = rules2,
            Members = new List<UserGroupMember>()
        };

        var dynamicGroups = new List<UserGroup> { group1, group2 };
        var mock = dynamicGroups.BuildMock();
        _groupRepository.Query().Returns(mock);

        // Mock group1 evaluation: user no longer matches
        _groupRepository.EvaluateDynamicGroupRulesAsync(group1.Rules, Arg.Any<CancellationToken>()).Returns(new List<User>());

        // Mock group2 evaluation: user matches
        _groupRepository.EvaluateDynamicGroupRulesAsync(group2.Rules, Arg.Any<CancellationToken>()).Returns(new List<User> { new User { Id = userId } });

        var handler = new UserAttributesUpdatedEventHandler(_groupRepository);
        var notification = new UserAttributesUpdatedEvent(userId, tenantId);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.Empty(group1.Members); // Removed
        Assert.Single(group2.Members); // Added
        Assert.Equal(userId, group2.Members.First().UserId);

        await _groupRepository.Received(1).UpdateAsync(group1, Arg.Any<CancellationToken>());
        await _groupRepository.Received(1).UpdateAsync(group2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UserDeactivatedEventHandler_ShouldRevokeTokensAndRemoveFromGroups()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            RefreshTokens = new List<RefreshToken>
            {
                new RefreshToken { Token = "token1", RevokedAt = null, ExpiresAt = DateTime.UtcNow.AddDays(1) }
            }
        };

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var group = new UserGroup
        {
            Id = Guid.NewGuid(),
            Members = new List<UserGroupMember> { new UserGroupMember { UserId = userId } }
        };

        var groups = new List<UserGroup> { group };
        var mock = groups.BuildMock();
        _groupRepository.Query().Returns(mock);

        var handler = new UserDeactivatedEventHandler(_userRepository, _groupRepository);
        var notification = new UserDeactivatedEvent(userId, tenantId);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.NotNull(user.RefreshTokens.First().RevokedAt); // Revoked
        Assert.Empty(group.Members); // Removed from group

        await _userRepository.Received(1).UpdateRefreshTokenAsync(user.RefreshTokens.First(), Arg.Any<CancellationToken>());
        await _groupRepository.Received(1).UpdateAsync(group, Arg.Any<CancellationToken>());
    }
}
