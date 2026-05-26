using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JLT.Application.Common.Models;
using JLT.Application.DTOs;
using JLT.Application.Features.UserGroups;
using JLT.Domain.Entities;
using JLT.Domain.Enums;
using JLT.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace JLT.Tests;

public class UserGroupTests
{
    private readonly IUserGroupRepository _groupRepository;
    private readonly IAuditService _auditService;

    public UserGroupTests()
    {
        _groupRepository = Substitute.For<IUserGroupRepository>();
        _auditService = Substitute.For<IAuditService>();
    }

    [Fact]
    public async Task CreateStaticGroup_ShouldCreateGroup_WithInitialMembers()
    {
        // Arrange
        var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var command = new CreateGroupCommand("Static Cohort", "Static group description", "Static", null, userIds);

        var handler = new CreateGroupHandler(_groupRepository, _auditService);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Static Cohort", result.Name);
        Assert.Equal("Static group description", result.Description);
        Assert.Equal("Static", result.Type);
        Assert.Equal(2, result.MemberCount);

        await _groupRepository.Received(1).AddAsync(Arg.Is<UserGroup>(g => g.Name == command.Name && g.Type == GroupType.Static && g.Members.Count == 2), Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogAsync("group.created", "UserGroup", Arg.Any<Guid>(), Arg.Any<object>(), Arg.Any<object>(), JLT.Domain.Enums.AuditSource.User, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDynamicGroup_ShouldEvaluateRulesAndAddMembers()
    {
        // Arrange
        var rules = "{\"department\":\"Sales\"}";
        var command = new CreateGroupCommand("Dynamic Cohort", "Dynamic group description", "Dynamic", rules, null);

        var matchingUsers = new List<User> { new User { Id = Guid.NewGuid() } };
        _groupRepository.EvaluateDynamicGroupRulesAsync(rules, Arg.Any<CancellationToken>()).Returns(matchingUsers);

        var handler = new CreateGroupHandler(_groupRepository, _auditService);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Dynamic Cohort", result.Name);
        Assert.Equal("Dynamic", result.Type);
        Assert.Equal(1, result.MemberCount);

        await _groupRepository.Received(1).EvaluateDynamicGroupRulesAsync(rules, Arg.Any<CancellationToken>());
        await _groupRepository.Received(1).AddAsync(Arg.Is<UserGroup>(g => g.Name == command.Name && g.Type == GroupType.Dynamic && g.Members.Count == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddGroupMembers_ShouldAddOnlyNewMembersToStaticGroup()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var user3Id = Guid.NewGuid();

        var group = new UserGroup
        {
            Id = groupId,
            Type = GroupType.Static,
            Members = new List<UserGroupMember> { new UserGroupMember { GroupId = groupId, UserId = user1Id } }
        };

        _groupRepository.GetWithMembersAsync(groupId, Arg.Any<CancellationToken>()).Returns(group);

        var command = new AddGroupMembersCommand(groupId, new List<Guid> { user1Id, user2Id, user3Id });
        var handler = new AddGroupMembersHandler(_groupRepository, _auditService);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(2, result);
        Assert.Equal(3, group.Members.Count);

        await _groupRepository.Received(1).UpdateAsync(group, Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogAsync("group.members_added", "UserGroup", groupId, Arg.Any<object>(), Arg.Any<object>(), JLT.Domain.Enums.AuditSource.User, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveGroupMembers_ShouldRemoveMembersFromStaticGroup()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        var group = new UserGroup
        {
            Id = groupId,
            Type = GroupType.Static,
            Members = new List<UserGroupMember>
            {
                new UserGroupMember { GroupId = groupId, UserId = user1Id },
                new UserGroupMember { GroupId = groupId, UserId = user2Id }
            }
        };

        _groupRepository.GetWithMembersAsync(groupId, Arg.Any<CancellationToken>()).Returns(group);

        var command = new RemoveGroupMembersCommand(groupId, new List<Guid> { user1Id });
        var handler = new RemoveGroupMembersHandler(_groupRepository, _auditService);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
        Assert.Single(group.Members);
        Assert.Equal(user2Id, group.Members.First().UserId);

        await _groupRepository.Received(1).UpdateAsync(group, Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogAsync("group.members_removed", "UserGroup", groupId, Arg.Any<object>(), Arg.Any<object>(), JLT.Domain.Enums.AuditSource.User, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshDynamicGroup_ShouldSyncMembersWithRules()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var user1Id = Guid.NewGuid(); // current member, should stay
        var user2Id = Guid.NewGuid(); // current member, should be removed
        var user3Id = Guid.NewGuid(); // new member, should be added
        var rules = "{\"department\":\"Sales\"}";

        var group = new UserGroup
        {
            Id = groupId,
            Type = GroupType.Dynamic,
            Rules = rules,
            Members = new List<UserGroupMember>
            {
                new UserGroupMember { GroupId = groupId, UserId = user1Id },
                new UserGroupMember { GroupId = groupId, UserId = user2Id }
            }
        };

        _groupRepository.GetWithMembersAsync(groupId, Arg.Any<CancellationToken>()).Returns(group);

        var matchingUsers = new List<User>
        {
            new User { Id = user1Id },
            new User { Id = user3Id }
        };
        _groupRepository.EvaluateDynamicGroupRulesAsync(rules, Arg.Any<CancellationToken>()).Returns(matchingUsers);

        var command = new RefreshDynamicGroupCommand(groupId);
        var handler = new RefreshDynamicGroupHandler(_groupRepository, _auditService);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.PreviousCount);
        Assert.Equal(2, result.NewCount);
        Assert.Single(result.AddedUserIds);
        Assert.Equal(user3Id, result.AddedUserIds.First());
        Assert.Single(result.RemovedUserIds);
        Assert.Equal(user2Id, result.RemovedUserIds.First());

        Assert.Equal(2, group.Members.Count);
        Assert.Contains(group.Members, m => m.UserId == user1Id);
        Assert.Contains(group.Members, m => m.UserId == user3Id);

        await _groupRepository.Received(1).UpdateAsync(group, Arg.Any<CancellationToken>());
    }
}
