using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JLT.Application.Common.Models;
using JLT.Application.DTOs;
using JLT.Application.Features.Tenants;
using JLT.Domain.Entities;
using JLT.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace JLT.Tests;

public class TenantTests
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IRepository<TenantFeature> _featureRepository;
    private readonly IAuditService _auditService;

    public TenantTests()
    {
        _tenantRepository = Substitute.For<ITenantRepository>();
        _featureRepository = Substitute.For<IRepository<TenantFeature>>();
        _auditService = Substitute.For<IAuditService>();
    }

    [Fact]
    public async Task CreateTenant_ShouldCreateTenant_WhenSlugDoesNotExist()
    {
        // Arrange
        var command = new CreateTenantCommand("Acme Corp", "acme", "acme.com", null, null, null, null, null, null);
        _tenantRepository.SlugExistsAsync(command.Slug, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new CreateTenantHandler(_tenantRepository, _auditService);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(command.Name, result.Name);
        Assert.Equal(command.Slug, result.Slug);
        Assert.Equal(command.Domain, result.Domain);
        Assert.True(result.IsActive);

        await _tenantRepository.Received(1).AddAsync(Arg.Is<Tenant>(t => t.Name == command.Name && t.Slug == command.Slug), Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogAsync("tenant.created", "Tenant", Arg.Any<Guid>(), Arg.Any<object>(), Arg.Any<object>(), JLT.Domain.Enums.AuditSource.System, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTenant_ShouldThrowException_WhenSlugAlreadyExists()
    {
        // Arrange
        var command = new CreateTenantCommand("Acme Corp", "acme", "acme.com", null, null, null, null, null, null);
        _tenantRepository.SlugExistsAsync(command.Slug, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new CreateTenantHandler(_tenantRepository, _auditService);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task GetTenantById_ShouldReturnTenant_WhenExists()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Acme", Slug = "acme" };
        _tenantRepository.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(tenant);

        var handler = new GetTenantByIdHandler(_tenantRepository);

        // Act
        var result = await handler.Handle(new GetTenantByIdQuery(tenantId), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenant.Name, result.Name);
        Assert.Equal(tenant.Id, result.Id);
    }

    [Fact]
    public async Task UpdateTenant_ShouldModifyTenant_WhenExists()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Acme", Slug = "acme", IsActive = true };
        _tenantRepository.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(tenant);

        var command = new UpdateTenantCommand(tenantId, "Acme New", "new.acme.com", null, null, null, null, null, null, false);
        var handler = new UpdateTenantHandler(_tenantRepository, _auditService);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Acme New", result.Name);
        Assert.Equal("new.acme.com", result.Domain);
        Assert.False(result.IsActive);

        await _tenantRepository.Received(1).UpdateAsync(tenant, Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogAsync("tenant.updated", "Tenant", tenantId, Arg.Any<object>(), Arg.Any<object>(), JLT.Domain.Enums.AuditSource.System, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleFeature_ShouldCreateFeature_WhenFeatureDoesNotExist()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Acme", Slug = "acme" };
        _tenantRepository.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(tenant);

        var features = new List<TenantFeature>().AsQueryable();
        _featureRepository.Query().Returns(features);

        var command = new ToggleFeatureCommand(tenantId, "scorm_enabled", true, "{}");
        var handler = new ToggleFeatureHandler(_tenantRepository, _featureRepository, _auditService);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        await _featureRepository.Received(1).AddAsync(Arg.Is<TenantFeature>(f => f.TenantId == tenantId && f.FeatureKey == "scorm_enabled" && f.IsEnabled), Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogAsync("tenant.feature_toggled", "TenantFeature", Arg.Any<Guid>(), Arg.Any<object>(), Arg.Any<object>(), JLT.Domain.Enums.AuditSource.System, Arg.Any<CancellationToken>());
    }
}
