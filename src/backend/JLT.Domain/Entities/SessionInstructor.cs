using System;
using JLT.Domain.Common;

namespace JLT.Domain.Entities;

public class SessionInstructor : BaseEntity
{
    public Guid SessionId { get; set; }
    public Guid InstructorProfileId { get; set; }

    // Navigation
    public virtual Session? Session { get; set; }
    public virtual InstructorProfile? InstructorProfile { get; set; }
}
