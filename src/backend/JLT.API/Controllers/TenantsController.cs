using System;
using System.Threading.Tasks;
using JLT.Application.Common;
using JLT.Application.Common.Models;
using JLT.Application.DTOs;
using JLT.Application.Features.Tenants;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace JLT.API.Controllers;

[ApiController]
[Route("api/tenants")]
public class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetTenants(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isActive = null)
    {
        var query = new GetTenantsQuery(pageNumber, pageSize, searchTerm, isActive);
        var result = await _mediator.Send(query);
        return Ok(ApiResponse<PaginatedList<TenantDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetTenantByIdQuery(id));
        if (result == null) return NotFound(ApiResponse.Fail("Tenant not found."));
        return Ok(ApiResponse<TenantDto>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantCommand command)
    {
        var result = await _mediator.Send(command);
        return Created($"/api/tenants/{result.Id}", ApiResponse<TenantDto>.Ok(result, "Tenant created."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantCommand command)
    {
        if (id != command.Id) return BadRequest(ApiResponse.Fail("ID mismatch."));
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<TenantDto>.Ok(result, "Tenant updated."));
    }

    [HttpPut("{id:guid}/features/{featureKey}")]
    public async Task<IActionResult> ToggleFeature(Guid id, string featureKey, [FromBody] ToggleFeatureRequest request)
    {
        var command = new ToggleFeatureCommand(id, featureKey, request.IsEnabled, request.Config);
        var result = await _mediator.Send(command);
        if (!result) return BadRequest(ApiResponse.Fail("Failed to toggle feature."));
        return Ok(ApiResponse.Ok("Feature toggled successfully."));
    }
}

public record ToggleFeatureRequest(bool IsEnabled, string? Config = null);
