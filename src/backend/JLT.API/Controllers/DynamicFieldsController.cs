using JLT.Application.Common;
using JLT.Application.DTOs;
using JLT.Application.Features.DynamicFields;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JLT.API.Controllers;

[ApiController]
[Route("api/fields")]
[Authorize]
public class DynamicFieldsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DynamicFieldsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllDynamicFieldsQuery());
        return Ok(ApiResponse<IReadOnlyList<DynamicFieldDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDynamicFieldCommand command)
    {
        var result = await _mediator.Send(command);
        return Created($"/api/fields/{result.Id}", ApiResponse<DynamicFieldDto>.Ok(result, "Dynamic field created."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDynamicFieldCommand command)
    {
        if (id != command.Id) return BadRequest(ApiResponse.Fail("ID mismatch."));
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<DynamicFieldDto>.Ok(result, "Dynamic field updated."));
    }
}
