using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GcpCleanup.Shared;
using Microsoft.Extensions.Logging;

namespace GcpCleanup.Services
{
    public class CleanupService
    {
        private readonly IEnumerable<IResourceService> _services;
        private readonly CleanupEligibilityValidator _validator;
        private readonly ILogger<CleanupService> _logger;

        public CleanupService(IEnumerable<IResourceService> services, ILogger<CleanupService> logger)
        {
            _services = services;
            _validator = new CleanupEligibilityValidator();
            _logger = logger;
        }

        public async Task<CleanupSummary> RunCleanupAsync(string projectId, CleanupCriteria criteria, string mode, ResourceType targetType = ResourceType.All)
        {
            var summary = new CleanupSummary();

            foreach (var service in _services)
            {
                bool isGcs = service is GcsResourceService;
                bool isCompute = service is ComputeResourceService;

                if (targetType != ResourceType.All)
                {
                    if (targetType == ResourceType.GcsBucket && !isGcs) continue;
                    if (targetType == ResourceType.ComputeInstance && !isCompute) continue;
                }

                var resources = await service.GetResourcesAsync(projectId);
                var resourceList = resources.ToList();

                foreach (var resource in resourceList)
                {
                    summary.TotalDiscovered++;
                    var evaluation = _validator.Evaluate(resource, criteria);

                    if (evaluation.IsEligible && mode != "list")
                    {
                        bool shouldDelete = await HandleModeAsync(resource, mode);

                        if (shouldDelete)
                        {
                            bool success = await service.DeleteResourceAsync(projectId, resource.Id);
                            summary.Details.Add(new OperationResult { ResourceId = resource.Id, Success = success });
                            if (success) summary.TotalDeleted++; else summary.TotalFailed++;
                        }
                    }
                }
            }
            return summary;
        }

        private async Task<bool> HandleModeAsync(CleanupResource resource, string mode)
        {
            switch (mode.ToLower())
            {
                case "dry-run":
                    Console.WriteLine($"[DRY-RUN] Target: {resource.Name}");
                    return false;
                case "force":
                    return true;
                case "interactive":
                default:
                    Console.Write($"\nCONFIRM: Delete {resource.Type} '{resource.Name}'? (y/n): ");
                    return Console.ReadLine()?.ToLower() == "y";
            }
        }
    }
}