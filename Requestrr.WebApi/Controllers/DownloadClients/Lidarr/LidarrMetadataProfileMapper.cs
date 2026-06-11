using Requestrr.WebApi.RequestrrBot.DownloadClients.Lidarr;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Requestrr.WebApi.Controllers.DownloadClients.Lidarr
{
    public static class LidarrMetadataProfileMapper
    {
        public static void ApplyProfileFiltersToCategories(LidarrSettingsCategory[] categories, IList<LidarrClient.JSONMetadataProfile> metadataProfiles)
        {
            var metadataProfilesById = metadataProfiles.ToDictionary(x => x.id, x => x);

            foreach (var category in categories)
            {
                if (!metadataProfilesById.TryGetValue(category.MetadataProfileId, out var metadataProfile))
                    throw new Exception($"Invalid metadata profile selected for category \"{category.Name}\".");

                // Legacy field no longer drives filtering when metadata profile sync is enabled.
                category.ReleaseTypes = Array.Empty<string>();
                category.PrimaryTypes = NormalizePrimaryTypes(metadataProfile.primaryTypes);
                category.SecondaryTypes = NormalizeSecondaryTypes(metadataProfile.secondaryTypes);
                category.ReleaseStatuses = NormalizeReleaseStatuses(metadataProfile.releaseStatuses);
            }
        }

        private static string[] NormalizePrimaryTypes(string[] primaryTypes)
        {
            return (primaryTypes ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Where(AllowedPrimaryTypes.Contains)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .ToArray();
        }

        private static string[] NormalizeSecondaryTypes(string[] secondaryTypes)
        {
            return (secondaryTypes ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(NormalizeSecondaryTypeValue)
                .Where(x => x != null)
                .Where(AllowedSecondaryTypes.Contains)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .ToArray();
        }

        private static string[] NormalizeReleaseStatuses(string[] releaseStatuses)
        {
            return (releaseStatuses ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(NormalizeReleaseStatusValue)
                .Where(x => x != null)
                .Where(AllowedReleaseStatuses.Contains)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .ToArray();
        }

        private static readonly HashSet<string> AllowedPrimaryTypes = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
        {
            "Album",
            "Broadcast",
            "EP",
            "Other",
            "Single"
        };

        private static readonly HashSet<string> AllowedSecondaryTypes = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
        {
            "Studio",
            "Spokenword",
            "Soundtrack",
            "Remix",
            "Mixtape/Street",
            "Live",
            "Interview",
            "DJ-mix",
            "Demo",
            "Compilation",
            "Audio drama"
        };

        private static readonly HashSet<string> AllowedReleaseStatuses = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
        {
            "Pseudo-Release",
            "Promotion",
            "Official",
            "Bootleg"
        };

        private static string NormalizeSecondaryTypeValue(string value)
        {
            string normalized = value?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return null;

            if (normalized.Equals("DJ-Mix", StringComparison.InvariantCultureIgnoreCase))
                return "DJ-mix";
            if (normalized.Equals("DJMix", StringComparison.InvariantCultureIgnoreCase))
                return "DJ-mix";
            if (normalized.Equals("SpokenWord", StringComparison.InvariantCultureIgnoreCase))
                return "Spokenword";
            if (normalized.Equals("AudioDrama", StringComparison.InvariantCultureIgnoreCase))
                return "Audio drama";
            if (normalized.Equals("MixtapeStreet", StringComparison.InvariantCultureIgnoreCase))
                return "Mixtape/Street";
            if (normalized.Equals("SoundTrack", StringComparison.InvariantCultureIgnoreCase))
                return "Soundtrack";
            return normalized;
        }

        private static string NormalizeReleaseStatusValue(string value)
        {
            string normalized = value?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return null;

            if (normalized.Equals("Pseudo Release", StringComparison.InvariantCultureIgnoreCase))
                return "Pseudo-Release";
            return normalized;
        }
    }
}
