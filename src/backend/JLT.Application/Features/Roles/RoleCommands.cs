using FluentValidation;
using JLT.Application.DTOs;
using JLT.Domain.Entities;
using JLT.Domain.Enums;
using JLT.Domain.Interfaces;
using MediatR;

namespace JLT.Application.Features.Roles;

// --- Create Role ---
public record CreateRoleCommand(string Name, string? Description, List<Guid>? PermissionIds) : IRequest<RoleDto>;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class CreateRoleHandler : IRequestHandler<CreateRoleCommand, RoleDto>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IAuditService _auditService;

    public CreateRoleHandler(IRoleRepository roleRepository, IAuditService auditService)
    {
        _roleRepository = roleRepository;
        _auditService = auditService;
    }

    public async Task<RoleDto> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var existing = await _roleRepository.GetByNameAsync(request.Name, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"Role '{request.Name}' already exists.");

        var role = new Role
        {
            Name = request.Name,
            Description = request.Description,
            IsSystemRole = false
        };

        if (request.PermissionIds?.Any() == true)
        {
            foreach (var permId in request.PermissionIds)
                role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permId });
        }

        await _roleRepository.AddAsync(role, cancellationToken);
        await _auditService.LogAsync("role.created", "Role", role.Id,
            newValues: new { role.Name }, cancellationToken: cancellationToken);

        var permKeys = role.RolePermissions.Select(rp => rp.Permission?.Key ?? rp.PermissionId.ToString()).ToList();
        return new RoleDto(role.Id, role.Name, role.Description, role.IsSystemRole, role.IsActive, permKeys);
    }
}

// --- Get All Roles ---
public record GetAllRolesQuery : IRequest<IReadOnlyList<RoleDto>>;

public class GetAllRolesHandler : IRequestHandler<GetAllRolesQuery, IReadOnlyList<RoleDto>>
{
    private readonly IRoleRepository _roleRepository;

    public GetAllRolesHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<IReadOnlyList<RoleDto>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _roleRepository.GetAllAsync(cancellationToken);
        var result = new List<RoleDto>();
        foreach (var role in roles)
        {
            var fullRole = await _roleRepository.GetWithPermissionsAsync(role.Id, cancellationToken);
            var permKeys = fullRole?.RolePermissions.Select(rp => rp.Permission!.Key).ToList() ?? new();
            result.Add(new RoleDto(role.Id, role.Name, role.Description, role.IsSystemRole, role.IsActive, permKeys));
        }
        return result;
    }
}

// --- Update Role Permissions ---
public record UpdateRolePermissionsCommand(Guid RoleId, List<Guid> PermissionIds) : IRequest<RoleDto>;

public class UpdateRolePermissionsHandler : IRequestHandler<UpdateRolePermissionsCommand, RoleDto>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IAuditService _auditService;

    public UpdateRolePermissionsHandler(IRoleRepository roleRepository, IAuditService auditService)
    {
        _roleRepository = roleRepository;
        _auditService = auditService;
    }

    public async Task<RoleDto> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetWithPermissionsAsync(request.RoleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Role {request.RoleId} not found.");

        if (role.IsSystemRole)
            throw new InvalidOperationException("Cannot modify permissions of a system role.");

        var oldPerms = role.RolePermissions.Select(rp => rp.PermissionId).ToList();

        role.RolePermissions.Clear();
        foreach (var permId in request.PermissionIds)
            role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permId });

        await _roleRepository.UpdateAsync(role, cancellationToken);

        await _auditService.LogAsync("role.permissions_updated", "Role", role.Id,
            oldValues: new { PermissionIds = oldPerms },
            newValues: new { PermissionIds = request.PermissionIds },
            cancellationToken: cancellationToken);

        var reloaded = await _roleRepository.GetWithPermissionsAsync(role.Id, cancellationToken);
        var permKeys = reloaded?.RolePermissions.Select(rp => rp.Permission!.Key).ToList() ?? new();
        return new RoleDto(role.Id, role.Name, role.Description, role.IsSystemRole, role.IsActive, permKeys);
    }
}

// --- Get All Permissions ---
public record GetAllPermissionsQuery : IRequest<IReadOnlyList<PermissionDto>>;

public class GetAllPermissionsHandler : IRequestHandler<GetAllPermissionsQuery, IReadOnlyList<PermissionDto>>
{
    private readonly IRepository<Permission> _permissionRepository;

    public GetAllPermissionsHandler(IRepository<Permission> permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    public async Task<IReadOnlyList<PermissionDto>> Handle(GetAllPermissionsQuery request, CancellationToken cancellationToken)
    {
        var perms = await _permissionRepository.GetAllAsync(cancellationToken);
        return perms.Select(p => new PermissionDto(p.Id, p.Key, p.Name, p.Category, p.Description)).ToList();
    }
}
