using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GcpCleanup.Shared
{
    public class CleanupSummary
    {
        public int TotalDiscovered { get; set; }
        public int TotalDeleted { get; set; }
        public int TotalFailed { get; set; }
        public List<OperationResult> Details { get; set; } = new List<OperationResult>();
    }
}
