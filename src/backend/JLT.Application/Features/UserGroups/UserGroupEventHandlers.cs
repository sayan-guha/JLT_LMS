using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JLT.Domain.Entities;
using JLT.Domain.Enums;
using JLT.Domain.Events;
using JLT.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JLT.Application.Features.UserGroups;

public class UserCreatedEventHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly IUserGroupRepository _groupRepository;

    public UserCreatedEventHandler(IUserGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Load all dynamic groups
        var dynamicGroups = await _groupRepository.Query()
            .Include(g => g.Members)
            .Where(g => g.Type == GroupType.Dynamic && g.Rules != null)
            .ToListAsync(cancellationToken);

        foreach (var group in dynamicGroups)
        {
            if (string.IsNullOrWhiteSpace(group.Rules)) continue;

            var matchingUsers = await _groupRepository.EvaluateDynamicGroupRulesAsync(group.Rules, cancellationToken);
            if (matchingUsers.Any(u => u.Id == notification.UserId))
            {
                if (!group.Members.Any(m => m.UserId == notification.UserId))
                {
                    group.Members.Add(new UserGroupMember
                    {
                        GroupId = group.Id,
                        UserId = notification.UserId
                    });
                    await _groupRepository.UpdateAsync(group, cancellationToken);
                }
            }
        }
    }
}

public class UserAttributesUpdatedEventHandler : INotificationHandler<UserAttributesUpdatedEvent>
{
    private readonly IUserGroupRepository _groupRepository;

    public UserAttributesUpdatedEventHandler(IUserGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task Handle(UserAttributesUpdatedEvent notification, CancellationToken cancellationToken)
    {
        // Load all dynamic groups
        var dynamicGroups = await _groupRepository.Query()
            .Include(g => g.Members)
            .Where(g => g.Type == GroupType.Dynamic && g.Rules != null)
            .ToListAsync(cancellationToken);

        foreach (var group in dynamicGroups)
        {
            if (string.IsNullOrWhiteSpace(group.Rules)) continue;

            var matchingUsers = await _groupRepository.EvaluateDynamicGroupRulesAsync(group.Rules, cancellationToken);
            var isMatch = matchingUsers.Any(u => u.Id == notification.UserId);
            var member = group.Members.FirstOrDefault(m => m.UserId == notification.UserId);

            if (isMatch && member == null)
            {
                // Add to group
                group.Members.Add(new UserGroupMember
                {
                    GroupId = group.Id,
                    UserId = notification.UserId
                });
                await _groupRepository.UpdateAsync(group, cancellationToken);
            }
            else if (!isMatch && member != null)
            {
                // Remove from group
                group.Members.Remove(member);
                await _groupRepository.UpdateAsync(group, cancellationToken);
            }
        }
    }
}

public class UserDeactivatedEventHandler : INotificationHandler<UserDeactivatedEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserGroupRepository _groupRepository;

    public UserDeactivatedEventHandler(IUserRepository userRepository, IUserGroupRepository groupRepository)
    {
        _userRepository = userRepository;
        _groupRepository = groupRepository;
    }

    public async Task Handle(UserDeactivatedEvent notification, CancellationToken cancellationToken)
    {
        // 1. Session Cleanup: Revoke all active refresh tokens for the user
        var user = await _userRepository.GetByIdAsync(notification.UserId, cancellationToken);
        if (user != null)
        {
            // We load the user. Ideally user has refresh tokens loaded, but we can revoke them in DB or loop
            foreach (var token in user.RefreshTokens.Where(t => t.IsActive))
            {
                token.RevokedAt = DateTime.UtcNow;
                await _userRepository.UpdateRefreshTokenAsync(token, cancellationToken);
            }
        }

        // 2. Remove user from all groups (since they are deactivated)
        var groupsWithUser = await _groupRepository.Query()
            .Include(g => g.Members)
            .Where(g => g.Members.Any(m => m.UserId == notification.UserId))
            .ToListAsync(cancellationToken);

        foreach (var group in groupsWithUser)
        {
            var member = group.Members.FirstOrDefault(m => m.UserId == notification.UserId);
            if (member != null)
            {
                group.Members.Remove(member);
                await _groupRepository.UpdateAsync(group, cancellationToken);
            }
        }
    }
}
