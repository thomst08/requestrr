using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Requestrr.WebApi.config;
using Requestrr.WebApi.Controllers.DownloadClients.Lidarr;
using Requestrr.WebApi.RequestrrBot.DownloadClients;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Radarr;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Sonarr;
using Requestrr.WebApi.RequestrrBot.Locale;
using Requestrr.WebApi.RequestrrBot.Movies;
using Requestrr.WebApi.RequestrrBot.Music;
using Requestrr.WebApi.RequestrrBot.TvShows;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Requestrr.WebApi.Controllers.DownloadClients
{
    [ApiController]
    [Authorize]
    [Route("/api/music")]
    public class MusicDownloadClientController : ControllerBase
    {
        private readonly MusicSettings _musicSettings;
        private readonly MoviesSettings _moviesSettings;
        private readonly TvShowsSettings _tvShowsSettings;
        private readonly DownloadClientsSettings _downloadClientsSettings;
        private readonly IHttpClientFactory _httpClientFactory;

        public MusicDownloadClientController(
            IHttpClientFactory httpClientFactory,
            MusicSettingsProvider musicSettingsProvider,
            MoviesSettingsProvider moviesSettingsProvider,
            TvShowsSettingsProvider tvShowsSettingsProvider,
            DownloadClientsSettingsProvider downloadClientsSettingsProvider )
        {
            _httpClientFactory = httpClientFactory;
            _musicSettings = musicSettingsProvider.Provide();
            _moviesSettings = moviesSettingsProvider.Provide();
            _tvShowsSettings = tvShowsSettingsProvider.Provide();
            _downloadClientsSettings = downloadClientsSettingsProvider.Provide();
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
                    if (otherCategories.Count == 0)
                        otherCategories.Add(Language.Current.DiscordCommandMovieRequestTitleName.ToLower());
                    break;
                case "Ombi":
                    otherCategories.Add(Language.Current.DiscordCommandMovieRequestTitleName.ToLower());
                    break;
            }

            switch (_tvShowsSettings.Client)
            {
                case "Sonarr":
                    foreach (SonarrCategory category in _downloadClientsSettings.Sonarr.Categories)
                    {
                        otherCategories.Add(category.Name.ToLower());
                    }
                    break;
                case "Overseerr":
                    foreach (RequestrrBot.DownloadClients.Overseerr.OverseerrTvShowCategory category in _downloadClientsSettings.Overseerr.TvShows.Categories)
                    {
                        otherCategories.Add(category.Name.ToLower());
                    }
                    if (otherCategories.Count == 0)
                        otherCategories.Add(Language.Current.DiscordCommandTvRequestTitleName.ToLower());
                    break;
                case "Ombi":
                    otherCategories.Add(Language.Current.DiscordCommandTvRequestTitleName.ToLower());
                    break;
            }

            return Ok(new MusicSettingsModel
            {
                Client = _musicSettings.Client,
                Restrictions = _musicSettings.Restrictions,
                Lidarr = new LidarrSettingsModel
                {
                    Hostname = _downloadClientsSettings.Lidarr.Hostname,
                    BaseUrl = _downloadClientsSettings.Lidarr.BaseUrl,
                    Port = _downloadClientsSettings.Lidarr.Port,
                    ApiKey = _downloadClientsSettings.Lidarr.ApiKey,
                    Categories = _downloadClientsSettings.Lidarr.Categories.Select(x => new LidarrSettingsCategory
                    {
                        Id = x.Id,
                        Name = x.Name,
                        ProfileId = x.ProfileId,
                        MetadataProfileId = x.MetadataProfileId,
                        RootFolder = x.RootFolder,
                        Tags = x.Tags,
                        ReleaseTypes = x.ReleaseTypes,
                        PrimaryTypes = x.PrimaryTypes,
                        SecondaryTypes = x.SecondaryTypes,
                        ReleaseStatuses = x.ReleaseStatuses
                    }).ToArray(),
                    UseSSL = _downloadClientsSettings.Lidarr.UseSSL,
                    SearchNewRequests = _downloadClientsSettings.Lidarr.SearchNewRequests,
                    MonitorNewRequests = _downloadClientsSettings.Lidarr.MonitorNewRequests,
                    AllowBulkAlbumRequests = _downloadClientsSettings.Lidarr.AllowBulkAlbumRequests,
                    Version = _downloadClientsSettings.Lidarr.Version
                },
                OtherCategories = otherCategories.ToArray()
            });
        }


        [HttpPost("disable")]
        public async Task<IActionResult> SaveAsync()
        {
            _musicSettings.Client = DownloadClient.Disabled;
            DownloadClientsSettingsRepository.SetDisabledClient(_musicSettings);
            return Ok(new { ok = true });
        }
    }
}
