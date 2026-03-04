using System;
using GcpCleanup.Shared;

namespace GcpCleanup.Services
{
    public class CleanupEligibilityValidator
    {
        public CleanupEvaluation Evaluate(CleanupResource resource, CleanupCriteria criteria)
        {
            var evaluation = new CleanupEvaluation { Resource = resource, IsEligible = true };

            if (resource.IsAttached)
            {
                evaluation.IsEligible = false;
                evaluation.Reason = "Resource is currently attached/in-use";
                return evaluation;
            }

            if (resource.CreatedAt > DateTimeOffset.Now.AddDays(-criteria.MinAgeDays))
            {
                evaluation.IsEligible = false;
                evaluation.Reason = $"Too new (Age < {criteria.MinAgeDays} days)";
                return evaluation;
            }

            if (!resource.Labels.TryGetValue(criteria.RequiredLabelKey, out string val) || val != criteria.RequiredLabelValue)
            {
                evaluation.IsEligible = false;
                evaluation.Reason = $"Missing label: {criteria.RequiredLabelKey}={criteria.RequiredLabelValue}";
                return evaluation;
            }

            if (resource.LastUsedAt.HasValue && resource.LastUsedAt > DateTimeOffset.Now.AddDays(-criteria.MinUnusedDays))
            {
                evaluation.IsEligible = false;
                evaluation.Reason = "Recently used";
                return evaluation;
            }

            return evaluation;
        }
    }
}