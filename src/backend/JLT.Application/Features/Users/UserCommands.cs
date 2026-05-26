using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using JLT.Application.Common.Models;
using JLT.Application.DTOs;
using JLT.Domain.Entities;
using JLT.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JLT.Application.Features.Users;

// --- Get User by ID ---
public record GetUserByIdQuery(Guid Id) : IRequest<UserDto?>;

public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public GetUserByIdHandler(IUserRepository userRepository, IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user == null) return null;

        var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();

        return new UserDto(user.Id, user.TenantId, user.Email, user.FirstName, user.LastName,
            user.FullName, user.AvatarUrl, user.Department, user.JobTitle, user.Location,
            user.ManagerId, user.Attributes, user.IsActive, user.LastLoginAt, user.CreatedAt,
            user.UpdatedAt, roles);
    }
}

// --- Get Users (Paginated & Filterable) ---
public record GetUsersQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? Role = null,
    string? Department = null,
    string? Location = null,
    bool? IsActive = null,
    string? SearchTerm = null,
    string? SortColumn = null,
    string? SortOrder = null) : IRequest<PaginatedList<UserDto>>;

public class GetUsersHandler : IRequestHandler<GetUsersQuery, PaginatedList<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PaginatedList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        IQueryable<User> query = _userRepository.Query()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(search) ||
                u.LastName.ToLower().Contains(search) ||
                u.Email.ToLower().Contains(search) ||
                (u.Department != null && u.Department.ToLower().Contains(search)) ||
                (u.JobTitle != null && u.JobTitle.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role!.Name.ToLower() == request.Role.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            query = query.Where(u => u.Department != null && u.Department.ToLower() == request.Department.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            query = query.Where(u => u.Location != null && u.Location.ToLower() == request.Location.ToLower());
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SortColumn))
        {
            var descending = request.SortOrder?.ToLower() == "desc";
            query = request.SortColumn.ToLower() switch
            {
                "email" => descending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                "firstname" => descending ? query.OrderByDescending(u => u.FirstName) : query.OrderBy(u => u.FirstName),
                "lastname" => descending ? query.OrderByDescending(u => u.LastName) : query.OrderBy(u => u.LastName),
                "department" => descending ? query.OrderByDescending(u => u.Department) : query.OrderBy(u => u.Department),
                _ => descending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt)
            };
        }
        else
        {
            query = query.OrderByDescending(u => u.CreatedAt);
        }

        var pagedUsers = await PaginatedList<User>.CreateAsync(query, request.PageNumber, request.PageSize, cancellationToken);

        var dtoItems = pagedUsers.Items.Select(u => new UserDto(
            u.Id, u.TenantId, u.Email, u.FirstName, u.LastName, u.FullName, u.AvatarUrl,
            u.Department, u.JobTitle, u.Location, u.ManagerId, u.Attributes, u.IsActive,
            u.LastLoginAt, u.CreatedAt, u.UpdatedAt,
            u.UserRoles.Select(ur => ur.Role!.Name).ToList()
        )).ToList();

        return new PaginatedList<UserDto>(dtoItems, pagedUsers.TotalCount, pagedUsers.PageIndex, request.PageSize);
    }
}

// --- Create User (Admin Direct Endpoint) ---
public record CreateUserCommand(
    string Email, string Password, string FirstName, string LastName,
    string? Department = null, string? JobTitle = null, string? Location = null,
    Guid? ManagerId = null, string? Attributes = null, List<Guid>? RoleIds = null) : IRequest<UserDto>;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public class CreateUserHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDynamicFieldService _dynamicFieldService;
    private readonly IAuditService _auditService;
    private readonly IMediator _mediator;

    public CreateUserHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IDynamicFieldService dynamicFieldService,
        IAuditService auditService,
        IMediator mediator)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _dynamicFieldService = dynamicFieldService;
        _auditService = auditService;
        _mediator = mediator;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException("A user with this email already exists.");

        if (!string.IsNullOrEmpty(request.Attributes))
            await _dynamicFieldService.ValidateAttributesAsync(request.Attributes, cancellationToken);

        var user = new User
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

        if (request.RoleIds != null && request.RoleIds.Any())
        {
            foreach (var roleId in request.RoleIds)
            {
                user.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId
                });
            }
        }

        await _userRepository.AddAsync(user, cancellationToken);

        await _auditService.LogAsync("user.created", "User", user.Id,
            newValues: new { user.Email, user.FirstName, user.LastName },
            cancellationToken: cancellationToken);

        // Publish event
        await _mediator.Publish(new JLT.Domain.Events.UserCreatedEvent(user.Id, user.TenantId), cancellationToken);

        var roles = await _roleRepository.GetUserRolesAsync(user.Id, cancellationToken);
        var roleNames = roles.Select(r => r.Name).ToList();

        return new UserDto(user.Id, user.TenantId, user.Email, user.FirstName, user.LastName,
            user.FullName, user.AvatarUrl, user.Department, user.JobTitle, user.Location,
            user.ManagerId, user.Attributes, user.IsActive, user.LastLoginAt, user.CreatedAt,
            user.UpdatedAt, roleNames);
    }
}


// --- Update User ---
public record UpdateUserCommand(
    Guid Id, string? FirstName, string? LastName,
    string? Department, string? JobTitle, string? Location,
    Guid? ManagerId, string? Attributes) : IRequest<UserDto>;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => x.FirstName != null);
        RuleFor(x => x.LastName).MaximumLength(100).When(x => x.LastName != null);
    }
}

public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IDynamicFieldService _dynamicFieldService;
    private readonly IAuditService _auditService;
    private readonly IMediator _mediator;

    public UpdateUserHandler(IUserRepository userRepository, IRoleRepository roleRepository,
        IDynamicFieldService dynamicFieldService, IAuditService auditService, IMediator mediator)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _dynamicFieldService = dynamicFieldService;
        _auditService = auditService;
        _mediator = mediator;
    }

    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.Id} not found.");

        var oldValues = new { user.FirstName, user.LastName, user.Department, user.JobTitle, user.Location, user.Attributes };

        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.Department != null) user.Department = request.Department;
        if (request.JobTitle != null) user.JobTitle = request.JobTitle;
        if (request.Location != null) user.Location = request.Location;
        user.ManagerId = request.ManagerId;

        bool attributesChanged = false;
        if (request.Attributes != null)
        {
            await _dynamicFieldService.ValidateAttributesAsync(request.Attributes, cancellationToken);
            if (user.Attributes != request.Attributes)
            {
                user.Attributes = request.Attributes;
                attributesChanged = true;
            }
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        await _auditService.LogAsync("user.updated", "User", user.Id,
            oldValues: oldValues, newValues: new { user.FirstName, user.LastName, user.Department, user.JobTitle, user.Location, user.Attributes },
            cancellationToken: cancellationToken);

        if (attributesChanged)
        {
            // Publish event
            await _mediator.Publish(new JLT.Domain.Events.UserAttributesUpdatedEvent(user.Id, user.TenantId), cancellationToken);
        }

        var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();
        return new UserDto(user.Id, user.TenantId, user.Email, user.FirstName, user.LastName,
            user.FullName, user.AvatarUrl, user.Department, user.JobTitle, user.Location,
            user.ManagerId, user.Attributes, user.IsActive, user.LastLoginAt, user.CreatedAt,
            user.UpdatedAt, roles);
    }
}


// --- Toggle User Status (Activate/Deactivate) ---
public record ToggleUserStatusCommand(Guid Id, bool IsActive) : IRequest<UserDto>;

public class ToggleUserStatusHandler : IRequestHandler<ToggleUserStatusCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditService _auditService;
    private readonly IMediator _mediator;

    public ToggleUserStatusHandler(IUserRepository userRepository, IAuditService auditService, IMediator mediator)
    {
        _userRepository = userRepository;
        _auditService = auditService;
        _mediator = mediator;
    }

    public async Task<UserDto> Handle(ToggleUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.Id} not found.");

        bool wasActive = user.IsActive;
        user.IsActive = request.IsActive;
        await _userRepository.UpdateAsync(user, cancellationToken);

        var action = request.IsActive ? "user.activated" : "user.deactivated";
        await _auditService.LogAsync(action, "User", user.Id, cancellationToken: cancellationToken);

        if (wasActive && !request.IsActive)
        {
            // Publish deactivation event
            await _mediator.Publish(new JLT.Domain.Events.UserDeactivatedEvent(user.Id, user.TenantId), cancellationToken);
        }

        var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();
        return new UserDto(user.Id, user.TenantId, user.Email, user.FirstName, user.LastName,
            user.FullName, user.AvatarUrl, user.Department, user.JobTitle, user.Location,
            user.ManagerId, user.Attributes, user.IsActive, user.LastLoginAt, user.CreatedAt,
            user.UpdatedAt, roles);
    }
}


// --- Assign Roles to User ---
public record AssignRolesCommand(Guid UserId, List<Guid> RoleIds) : IRequest<UserDto>;

public class AssignRolesHandler : IRequestHandler<AssignRolesCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IAuditService _auditService;

    public AssignRolesHandler(IUserRepository userRepository, IRoleRepository roleRepository, IAuditService auditService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _auditService = auditService;
    }

    public async Task<UserDto> Handle(AssignRolesCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found.");

        var oldRoles = user.UserRoles.Select(ur => ur.RoleId).ToList();

        user.UserRoles.Clear();
        foreach (var roleId in request.RoleIds)
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = roleId
            });
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        await _auditService.LogAsync("user.roles_assigned", "User", user.Id,
            oldValues: new { RoleIds = oldRoles },
            newValues: new { RoleIds = request.RoleIds },
            cancellationToken: cancellationToken);

        var roles = await _roleRepository.GetUserRolesAsync(user.Id, cancellationToken);
        var roleNames = roles.Select(r => r.Name).ToList();

        return new UserDto(user.Id, user.TenantId, user.Email, user.FirstName, user.LastName,
            user.FullName, user.AvatarUrl, user.Department, user.JobTitle, user.Location,
            user.ManagerId, user.Attributes, user.IsActive, user.LastLoginAt, user.CreatedAt,
            user.UpdatedAt, roleNames);
    }
}

// --- Change Password Command ---
public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest<bool>;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _auditService;

    public ChangePasswordHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IAuditService auditService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _auditService = auditService;
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found.");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Incorrect current password.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _userRepository.UpdateAsync(user, cancellationToken);

        await _auditService.LogAsync("user.password_changed", "User", user.Id, cancellationToken: cancellationToken);

        return true;
    }
}

// --- Get Users by Attributes JSONB ---
public record GetUsersByAttributesQuery(string AttributesJson) : IRequest<IReadOnlyList<UserDto>>;

public class GetUsersByAttributesHandler : IRequestHandler<GetUsersByAttributesQuery, IReadOnlyList<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersByAttributesHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<UserDto>> Handle(GetUsersByAttributesQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetByAttributesAsync(request.AttributesJson, cancellationToken);
        return users.Select(u => new UserDto(
            u.Id, u.TenantId, u.Email, u.FirstName, u.LastName, u.FullName, u.AvatarUrl,
            u.Department, u.JobTitle, u.Location, u.ManagerId, u.Attributes, u.IsActive,
            u.LastLoginAt, u.CreatedAt, u.UpdatedAt, new List<string>()
        )).ToList();
    }
}

// --- Bulk Update Users ---
public record BulkUpdateUsersCommand(
    UserFilterModel Filter,
    string Action,
    UserUpdatesModel? Updates = null) : IRequest<BulkUpdateResultDto>;

public record UserFilterModel(
    string? Role = null,
    string? Department = null,
    string? Location = null,
    bool? IsActive = null,
    string? AttributesJson = null);

public record UserUpdatesModel(
    string? Department = null,
    string? Location = null,
    string? AttributesJson = null);

public record BulkUpdateResultDto(int AffectedCount, List<Guid> UserIds);

public class BulkUpdateUsersHandler : IRequestHandler<BulkUpdateUsersCommand, BulkUpdateResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditService _auditService;

    public BulkUpdateUsersHandler(IUserRepository userRepository, IAuditService auditService)
    {
        _userRepository = userRepository;
        _auditService = auditService;
    }

    public async Task<BulkUpdateResultDto> Handle(BulkUpdateUsersCommand request, CancellationToken cancellationToken)
    {
        var query = _userRepository.Query();

        if (!string.IsNullOrWhiteSpace(request.Filter.Role))
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role!.Name.ToLower() == request.Filter.Role.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.Filter.Department))
        {
            query = query.Where(u => u.Department != null && u.Department.ToLower() == request.Filter.Department.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(request.Filter.Location))
        {
            query = query.Where(u => u.Location != null && u.Location.ToLower() == request.Filter.Location.ToLower());
        }

        if (request.Filter.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == request.Filter.IsActive.Value);
        }

        var users = await query.ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Filter.AttributesJson))
        {
            var filterDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(request.Filter.AttributesJson);
            if (filterDict != null && filterDict.Any())
            {
                users = users.Where(u =>
                {
                    if (string.IsNullOrEmpty(u.Attributes)) return false;
                    var userDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(u.Attributes);
                    if (userDict == null) return false;
                    foreach (var kvp in filterDict)
                    {
                        if (!userDict.TryGetValue(kvp.Key, out var val) || val?.ToString() != kvp.Value?.ToString())
                            return false;
                    }
                    return true;
                }).ToList();
            }
        }

        var userIds = users.Select(u => u.Id).ToList();
        if (!userIds.Any())
        {
            return new BulkUpdateResultDto(0, new List<Guid>());
        }

        int affected = 0;

        if (request.Action.Equals("activate", StringComparison.OrdinalIgnoreCase))
        {
            affected = await _userRepository.BulkUpdateStatusAsync(userIds, true, cancellationToken);
            foreach (var id in userIds)
            {
                await _auditService.LogAsync("user.activated", "User", id, cancellationToken: cancellationToken);
            }
        }
        else if (request.Action.Equals("deactivate", StringComparison.OrdinalIgnoreCase))
        {
            affected = await _userRepository.BulkUpdateStatusAsync(userIds, false, cancellationToken);
            foreach (var id in userIds)
            {
                await _auditService.LogAsync("user.deactivated", "User", id, cancellationToken: cancellationToken);
            }
        }
        else if (request.Action.Equals("update_attributes", StringComparison.OrdinalIgnoreCase) && request.Updates != null)
        {
            if (!string.IsNullOrEmpty(request.Updates.AttributesJson))
            {
                affected = await _userRepository.BulkUpdateAttributesAsync(userIds, request.Updates.AttributesJson, cancellationToken);
                foreach (var id in userIds)
                {
                    await _auditService.LogAsync("user.attributes_updated", "User", id,
                        newValues: new { Attributes = request.Updates.AttributesJson },
                        cancellationToken: cancellationToken);
                }
            }
        }

        return new BulkUpdateResultDto(affected, userIds);
    }
}
