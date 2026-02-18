using System.Collections.Generic;
using System.Threading.Tasks;

namespace GcpCleanup.Shared
{
    public interface IResourceService
    {
        Task<IEnumerable<CleanupResource>> GetResourcesAsync(string projectId);
        Task<bool> DeleteResourceAsync(string projectId, string resourceId);
    }
}