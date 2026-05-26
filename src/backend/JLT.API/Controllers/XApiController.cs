using JLT.Application.Common;
using JLT.Application.Common.Models;
using JLT.Application.DTOs;
using JLT.Application.Features.LearningContent;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JLT.API.Controllers;

[ApiController]
[Route("api/xapi/statements")]
[Authorize]
public class XApiController : ControllerBase
{
    private readonly IMediator _mediator;

    public XApiController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST /api/xapi/statements
    [HttpPost]
    public async Task<IActionResult> StoreStatement([FromBody] StoreXApiStatementCommand command)
    {
        var result = await _mediator.Send(command);
        return Created($"/api/xapi/statements/{result.Id}", ApiResponse<XApiStatementDto>.Ok(result, "Statement stored."));
    }

    // GET /api/xapi/statements
    [HttpGet]
    public async Task<IActionResult> GetStatements(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? verbId = null,
        [FromQuery] string? actorJson = null)
    {
        var query = new GetXApiStatementsQuery(pageNumber, pageSize, verbId, actorJson);
        var result = await _mediator.Send(query);
        return Ok(ApiResponse<PaginatedList<XApiStatementDto>>.Ok(result));
    }
}
