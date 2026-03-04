using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Compute.V1;
using GcpCleanup.Shared;
using Microsoft.Extensions.Logging;

namespace GcpCleanup.Services
{
    public class ComputeResourceService : IResourceService
    {
        private readonly DisksClient _disksClient;
        private readonly ILogger<ComputeResourceService> _logger;

        private string ExtractInstanceName(string userUrl)
        {
            if (string.IsNullOrWhiteSpace(userUrl)) return "Unknown";
            return userUrl.Split('/').Last();
        }
        public ComputeResourceService(ILogger<ComputeResourceService> logger)
        {
            _disksClient = DisksClient.Create();
            _logger = logger;
        }

        public async Task<IEnumerable<CleanupResource>> GetResourcesAsync(string projectId)
        {
            var resources = new List<CleanupResource>();

            try
            {
                var pagedSource = _disksClient.AggregatedListAsync(projectId);

                await foreach (var zoneData in pagedSource)
                {
                    if (zoneData.Value.Disks == null) continue;

                    foreach (var disk in zoneData.Value.Disks)
                    {
                        var labelDict = disk.Labels.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                        var zoneName = zoneData.Key.Replace("zones/", "");

                        resources.Add(new CleanupResource
                        {
                            Id = $"{zoneName}/{disk.Name}",
                            Name = disk.Name,
                            CreatedAt = DateTimeOffset.Parse(disk.CreationTimestamp),
                            LastUsedAt = GetDiskLastModifiedTime(disk),
                            Labels = labelDict,
                            IsAttached = disk.Users != null && disk.Users.Count > 0,
                            Type = ResourceType.ComputeInstance
                        });
                    }
                }
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                _logger.LogError($"Network Error: Could not reach Compute Engine API. Check your internet connection or if the API is enabled. Details: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred while discovering disks: {ex.Message}");
            }

            return resources;
        }

        public async Task<bool> DeleteResourceAsync(string projectId, string resourceId)
        {
            try
            {
                var parts = resourceId.Split('/');
                var zone = parts[0];
                var diskName = parts[1];

                _logger.LogInformation($"Attempting to delete disk: {diskName} in zone: {zone}");

                var operation = await _disksClient.DeleteAsync(projectId, zone, diskName);
                await operation.PollUntilCompletedAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to delete disk {resourceId}: {ex.Message}");
                return false;
            }
        }

        private DateTimeOffset? GetDiskLastModifiedTime(Disk disk)
        {
            if (!string.IsNullOrEmpty(disk.LastDetachTimestamp))
                return DateTimeOffset.Parse(disk.LastDetachTimestamp);

            return DateTimeOffset.Parse(disk.CreationTimestamp);
        }
    }
}