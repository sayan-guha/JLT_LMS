using System;
using Xunit;
using JLT.Domain.Entities;
using JLT.Domain.Enums;

namespace JLT.Tests.Domain.Classrooms;

public class BatchAndSessionTests
{
    [Fact]
    public void TrainingBatch_DefaultsToScheduled()
    {
        var batch = new TrainingBatch
        {
            Name = "July 2026 Cohort",
            StartDate = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc)
        };
        Assert.Equal(BatchStatus.Scheduled, batch.Status);
    }

    [Fact]
    public void Session_CanAssignInstructorAndResource()
    {
        var instructorId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        var session = new Session
        {
            Title = "Day 1: Intro",
            SessionMode = SessionMode.Hybrid,
            StartTime = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 7, 1, 11, 0, 0, DateTimeKind.Utc),
            PhysicalResourceId = resourceId
        };

        session.SessionInstructors.Add(new SessionInstructor
        {
            InstructorProfileId = instructorId
        });

        Assert.Equal(SessionStatus.Scheduled, session.Status);
        Assert.Equal(SessionMode.Hybrid, session.SessionMode);
        Assert.Single(session.SessionInstructors);
    }
}
