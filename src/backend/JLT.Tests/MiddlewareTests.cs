using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentValidation.Results;
using JLT.API.Middleware;
using JLT.Application.Common;
using JLT.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace JLT.Tests;

public class MiddlewareTests
{
    private readonly ILogger<ExceptionHandlingMiddleware> _loggerMock;

    public MiddlewareTests()
    {
        _loggerMock = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
    }

    [Fact]
    public async Task ExceptionHandlingMiddleware_ShouldCallNext_WhenNoExceptionOccurs()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task ExceptionHandlingMiddleware_ShouldHandleUnauthorizedAccessException_With401Status()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        RequestDelegate next = (ctx) => throw new UnauthorizedAccessException("Unauthorized user");

        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(401, context.Response.StatusCode);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseString = await reader.ReadToEndAsync();
        
        var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseString, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Equal("Unauthorized user", apiResponse.Message);
    }

    [Fact]
    public async Task ExceptionHandlingMiddleware_ShouldHandleNotFoundException_With404Status()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        RequestDelegate next = (ctx) => throw new NotFoundException("User", "123");

        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(404, context.Response.StatusCode);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseString = await reader.ReadToEndAsync();
        
        var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseString, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Contains("not found", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExceptionHandlingMiddleware_ShouldHandleValidationException_With400StatusAndErrors()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var validationErrors = new List<ValidationFailure>
        {
            new("Email", "Email is invalid")
        };
        RequestDelegate next = (ctx) => throw new FluentValidation.ValidationException("Validation failed.", validationErrors);

        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(400, context.Response.StatusCode);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseString = await reader.ReadToEndAsync();
        
        var errorResponse = JsonSerializer.Deserialize<ValidationErrorResponse>(responseString, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.NotNull(errorResponse);
        Assert.False(errorResponse.Success);
        Assert.Equal("Validation failed.", errorResponse.Message);
        Assert.Single(errorResponse.Errors);
        Assert.Equal("Email is invalid", errorResponse.Errors[0]);
    }

    [Fact]
    public async Task ExceptionHandlingMiddleware_ShouldHandleUnexpectedException_With500Status()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        RequestDelegate next = (ctx) => throw new Exception("Database failure");

        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseString = await reader.ReadToEndAsync();
        
        var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseString, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Equal("An unexpected error occurred on the server.", apiResponse.Message);
    }

    private class ValidationErrorResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }
}
