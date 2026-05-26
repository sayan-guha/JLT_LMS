using JLT.Application.Common;
using JLT.Application.DTOs;
using JLT.Application.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace JLT.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();
        
        var loginCommand = command with { IpAddress = ipAddress, UserAgent = userAgent };
        var result = await _mediator.Send(loginCommand);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Login successful."));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        return Created($"/api/users/{result.Id}", ApiResponse<UserDto>.Ok(result, "User registered."));
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();

        var command = new RefreshTokenCommand(request.RefreshToken, ipAddress, userAgent);
        var result = await _mediator.Send(command);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Token refreshed."));
    }

    [HttpPost("revoke-token")]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new RevokeTokenCommand(request.RefreshToken, ipAddress);
        var result = await _mediator.Send(command);
        
        if (!result)
            return BadRequest(ApiResponse.Fail("Token not found or already inactive."));
            
        return Ok(ApiResponse.Ok("Token revoked."));
    }
}

public record RefreshTokenRequest(string RefreshToken);
public record RevokeTokenRequest(string RefreshToken);
