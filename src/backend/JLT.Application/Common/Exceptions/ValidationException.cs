using System;
using System.Collections.Generic;
using System.Linq;

namespace JLT.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = Array.Empty<string>();
    }

    public ValidationException(string message)
        : base(message)
    {
        Errors = new List<string> { message };
    }

    public ValidationException(IEnumerable<string> errors)
        : base($"Validation failed: {string.Join("; ", errors)}")
    {
        Errors = errors.ToList().AsReadOnly();
    }
}
