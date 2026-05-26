using System.Security.Claims;
using JLT.Application.Common;
using JLT.Application.DTOs;
using JLT.Application.Features.LearningContent;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JLT.API.Controllers;

[ApiController]
[Route("api/scorm")]
[Authorize]
public class ScormController : ControllerBase
{
    private readonly IMediator _mediator;

    public ScormController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/scorm/{contentId}/runtime
    [HttpGet("{contentId:guid}/runtime")]
    public async Task<IActionResult> GetRuntimeState(Guid contentId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(ApiResponse.Fail("Invalid token."));
        var result = await _mediator.Send(new GetScormRuntimeStateQuery(userId.Value, contentId));
        if (result == null) return NotFound(ApiResponse.Fail("No SCORM runtime state found."));
        return Ok(ApiResponse<ScormRuntimeStateDto>.Ok(result));
    }

    // PUT /api/scorm/{contentId}/runtime
    [HttpPut("{contentId:guid}/runtime")]
    public async Task<IActionResult> UpsertRuntimeState(Guid contentId, [FromBody] UpsertScormRuntimeStateCommand command)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(ApiResponse.Fail("Invalid token."));
        var cmd = command with { UserId = userId.Value, LearningContentId = contentId };
        var result = await _mediator.Send(cmd);
        return Ok(ApiResponse<ScormRuntimeStateDto>.Ok(result, "SCORM runtime state updated."));
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
