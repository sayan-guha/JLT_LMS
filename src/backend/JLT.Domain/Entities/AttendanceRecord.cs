using System;
using JLT.Domain.Common;
using JLT.Domain.Enums;

namespace JLT.Domain.Entities;

public class AttendanceRecord : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public AttendanceStatus Status { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public AttendanceSource Source { get; set; } = AttendanceSource.Manual;

    // Navigation
    public virtual Session? Session { get; set; }
    public virtual User? User { get; set; }
}
