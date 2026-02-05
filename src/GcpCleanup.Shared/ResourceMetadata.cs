namespace GcpCleanup.Shared;

public enum ResourceType
{
    GcsBucket,
    PersistentDisk
}

public class ResourceMetadata
{
    public ResourceType Type { get; init; }
    public string Name { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public DateTime LastUsedAt { get; init; }
    public Dictionary<string, string> Labels { get; init; } = new();
    public bool IsAttached { get; init; }
}
