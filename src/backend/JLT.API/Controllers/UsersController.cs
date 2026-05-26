using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using JLT.Application.Common;
using JLT.Application.Common.Models;
using JLT.Application.DTOs;
using JLT.Application.Features.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JLT.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? role = null,
        [FromQuery] string? department = null,
        [FromQuery] string? location = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortColumn = null,
        [FromQuery] string? sortOrder = null)
    {
        var query = new GetUsersQuery(pageNumber, pageSize, role, department, location, isActive, searchTerm, sortColumn, sortOrder);
        var result = await _mediator.Send(query);
        return Ok(ApiResponse<PaginatedList<UserDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id));
        if (result == null) return NotFound(ApiResponse.Fail("User not found."));
        return Ok(ApiResponse<UserDto>.Ok(result));
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid user token."));

        var result = await _mediator.Send(new GetUserByIdQuery(userId));
        if (result == null) return NotFound(ApiResponse.Fail("User not found."));
        return Ok(ApiResponse<UserDto>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command)
    {
        var result = await _mediator.Send(command);
        return Created($"/api/users/{result.Id}", ApiResponse<UserDto>.Ok(result, "User created."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserCommand command)
    {
        if (id != command.Id) return BadRequest(ApiResponse.Fail("ID mismatch."));
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<UserDto>.Ok(result, "User updated."));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ToggleStatus(Guid id, [FromBody] ToggleUserStatusCommand command)
    {
        if (id != command.Id) return BadRequest(ApiResponse.Fail("ID mismatch."));
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<UserDto>.Ok(result, result.IsActive ? "User activated." : "User deactivated."));
    }

    [HttpPost("bulk-update")]
    public async Task<IActionResult> BulkUpdate([FromBody] BulkUpdateUsersCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<BulkUpdateResultDto>.Ok(result, $"{result.AffectedCount} users updated."));
    }

    [HttpPut("{id:guid}/roles")]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignRolesCommand command)
    {
        if (id != command.UserId) return BadRequest(ApiResponse.Fail("ID mismatch."));
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<UserDto>.Ok(result, "Roles assigned."));
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse.Fail("Invalid user token."));

        var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
        var result = await _mediator.Send(command);
        if (!result) return BadRequest(ApiResponse.Fail("Password change failed."));
        return Ok(ApiResponse.Ok("Password changed successfully."));
    }

    [HttpPost("search-by-attributes")]
    public async Task<IActionResult> SearchByAttributes([FromBody] SearchByAttributesRequest request)
    {
        var query = new GetUsersByAttributesQuery(request.AttributesJson);
        var result = await _mediator.Send(query);
        return Ok(ApiResponse<IReadOnlyList<UserDto>>.Ok(result, $"{result.Count} users matched."));
    }
}

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record SearchByAttributesRequest(string AttributesJson);
