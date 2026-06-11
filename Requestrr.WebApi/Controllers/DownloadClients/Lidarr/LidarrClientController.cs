using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Requestrr.WebApi.RequestrrBot.DownloadClients;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Lidarr;
using Requestrr.WebApi.RequestrrBot.Music;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Requestrr.WebApi.Controllers.DownloadClients.Lidarr
{
    [ApiController]
    [Authorize]
    [Route("/api/music/lidarr")]
    public class LidarrClientController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LidarrClient> _logger;

        public LidarrClientController(IHttpClientFactory httpClientFactory, ILogger<LidarrClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }



        /// <summary>
        /// Handles the testing of Lidarr settings, pass the settings into a Lidarr client
        /// </summary>
        /// <param name="model">Test settings for Lidarr</param>
        /// <returns>Returns if connection was successful</returns>
        [HttpPost("test")]
        public async Task<IActionResult> TestLidarrSettings([FromBody] TestLidarrSettingsModel model)
        {
            try
            {
                await LidarrClient.TestConnectionAsync(_httpClientFactory.CreateClient(), _logger, ConvertToLidarrSettings(model));
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        /// <summary>
        /// Handles the fetching of Root Paths from Lidarr
        /// </summary>
        /// <param name="model">Lidarr Settings the user is currently using</param>
        /// <returns>All paths returned by Lidarr</returns>
        [HttpPost("rootpath")]
        public async Task<IActionResult> GetLidarrRootPaths([FromBody] TestLidarrSettingsModel model)
        {
            try
            {
                IList<LidarrClient.JSONRootPath> paths = await LidarrClient.GetRootPaths(_httpClientFactory.CreateClient(), _logger, ConvertToLidarrSettings(model));
                return Ok(paths.Select(x => new LidarrPath
                {
                    Id = x.id,
                    Path = x.path
                }));
            }
            catch (Exception)
            {
                return BadRequest("Could not load the paths form Lidarr, check your settings.");
            }
        }



        /// <summary>
        /// Handles the fetching of Profiles from Lidarr
        /// </summary>
        /// <param name="model">Lidarr settings the user is currently using</param>
        /// <returns>All profiles returned by Lidarr</returns>
        [HttpPost("profile")]
        public async Task<IActionResult> GetLidarrProfiles([FromBody] TestLidarrSettingsModel model)
        {
            try
            {
                IList<LidarrClient.JSONProfile> profiles = await LidarrClient.GetProfiles(_httpClientFactory.CreateClient(), _logger, ConvertToLidarrSettings(model));
                return Ok(profiles.Select(x => new LidarrProfile
                {
                    Id = x.id,
                    Name = x.name
                }));
            }
            catch (Exception)
            {
                return BadRequest("Could not load profiles from Lidarr, check your settings.");
            }
        }



        /// <summary>
        /// Handles the fetching of Metadata profiles form Lidarr
        /// </summary>
        /// <param name="model">Lidarr setttings the user is currently using</param>
        /// <returns>All metadata profiles returned by Lidarr</returns>
        [HttpPost("metadataprofile")]
        public async Task<IActionResult> GetLidarrMetadatProfile([FromBody] TestLidarrSettingsModel model)
        {
            try
            {
                IList<LidarrClient.JSONProfile> metadataProfiles = await LidarrClient.GetMetadataProfiles(_httpClientFactory.CreateClient(), _logger, ConvertToLidarrSettings(model));
                return Ok(metadataProfiles.Select(x => new LidarrProfile
                {
                    Id = x.id,
                    Name = x.name
                }));
            }
            catch (Exception)
            {
                return BadRequest("Could not load metadata profiles from Lidarr, check your settings.");
            }
        }



        /// <summary>
        /// Handles the fetching of Tags from Lidarr
        /// </summary>
        /// <param name="model">Lidarr settings the user is currently using</param>
        /// <returns>All tags returned by Lidarr</returns>
        [HttpPost("tag")]
        public async Task<IActionResult> GetLidarrTags([FromBody] TestLidarrSettingsModel model)
        {
            try
            {
                IList<LidarrClient.JSONTag> tags = await LidarrClient.GetTags(_httpClientFactory.CreateClient(), _logger, ConvertToLidarrSettings(model));
                return Ok(tags.Select(x => new LidarrTag
                {
                    Id = x.id,
                    Name = x.label
                }));
            }
            catch (Exception)
            {
                return BadRequest("Could not load tags from Lidarr, check your settings.");
            }
        }



        /// <summary>
        /// Handles the saving of Lidarr settings
        /// </summary>
        /// <param name="model">Lidarr Settings</param>
        /// <returns></returns>
        [HttpPost()]
        public async Task<IActionResult> SaveAsync([FromBody] LidarrSettingsModel model)
        {
            MusicSettings musicSettings = new MusicSettings
            {
                Client = DownloadClient.Lidarr,
                Restrictions = string.IsNullOrWhiteSpace(model.Restrictions) ? MusicRestrictions.None : model.Restrictions
            };

            if (!model.Categories.Any())
                return BadRequest("At least one category is required.");

            if (model.Categories.Any(x => string.IsNullOrWhiteSpace(x.Name)))
                return BadRequest("A category name is required");

            foreach (var category in model.Categories)
            {
                category.Name = category.Name.Trim();
            }

            if (new HashSet<string>(model.Categories.Select(x => x.Name.ToLower())).Count != model.Categories.Length)
                return BadRequest("All categories must have different names.");

            if (new HashSet<int>(model.Categories.Select(x => x.Id)).Count != model.Categories.Length)
                return BadRequest("All categories must have different ids.");

            if (model.Categories.Any(x => !Regex.IsMatch(x.Name, @"^[\w-]{1,32}$")))
                return BadRequest("Invalid categorie names, make sure they only contain alphanumeric characters, dashes and underscores. (No spaces, etc)");

            IList<LidarrClient.JSONMetadataProfile> metadataProfiles;
            try
            {
                metadataProfiles = await LidarrClient.GetMetadataProfilesDetailed(
                    _httpClientFactory.CreateClient(),
                    _logger,
                    ConvertToLidarrSettings(model));
            }
            catch (Exception)
            {
                return BadRequest("Could not load metadata profile details from Lidarr, check your settings.");
            }

            try
            {
                LidarrMetadataProfileMapper.ApplyProfileFiltersToCategories(model.Categories, metadataProfiles);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            LidarrSettingsModel lidarrSettings = new LidarrSettingsModel
            {
                Hostname = model.Hostname.Trim(),
                ApiKey = model.ApiKey.Trim(),
                BaseUrl = model.BaseUrl.Trim(),
                Port = model.Port,
                Categories = model.Categories,
                SearchNewRequests = model.SearchNewRequests,
                MonitorNewRequests = model.MonitorNewRequests,
                AllowBulkAlbumRequests = model.AllowBulkAlbumRequests,
                UseSSL = model.UseSSL,
                Version = model.Version
            };

            DownloadClientsSettingsRepository.SetLidarr(musicSettings, lidarrSettings);
            return Ok(new { ok = true });
        }



        /// <summary>
        /// Converts the Lidarr test settings into LidarrSettings object
        /// </summary>
        /// <param name="model">Lidarr test settings</param>
        /// <returns>Returns a Lidarr Settings object</returns>
        private static LidarrSettings ConvertToLidarrSettings(TestLidarrSettingsModel model)
        {
            return new LidarrSettings
            {
                ApiKey = model.ApiKey.Trim(),
                Hostname = model.Hostname.Trim(),
                BaseUrl = model.BaseUrl.Trim(),
                Port = model.Port,
                UseSSL = model.UseSSL,
                Version = model.Version
            };
        }
    }
}
