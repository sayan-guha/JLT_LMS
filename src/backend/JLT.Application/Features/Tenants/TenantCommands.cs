using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using JLT.Application.Common.Models;
using JLT.Application.DTOs;
using JLT.Domain.Interfaces;
using MediatR;

namespace JLT.Application.Features.Tenants;

// --- Create Tenant ---
public record CreateTenantCommand(
    string Name, string Slug, string? Domain,
    string? LogoUrl, string? PrimaryColor, string? SecondaryColor,
    string? PlanType, int? MaxUsers, int? MaxStorageGb) : IRequest<TenantDto>;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100)
            .Matches(@"^[a-z0-9\-]+$").WithMessage("Slug must be lowercase alphanumeric with hyphens only.");
    }
}

public class CreateTenantHandler : IRequestHandler<CreateTenantCommand, TenantDto>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IAuditService _auditService;

    public CreateTenantHandler(ITenantRepository tenantRepository, IAuditService auditService)
    {
        _tenantRepository = tenantRepository;
        _auditService = auditService;
    }

    public async Task<TenantDto> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        if (await _tenantRepository.SlugExistsAsync(request.Slug, cancellationToken))
            throw new InvalidOperationException($"Tenant with slug '{request.Slug}' already exists.");

        var tenant = new Domain.Entities.Tenant
        {
            Name = request.Name,
            Slug = request.Slug,
            Domain = request.Domain,
            LogoUrl = request.LogoUrl,
            PrimaryColor = request.PrimaryColor,
            SecondaryColor = request.SecondaryColor,
            PlanType = request.PlanType ?? "standard",
            MaxUsers = request.MaxUsers,
            MaxStorageGb = request.MaxStorageGb
        };

        await _tenantRepository.AddAsync(tenant, cancellationToken);
        await _auditService.LogAsync("tenant.created", "Tenant", tenant.Id,
            newValues: new { tenant.Name, tenant.Slug },
            source: Domain.Enums.AuditSource.System, cancellationToken: cancellationToken);

        return new TenantDto(tenant.Id, tenant.Name, tenant.Slug, tenant.Domain, tenant.LogoUrl,
            tenant.PrimaryColor, tenant.SecondaryColor, tenant.PlanType,
            tenant.MaxUsers, tenant.MaxStorageGb, tenant.IsActive, tenant.CreatedAt);
    }
}

// --- Get Tenants (Paginated) ---
public record GetTenantsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    bool? IsActive = null) : IRequest<PaginatedList<TenantDto>>;

public class GetTenantsHandler : IRequestHandler<GetTenantsQuery, PaginatedList<TenantDto>>
{
    private readonly ITenantRepository _tenantRepository;

    public GetTenantsHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<PaginatedList<TenantDto>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        var query = _tenantRepository.Query();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(search) || t.Slug.ToLower().Contains(search));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(t => t.IsActive == request.IsActive.Value);
        }

        query = query.OrderByDescending(t => t.CreatedAt);

        var pagedTenants = await PaginatedList<Domain.Entities.Tenant>.CreateAsync(query, request.PageNumber, request.PageSize, cancellationToken);

        var dtoItems = pagedTenants.Items.Select(t => new TenantDto(t.Id, t.Name, t.Slug, t.Domain, t.LogoUrl,
            t.PrimaryColor, t.SecondaryColor, t.PlanType,
            t.MaxUsers, t.MaxStorageGb, t.IsActive, t.CreatedAt)).ToList();

        return new PaginatedList<TenantDto>(dtoItems, pagedTenants.TotalCount, pagedTenants.PageIndex, request.PageSize);
    }
}

// --- Get Tenant By ID ---
public record GetTenantByIdQuery(Guid Id) : IRequest<TenantDto?>;

public class GetTenantByIdHandler : IRequestHandler<GetTenantByIdQuery, TenantDto?>
{
    private readonly ITenantRepository _tenantRepository;

    public GetTenantByIdHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<TenantDto?> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.Id, cancellationToken);
        if (tenant == null) return null;

        return new TenantDto(tenant.Id, tenant.Name, tenant.Slug, tenant.Domain, tenant.LogoUrl,
            tenant.PrimaryColor, tenant.SecondaryColor, tenant.PlanType,
            tenant.MaxUsers, tenant.MaxStorageGb, tenant.IsActive, tenant.CreatedAt);
    }
}

// --- Update Tenant ---
public record UpdateTenantCommand(
    Guid Id, string? Name, string? Domain,
    string? LogoUrl, string? PrimaryColor, string? SecondaryColor,
    string? PlanType, int? MaxUsers, int? MaxStorageGb, bool? IsActive) : IRequest<TenantDto>;

public class UpdateTenantHandler : IRequestHandler<UpdateTenantCommand, TenantDto>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IAuditService _auditService;

    public UpdateTenantHandler(ITenantRepository tenantRepository, IAuditService auditService)
    {
        _tenantRepository = tenantRepository;
        _auditService = auditService;
    }

    public async Task<TenantDto> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Tenant {request.Id} not found.");

        if (request.Name != null) tenant.Name = request.Name;
        if (request.Domain != null) tenant.Domain = request.Domain;
        if (request.LogoUrl != null) tenant.LogoUrl = request.LogoUrl;
        if (request.PrimaryColor != null) tenant.PrimaryColor = request.PrimaryColor;
        if (request.SecondaryColor != null) tenant.SecondaryColor = request.SecondaryColor;
        if (request.PlanType != null) tenant.PlanType = request.PlanType;
        if (request.MaxUsers.HasValue) tenant.MaxUsers = request.MaxUsers;
        if (request.MaxStorageGb.HasValue) tenant.MaxStorageGb = request.MaxStorageGb;
        if (request.IsActive.HasValue) tenant.IsActive = request.IsActive.Value;

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _auditService.LogAsync("tenant.updated", "Tenant", tenant.Id,
            source: Domain.Enums.AuditSource.System, cancellationToken: cancellationToken);

        return new TenantDto(tenant.Id, tenant.Name, tenant.Slug, tenant.Domain, tenant.LogoUrl,
            tenant.PrimaryColor, tenant.SecondaryColor, tenant.PlanType,
            tenant.MaxUsers, tenant.MaxStorageGb, tenant.IsActive, tenant.CreatedAt);
    }
}

// --- Toggle Feature Command ---
public record ToggleFeatureCommand(Guid TenantId, string FeatureKey, bool IsEnabled, string? Config = null) : IRequest<bool>;

public class ToggleFeatureHandler : IRequestHandler<ToggleFeatureCommand, bool>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IRepository<Domain.Entities.TenantFeature> _featureRepository;
    private readonly IAuditService _auditService;

    public ToggleFeatureHandler(
        ITenantRepository tenantRepository,
        IRepository<Domain.Entities.TenantFeature> featureRepository,
        IAuditService auditService)
    {
        _tenantRepository = tenantRepository;
        _featureRepository = featureRepository;
        _auditService = auditService;
    }

    public async Task<bool> Handle(ToggleFeatureCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException($"Tenant {request.TenantId} not found.");

        var feature = _featureRepository.Query()
            .FirstOrDefault(f => f.TenantId == request.TenantId && f.FeatureKey == request.FeatureKey);

        if (feature == null)
        {
            feature = new Domain.Entities.TenantFeature
            {
                TenantId = request.TenantId,
                FeatureKey = request.FeatureKey,
                IsEnabled = request.IsEnabled,
                Config = request.Config
            };
            await _featureRepository.AddAsync(feature, cancellationToken);
        }
        else
        {
            feature.IsEnabled = request.IsEnabled;
            if (request.Config != null) feature.Config = request.Config;
            await _featureRepository.UpdateAsync(feature, cancellationToken);
        }

        await _auditService.LogAsync("tenant.feature_toggled", "TenantFeature", feature.Id,
            newValues: new { request.FeatureKey, request.IsEnabled },
            source: Domain.Enums.AuditSource.System, cancellationToken: cancellationToken);

        return true;
    }
}
