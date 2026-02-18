using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GcpCleanup.Services;
using GcpCleanup.Shared;
using Microsoft.Extensions.Logging;

namespace GcpCleanup.Cli
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", @"C:\Users\user_2\Downloads\graphic-pathway-348514-ee7c984bbc85.json");

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("GcpCleanup", LogLevel.Information)
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = false;
                        options.SingleLine = true;
                        options.TimestampFormat = "[HH:mm:ss] ";
                    });
            });

            var logger = loggerFactory.CreateLogger<GcsResourceService>();
            var gcsService = new GcsResourceService(logger);

            string projectId = "graphic-pathway-348514";
            var criteria = new CleanupCriteria();
            var summary = new CleanupSummary();

            Console.WriteLine($"--- GCP Cleanup: {projectId} ---");
            logger.LogWarning("test");
            var resources = await gcsService.GetResourcesAsync(projectId);

            foreach (var resource in resources)
            {
                summary.TotalDiscovered++;
                var evaluation = EvaluateResource(resource, criteria);

                //Console.WriteLine("----------------------------------");
                //Console.WriteLine($"Bucket:    {resource.Name} [{resource.Type}]");
                //Console.WriteLine($"Created:   {resource.CreatedAt:d}");
                //Console.WriteLine($"Last Used: {(resource.LastUsedAt.HasValue ? resource.LastUsedAt.Value.ToString("g") : "Never/Empty")}");

                if (evaluation.IsEligible)
                {
                    Console.WriteLine("----------------------------------");
                    Console.WriteLine($"Bucket:    {resource.Name} [{resource.Type}]");
                    Console.WriteLine($"Created:   {resource.CreatedAt:d}");
                    Console.WriteLine($"Last Used: {(resource.LastUsedAt.HasValue ? resource.LastUsedAt.Value.ToString("g") : "Never/Empty")}");
                    Console.WriteLine("STATUS:    ELIGIBLE for cleanup.");
                    Console.Write("Action:    Delete now? (y/n): ");
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        bool success = await gcsService.DeleteResourceAsync(projectId, resource.Id);

                        await Task.Delay(200);

                        summary.Details.Add(new OperationResult { ResourceId = resource.Id, Success = success });
                        if (success) summary.TotalDeleted++; else summary.TotalFailed++;
                    }
                }
                //else
                //{
                //    Console.WriteLine($"STATUS:    SKIPPED ({evaluation.Reason})");
                //}
            }

            Console.WriteLine("\n--- FINAL SUMMARY ---");
            Console.WriteLine($"Discovered: {summary.TotalDiscovered}");
            Console.WriteLine($"Deleted:    {summary.TotalDeleted}");
            Console.WriteLine($"Failed:     {summary.TotalFailed}");

            loggerFactory.Dispose();
            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }

        private static CleanupEvaluation EvaluateResource(CleanupResource r, CleanupCriteria c)
        {
            var eval = new CleanupEvaluation { Resource = r, IsEligible = true };

            if (r.CreatedAt > DateTimeOffset.Now.AddDays(-c.MinAgeDays))
            {
                eval.IsEligible = false;
                eval.Reason = $"Too new (Age < {c.MinAgeDays} days)";
            }
            else if (!r.Labels.TryGetValue(c.RequiredLabelKey, out string val) || val != c.RequiredLabelValue)
            {
                eval.IsEligible = false;
                eval.Reason = $"Missing label {c.RequiredLabelKey}={c.RequiredLabelValue}";
            }
            else if (r.LastUsedAt.HasValue && r.LastUsedAt > DateTimeOffset.Now.AddDays(-c.MinUnusedDays))
            {
                eval.IsEligible = false;
                eval.Reason = "Recently used";
            }

            return eval;
        }
    }
}