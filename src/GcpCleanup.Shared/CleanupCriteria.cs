using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GcpCleanup.Shared
{
    public class CleanupCriteria
    {
        public int MinAgeDays { get; set; } = 2;
        public string RequiredLabelKey { get; set; } = "develop";
        public string RequiredLabelValue { get; set; } = "true";
        public int MinUnusedDays { get; set; } = 2;
    }
}
