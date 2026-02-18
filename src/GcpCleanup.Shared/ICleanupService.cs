using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GcpCleanup.Shared
{
    public interface ICleanupService
    {
        Task RunCleanupAsync(string projectId, CleanupCriteria criteria);
        Task ListResourcesAsync(string projectId);
    }
}
