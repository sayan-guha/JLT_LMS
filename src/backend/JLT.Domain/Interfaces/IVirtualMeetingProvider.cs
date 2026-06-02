using System;
using System.Threading;
using System.Threading.Tasks;

namespace JLT.Domain.Interfaces;

public record VirtualMeetingResult(string ExternalMeetingId, string JoinUrl);

public interface IVirtualMeetingProvider
{
    Task<VirtualMeetingResult> ScheduleMeetingAsync(string title, DateTime startTime, DateTime endTime, CancellationToken ct = default);
    Task CancelMeetingAsync(string externalMeetingId, CancellationToken ct = default);
}
