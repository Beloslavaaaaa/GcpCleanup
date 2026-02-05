using GcpCleanup.Shared;

namespace GcpCleanup.Core;

public record CleanupDecision(bool ShouldDelete, string Reason);

public interface ICleanupEvaluator
{
    CleanupDecision Evaluate(ResourceMetadata resource);
}

public class DefaultCleanupEvaluator : ICleanupEvaluator
{
    private static readonly TimeSpan Threshold = TimeSpan.FromDays(2);

    public CleanupDecision Evaluate(ResourceMetadata r)
    {
        if (!r.Labels.TryGetValue("develop", out var v) || v != "true")
            return new(false, "Missing develop=true label");

        if (DateTime.UtcNow - r.CreatedAt < Threshold)
            return new(false, "Resource too new");

        if (DateTime.UtcNow - r.LastUsedAt < Threshold)
            return new(false, "Recently used");

        if (r.Type == ResourceType.PersistentDisk && r.IsAttached)
            return new(false, "Disk is attached");

        return new(true, "Eligible for deletion");
    }
}
