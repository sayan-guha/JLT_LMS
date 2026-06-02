using System;
using System.Collections.Generic;
using JLT.Domain.Common;
using JLT.Domain.Enums;

namespace JLT.Domain.Entities;

public class Session : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TrainingBatchId { get; set; }
    public Guid? TemplateSessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SessionMode SessionMode { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public MeetingProvider? MeetingProvider { get; set; }
    public string? ExternalMeetingId { get; set; }
    public string? JoinUrl { get; set; }
    public Guid? PhysicalResourceId { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;

    // Navigation
    public virtual TrainingBatch? TrainingBatch { get; set; }
    public virtual PhysicalResource? PhysicalResource { get; set; }
    public virtual ICollection<SessionInstructor> SessionInstructors { get; set; } = new List<SessionInstructor>();
    public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
