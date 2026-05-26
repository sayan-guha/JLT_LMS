using Xunit;
using JLT.Domain.Entities;
using JLT.Domain.Enums;

namespace JLT.Tests.Domain.Classrooms;

public class TemplateTests
{
    [Fact]
    public void TrainingTemplate_CanAddTemplateSessions()
    {
        var template = new TrainingTemplate
        {
            Name = "Onboarding Program",
            Description = "3-day onboarding",
            Category = "HR"
        };

        template.Sessions.Add(new TemplateSession
        {
            Title = "Day 1: Company Overview",
            SortOrder = 1,
            DurationMinutes = 120,
            SessionMode = SessionMode.Physical
        });

        template.Sessions.Add(new TemplateSession
        {
            Title = "Day 2: Tools & Systems",
            SortOrder = 2,
            DurationMinutes = 180,
            SessionMode = SessionMode.Virtual
        });

        Assert.Equal(2, template.Sessions.Count);
        Assert.True(template.IsActive);
    }
}
