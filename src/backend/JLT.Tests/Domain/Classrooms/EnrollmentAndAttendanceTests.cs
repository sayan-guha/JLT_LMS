using System;
using Xunit;
using JLT.Domain.Entities;
using JLT.Domain.Enums;

namespace JLT.Tests.Domain.Classrooms;

public class EnrollmentAndAttendanceTests
{
    [Fact]
    public void Enrollment_DefaultsToEnrolledStatus()
    {
        var enrollment = new Enrollment
        {
            UserId = Guid.NewGuid(),
            TrainingBatchId = Guid.NewGuid(),
            EnrolledAt = DateTime.UtcNow
        };
        Assert.Equal(EnrollmentStatus.Enrolled, enrollment.Status);
    }

    [Fact]
    public void AttendanceRecord_CanTrackVirtualAutomatic()
    {
        var record = new AttendanceRecord
        {
            SessionId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = AttendanceStatus.Present,
            Source = AttendanceSource.VirtualAutomatic,
            CheckInTime = DateTime.UtcNow
        };
        Assert.Equal(AttendanceSource.VirtualAutomatic, record.Source);
        Assert.Equal(AttendanceStatus.Present, record.Status);
    }
}
