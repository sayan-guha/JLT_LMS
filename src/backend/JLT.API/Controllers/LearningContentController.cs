using JLT.Application.Common;
using JLT.Application.Common.Models;
using JLT.Application.DTOs;
using JLT.Application.Features.LearningContent;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JLT.API.Controllers;

[ApiController]
[Route("api/learning-content")]
[Authorize]
public class LearningContentController : ControllerBase
{
    private readonly IMediator _mediator;

    public LearningContentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/learning-content
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? contentType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? category = null,
        [FromQuery] string? language = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortColumn = null,
        [FromQuery] string? sortOrder = null)
    {
        var query = new GetLearningContentListQuery(
            pageNumber, pageSize, contentType, status, category, language, searchTerm, sortColumn, sortOrder);
        var result = await _mediator.Send(query);
        return Ok(ApiResponse<PaginatedList<LearningContentSummaryDto>>.Ok(result));
    }

    // GET /api/learning-content/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetLearningContentByIdQuery(id));
        if (result == null) return NotFound(ApiResponse.Fail("Content not found."));
        return Ok(ApiResponse<LearningContentDto>.Ok(result));
    }

    // POST /api/learning-content
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLearningContentCommand command)
    {
        var result = await _mediator.Send(command);
        return Created($"/api/learning-content/{result.Id}", ApiResponse<LearningContentDto>.Ok(result, "Content created."));
    }

    // PUT /api/learning-content/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLearningContentCommand command)
    {
        if (id != command.Id) return BadRequest(ApiResponse.Fail("ID mismatch."));
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<LearningContentDto>.Ok(result, "Content updated."));
    }

    // PATCH /api/learning-content/{id}/status
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateContentStatusCommand command)
    {
        if (id != command.Id) return BadRequest(ApiResponse.Fail("ID mismatch."));
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<LearningContentDto>.Ok(result, $"Status changed to {result.Status}."));
    }

    // DELETE /api/learning-content/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteLearningContentCommand(id));
        if (!result) return BadRequest(ApiResponse.Fail("Only draft content can be deleted."));
        return Ok(ApiResponse.Ok("Content deleted."));
    }
}
