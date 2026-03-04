using System;
using System.Collections.Generic;
using GcpCleanup.Shared;
using GcpCleanup.Services;

namespace GcpCleanup.Cli
{
    public static class ConsoleHelper
    {
        public static void DisplayResourceTable(IEnumerable<CleanupResource> resources, CleanupEligibilityValidator validator, CleanupCriteria criteria)
        {
            Console.WriteLine("\n{0,-40} | {1,-12} | {2,-10} | {3,-15} | {4}", "NAME", "TYPE", "AGE (DAYS)", "LAST USED", "STATUS");
            Console.WriteLine(new string('-', 110));

            foreach (var r in resources)
            {
                var eval = validator.Evaluate(r, criteria);
                string name = r.Name.Length > 37 ? r.Name.Substring(0, 37) + "..." : r.Name;
                double age = (DateTimeOffset.Now - r.CreatedAt).TotalDays;
                string lastUsed = r.LastUsedAt.HasValue ? $"{(DateTimeOffset.Now - r.LastUsedAt.Value).TotalDays:F1}d ago" : "Never";

                Console.WriteLine("{0,-40} | {1,-12} | {2,-10:F1} | {3,-15} | {4}",
                    name, r.Type, age, lastUsed, eval.IsEligible ? "ELIGIBLE" : "SKIPPED");

                if (!eval.IsEligible)
                {
                    Console.WriteLine($"  └─ Reason: {eval.Reason}");
                }

                if (r.IsAttached)
                {
                    Console.WriteLine("  └─ Attached to Instance: Yes");
                }
            }
        }

        public static void DisplaySummary(CleanupSummary summary)
        {
            Console.WriteLine("\n" + new string('=', 45));
            Console.WriteLine("                FINAL CLEANUP REPORT");
            Console.WriteLine(new string('=', 45));
            Console.WriteLine($"{"Resources Discovered:",-30} {summary.TotalDiscovered}");
            Console.WriteLine($"{"Resources Deleted:",-30} {summary.TotalDeleted} ✓");
            Console.WriteLine($"{"Operations Failed:",-30} {summary.TotalFailed} ✗");
            Console.WriteLine(new string('=', 45));

            foreach (var detail in summary.Details)
            {
                if (!detail.Success && !string.IsNullOrEmpty(detail.ResourceId))
                {
                    Console.WriteLine($"[!] FAILED: {detail.ResourceId}");
                }
            }
        }

        public static void DisplayUsage()
        {
            Console.WriteLine("\nGCP CLEANUP TOOL HELP");
            Console.WriteLine(new string('-', 30));
            Console.WriteLine("Usage: dotnet run -- --project [ID] --mode [mode]");
            Console.WriteLine("\nModes:");
            Console.WriteLine("  list        - Show resources and eligibility only");
            Console.WriteLine("  dry-run     - Show intended deletions without executing");
            Console.WriteLine("  interactive - Prompt for each eligible resource");
            Console.WriteLine("  force       - Delete all eligible resources immediately");
        }
    }
}