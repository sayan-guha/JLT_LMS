using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using JLT.Application.Common.Models;
using JLT.Application.DTOs;
using JLT.Domain.Entities;
using JLT.Domain.Enums;
using JLT.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JLT.Application.Features.LearningContent;

// =============================================================================
// Mapping helper
// =============================================================================
internal static class LearningContentMapper
{
    public static LearningContentDto ToDto(Domain.Entities.LearningContent c) => new(
        c.Id, c.TenantId, c.Title, c.Description,
        c.ContentType.ToString(), c.Status.ToString(), c.Version,
        c.MimeType, c.StorageUrl, c.FileSize, c.DurationSeconds,
        c.ExternalUrl, c.Config, c.ThumbnailUrl,
        c.CreatedBy, c.UpdatedBy, c.PublishedAt,
        c.ValidFrom, c.ValidTill, c.RetiredAt,
        c.NextReviewDate, c.ReviewedBy, c.ReviewedAt,
        c.ContentSource.ToString(), c.SourceUrl, c.Author,
        c.Publisher, c.Copyright, c.LicenseType,
        c.Language, c.Locale, c.EstimatedDurationMinutes,
        c.Category, c.Tags, c.CreatedAt, c.UpdatedAt);

    public static LearningContentSummaryDto ToSummaryDto(Domain.Entities.LearningContent c) => new(
        c.Id, c.Title, c.ContentType.ToString(), c.Status.ToString(),
        c.ThumbnailUrl, c.Category, c.Language,
        c.EstimatedDurationMinutes, c.ContentSource.ToString(),
        c.CreatedAt, c.UpdatedAt);
}

// =============================================================================
// Get by ID
// =============================================================================
public record GetLearningContentByIdQuery(Guid Id) : IRequest<LearningContentDto?>;

public class GetLearningContentByIdHandler : IRequestHandler<GetLearningContentByIdQuery, LearningContentDto?>
{
    private readonly ILearningContentRepository _repo;
    public GetLearningContentByIdHandler(ILearningContentRepository repo) => _repo = repo;

    public async Task<LearningContentDto?> Handle(GetLearningContentByIdQuery request, CancellationToken ct)
    {
        var content = await _repo.GetByIdAsync(request.Id, ct);
        return content == null ? null : LearningContentMapper.ToDto(content);
    }
}

// =============================================================================
// List (paginated + filtered)
// =============================================================================
public record GetLearningContentListQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? ContentType = null,
    string? Status = null,
    string? Category = null,
    string? Language = null,
    string? SearchTerm = null,
    string? SortColumn = null,
    string? SortOrder = null) : IRequest<PaginatedList<LearningContentSummaryDto>>;

public class GetLearningContentListHandler : IRequestHandler<GetLearningContentListQuery, PaginatedList<LearningContentSummaryDto>>
{
    private readonly ILearningContentRepository _repo;
    public GetLearningContentListHandler(ILearningContentRepository repo) => _repo = repo;

    public async Task<PaginatedList<LearningContentSummaryDto>> Handle(GetLearningContentListQuery request, CancellationToken ct)
    {
        var query = _repo.Query();

        if (!string.IsNullOrWhiteSpace(request.ContentType) && Enum.TryParse<ContentType>(request.ContentType, true, out var cType))
            query = query.Where(c => c.ContentType == cType);

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<ContentStatus>(request.Status, true, out var cStatus))
            query = query.Where(c => c.Status == cStatus);

        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(c => c.Category != null && c.Category.ToLower() == request.Category.ToLower());

        if (!string.IsNullOrWhiteSpace(request.Language))
            query = query.Where(c => c.Language.ToLower() == request.Language.ToLower());

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(c =>
                c.Title.ToLower().Contains(search) ||
                (c.Description != null && c.Description.ToLower().Contains(search)) ||
                (c.Author != null && c.Author.ToLower().Contains(search)) ||
                (c.Category != null && c.Category.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(request.SortColumn))
        {
            var desc = request.SortOrder?.ToLower() == "desc";
            query = request.SortColumn.ToLower() switch
            {
                "title" => desc ? query.OrderByDescending(c => c.Title) : query.OrderBy(c => c.Title),
                "createdat" => desc ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
                "updatedat" => desc ? query.OrderByDescending(c => c.UpdatedAt) : query.OrderBy(c => c.UpdatedAt),
                "category" => desc ? query.OrderByDescending(c => c.Category) : query.OrderBy(c => c.Category),
                _ => desc ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt)
            };
        }
        else
        {
            query = query.OrderByDescending(c => c.CreatedAt);
        }

        var paged = await PaginatedList<Domain.Entities.LearningContent>.CreateAsync(query, request.PageNumber, request.PageSize, ct);
        var items = paged.Items.Select(LearningContentMapper.ToSummaryDto).ToList();
        return new PaginatedList<LearningContentSummaryDto>(items, paged.TotalCount, paged.PageIndex, request.PageSize);
    }
}

// =============================================================================
// Create
// =============================================================================
public record CreateLearningContentCommand(
    string Title,
    string ContentType,
    Guid CreatedBy,
    string? Description = null,
    string? MimeType = null,
    string? StorageUrl = null,
    long? FileSize = null,
    int? DurationSeconds = null,
    string? ExternalUrl = null,
    string? Config = null,
    string? ThumbnailUrl = null,
    string? ContentSource = null,
    string? SourceUrl = null,
    string? Author = null,
    string? Publisher = null,
    string? Copyright = null,
    string? LicenseType = null,
    string? Language = null,
    string? Locale = null,
    int? EstimatedDurationMinutes = null,
    string? Category = null,
    string? Tags = null,
    DateTime? ValidFrom = null,
    DateTime? ValidTill = null,
    DateTime? NextReviewDate = null) : IRequest<LearningContentDto>;

public class CreateLearningContentCommandValidator : AbstractValidator<CreateLearningContentCommand>
{
    public CreateLearningContentCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ContentType).NotEmpty()
            .Must(t => Enum.TryParse<ContentType>(t, true, out _))
            .WithMessage("Invalid content type.");
    }
}

public class CreateLearningContentHandler : IRequestHandler<CreateLearningContentCommand, LearningContentDto>
{
    private readonly ILearningContentRepository _repo;
    public CreateLearningContentHandler(ILearningContentRepository repo) => _repo = repo;

    public async Task<LearningContentDto> Handle(CreateLearningContentCommand request, CancellationToken ct)
    {
        var entity = new Domain.Entities.LearningContent
        {
            Title = request.Title,
            Description = request.Description,
            ContentType = Enum.Parse<ContentType>(request.ContentType, true),
            Status = ContentStatus.Draft,
            Version = "1.0",
            MimeType = request.MimeType,
            StorageUrl = request.StorageUrl,
            FileSize = request.FileSize,
            DurationSeconds = request.DurationSeconds,
            ExternalUrl = request.ExternalUrl,
            Config = request.Config,
            ThumbnailUrl = request.ThumbnailUrl,
            CreatedBy = request.CreatedBy,
            UpdatedBy = request.CreatedBy,
            ContentSource = !string.IsNullOrEmpty(request.ContentSource) && Enum.TryParse<Domain.Enums.ContentSource>(request.ContentSource, true, out var cs)
                ? cs : Domain.Enums.ContentSource.Internal,
            SourceUrl = request.SourceUrl,
            Author = request.Author,
            Publisher = request.Publisher,
            Copyright = request.Copyright,
            LicenseType = request.LicenseType,
            Language = request.Language ?? "en",
            Locale = request.Locale,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            Category = request.Category,
            Tags = request.Tags,
            ValidFrom = request.ValidFrom,
            ValidTill = request.ValidTill,
            NextReviewDate = request.NextReviewDate
        };

        await _repo.AddAsync(entity, ct);
        return LearningContentMapper.ToDto(entity);
    }
}

// =============================================================================
// Update
// =============================================================================
public record UpdateLearningContentCommand(
    Guid Id,
    string? Title = null,
    string? Description = null,
    string? MimeType = null,
    string? StorageUrl = null,
    long? FileSize = null,
    int? DurationSeconds = null,
    string? ExternalUrl = null,
    string? Config = null,
    string? ThumbnailUrl = null,
    string? ContentSource = null,
    string? SourceUrl = null,
    string? Author = null,
    string? Publisher = null,
    string? Copyright = null,
    string? LicenseType = null,
    string? Language = null,
    string? Locale = null,
    int? EstimatedDurationMinutes = null,
    string? Category = null,
    string? Tags = null,
    DateTime? ValidFrom = null,
    DateTime? ValidTill = null,
    DateTime? NextReviewDate = null,
    Guid? UpdatedBy = null) : IRequest<LearningContentDto>;

public class UpdateLearningContentCommandValidator : AbstractValidator<UpdateLearningContentCommand>
{
    public UpdateLearningContentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).MaximumLength(500).When(x => x.Title != null);
    }
}

public class UpdateLearningContentHandler : IRequestHandler<UpdateLearningContentCommand, LearningContentDto>
{
    private readonly ILearningContentRepository _repo;
    public UpdateLearningContentHandler(ILearningContentRepository repo) => _repo = repo;

    public async Task<LearningContentDto> Handle(UpdateLearningContentCommand request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new InvalidOperationException("Content not found.");

        if (request.Title != null) entity.Title = request.Title;
        if (request.Description != null) entity.Description = request.Description;
        if (request.MimeType != null) entity.MimeType = request.MimeType;
        if (request.StorageUrl != null) entity.StorageUrl = request.StorageUrl;
        if (request.FileSize.HasValue) entity.FileSize = request.FileSize;
        if (request.DurationSeconds.HasValue) entity.DurationSeconds = request.DurationSeconds;
        if (request.ExternalUrl != null) entity.ExternalUrl = request.ExternalUrl;
        if (request.Config != null) entity.Config = request.Config;
        if (request.ThumbnailUrl != null) entity.ThumbnailUrl = request.ThumbnailUrl;
        if (request.ContentSource != null && Enum.TryParse<Domain.Enums.ContentSource>(request.ContentSource, true, out var cs))
            entity.ContentSource = cs;
        if (request.SourceUrl != null) entity.SourceUrl = request.SourceUrl;
        if (request.Author != null) entity.Author = request.Author;
        if (request.Publisher != null) entity.Publisher = request.Publisher;
        if (request.Copyright != null) entity.Copyright = request.Copyright;
        if (request.LicenseType != null) entity.LicenseType = request.LicenseType;
        if (request.Language != null) entity.Language = request.Language;
        if (request.Locale != null) entity.Locale = request.Locale;
        if (request.EstimatedDurationMinutes.HasValue) entity.EstimatedDurationMinutes = request.EstimatedDurationMinutes;
        if (request.Category != null) entity.Category = request.Category;
        if (request.Tags != null) entity.Tags = request.Tags;
        if (request.ValidFrom.HasValue) entity.ValidFrom = request.ValidFrom;
        if (request.ValidTill.HasValue) entity.ValidTill = request.ValidTill;
        if (request.NextReviewDate.HasValue) entity.NextReviewDate = request.NextReviewDate;
        if (request.UpdatedBy.HasValue) entity.UpdatedBy = request.UpdatedBy.Value;

        await _repo.UpdateAsync(entity, ct);
        return LearningContentMapper.ToDto(entity);
    }
}

// =============================================================================
// Update Status (lifecycle transition)
// =============================================================================
public record UpdateContentStatusCommand(Guid Id, string NewStatus) : IRequest<LearningContentDto>;

public class UpdateContentStatusHandler : IRequestHandler<UpdateContentStatusCommand, LearningContentDto>
{
    private readonly ILearningContentRepository _repo;
    public UpdateContentStatusHandler(ILearningContentRepository repo) => _repo = repo;

    public async Task<LearningContentDto> Handle(UpdateContentStatusCommand request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new InvalidOperationException("Content not found.");

        if (!Enum.TryParse<ContentStatus>(request.NewStatus, true, out var newStatus))
            throw new InvalidOperationException($"Invalid status: {request.NewStatus}");

        ValidateTransition(entity.Status, newStatus);

        entity.Status = newStatus;

        // Side effects per transition
        switch (newStatus)
        {
            case ContentStatus.Published:
                entity.PublishedAt = DateTime.UtcNow;
                break;
            case ContentStatus.Archived:
                entity.RetiredAt = DateTime.UtcNow;
                break;
        }

        await _repo.UpdateAsync(entity, ct);
        return LearningContentMapper.ToDto(entity);
    }

    private static void ValidateTransition(ContentStatus current, ContentStatus next)
    {
        var valid = current switch
        {
            ContentStatus.Draft => next == ContentStatus.InReview,
            ContentStatus.InReview => next is ContentStatus.Published or ContentStatus.Draft,
            ContentStatus.Published => next == ContentStatus.Archived,
            _ => false
        };

        if (!valid)
            throw new InvalidOperationException($"Cannot transition from {current} to {next}.");
    }
}

// =============================================================================
// Delete (Draft only)
// =============================================================================
public record DeleteLearningContentCommand(Guid Id) : IRequest<bool>;

public class DeleteLearningContentHandler : IRequestHandler<DeleteLearningContentCommand, bool>
{
    private readonly ILearningContentRepository _repo;
    public DeleteLearningContentHandler(ILearningContentRepository repo) => _repo = repo;

    public async Task<bool> Handle(DeleteLearningContentCommand request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(request.Id, ct);
        if (entity == null) return false;
        if (entity.Status != ContentStatus.Draft) return false;

        await _repo.DeleteAsync(entity, ct);
        return true;
    }
}
