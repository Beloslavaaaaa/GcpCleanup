using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GcpCleanup.Shared
{
    public class CleanupEvaluation
    {
        public CleanupResource Resource { get; set; }
        public bool IsEligible { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
