using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using JLT.Application.Common.Models;
using JLT.Application.DTOs;
using JLT.Domain.Entities;
using JLT.Domain.Enums;
using JLT.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JLT.Application.Features.UserGroups;

// --- Create Group ---
public record CreateGroupCommand(
    string Name, string? Description, string Type, string? Rules, List<Guid>? UserIds = null) : IRequest<UserGroupDto>;

public class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Type).NotEmpty().Must(t => t is "Static" or "Dynamic");
    }
}

public class CreateGroupHandler : IRequestHandler<CreateGroupCommand, UserGroupDto>
{
    private readonly IUserGroupRepository _groupRepository;
    private readonly IAuditService _auditService;

    public CreateGroupHandler(IUserGroupRepository groupRepository, IAuditService auditService)
    {
        _groupRepository = groupRepository;
        _auditService = auditService;
    }

    public async Task<UserGroupDto> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        var group = new UserGroup
        {
            Name = request.Name,
            Description = request.Description,
            Type = Enum.Parse<GroupType>(request.Type),
            Rules = request.Rules
        };

        if (group.Type == GroupType.Static && request.UserIds != null && request.UserIds.Any())
        {
            foreach (var userId in request.UserIds)
            {
                group.Members.Add(new UserGroupMember
                {
                    GroupId = group.Id,
                    UserId = userId
                });
            }
        }
        else if (group.Type == GroupType.Dynamic && !string.IsNullOrWhiteSpace(group.Rules))
        {
            var matchingUsers = await _groupRepository.EvaluateDynamicGroupRulesAsync(group.Rules, cancellationToken);
            foreach (var user in matchingUsers)
            {
                group.Members.Add(new UserGroupMember
                {
                    GroupId = group.Id,
                    UserId = user.Id
                });
            }
        }

        await _groupRepository.AddAsync(group, cancellationToken);
        await _auditService.LogAsync("group.created", "UserGroup", group.Id,
            newValues: new { group.Name, group.Type }, cancellationToken: cancellationToken);

        return new UserGroupDto(group.Id, group.Name, group.Description, group.Type.ToString(),
            group.Rules, group.Members.Count, group.CreatedAt);
    }
}

// --- Get Group with Members ---
public record GetGroupWithMembersQuery(Guid Id) : IRequest<UserGroupDto?>;

public class GetGroupWithMembersHandler : IRequestHandler<GetGroupWithMembersQuery, UserGroupDto?>
{
    private readonly IUserGroupRepository _groupRepository;

    public GetGroupWithMembersHandler(IUserGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task<UserGroupDto?> Handle(GetGroupWithMembersQuery request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetWithMembersAsync(request.Id, cancellationToken);
        if (group == null) return null;

        return new UserGroupDto(group.Id, group.Name, group.Description, group.Type.ToString(),
            group.Rules, group.Members.Count, group.CreatedAt);
    }
}

// --- Get User Groups (Paginated) ---
public record GetUserGroupsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? Type = null,
    string? SearchTerm = null) : IRequest<PaginatedList<UserGroupDto>>;

public class GetUserGroupsHandler : IRequestHandler<GetUserGroupsQuery, PaginatedList<UserGroupDto>>
{
    private readonly IUserGroupRepository _groupRepository;

    public GetUserGroupsHandler(IUserGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task<PaginatedList<UserGroupDto>> Handle(GetUserGroupsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<UserGroup> query = _groupRepository.Query()
            .Include(g => g.Members);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(g => g.Name.ToLower().Contains(search) || (g.Description != null && g.Description.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            if (Enum.TryParse<GroupType>(request.Type, true, out var groupType))
            {
                query = query.Where(g => g.Type == groupType);
            }
        }

        query = query.OrderByDescending(g => g.CreatedAt);

        var pagedGroups = await PaginatedList<UserGroup>.CreateAsync(query, request.PageNumber, request.PageSize, cancellationToken);

        var dtoItems = pagedGroups.Items.Select(g => new UserGroupDto(g.Id, g.Name, g.Description, g.Type.ToString(),
            g.Rules, g.Members.Count, g.CreatedAt)).ToList();

        return new PaginatedList<UserGroupDto>(dtoItems, pagedGroups.TotalCount, pagedGroups.PageIndex, request.PageSize);
    }
}

// --- Update User Group ---
public record UpdateUserGroupCommand(Guid Id, string? Name, string? Description, string? Rules) : IRequest<UserGroupDto>;

public class UpdateUserGroupHandler : IRequestHandler<UpdateUserGroupCommand, UserGroupDto>
{
    private readonly IUserGroupRepository _groupRepository;
    private readonly IAuditService _auditService;

    public UpdateUserGroupHandler(IUserGroupRepository groupRepository, IAuditService auditService)
    {
        _groupRepository = groupRepository;
        _auditService = auditService;
    }

    public async Task<UserGroupDto> Handle(UpdateUserGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetWithMembersAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Group {request.Id} not found.");

        var oldValues = new { group.Name, group.Description, group.Rules };

        if (request.Name != null) group.Name = request.Name;
        if (request.Description != null) group.Description = request.Description;

        bool rulesChanged = false;
        if (request.Rules != null && request.Rules != group.Rules)
        {
            if (group.Type != GroupType.Dynamic)
                throw new InvalidOperationException("Rules can only be set on dynamic groups.");
            group.Rules = request.Rules;
            rulesChanged = true;
        }

        await _groupRepository.UpdateAsync(group, cancellationToken);

        await _auditService.LogAsync("group.updated", "UserGroup", group.Id,
            oldValues: oldValues,
            newValues: new { group.Name, group.Description, group.Rules },
            cancellationToken: cancellationToken);

        int memberCount = group.Members.Count;
        if (rulesChanged && !string.IsNullOrWhiteSpace(group.Rules))
        {
            var matchingUsers = await _groupRepository.EvaluateDynamicGroupRulesAsync(group.Rules, cancellationToken);
            var matchingUserIds = matchingUsers.Select(u => u.Id).ToHashSet();

            group.Members.Clear();
            foreach (var userId in matchingUserIds)
            {
                group.Members.Add(new UserGroupMember
                {
                    GroupId = group.Id,
                    UserId = userId
                });
            }
            await _groupRepository.UpdateAsync(group, cancellationToken);
            memberCount = matchingUserIds.Count;
        }

        return new UserGroupDto(group.Id, group.Name, group.Description, group.Type.ToString(),
            group.Rules, memberCount, group.CreatedAt);
    }
}

// --- Delete User Group ---
public record DeleteUserGroupCommand(Guid Id) : IRequest<bool>;

public class DeleteUserGroupHandler : IRequestHandler<DeleteUserGroupCommand, bool>
{
    private readonly IUserGroupRepository _groupRepository;
    private readonly IAuditService _auditService;

    public DeleteUserGroupHandler(IUserGroupRepository groupRepository, IAuditService auditService)
    {
        _groupRepository = groupRepository;
        _auditService = auditService;
    }

    public async Task<bool> Handle(DeleteUserGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdAsync(request.Id, cancellationToken);
        if (group == null) return false;

        await _groupRepository.DeleteAsync(group, cancellationToken);
        await _auditService.LogAsync("group.deleted", "UserGroup", group.Id, cancellationToken: cancellationToken);
        return true;
    }
}

// --- Add Members to Group ---
public record AddGroupMembersCommand(Guid GroupId, List<Guid> UserIds) : IRequest<int>;

public class AddGroupMembersHandler : IRequestHandler<AddGroupMembersCommand, int>
{
    private readonly IUserGroupRepository _groupRepository;
    private readonly IAuditService _auditService;

    public AddGroupMembersHandler(IUserGroupRepository groupRepository, IAuditService auditService)
    {
        _groupRepository = groupRepository;
        _auditService = auditService;
    }

    public async Task<int> Handle(AddGroupMembersCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetWithMembersAsync(request.GroupId, cancellationToken)
            ?? throw new KeyNotFoundException($"Group {request.GroupId} not found.");

        if (group.Type != GroupType.Static)
            throw new InvalidOperationException("Members can only be manually added to static groups.");

        var existingUserIds = group.Members.Select(m => m.UserId).ToHashSet();
        var newMembers = request.UserIds.Where(id => !existingUserIds.Contains(id)).ToList();

        foreach (var userId in newMembers)
        {
            group.Members.Add(new UserGroupMember
            {
                GroupId = group.Id,
                UserId = userId
            });
        }

        await _groupRepository.UpdateAsync(group, cancellationToken);
        await _auditService.LogAsync("group.members_added", "UserGroup", group.Id,
            newValues: new { AddedUserIds = newMembers }, cancellationToken: cancellationToken);

        return newMembers.Count;
    }
}

// --- Remove Members from Group ---
public record RemoveGroupMembersCommand(Guid GroupId, List<Guid> UserIds) : IRequest<int>;

public class RemoveGroupMembersHandler : IRequestHandler<RemoveGroupMembersCommand, int>
{
    private readonly IUserGroupRepository _groupRepository;
    private readonly IAuditService _auditService;

    public RemoveGroupMembersHandler(IUserGroupRepository groupRepository, IAuditService auditService)
    {
        _groupRepository = groupRepository;
        _auditService = auditService;
    }

    public async Task<int> Handle(RemoveGroupMembersCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetWithMembersAsync(request.GroupId, cancellationToken)
            ?? throw new KeyNotFoundException($"Group {request.GroupId} not found.");

        if (group.Type != GroupType.Static)
            throw new InvalidOperationException("Members can only be manually removed from static groups.");

        var removedMembers = group.Members.Where(m => request.UserIds.Contains(m.UserId)).ToList();
        foreach (var member in removedMembers)
        {
            group.Members.Remove(member);
        }

        await _groupRepository.UpdateAsync(group, cancellationToken);
        await _auditService.LogAsync("group.members_removed", "UserGroup", group.Id,
            newValues: new { RemovedUserIds = request.UserIds }, cancellationToken: cancellationToken);

        return removedMembers.Count;
    }
}

// --- Refresh Dynamic Group ---
public record RefreshDynamicGroupCommand(Guid GroupId) : IRequest<RefreshDynamicGroupResultDto>;

public record RefreshDynamicGroupResultDto(
    int PreviousCount,
    int NewCount,
    List<Guid> AddedUserIds,
    List<Guid> RemovedUserIds);

public class RefreshDynamicGroupHandler : IRequestHandler<RefreshDynamicGroupCommand, RefreshDynamicGroupResultDto>
{
    private readonly IUserGroupRepository _groupRepository;
    private readonly IAuditService _auditService;

    public RefreshDynamicGroupHandler(IUserGroupRepository groupRepository, IAuditService auditService)
    {
        _groupRepository = groupRepository;
        _auditService = auditService;
    }

    public async Task<RefreshDynamicGroupResultDto> Handle(RefreshDynamicGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetWithMembersAsync(request.GroupId, cancellationToken)
            ?? throw new KeyNotFoundException($"Group {request.GroupId} not found.");

        if (group.Type != GroupType.Dynamic)
            throw new InvalidOperationException("Only dynamic groups can be refreshed.");

        if (string.IsNullOrWhiteSpace(group.Rules))
            throw new InvalidOperationException("Dynamic group has no rules configured.");

        var matchingUsers = await _groupRepository.EvaluateDynamicGroupRulesAsync(group.Rules, cancellationToken);
        var matchingUserIds = matchingUsers.Select(u => u.Id).ToHashSet();

        var existingUserIds = group.Members.Select(m => m.UserId).ToHashSet();

        var added = matchingUserIds.Except(existingUserIds).ToList();
        var removed = existingUserIds.Except(matchingUserIds).ToList();

        var membersToRemove = group.Members.Where(m => removed.Contains(m.UserId)).ToList();
        foreach (var member in membersToRemove)
        {
            group.Members.Remove(member);
        }

        foreach (var userId in added)
        {
            group.Members.Add(new UserGroupMember
            {
                GroupId = group.Id,
                UserId = userId
            });
        }

        await _groupRepository.UpdateAsync(group, cancellationToken);

        await _auditService.LogAsync("group.refreshed", "UserGroup", group.Id,
            oldValues: new { MemberCount = existingUserIds.Count },
            newValues: new { MemberCount = matchingUserIds.Count, AddedCount = added.Count, RemovedCount = removed.Count },
            cancellationToken: cancellationToken);

        return new RefreshDynamicGroupResultDto(existingUserIds.Count, matchingUserIds.Count, added, removed);
    }
}

// --- Preview Dynamic Group Rules ---
public record PreviewDynamicGroupQuery(string RulesJson) : IRequest<IReadOnlyList<UserDto>>;

public class PreviewDynamicGroupHandler : IRequestHandler<PreviewDynamicGroupQuery, IReadOnlyList<UserDto>>
{
    private readonly IUserGroupRepository _groupRepository;

    public PreviewDynamicGroupHandler(IUserGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task<IReadOnlyList<UserDto>> Handle(PreviewDynamicGroupQuery request, CancellationToken cancellationToken)
    {
        var users = await _groupRepository.EvaluateDynamicGroupRulesAsync(request.RulesJson, cancellationToken);
        return users.Select(u => new UserDto(u.Id, u.TenantId, u.Email, u.FirstName, u.LastName,
            u.FullName, u.AvatarUrl, u.Department, u.JobTitle, u.Location,
            u.ManagerId, u.Attributes, u.IsActive, u.LastLoginAt, u.CreatedAt,
            u.UpdatedAt, new List<string>())).ToList();
    }
}
