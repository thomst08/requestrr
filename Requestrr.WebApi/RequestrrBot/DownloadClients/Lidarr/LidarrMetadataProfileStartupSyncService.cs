using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Requestrr.WebApi.Controllers.DownloadClients;
using Requestrr.WebApi.Controllers.DownloadClients.Lidarr;
using Requestrr.WebApi.RequestrrBot.Music;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.DownloadClients.Lidarr
{
    public class LidarrMetadataProfileStartupSyncService : BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LidarrMetadataProfileStartupSyncService> _logger;
        private readonly ILogger<LidarrClient> _lidarrLogger;
        private readonly LidarrSettingsProvider _lidarrSettingsProvider;
        private readonly MusicSettingsProvider _musicSettingsProvider;

        public LidarrMetadataProfileStartupSyncService(
            IHttpClientFactory httpClientFactory,
            ILogger<LidarrMetadataProfileStartupSyncService> logger,
            ILogger<LidarrClient> lidarrLogger,
            LidarrSettingsProvider lidarrSettingsProvider,
            MusicSettingsProvider musicSettingsProvider)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _lidarrLogger = lidarrLogger;
            _lidarrSettingsProvider = lidarrSettingsProvider;
            _musicSettingsProvider = musicSettingsProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            await SyncMetadataProfileFiltersAsync();
        }

        private async Task SyncMetadataProfileFiltersAsync()
        {
            try
            {
                MusicSettings musicSettings = _musicSettingsProvider.Provide();
                if (!string.Equals(musicSettings.Client, DownloadClient.Lidarr, StringComparison.InvariantCultureIgnoreCase))
                    return;

                LidarrSettings currentSettings = _lidarrSettingsProvider.Provider();
                if (!IsLidarrConfigured(currentSettings))
                    return;

                if (currentSettings.Categories == null || !currentSettings.Categories.Any())
                    return;

                IList<LidarrClient.JSONMetadataProfile> metadataProfiles = await LidarrClient.GetMetadataProfilesDetailed(
                    _httpClientFactory.CreateClient(),
                    _lidarrLogger,
                    currentSettings);

                LidarrSettingsModel updatedSettings = ConvertToModel(currentSettings);
                LidarrMetadataProfileMapper.ApplyProfileFiltersToCategories(updatedSettings.Categories, metadataProfiles);

                if (!HasFilterChanges(currentSettings.Categories, updatedSettings.Categories))
                    return;

                DownloadClientsSettingsRepository.SetLidarr(musicSettings, updatedSettings);
                _logger.LogInformation("Refreshed Lidarr metadata profile filters on startup.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh Lidarr metadata profile filters on startup.");
            }
        }

        private static bool IsLidarrConfigured(LidarrSettings settings)
        {
            return settings != null
                && !string.IsNullOrWhiteSpace(settings.Hostname)
                && !string.IsNullOrWhiteSpace(settings.ApiKey)
                && !string.IsNullOrWhiteSpace(settings.Version)
                && settings.Port > 0;
        }

        private static LidarrSettingsModel ConvertToModel(LidarrSettings settings)
        {
            return new LidarrSettingsModel
            {
                Hostname = settings.Hostname,
                ApiKey = settings.ApiKey,
                BaseUrl = settings.BaseUrl,
                Port = settings.Port,
                UseSSL = settings.UseSSL,
                Version = settings.Version,
                SearchNewRequests = settings.SearchNewRequests,
                MonitorNewRequests = settings.MonitorNewRequests,
                AllowBulkAlbumRequests = settings.AllowBulkAlbumRequests,
                Categories = (settings.Categories ?? Array.Empty<LidarrCategory>()).Select(x => new LidarrSettingsCategory
                {
                    Id = x.Id,
                    Name = x.Name,
                    ProfileId = x.ProfileId,
                    MetadataProfileId = x.MetadataProfileId,
                    RootFolder = x.RootFolder,
                    Tags = x.Tags ?? Array.Empty<int>(),
                    ReleaseTypes = x.ReleaseTypes ?? Array.Empty<string>(),
                    PrimaryTypes = x.PrimaryTypes ?? Array.Empty<string>(),
                    SecondaryTypes = x.SecondaryTypes ?? Array.Empty<string>(),
                    ReleaseStatuses = x.ReleaseStatuses ?? Array.Empty<string>()
                }).ToArray()
            };
        }

        private static bool HasFilterChanges(LidarrCategory[] oldCategories, LidarrSettingsCategory[] newCategories)
        {
            var oldById = (oldCategories ?? Array.Empty<LidarrCategory>()).ToDictionary(x => x.Id, x => x);
            foreach (var category in newCategories ?? Array.Empty<LidarrSettingsCategory>())
            {
                if (!oldById.TryGetValue(category.Id, out var oldCategory))
                    return true;

                if (!SetEquals(oldCategory.PrimaryTypes, category.PrimaryTypes))
                    return true;

                if (!SetEquals(oldCategory.SecondaryTypes, category.SecondaryTypes))
                    return true;

                if (!SetEquals(oldCategory.ReleaseStatuses, category.ReleaseStatuses))
                    return true;
            }

            return false;
        }

        private static bool SetEquals(string[] oldValues, string[] newValues)
        {
            var oldSet = (oldValues ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            var newSet = (newValues ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            return oldSet.SetEquals(newSet);
        }
    }
}
