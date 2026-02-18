using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using GcpCleanup.Shared;
using Microsoft.Extensions.Logging;

namespace GcpCleanup.Services
{
    public class GcsResourceService : IResourceService
    {
        private readonly StorageClient _storageClient;
        private readonly ILogger<GcsResourceService> _logger;

        public GcsResourceService(ILogger<GcsResourceService> logger)
        {
            _storageClient = StorageClient.Create();
            _logger = logger;
        }

        public async Task<IEnumerable<CleanupResource>> GetResourcesAsync(string projectId)
        {
            var resources = new List<CleanupResource>();
            var buckets = _storageClient.ListBucketsAsync(projectId);

            await foreach (var bucket in buckets)
            {
                var lastUsed = await GetBucketLastModifiedTimeAsync(bucket.Name);

                resources.Add(new CleanupResource
                {
                    Id = bucket.Name,
                    Name = bucket.Name,
                    CreatedAt = bucket.TimeCreatedDateTimeOffset ?? DateTimeOffset.MinValue,
                    LastUsedAt = lastUsed,
                    Labels = bucket.Labels ?? new Dictionary<string, string>(),
                    Type = ResourceType.GcsBucket,
                    IsAttached = false
                });
            }
            return resources;
        }

        public async Task<bool> DeleteResourceAsync(string projectId, string resourceId)
        {
            try
            {
                _logger.LogInformation($"Starting safe deletion for: {resourceId}");
                await EmptyBucketAsync(resourceId);
                await _storageClient.DeleteBucketAsync(resourceId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting {resourceId}: {ex.Message}");
                return false;
            }
        }

        private async Task EmptyBucketAsync(string bucketName)
        {
            var objects = _storageClient.ListObjectsAsync(bucketName);
            int count = 0;
            await foreach (var obj in objects)
            {
                await _storageClient.DeleteObjectAsync(bucketName, obj.Name);
                count++;
                if (count % 100 == 0)
                {
                    _logger.LogInformation($"[Progress] {bucketName}: Deleted {count} objects...");
                }
            }
        }

        private async Task<DateTimeOffset?> GetBucketLastModifiedTimeAsync(string bucketName)
        {
            try
            {
                var asyncEnumerable = _storageClient.ListObjectsAsync(bucketName);

                await foreach (var obj in asyncEnumerable)
                {
                    return obj.UpdatedDateTimeOffset;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not check activity for {bucketName}: {ex.Message}");
            }

            return null;
        }
    }
}