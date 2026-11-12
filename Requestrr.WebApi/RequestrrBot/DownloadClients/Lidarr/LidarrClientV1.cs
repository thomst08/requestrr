using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Requestrr.WebApi.Extensions;
using Requestrr.WebApi.RequestrrBot.Music;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static Requestrr.WebApi.RequestrrBot.DownloadClients.Lidarr.LidarrClient;

namespace Requestrr.WebApi.RequestrrBot.DownloadClients.Lidarr
{
    public class LidarrClientV1 : IMusicSearcher, IMusicRequester
    {
        private IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LidarrClient> _logger;
        private LidarrSettingsProvider _lidarrSettingProvider;
        private LidarrSettings _lidarrSettings => _lidarrSettingProvider.Provider();

        private string BaseURL => GetBaseURL(_lidarrSettings);


        public LidarrClientV1(IHttpClientFactory httpClientFactory, ILogger<LidarrClient> logger, LidarrSettingsProvider lidarrSettingsProvider)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _lidarrSettingProvider = lidarrSettingsProvider;
        }



        /// <summary>
        /// Used to test if Lidarr service can be found
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task TestConnectionAsync(HttpClient httpClient, ILogger<LidarrClient> logger, LidarrSettings settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.BaseUrl) && !settings.BaseUrl.StartsWith("/"))
            {
                throw new Exception("Invalid base URL, must start with /");
            }

            var testSuccessful = false;

            try
            {
                var response = await HttpGetAsync(httpClient, settings, $"{GetBaseURL(settings)}/config/host");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new Exception("Invalid api key");
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new Exception("Incorrect api version");
                }

                try
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JObject.Parse(responseString);

                    if (!jsonResponse.urlBase.ToString().Equals(settings.BaseUrl, StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new Exception("Base url does not match what is set in Lidarr");
                    }
                }
                catch
                {
                    throw new Exception("Base url does not match what is set in Lidarr");
                }

                testSuccessful = true;
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "Error while testing Lidarr connection: " + ex.Message);
                throw new Exception("Invalid host and/or port");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error while testing Lidarr connection: " + ex.Message);

                if (ex.GetType() == typeof(Exception))
                {
                    throw;
                }
                else
                {
                    throw new Exception("Invalid host and/or port");
                }
            }

            if (!testSuccessful)
            {
                throw new Exception("Invalid host and/or port");
            }
        }


        public static async Task<IList<JSONRootPath>> GetRootPaths(HttpClient httpClient, ILogger<LidarrClient> logger, LidarrSettings settings)
        {
            try
            {
                HttpResponseMessage response = await HttpGetAsync(httpClient, settings, $"{GetBaseURL(settings)}/rootfolder");
                string jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IList<JSONRootPath>>(jsonResponse);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "An error while getting Lidarr root paths: " + ex.Message);
            }

            throw new Exception("An error occurred while getting Lidarr root paths");
        }


        /// <summary>
        /// Fetches profile information from Lidarr
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<IList<JSONProfile>> GetProfiles(HttpClient httpClient, ILogger<LidarrClient> logger, LidarrSettings settings)
        {
            try
            {
                HttpResponseMessage response = await HttpGetAsync(httpClient, settings, $"{GetBaseURL(settings)}/qualityprofile");
                string jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IList<JSONProfile>>(jsonResponse);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "An error while getting Lidarr profiles: " + ex.Message);
            }

            throw new Exception("An error occurred while getting Lidarr profiles");
        }



        /// <summary>
        /// Fetches metadata profile information from Lidarr
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<IList<JSONProfile>> GetMetadataProfiles(HttpClient httpClient, ILogger<LidarrClient> logger, LidarrSettings settings)
        {
            try
            {
                HttpResponseMessage response = await HttpGetAsync(httpClient, settings, $"{GetBaseURL(settings)}/metadataprofile");
                string jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IList<JSONProfile>>(jsonResponse);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "An error while getting Lidarr metadata profiles: " + ex.Message);
            }

            throw new Exception("An error occurred while getting Lidarr metadata profiles");
        }



        public static async Task<IList<JSONTag>> GetTags(HttpClient httpClient, ILogger<LidarrClient> logger, LidarrSettings settings)
        {
            try
            {
                HttpResponseMessage response = await HttpGetAsync(httpClient, settings, $"{GetBaseURL(settings)}/tag");
                string jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IList<JSONTag>>(jsonResponse);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "An error while getting Lidarr tags: " + ex.Message);
            }

            throw new Exception("An error occurred while getting Lidarr tags");
        }


        /// <summary>
        /// Handle 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private Task<HttpResponseMessage> HttpGetAsync(string url)
        {
            return HttpGetAsync(_httpClientFactory.CreateClient(), _lidarrSettings, url);
        }


        /// <summary>
        /// Makes a connection to Lidarr and returns a response from API
        /// </summary>
        /// <param name="client"></param>
        /// <param name="settings"></param>
        /// <param name="url">Full URL to the API</param>
        /// <returns>Returns the HttpReponseMessage from the API</returns>
        private static async Task<HttpResponseMessage> HttpGetAsync(HttpClient client, LidarrSettings settings, string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("X-Api-Key", settings.ApiKey);

            using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)))
            {
                return await client.SendAsync(request, cts.Token);
            }
        }


        /// <summary>
        /// Gets Base URL for Lidarr server
        /// </summary>
        /// <param name="settings">Lidarr Settings</param>
        /// <returns>Returns a string of the URL</returns>
        private static string GetBaseURL(LidarrSettings settings)
        {
            var protocol = settings.UseSSL ? "https" : "http";

            return $"{protocol}://{settings.Hostname}:{settings.Port}{settings.BaseUrl}/api/v{settings.Version}";
        }



        /// <summary>
        /// Handles the fetching of a single query based on Music DB Id
        /// </summary>
        /// <param name="artistId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<MusicArtist> SearchMusicForArtistIdAsync(MusicRequest request, string artistId)
        {
            try
            {
                JSONMusicArtist foundArtistJson = await FindExistingArtistByMusicDbIdAsync(artistId);

                if (foundArtistJson == null)
                {
                    HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/artist/lookup?term=lidarr:{artistId}");
                    await response.ThrowIfNotSuccessfulAsync("LidarrMusicLookup failed", x => x.error);

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    foundArtistJson = JsonConvert.DeserializeObject<List<JSONMusicArtist>>(jsonResponse).First();
                }

                return foundArtistJson != null ? ConvertToMusic(foundArtistJson) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while searching for music by Id \"{artistId}\" with Lidarr: {ex.Message}");
            }

            throw new Exception("An error occurred while searching for music by Id with Lidarr");
        }



        /// <summary>
        /// Handles the fetching of a 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<IReadOnlyList<MusicArtist>> SearchMusicForArtistAsync(MusicRequest request, string artistName)
        {
            try
            {
                string searchTerm = Uri.EscapeDataString(artistName.ToLower().Trim());
                HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/artist/lookup?term={searchTerm}");
                await response.ThrowIfNotSuccessfulAsync("LidarrMusicArtistLookup failed", x => x.error);

                string jsonResponse = await response.Content.ReadAsStringAsync();
                List<JSONMusicArtist> jsonMusic = JsonConvert.DeserializeObject<List<JSONMusicArtist>>(jsonResponse);

                //TODO: Correct this, searching should handle both artist and albums
                return jsonMusic.Where(x => x != null).Select(x => ConvertToMusic(x)).ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while searching for music artist with Lidarr: " + ex.Message);
            }

            throw new Exception("An error occurred while searching for music artist with Lidarr");
        }



        private async Task<JSONMusicArtist> FindExistingArtistByMusicDbIdAsync(string artistId)
        {
            try
            {
                HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/artist?mbId={artistId}");
                await response.ThrowIfNotSuccessfulAsync("Could not search artist by Id", x => x.error);

                string jsonResponse = await response.Content.ReadAsStringAsync();
                JSONMusicArtist[] jsonMusicArtists = JsonConvert.DeserializeObject<List<JSONMusicArtist>>(jsonResponse).ToArray();

                if (jsonMusicArtists.Any())
                    return jsonMusicArtists.First();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred finding existing music artist by Id \"{artistId}\" with Lidarr: {ex.Message}");
            }

            return null;
        }



        public async Task<Dictionary<string, MusicArtist>> SearchAvailableMusicArtistAsync(HashSet<string> artistIds, CancellationToken token)
        {
            try
            {
                List<MusicArtist> convertedMusicArtists = new List<MusicArtist>();

                foreach (string artistId in artistIds)
                {
                    JSONMusicArtist existingMusic = await FindExistingArtistByMusicDbIdAsync(artistId);
                    if (existingMusic != null)
                        convertedMusicArtists.Add(ConvertToMusic(existingMusic));
                }

                return convertedMusicArtists.Where(x => x.Available).ToDictionary(x => x.ArtistId, x => x);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while searching available music artist with Lidarr: " + ex.Message);
            }

            throw new Exception("An error occurred while searching available music artist with Lidarr");
        }




        public async Task<MusicRequestResult> RequestMusicAsync(MusicRequest request, MusicArtist music)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(music.DownloadClientId))
                    await CreateMusicInLidarr(request, music);
                else
                    await UpdateExistingMusic(request, music);

                return new MusicRequestResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error while requesting music \"{music.ArtistName}\" from Lidarr: " + ex.Message);
            }

            throw new Exception("An error occurred while requesting a music from Lidarr");
        }



        private async Task CreateMusicInLidarr(MusicRequest request, MusicArtist music)
        {
            LidarrCategory category = null;

            try
            {
                category = _lidarrSettings.Categories.SingleOrDefault(x => x.Id == request.CategoryId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occured while requesting music \"{music.ArtistName}\" from Lidarr, could not find category with id {request.CategoryId}");
                throw new Exception($"An error occurred while requesting music \"{music.ArtistName}\" from Lidarr, could not find category with id {request.CategoryId}");
            }

            MusicArtist jsonMusic = await SearchMusicForArtistIdAsync(request, music.ArtistId);
            HttpResponseMessage response = await HttpPostAsync($"{BaseURL}/artist", JsonConvert.SerializeObject(new
            {
                foreignArtistId = jsonMusic.ArtistId,
                artistName = jsonMusic.ArtistName,
                mbId = jsonMusic.ArtistId,
                qualityProfileId = category.ProfileId,
                metadataProfileId = category.MetadataProfileId,
                monitored = _lidarrSettings.MonitorNewRequests,
                tags = JToken.FromObject(category.Tags),
                rootFolderPath = category.RootFolder,
                addOptions = new
                {
                    // monitored = _lidarrSettings.MonitorNewRequests,
                    searchForMissingAlbums = _lidarrSettings.SearchNewRequests
                }
            }));

            await response.ThrowIfNotSuccessfulAsync("LidarrMusicCreation failed", x => x.error);
        }


        private async Task UpdateExistingMusic(MusicRequest request, MusicArtist music)
        {
            LidarrCategory category = null;
            int lidarrMusicId = int.Parse(music.DownloadClientId);
            HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/artist/{lidarrMusicId}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    await CreateMusicInLidarr(request, music);
                    return;
                }

                await response.ThrowIfNotSuccessfulAsync("LidarrGetMusic failed", x => x.error);
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            dynamic lidarrMusic = JObject.Parse(jsonResponse);

            try
            {
                category = _lidarrSettings.Categories.Single(x => x.Id == request.CategoryId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while requesting music \"{music.ArtistName}\" from Lidarr, cound not find category with id {request.CategoryId}");
                throw new Exception($"An error occurred while requesting music \"{music.ArtistName}\" from Lidarr, could not find category with id {request.CategoryId}");
            }

            lidarrMusic.tags = JToken.FromObject(category.Tags);
            lidarrMusic.monitored = _lidarrSettings.MonitorNewRequests;

            response = await HttpPutAsync($"{BaseURL}/artist/{lidarrMusicId}", JsonConvert.SerializeObject(lidarrMusic));
            await response.ThrowIfNotSuccessfulAsync("LidarrUpdateMusic failed", x => x.error);

            if (_lidarrSettings.SearchNewRequests)
            {
                try
                {
                    response = await HttpPostAsync($"{BaseURL}/command", JsonConvert.SerializeObject(new
                    {
                        name = "musicSearch",
                        musicIds = new[] { lidarrMusicId }
                    }));

                    await response.ThrowIfNotSuccessfulAsync("LidarrMusicSearchCommand failed", x => x.error);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error while sending search command for music \"{music.ArtistName}\" to Lidarr: " + ex.Message);
                    throw;
                }
            }
        }



        private async Task<HttpResponseMessage> HttpPostAsync(string url, string content)
        {
            StringContent postRequest = new StringContent(content);
            postRequest.Headers.Clear();
            postRequest.Headers.Add("Content-Type", "application/json");
            postRequest.Headers.Add("X-Api-Key", _lidarrSettings.ApiKey);

            HttpClient client = _httpClientFactory.CreateClient();
            return await client.PostAsync(url, postRequest);
        }


        private async Task<HttpResponseMessage> HttpPutAsync(string url, string content)
        {
            StringContent postRequest = new StringContent(content);
            postRequest.Headers.Clear();
            postRequest.Headers.Add("Content-Type", "application/json");
            postRequest.Headers.Add("X-Api-Key", _lidarrSettings.ApiKey);

            HttpClient client = _httpClientFactory.CreateClient();
            return await client.PutAsync(url, postRequest);
        }



        private MusicArtist ConvertToMusic(JSONMusicArtist jsonArtist)
        {
            string downloadClientId = jsonArtist.Id.ToString();

            return new MusicArtist
            {
                DownloadClientId = downloadClientId,
                ArtistId = jsonArtist.ForeignArtistId.ToString(),
                ArtistName = jsonArtist.ArtistName,
                Overview = jsonArtist.Overview,

                Available = (jsonArtist.Statistics?.SizeOnDisk ?? -1) > 0,
                Monitored = jsonArtist.Monitored,
                Quality = string.Empty,
                Requested = !string.IsNullOrWhiteSpace(downloadClientId),

                PlexUrl = string.Empty,
                EmbyUrl = string.Empty,
                PosterPath = GetPosterImageUrl(jsonArtist.Images)
            };
        }


        private string GetPosterImageUrl(List<JSONImage> images)
        {
            JSONImage posterImage = images.Where(x => x.CoverType.Equals("poster", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (posterImage != null)
            {
                if (!string.IsNullOrWhiteSpace(posterImage.RemoteUrl))
                    return posterImage.RemoteUrl;

                return posterImage.Url;
            }
            return string.Empty;
        }



        public class JSONLink
        {
            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }

        public class JSONImage
        {
            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("coverType")]
            public string CoverType { get; set; }

            [JsonProperty("extension")]
            public string Extension { get; set; }

            [JsonProperty("remoteUrl")]
            public string RemoteUrl { get; set; }
        }

        public class JSONRating
        {
            [JsonProperty("votes")]
            public int Votes { get; set; }

            [JsonProperty("value")]
            public float Value { get; set; }
        }

        public class JSONStatistics
        {
            [JsonProperty("albumCount")]
            public int AlbumCount { get; set; }

            [JsonProperty("trackFileCount")]
            public int TrackFileCount { get; set; }

            [JsonProperty("trackCount")]
            public int TrackCount { get; set; }

            [JsonProperty("totalTrackCount")]
            public int TotalTrackCount { get; set; }

            [JsonProperty("sizeOnDisk")]
            public double SizeOnDisk { get; set; }

            [JsonProperty("percentOfTracks")]
            public double PercentOfTracks { get; set; }
        }

        public class JSONMedia
        {
            [JsonProperty("mediumNumber")]
            public int MediumNumber { get; set; }

            [JsonProperty("mediumName")]
            public string mediumName { get; set; }

            [JsonProperty("mediumFormat")]
            public string MediumFormat { get; set; }
        }

        public class JSONReleases
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("albumId")]
            public int AlbumId { get; set; }

            [JsonProperty("foreignReleaseId")]
            public string ForeignReleaseId { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("duration")]
            public int Duration { get; set; }

            [JsonProperty("trackCount")]
            public int TrackCount { get; set;  }

            [JsonProperty("media")]
            public List<JSONMedia> Media { get; set; }

            [JsonProperty("mediumCount")]
            public int MediumCount { get; set; }

            [JsonProperty("disambiguation")]
            public string Disambiguation { get; set; }

            [JsonProperty("country")]
            public List<string> Country { get; set; }

            [JsonProperty("label")]
            public List<string> Label { get; set; }

            [JsonProperty("format")]
            public string Format { get; set; }

            [JsonProperty("monitored")]
            public bool Monitored { get; set;  }

        }


        private class JSONMusicArtist
        {
            [JsonProperty("id")]
            public int? Id { get; set; }

            [JsonProperty("artistMetadataId")]
            public int? ArtistMetadataId { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("ended")]
            public bool Ended { get; set; }

            [JsonProperty("artistName")]
            public string ArtistName { get; set; }

            [JsonProperty("foreignArtistId")]
            public Guid ForeignArtistId { get; set; }

            [JsonProperty("tadbId")]
            public int TadbId { get; set; }

            [JsonProperty("discogsId")]
            public int DiscogsId { get; set; }

            [JsonProperty("overview")]
            public string Overview { get; set; }

            [JsonProperty("artistType")]
            public string ArtistType { get; set; }

            [JsonProperty("disambiguation")]
            public string Disambiguation { get; set; }

            [JsonProperty("links")]
            public List<JSONLink> Links { get; set; }

            [JsonProperty("images")]
            public List<JSONImage> Images { get; set; }

            [JsonProperty("path")]
            public string Path { get; set; } = null;

            [JsonProperty("qualityProfileId")]
            public int QualityProfileId { get; set; }

            [JsonProperty("metadataProfileId")]
            public int MetadataProfileId { get; set; }

            [JsonProperty("monitored")]
            public bool Monitored { get; set; }

            [JsonProperty("monitorNewItems")]
            public string MonitorNewItems { get; set; }

            [JsonProperty("folder")]
            public string Folder { get; set; }

            [JsonProperty("genres")]
            public List<string> Genres { get; set; }

            [JsonProperty("tags")]
            public List<int> Tags { get; set; }

            [JsonProperty("added")]
            public DateTime Added { get; set; }

            [JsonProperty("ratings")]
            public JSONRating Ratings { get; set; }

            [JsonProperty("statistics")]
            public JSONStatistics Statistics { get; set; }
        }


        private class JSONMusicAlbum
        {
            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("disambiguation")]
            public string Disambiguation { get; set; }

            [JsonProperty("overview")]
            public string Overview { get; set; }

            [JsonProperty("artistId")]
            public int ArtistId { get; set; }

            [JsonProperty("foreignAlbumId")]
            public Guid ForeignAlbumId { get; set; }

            [JsonProperty("monitored")]
            public bool Monitored { get; set; }

            [JsonProperty("anyReleaseOk")]
            public bool AnyReleaseOk { get; set; }

            [JsonProperty("profileId")]
            public int ProfileId { get; set; }

            [JsonProperty("duration")]
            public int Duration { get; set; }

            [JsonProperty("albumType")]
            public string AlbumType { get; set; }

            [JsonProperty("secondaryTypes")]
            public List<string> SecondaryTypes { get; set; }

            [JsonProperty("mediumCount")]
            public int MediumCount { get; set; }

            [JsonProperty("ratings")]
            public JSONRating Ratings { get; set; }


            [JsonProperty("releaseDate")]
            public DateTime ReleaseDate { get; set; }

            [JsonProperty("releases")]
            public List<JSONReleases> Releases { get; set; }

            [JsonProperty("genres")]
            public List<string> Genres { get; set; }

            [JsonProperty("media")]
            public List<JSONMedia> Media { get; set; }

            [JsonProperty("artist")]
            public JSONMusicArtist Artist { get; set; }

            [JsonProperty("images")]
            public List<JSONImage> Images { get; set; }

            [JsonProperty("links")]
            public List<JSONLink> Links { get; set; }

            [JsonProperty("remoteCover")]
            public string RemoteCover { get; set; }

            [JsonProperty("grabbed")]
            public bool Grabbed { get; set; }
        }
    }
}
