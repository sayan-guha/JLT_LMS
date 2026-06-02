using System;
using Xunit;
using JLT.Domain.Entities;
using JLT.Domain.Enums;

namespace JLT.Tests.Domain.Classrooms;

public class ResourceAndInstructorTests
{
    [Fact]
    public void PhysicalResource_DefaultsToActive()
    {
        var resource = new PhysicalResource
        {
            Name = "Room A101",
            Type = ResourceType.Room,
            Location = "Building A, Floor 1",
            Capacity = 30
        };
        Assert.True(resource.IsActive);
        Assert.Equal("Room A101", resource.Name);
        Assert.Equal(ResourceType.Room, resource.Type);
    }

    [Fact]
    public void InstructorProfile_CanBeCreatedWithUserId()
    {
        var userId = Guid.NewGuid();
        var profile = new InstructorProfile
        {
            UserId = userId,
            Bio = "Senior .NET instructor",
            IsActive = true
        };
        Assert.Equal(userId, profile.UserId);
        Assert.True(profile.IsActive);
    }
}
