using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using JLT.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace JLT.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred during request execution.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errors) = exception switch
        {
            UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, exception.Message, null),
            JLT.Application.Common.Exceptions.ForbiddenException => ((int)HttpStatusCode.Forbidden, exception.Message, null),
            JLT.Application.Common.Exceptions.NotFoundException => ((int)HttpStatusCode.NotFound, exception.Message, null),
            KeyNotFoundException => ((int)HttpStatusCode.NotFound, exception.Message, null),
            JLT.Application.Common.Exceptions.ConflictException => ((int)HttpStatusCode.Conflict, exception.Message, null),
            InvalidOperationException => ((int)HttpStatusCode.Conflict, exception.Message, null),
            FluentValidation.ValidationException validationEx => ((int)HttpStatusCode.BadRequest, "Validation failed.",
                validationEx.Errors.Select(e => e.ErrorMessage).ToList() as IEnumerable<string>),
            JLT.Application.Common.Exceptions.ValidationException customValEx => ((int)HttpStatusCode.BadRequest, customValEx.Message,
                customValEx.Errors as IEnumerable<string>),
            JLT.Infrastructure.Services.ValidationException infraValEx => ((int)HttpStatusCode.BadRequest, infraValEx.Message,
                infraValEx.Errors as IEnumerable<string>),
            _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred on the server.", null)
        };

        context.Response.StatusCode = statusCode;

        var response = ApiResponse.Fail(message);
        if (errors != null)
        {
            // If validation errors, we format them accordingly
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message,
                errors
            });
        }
        else
        {
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
