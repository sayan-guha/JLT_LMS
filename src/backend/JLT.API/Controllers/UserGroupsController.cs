using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JLT.Application.Common;
using JLT.Application.Common.Models;
using JLT.Application.DTOs;
using JLT.Application.Features.UserGroups;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JLT.API.Controllers;

[ApiController]
[Route("api/user-groups")]
[Authorize]
public class UserGroupsController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserGroupsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetUserGroups(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? type = null,
        [FromQuery] string? searchTerm = null)
    {
        var query = new GetUserGroupsQuery(pageNumber, pageSize, type, searchTerm);
        var result = await _mediator.Send(query);
        return Ok(ApiResponse<PaginatedList<UserGroupDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetGroupWithMembersQuery(id));
        if (result == null) return NotFound(ApiResponse.Fail("Group not found."));
        return Ok(ApiResponse<UserGroupDto>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGroupCommand command)
    {
        var result = await _mediator.Send(command);
        return Created($"/api/user-groups/{result.Id}", ApiResponse<UserGroupDto>.Ok(result, "Group created."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserGroupCommand command)
    {
        if (id != command.Id) return BadRequest(ApiResponse.Fail("ID mismatch."));
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<UserGroupDto>.Ok(result, "Group updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteUserGroupCommand(id));
        if (!result) return NotFound(ApiResponse.Fail("Group not found."));
        return Ok(ApiResponse.Ok("Group deleted."));
    }

    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddMembers(Guid id, [FromBody] List<Guid> userIds)
    {
        var command = new AddGroupMembersCommand(id, userIds);
        var added = await _mediator.Send(command);
        return Ok(ApiResponse<int>.Ok(added, $"{added} members added."));
    }

    [HttpDelete("{id:guid}/members")]
    public async Task<IActionResult> RemoveMembers(Guid id, [FromBody] List<Guid> userIds)
    {
        var command = new RemoveGroupMembersCommand(id, userIds);
        var removed = await _mediator.Send(command);
        return Ok(ApiResponse<int>.Ok(removed, $"{removed} members removed."));
    }

    [HttpPost("{id:guid}/refresh")]
    public async Task<IActionResult> RefreshDynamicGroup(Guid id)
    {
        var result = await _mediator.Send(new RefreshDynamicGroupCommand(id));
        return Ok(ApiResponse<RefreshDynamicGroupResultDto>.Ok(result, "Dynamic group membership refreshed."));
    }

    [HttpPost("preview")]
    public async Task<IActionResult> PreviewDynamicGroup([FromBody] PreviewRequest request)
    {
        var query = new PreviewDynamicGroupQuery(request.Rules);
        var result = await _mediator.Send(query);
        return Ok(ApiResponse<IReadOnlyList<UserDto>>.Ok(result, $"{result.Count} users matched."));
    }
}

public record PreviewRequest(string Rules);
