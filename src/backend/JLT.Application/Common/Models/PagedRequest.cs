namespace JLT.Application.Common.Models;

public record PagedRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public string? SortColumn { get; init; }
    public string? SortOrder { get; init; } // "asc" or "desc"
}
