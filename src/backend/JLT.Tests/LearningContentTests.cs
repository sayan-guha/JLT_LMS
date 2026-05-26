using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JLT.Application.Common.Models;
using JLT.Application.DTOs;
using JLT.Application.Features.LearningContent;
using JLT.Domain.Entities;
using JLT.Domain.Enums;
using JLT.Domain.Interfaces;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace JLT.Tests;

public class LearningContentTests
{
    private readonly ILearningContentRepository _contentRepository;
    private readonly IContentProgressRepository _progressRepository;
    private readonly IScormRepository _scormRepository;
    private readonly IXApiStatementRepository _xApiRepository;

    public LearningContentTests()
    {
        _contentRepository = Substitute.For<ILearningContentRepository>();
        _progressRepository = Substitute.For<IContentProgressRepository>();
        _scormRepository = Substitute.For<IScormRepository>();
        _xApiRepository = Substitute.For<IXApiStatementRepository>();
    }

    [Fact]
    public void LearningContent_ShouldHaveDefaultValues_WhenCreated()
    {
        // Act
        var content = new LearningContent();

        // Assert
        Assert.Equal(ContentStatus.Draft, content.Status);
        Assert.Equal("1.0", content.Version);
        Assert.Equal("en", content.Language);
        Assert.Equal(ContentSource.Internal, content.ContentSource);
    }

    [Fact]
    public async Task CreateLearningContent_ShouldCreateContentWithCorrectDefaults()
    {
        // Arrange
        var command = new CreateLearningContentCommand(
            Title: "Test Title",
            ContentType: "Document",
            CreatedBy: Guid.NewGuid(),
            Description: "Test Description"
        );

        var handler = new CreateLearningContentHandler(_contentRepository);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(command.Title, result.Title);
        Assert.Equal(command.ContentType, result.ContentType);
        Assert.Equal(command.CreatedBy, result.CreatedBy);
        Assert.Equal("Draft", result.Status);
        Assert.Equal("1.0", result.Version);
        Assert.Equal("en", result.Language);
        Assert.Equal("Internal", result.ContentSource);

        await _contentRepository.Received(1).AddAsync(Arg.Is<LearningContent>(c =>
            c.Title == command.Title &&
            c.ContentType == ContentType.Document &&
            c.Status == ContentStatus.Draft &&
            c.CreatedBy == command.CreatedBy
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateContentStatus_ShouldTransitionStatusAndSetSideEffects_WhenValidTransition()
    {
        // Arrange
        var contentId = Guid.NewGuid();
        var content = new LearningContent
        {
            Id = contentId,
            Title = "Test Content",
            Status = ContentStatus.Draft
        };

        _contentRepository.GetByIdAsync(contentId, Arg.Any<CancellationToken>()).Returns(content);

        var handler = new UpdateContentStatusHandler(_contentRepository);

        // 1. Transition Draft -> InReview
        var command1 = new UpdateContentStatusCommand(contentId, "InReview");
        var result1 = await handler.Handle(command1, CancellationToken.None);

        Assert.Equal("InReview", result1.Status);
        Assert.Null(result1.PublishedAt);

        // 2. Transition InReview -> Published (should set PublishedAt)
        var command2 = new UpdateContentStatusCommand(contentId, "Published");
        var result2 = await handler.Handle(command2, CancellationToken.None);

        Assert.Equal("Published", result2.Status);
        Assert.NotNull(result2.PublishedAt);
        Assert.Null(result2.RetiredAt);

        // 3. Transition Published -> Archived (should set RetiredAt)
        var command3 = new UpdateContentStatusCommand(contentId, "Archived");
        var result3 = await handler.Handle(command3, CancellationToken.None);

        Assert.Equal("Archived", result3.Status);
        Assert.NotNull(result3.RetiredAt);

        await _contentRepository.Received(3).UpdateAsync(content, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateContentStatus_ShouldThrowException_WhenInvalidTransition()
    {
        // Arrange
        var contentId = Guid.NewGuid();
        var content = new LearningContent
        {
            Id = contentId,
            Title = "Test Content",
            Status = ContentStatus.Published
        };

        _contentRepository.GetByIdAsync(contentId, Arg.Any<CancellationToken>()).Returns(content);

        var handler = new UpdateContentStatusHandler(_contentRepository);
        var command = new UpdateContentStatusCommand(contentId, "Draft"); // Published -> Draft is invalid

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task GetLearningContentList_ShouldReturnFilteredAndPaginatedContent()
    {
        // Arrange
        var contentList = new List<LearningContent>
        {
            new() { Id = Guid.NewGuid(), Title = "Doc Content 1", ContentType = ContentType.Document, Category = "Compliance", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new() { Id = Guid.NewGuid(), Title = "Doc Content 2", ContentType = ContentType.Document, Category = "Safety", CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = Guid.NewGuid(), Title = "Media Content 1", ContentType = ContentType.Media, Category = "Compliance", CreatedAt = DateTime.UtcNow }
        };

        var mockDbSet = contentList.BuildMock();
        _contentRepository.Query().Returns(mockDbSet);

        var query = new GetLearningContentListQuery(
            PageNumber: 1,
            PageSize: 10,
            ContentType: "Document",
            Category: "Compliance"
        );
        var handler = new GetLearningContentListHandler(_contentRepository);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Doc Content 1", result.Items.First().Title);
        Assert.Equal("Document", result.Items.First().ContentType);
        Assert.Equal("Compliance", result.Items.First().Category);
    }

    [Fact]
    public async Task UpsertContentProgress_ShouldCreateProgress_WhenProgressDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var command = new UpsertContentProgressCommand(userId, contentId, 50.5m, "page-5", 120);

        _progressRepository.GetByUserAndContentAsync(userId, contentId, Arg.Any<CancellationToken>()).Returns((ContentProgress?)null);

        var handler = new UpsertContentProgressHandler(_progressRepository);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("InProgress", result.Status);
        Assert.Equal(50.5m, result.ProgressPercent);
        Assert.Equal("page-5", result.BookmarkData);
        Assert.Equal(120, result.TimeSpentSeconds);
        Assert.Null(result.CompletedAt);

        await _progressRepository.Received(1).AddAsync(Arg.Is<ContentProgress>(p =>
            p.UserId == userId &&
            p.LearningContentId == contentId &&
            p.ProgressPercent == 50.5m &&
            p.Status == ProgressStatus.InProgress &&
            p.BookmarkData == "page-5" &&
            p.TimeSpentSeconds == 120
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertContentProgress_ShouldUpdateProgressAndComplete_WhenProgressReaches100Percent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var existingProgress = new ContentProgress
        {
            UserId = userId,
            LearningContentId = contentId,
            ProgressPercent = 50m,
            Status = ProgressStatus.InProgress
        };

        _progressRepository.GetByUserAndContentAsync(userId, contentId, Arg.Any<CancellationToken>()).Returns(existingProgress);

        var command = new UpsertContentProgressCommand(userId, contentId, 100m, "completed-bookmark", 300);
        var handler = new UpsertContentProgressHandler(_progressRepository);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Completed", result.Status);
        Assert.Equal(100m, result.ProgressPercent);
        Assert.Equal("completed-bookmark", result.BookmarkData);
        Assert.Equal(300, result.TimeSpentSeconds);
        Assert.NotNull(result.CompletedAt);

        await _progressRepository.Received(1).UpdateAsync(existingProgress, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteLearningContent_ShouldDeleteDraftContent()
    {
        // Arrange
        var contentId = Guid.NewGuid();
        var content = new LearningContent { Id = contentId, Status = ContentStatus.Draft };
        _contentRepository.GetByIdAsync(contentId, Arg.Any<CancellationToken>()).Returns(content);

        var handler = new DeleteLearningContentHandler(_contentRepository);
        var command = new DeleteLearningContentCommand(contentId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        await _contentRepository.Received(1).DeleteAsync(content, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteLearningContent_ShouldNotDeleteNonDraftContent()
    {
        // Arrange
        var contentId = Guid.NewGuid();
        var content = new LearningContent { Id = contentId, Status = ContentStatus.Published };
        _contentRepository.GetByIdAsync(contentId, Arg.Any<CancellationToken>()).Returns(content);

        var handler = new DeleteLearningContentHandler(_contentRepository);
        var command = new DeleteLearningContentCommand(contentId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result);
        await _contentRepository.DidNotReceive().DeleteAsync(Arg.Any<LearningContent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetScormRuntimeState_ShouldReturnState_WhenStateExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var packageId = Guid.NewGuid();
        var package = new ScormPackage { Id = packageId, LearningContentId = contentId };
        var state = new ScormRuntimeState { UserId = userId, ScormPackageId = packageId, LessonStatus = "incomplete" };

        _scormRepository.GetPackageByContentIdAsync(contentId, Arg.Any<CancellationToken>()).Returns(package);
        _scormRepository.GetRuntimeStateAsync(userId, packageId, Arg.Any<CancellationToken>()).Returns(state);

        var handler = new GetScormRuntimeStateHandler(_scormRepository);
        var query = new GetScormRuntimeStateQuery(userId, contentId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("incomplete", result.LessonStatus);
    }

    [Fact]
    public async Task UpsertScormRuntimeState_ShouldUpsertState_WhenPackageExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var packageId = Guid.NewGuid();
        var package = new ScormPackage { Id = packageId, LearningContentId = contentId };
        var state = new ScormRuntimeState { UserId = userId, ScormPackageId = packageId, LessonStatus = "completed" };

        _scormRepository.GetPackageByContentIdAsync(contentId, Arg.Any<CancellationToken>()).Returns(package);
        _scormRepository.UpsertRuntimeStateAsync(Arg.Any<ScormRuntimeState>(), Arg.Any<CancellationToken>()).Returns(state);

        var handler = new UpsertScormRuntimeStateHandler(_scormRepository);
        var command = new UpsertScormRuntimeStateCommand(userId, contentId, "completed");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("completed", result.LessonStatus);
        await _scormRepository.Received(1).UpsertRuntimeStateAsync(Arg.Is<ScormRuntimeState>(s =>
            s.UserId == userId &&
            s.ScormPackageId == packageId &&
            s.LessonStatus == "completed"
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StoreXApiStatement_ShouldStoreStatement()
    {
        // Arrange
        var command = new StoreXApiStatementCommand(
            ActorJson: "{\"mbox\":\"mailto:user@example.com\"}",
            VerbId: "http://adlnet.gov/expapi/verbs/completed",
            ObjectJson: "{\"id\":\"http://example.com/activities/course1\"}"
        );
        var handler = new StoreXApiStatementHandler(_xApiRepository);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(command.VerbId, result.VerbId);
        Assert.Equal(command.ActorJson, result.ActorJson);

        await _xApiRepository.Received(1).AddAsync(Arg.Is<XApiStatement>(x =>
            x.VerbId == command.VerbId &&
            x.ActorJson == command.ActorJson &&
            x.ObjectJson == command.ObjectJson
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetXApiStatements_ShouldReturnFilteredStatements()
    {
        // Arrange
        var statements = new List<XApiStatement>
        {
            new() { Id = Guid.NewGuid(), VerbId = "completed", ActorJson = "user1", Timestamp = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), VerbId = "attempted", ActorJson = "user1", Timestamp = DateTime.UtcNow.AddMinutes(-5) },
            new() { Id = Guid.NewGuid(), VerbId = "completed", ActorJson = "user2", Timestamp = DateTime.UtcNow.AddMinutes(-10) }
        };

        var mockDbSet = statements.BuildMock();
        _xApiRepository.Query().Returns(mockDbSet);

        var query = new GetXApiStatementsQuery(VerbId: "completed", ActorJson: "user1");
        var handler = new GetXApiStatementsHandler(_xApiRepository);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("completed", result.Items.First().VerbId);
        Assert.Equal("user1", result.Items.First().ActorJson);
    }
}
