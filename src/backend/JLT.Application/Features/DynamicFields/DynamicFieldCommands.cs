using FluentValidation;
using JLT.Application.DTOs;
using JLT.Domain.Common;
using JLT.Domain.Entities;
using JLT.Domain.Enums;
using JLT.Domain.Interfaces;
using MediatR;

namespace JLT.Application.Features.DynamicFields;

// --- Create Dynamic Field Definition ---
public record CreateDynamicFieldCommand(
    string FieldKey, string DisplayName, string FieldType,
    bool IsRequired, string? Options, string? DefaultValue,
    int SortOrder) : IRequest<DynamicFieldDto>;

public class CreateDynamicFieldValidator : AbstractValidator<CreateDynamicFieldCommand>
{
    public CreateDynamicFieldValidator()
    {
        RuleFor(x => x.FieldKey).NotEmpty().MaximumLength(200)
            .Matches(@"^[a-z0-9_]+$").WithMessage("FieldKey must be lowercase alphanumeric with underscores only.");
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.FieldType).NotEmpty().Must(t =>
            Enum.TryParse<DynamicFieldType>(t, true, out _)).WithMessage("Invalid field type.");
    }
}

public class CreateDynamicFieldHandler : IRequestHandler<CreateDynamicFieldCommand, DynamicFieldDto>
{
    private readonly IRepository<DynamicFieldDefinition> _fieldRepository;
    private readonly IDynamicFieldService _dynamicFieldService;
    private readonly IAuditService _auditService;

    public CreateDynamicFieldHandler(IRepository<DynamicFieldDefinition> fieldRepository,
        IDynamicFieldService dynamicFieldService, IAuditService auditService)
    {
        _fieldRepository = fieldRepository;
        _dynamicFieldService = dynamicFieldService;
        _auditService = auditService;
    }

    public async Task<DynamicFieldDto> Handle(CreateDynamicFieldCommand request, CancellationToken cancellationToken)
    {
        // Check for duplicate key using existing field definitions
        var existing = await _dynamicFieldService.GetFieldDefinitionsAsync(cancellationToken);
        if (existing.Any(d => d.FieldKey == request.FieldKey))
            throw new InvalidOperationException($"Field '{request.FieldKey}' already exists.");

        var field = new DynamicFieldDefinition
        {
            FieldKey = request.FieldKey,
            DisplayName = request.DisplayName,
            FieldType = Enum.Parse<DynamicFieldType>(request.FieldType, true),
            IsRequired = request.IsRequired,
            Options = request.Options,
            DefaultValue = request.DefaultValue,
            SortOrder = request.SortOrder
        };

        await _fieldRepository.AddAsync(field, cancellationToken);

        await _auditService.LogAsync("field.created", "DynamicFieldDefinition", field.Id,
            newValues: new { field.FieldKey, field.DisplayName, field.FieldType },
            cancellationToken: cancellationToken);

        return new DynamicFieldDto(field.Id, field.FieldKey, field.DisplayName, field.FieldType.ToString(),
            field.IsRequired, field.Options, field.DefaultValue, field.SortOrder, field.IsActive);
    }
}

// --- Get All Dynamic Fields ---
public record GetAllDynamicFieldsQuery : IRequest<IReadOnlyList<DynamicFieldDto>>;

public class GetAllDynamicFieldsHandler : IRequestHandler<GetAllDynamicFieldsQuery, IReadOnlyList<DynamicFieldDto>>
{
    private readonly IDynamicFieldService _dynamicFieldService;

    public GetAllDynamicFieldsHandler(IDynamicFieldService dynamicFieldService)
    {
        _dynamicFieldService = dynamicFieldService;
    }

    public async Task<IReadOnlyList<DynamicFieldDto>> Handle(GetAllDynamicFieldsQuery request, CancellationToken cancellationToken)
    {
        var fields = await _dynamicFieldService.GetFieldDefinitionsAsync(cancellationToken);
        return fields.Select(f => new DynamicFieldDto(f.Id, f.FieldKey, f.DisplayName, f.FieldType.ToString(),
            f.IsRequired, f.Options, f.DefaultValue, f.SortOrder, f.IsActive)).ToList();
    }
}

// --- Update Dynamic Field ---
public record UpdateDynamicFieldCommand(
    Guid Id, string? DisplayName, bool? IsRequired,
    string? Options, string? DefaultValue, int? SortOrder, bool? IsActive) : IRequest<DynamicFieldDto>;

public class UpdateDynamicFieldHandler : IRequestHandler<UpdateDynamicFieldCommand, DynamicFieldDto>
{
    private readonly IRepository<DynamicFieldDefinition> _fieldRepository;
    private readonly IAuditService _auditService;

    public UpdateDynamicFieldHandler(IRepository<DynamicFieldDefinition> fieldRepository, IAuditService auditService)
    {
        _fieldRepository = fieldRepository;
        _auditService = auditService;
    }

    public async Task<DynamicFieldDto> Handle(UpdateDynamicFieldCommand request, CancellationToken cancellationToken)
    {
        var field = await _fieldRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Field {request.Id} not found.");

        if (request.DisplayName != null) field.DisplayName = request.DisplayName;
        if (request.IsRequired.HasValue) field.IsRequired = request.IsRequired.Value;
        if (request.Options != null) field.Options = request.Options;
        if (request.DefaultValue != null) field.DefaultValue = request.DefaultValue;
        if (request.SortOrder.HasValue) field.SortOrder = request.SortOrder.Value;
        if (request.IsActive.HasValue) field.IsActive = request.IsActive.Value;

        await _fieldRepository.UpdateAsync(field, cancellationToken);

        await _auditService.LogAsync("field.updated", "DynamicFieldDefinition", field.Id,
            cancellationToken: cancellationToken);

        return new DynamicFieldDto(field.Id, field.FieldKey, field.DisplayName, field.FieldType.ToString(),
            field.IsRequired, field.Options, field.DefaultValue, field.SortOrder, field.IsActive);
    }
}
