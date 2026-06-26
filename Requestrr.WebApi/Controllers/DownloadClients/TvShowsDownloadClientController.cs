using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Requestrr.WebApi.config;
using Requestrr.WebApi.Controllers.DownloadClients.Ombi;
using Requestrr.WebApi.Controllers.DownloadClients.Overseerr;
using Requestrr.WebApi.Controllers.DownloadClients.Sonarr;
using Requestrr.WebApi.RequestrrBot.DownloadClients;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Lidarr;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Overseerr;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Radarr;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Sonarr;
using Requestrr.WebApi.RequestrrBot.Locale;
using Requestrr.WebApi.RequestrrBot.Movies;
using Requestrr.WebApi.RequestrrBot.Music;
using Requestrr.WebApi.RequestrrBot.TvShows;
using SonarrSettingsCategory = Requestrr.WebApi.Controllers.DownloadClients.Sonarr.SonarrSettingsCategory;

namespace Requestrr.WebApi.Controllers.DownloadClients
{
    [ApiController]
    [Authorize]
    [Route("/api/tvshows")]
    public class TvShowsDownloadClientController : ControllerBase
    {
        private readonly MoviesSettings _moviesSettings;
        private readonly TvShowsSettings _tvShowsSettings;
        private readonly MusicSettings _musicSettings;
        private readonly DownloadClientsSettings _downloadClientsSettings;
        private readonly IHttpClientFactory _httpClientFactory;

        public TvShowsDownloadClientController(
            IHttpClientFactory httpClientFactory,
            MoviesSettingsProvider moviesSettingsProvider,
            TvShowsSettingsProvider tvShowsSettingsProvider,
            MusicSettingsProvider musicSettingsProvider,
            DownloadClientsSettingsProvider downloadClientsSettingsProvider)
        {
            _moviesSettings = moviesSettingsProvider.Provide();
            _tvShowsSettings = tvShowsSettingsProvider.Provide();
            _musicSettings = musicSettingsProvider.Provide();
            _downloadClientsSettings = downloadClientsSettingsProvider.Provide();
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet()]
        public async Task<IActionResult> GetAsync()
        {
            List<string> otherCategories = new List<string>();
            switch (_moviesSettings.Client)
            {
                case "Radarr":
                    foreach (RadarrCategory category in _downloadClientsSettings.Radarr.Categories)
                    {
                        otherCategories.Add(category.Name.ToLower());
                    }
                    break;
                case "Overseerr":
                    foreach (RequestrrBot.DownloadClients.Overseerr.OverseerrMovieCategory category in _downloadClientsSettings.Overseerr.Movies.Categories)
                    {
                        otherCategories.Add(category.Name.ToLower());
                    }
                    if(otherCategories.Count == 0)
                        otherCategories.Add(Language.Current.DiscordCommandMovieRequestTitleName.ToLower());
                    break;
                case "Ombi":
                    otherCategories.Add(Language.Current.DiscordCommandMovieRequestTitleName.ToLower());
                    break;
            }

            switch (_musicSettings.Client)
            {
                case "Lidarr":
                    foreach (LidarrCategory category in _downloadClientsSettings.Lidarr.Categories)
                    {
                        otherCategories.Add(category.Name.ToLower());
                    }
                    break;
            }

            return Ok(new TvShowsSettingsModel
            {
                Client = _tvShowsSettings.Client,
                Sonarr = new SonarrSettingsModel
                {
                    Hostname = _downloadClientsSettings.Sonarr.Hostname,
                    BaseUrl = _downloadClientsSettings.Sonarr.BaseUrl,
                    Port = _downloadClientsSettings.Sonarr.Port,
                    ApiKey = _downloadClientsSettings.Sonarr.ApiKey,
                    Categories = _downloadClientsSettings.Sonarr.Categories.Select(x => new SonarrSettingsCategory
                    {
                        Id = x.Id,
                        Name = x.Name,
                        LanguageId = x.LanguageId,
                        ProfileId = x.ProfileId,
                        RootFolder = x.RootFolder,
                        Tags = x.Tags,
                        UseSeasonFolders = x.UseSeasonFolders,
                        SeriesType = x.SeriesType,
                    }).ToArray(),
                    UseSSL = _downloadClientsSettings.Sonarr.UseSSL,
                    SearchNewRequests = _downloadClientsSettings.Sonarr.SearchNewRequests,
                    MonitorNewRequests = _downloadClientsSettings.Sonarr.MonitorNewRequests,
                    AutoTagRequesters = _downloadClientsSettings.Sonarr.AutoTagRequesters,
                    Version = _downloadClientsSettings.Sonarr.Version
                },
                Ombi = new OmbiTVSettings
                {
                    Hostname = _downloadClientsSettings.Ombi.Hostname,
                    BaseUrl = _downloadClientsSettings.Ombi.BaseUrl,
                    Port = _downloadClientsSettings.Ombi.Port,
                    ApiKey = _downloadClientsSettings.Ombi.ApiKey,
                    ApiUsername = _downloadClientsSettings.Ombi.ApiUsername,
                    UseSSL = _downloadClientsSettings.Ombi.UseSSL,
                    Version = _downloadClientsSettings.Ombi.Version,
                    UseTVIssue = _downloadClientsSettings.Ombi.UseTVIssue
                },
                Overseerr = _downloadClientsSettings.Overseerr,
                Restrictions = _tvShowsSettings.Restrictions,
                OtherCategories = otherCategories.ToArray()
            });
        }

        [HttpPost("disable")]
        public async Task<IActionResult> SaveAsync()
        {
            _tvShowsSettings.Client = DownloadClient.Disabled;
            DownloadClientsSettingsRepository.SetDisabledClient(_tvShowsSettings);
            return Ok(new { ok = true });
        }
    }
}
