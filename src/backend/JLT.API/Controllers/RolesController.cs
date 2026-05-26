using JLT.Application.Common;
using JLT.Application.DTOs;
using JLT.Application.Features.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JLT.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllRolesQuery());
        return Ok(ApiResponse<IReadOnlyList<RoleDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleCommand command)
    {
        var result = await _mediator.Send(command);
        return Created($"/api/roles/{result.Id}", ApiResponse<RoleDto>.Ok(result, "Role created."));
    }

    [HttpPut("{id:guid}/permissions")]
    public async Task<IActionResult> UpdatePermissions(Guid id, [FromBody] UpdateRolePermissionsCommand command)
    {
        if (id != command.RoleId) return BadRequest(ApiResponse.Fail("ID mismatch."));
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<RoleDto>.Ok(result, "Role permissions updated."));
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetAllPermissions()
    {
        var result = await _mediator.Send(new GetAllPermissionsQuery());
        return Ok(ApiResponse<IReadOnlyList<PermissionDto>>.Ok(result));
    }
}
