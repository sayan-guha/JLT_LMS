using System.Security.Claims;
using JLT.Application.Common;
using JLT.Application.DTOs;
using JLT.Application.Features.LearningContent;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JLT.API.Controllers;

[ApiController]
[Route("api/content-progress")]
[Authorize]
public class ContentProgressController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContentProgressController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/content-progress/me
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProgress()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(ApiResponse.Fail("Invalid token."));
        var result = await _mediator.Send(new GetUserProgressListQuery(userId.Value));
        return Ok(ApiResponse<List<ContentProgressDto>>.Ok(result));
    }

    // GET /api/content-progress/{contentId}
    [HttpGet("{contentId:guid}")]
    public async Task<IActionResult> GetProgress(Guid contentId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(ApiResponse.Fail("Invalid token."));
        var result = await _mediator.Send(new GetContentProgressQuery(userId.Value, contentId));
        if (result == null) return NotFound(ApiResponse.Fail("No progress found."));
        return Ok(ApiResponse<ContentProgressDto>.Ok(result));
    }

    // PUT /api/content-progress/{contentId}
    [HttpPut("{contentId:guid}")]
    public async Task<IActionResult> UpsertProgress(Guid contentId, [FromBody] UpsertContentProgressRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(ApiResponse.Fail("Invalid token."));
        var command = new UpsertContentProgressCommand(
            userId.Value, contentId, request.ProgressPercent, request.BookmarkData, request.TimeSpentSeconds);
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<ContentProgressDto>.Ok(result, "Progress updated."));
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

public record UpsertContentProgressRequest(decimal ProgressPercent, string? BookmarkData, int TimeSpentSeconds);
