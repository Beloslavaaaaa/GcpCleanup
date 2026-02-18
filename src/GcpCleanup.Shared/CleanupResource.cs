using System;
using System.Collections.Generic;

namespace GcpCleanup.Shared
{
    public class CleanupResource
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastUsedAt { get; set; }
        public IDictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
        public bool IsAttached { get; set; }
        public ResourceType Type { get; set; }
    }
}