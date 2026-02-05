using FluentAssertions;
using GcpCleanup.Core;
using GcpCleanup.Shared;

namespace GcpCleanup.Tests;

public class CleanupEvaluatorTests
{
    [Fact]
    public void Attached_disk_should_not_be_deleted()
    {
        var evaluator = new DefaultCleanupEvaluator();

        var resource = new ResourceMetadata
        {
            Type = ResourceType.PersistentDisk,
            IsAttached = true,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            LastUsedAt = DateTime.UtcNow.AddDays(-10),
            Labels = { ["develop"] = "true" }
        };

        var result = evaluator.Evaluate(resource);

        result.ShouldDelete.Should().BeFalse();
    }
}
