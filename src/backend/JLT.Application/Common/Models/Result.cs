using System.Collections.Generic;
using System.Linq;

namespace JLT.Application.Common.Models;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Message { get; }
    public IReadOnlyList<string> Errors { get; }

    protected Result(bool isSuccess, string? message, IEnumerable<string>? errors)
    {
        IsSuccess = isSuccess;
        Message = message;
        Errors = errors?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
    }

    public static Result Success(string? message = null) => new(true, message, null);
    public static Result Failure(string message) => new(false, message, new[] { message });
    public static Result Failure(IEnumerable<string> errors) => new(false, "One or more errors occurred.", errors);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(T? value, bool isSuccess, string? message, IEnumerable<string>? errors)
        : base(isSuccess, message, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value, string? message = null) => new(value, true, message, null);
    public new static Result<T> Failure(string message) => new(default, false, message, new[] { message });
    public new static Result<T> Failure(IEnumerable<string> errors) => new(default, false, "One or more errors occurred.", errors);
}
