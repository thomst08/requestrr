using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Requestrr.WebApi.Extensions;
using Requestrr.WebApi.RequestrrBot.Music;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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

        private static readonly SemaphoreSlim _musicBrainzSemaphore = new SemaphoreSlim(1, 1);
        private static DateTime _lastMusicBrainzRequestUtc = DateTime.MinValue;

        private static readonly ConcurrentDictionary<string, (IReadOnlyList<MusicAlbum> albums, DateTime expiry)> _musicBrainzAlbumCache
            = new ConcurrentDictionary<string, (IReadOnlyList<MusicAlbum>, DateTime)>();
        private static readonly TimeSpan _musicBrainzCacheTtl = TimeSpan.FromMinutes(10);


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

        public static async Task<IList<JSONMetadataProfile>> GetMetadataProfilesDetailed(HttpClient httpClient, ILogger<LidarrClient> logger, LidarrSettings settings)
        {
            try
            {
                HttpResponseMessage response = await HttpGetAsync(httpClient, settings, $"{GetBaseURL(settings)}/metadataprofile");
                string jsonResponse = await response.Content.ReadAsStringAsync();
                JArray profiles = JArray.Parse(jsonResponse);

                return profiles.Select(x => new JSONMetadataProfile
                {
                    id = x["id"]?.Value<int>() ?? 0,
                    name = x["name"]?.Value<string>() ?? string.Empty,
                    primaryTypes = GetMetadataPrimaryTypes(x),
                    secondaryTypes = GetMetadataSecondaryTypes(x)
                        .Select(NormalizeSecondaryTypeValue)
                        .Where(v => v != null)
                        .Distinct(StringComparer.InvariantCultureIgnoreCase)
                        .ToArray(),
                    releaseStatuses = GetMetadataReleaseStatuses(x)
                        .Select(NormalizeReleaseStatusValue)
                        .Where(v => v != null)
                        .Distinct(StringComparer.InvariantCultureIgnoreCase)
                        .ToArray()
                }).ToArray();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "An error while getting detailed Lidarr metadata profiles: " + ex.Message);
            }

            throw new Exception("An error occurred while getting detailed Lidarr metadata profiles");
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
            string searchTerm = Uri.EscapeDataString(artistName.ToLower().Trim());
            try
            {
                HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/artist/lookup?term={searchTerm}");
                await response.ThrowIfNotSuccessfulAsync("LidarrMusicArtistLookup failed", x => x.error);

                string jsonResponse = await response.Content.ReadAsStringAsync();
                List<JSONMusicArtist> jsonMusic = JsonConvert.DeserializeObject<List<JSONMusicArtist>>(jsonResponse);

                //TODO: Correct this, searching should handle both artist and albums
                return jsonMusic.Where(x => x != null).Select(x => ConvertToMusic(x)).ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lidarr artist lookup failed, attempting local artist fallback search: " + ex.Message);
            }

            try
            {
                return await SearchExistingArtistsByTermAsync(searchTerm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while searching for music artist with Lidarr: " + ex.Message);
            }

            throw new Exception("An error occurred while searching for music artist with Lidarr");
        }

        private async Task<IReadOnlyList<MusicArtist>> SearchExistingArtistsByTermAsync(string searchTerm)
        {
            HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/artist?term={searchTerm}");
            await response.ThrowIfNotSuccessfulAsync("LidarrMusicArtistLocalLookup failed", x => x.error);

            string jsonResponse = await response.Content.ReadAsStringAsync();
            List<JSONMusicArtist> artists = JsonConvert.DeserializeObject<List<JSONMusicArtist>>(jsonResponse) ?? new List<JSONMusicArtist>();

            return artists
                .Where(x => x != null)
                .Select(ConvertToMusic)
                .ToArray();
        }

        public async Task<IReadOnlyList<MusicAlbum>> SearchMusicAlbumsForArtistAsync(MusicRequest request, MusicArtist artist)
        {
            try
            {
                if (artist == null)
                    return Array.Empty<MusicAlbum>();

                if (string.IsNullOrWhiteSpace(artist.DownloadClientId))
                {
                    JSONMusicArtist existingArtist = await FindExistingArtistByMusicDbIdAsync(artist.ArtistId);
                    if (existingArtist != null)
                    {
                        if (!existingArtist.Id.HasValue)
                            return Array.Empty<MusicAlbum>();

                        artist.DownloadClientId = existingArtist.Id.Value.ToString();
                        MusicArtist refreshedArtist = await SearchMusicForArtistIdAsync(request, artist.ArtistId);
                        if (refreshedArtist != null)
                            artist = refreshedArtist;
                    }
                }

                List<JSONMusicAlbum> jsonAlbums = new List<JSONMusicAlbum>();

                if (!string.IsNullOrWhiteSpace(artist?.DownloadClientId))
                {
                    HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/album?artistId={artist.DownloadClientId}");
                    await response.ThrowIfNotSuccessfulAsync("LidarrAlbumLookup failed", x => x.error);

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    jsonAlbums = JsonConvert.DeserializeObject<List<JSONMusicAlbum>>(jsonResponse);

                    ReleaseFilters filters = GetReleaseFilters(request.CategoryId);
                    var filteredAlbums = jsonAlbums
                        .Where(x => x != null && IsRequestedReleaseType(x, filters))
                        .Select(x => ConvertToAlbum(x, artist))
                        .OrderByDescending(x => x.ReleaseDate ?? DateTime.MinValue)
                        .ToArray();

                    _logger.LogWarning(
                        "Lidarr albums for artist \"{ArtistName}\" ({ArtistId}): total={TotalAlbums}, filtered={FilteredAlbums}, primary={PrimaryTypes}, secondary={SecondaryTypes}, status={ReleaseStatuses}",
                        artist.ArtistName,
                        artist.ArtistId,
                        jsonAlbums?.Count ?? 0,
                        filteredAlbums.Length,
                        string.Join(", ", filters.PrimaryTypes.OrderBy(x => x)),
                        string.Join(", ", filters.SecondaryTypes.OrderBy(x => x)),
                        string.Join(", ", filters.ReleaseStatuses.OrderBy(x => x)));

                    return filteredAlbums;
                }

                return await SearchMusicBrainzAlbumsForArtistAsync(artist, request.CategoryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while searching for music albums with Lidarr: " + ex.Message);
            }

            throw new Exception("An error occurred while searching for music albums with Lidarr");
        }



        private async Task<IReadOnlyList<MusicAlbum>> SearchMusicBrainzAlbumsForArtistAsync(MusicArtist artist, int categoryId)
        {
            if (artist == null || string.IsNullOrWhiteSpace(artist.ArtistId))
                return Array.Empty<MusicAlbum>();

            string cacheKey = $"{artist.ArtistId}_{categoryId}";

            if (_musicBrainzAlbumCache.TryGetValue(cacheKey, out var cached) && cached.expiry > DateTime.UtcNow)
                return cached.albums;

            ReleaseFilters filters = GetReleaseFilters(categoryId);

            try
            {
                string url = $"https://musicbrainz.org/ws/2/release-group?artist={artist.ArtistId}&fmt=json&inc=artist-credits&limit=100&offset=0";
                MusicBrainzReleaseGroupResponse firstPage = await FetchMusicBrainzReleaseGroupsAsync(url);

                if (firstPage?.ReleaseGroups == null)
                    return Array.Empty<MusicAlbum>();

                IReadOnlyList<MusicAlbum> albums = firstPage.ReleaseGroups
                    .Where(x => IsRequestedReleaseType(x, artist, filters))
                    .Select(x => ConvertToAlbum(x, artist))
                    .OrderByDescending(x => x.ReleaseDate ?? DateTime.MinValue)
                    .ToArray();

                DateTime expiry = DateTime.UtcNow.Add(_musicBrainzCacheTtl);
                _musicBrainzAlbumCache[cacheKey] = (albums, expiry);

                if (firstPage.TotalCount > 100)
                    _ = Task.Run(() => FetchRemainingMusicBrainzPagesAsync(artist, filters, cacheKey, 100, firstPage.TotalCount, expiry));

                return albums;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while searching MusicBrainz albums for artist \"{artist.ArtistId}\": {ex.Message}");
            }

            return Array.Empty<MusicAlbum>();
        }

        private async Task FetchRemainingMusicBrainzPagesAsync(MusicArtist artist, ReleaseFilters filters, string cacheKey, int startOffset, int totalMbCount, DateTime expectedExpiry)
        {
            const int pageSize = 100;
            int offset = startOffset;

            while (offset < totalMbCount)
            {
                if (!_musicBrainzAlbumCache.TryGetValue(cacheKey, out var current) || current.expiry != expectedExpiry)
                    break;

                try
                {
                    string url = $"https://musicbrainz.org/ws/2/release-group?artist={artist.ArtistId}&fmt=json&inc=artist-credits&limit={pageSize}&offset={offset}";
                    MusicBrainzReleaseGroupResponse response = await FetchMusicBrainzReleaseGroupsAsync(url);

                    if (response?.ReleaseGroups == null || response.ReleaseGroups.Count == 0)
                        break;

                    IReadOnlyList<MusicAlbum> newAlbums = response.ReleaseGroups
                        .Where(x => IsRequestedReleaseType(x, artist, filters))
                        .Select(x => ConvertToAlbum(x, artist))
                        .ToArray();

                    if (_musicBrainzAlbumCache.TryGetValue(cacheKey, out current) && current.expiry == expectedExpiry)
                    {
                        IReadOnlyList<MusicAlbum> updatedAlbums = current.albums
                            .Concat(newAlbums)
                            .OrderByDescending(x => x.ReleaseDate ?? DateTime.MinValue)
                            .ToArray();

                        _musicBrainzAlbumCache[cacheKey] = (updatedAlbums, expectedExpiry);
                    }

                    if (response.ReleaseGroups.Count < pageSize)
                        break;

                    offset += pageSize;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Background MusicBrainz fetch failed at offset {offset} for artist \"{artist.ArtistId}\": {ex.Message}");
                    break;
                }
            }
        }

        private async Task<MusicBrainzReleaseGroupResponse> FetchMusicBrainzReleaseGroupsAsync(string url)
        {
            const int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                await _musicBrainzSemaphore.WaitAsync();
                try
                {
                    TimeSpan elapsed = DateTime.UtcNow - _lastMusicBrainzRequestUtc;
                    if (elapsed < TimeSpan.FromSeconds(1))
                        await Task.Delay(TimeSpan.FromSeconds(1) - elapsed);

                    _lastMusicBrainzRequestUtc = DateTime.UtcNow;

                    HttpClient client = _httpClientFactory.CreateClient();
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Add("Accept", "application/json");
                    request.Headers.UserAgent.ParseAdd("Requestrr/2.1.9 (github.com/darkalfx/requestrr)");

                    using var response = await client.SendAsync(request);

                    if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        int backoffSeconds = (int)Math.Pow(2, attempt);
                        _logger.LogWarning($"MusicBrainz rate limited (attempt {attempt}/{maxAttempts}), backing off {backoffSeconds}s before retry.");
                        await Task.Delay(TimeSpan.FromSeconds(backoffSeconds));
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning($"MusicBrainz lookup failed with status {(int)response.StatusCode} ({response.ReasonPhrase}).");
                        return null;
                    }

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<MusicBrainzReleaseGroupResponse>(jsonResponse);
                }
                finally
                {
                    _musicBrainzSemaphore.Release();
                }
            }

            _logger.LogWarning("MusicBrainz lookup failed after all retry attempts.");
            return null;
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

        private async Task<JSONMusicAlbum> FindExistingAlbumByMusicDbIdAsync(string albumId, string artistDownloadClientId)
        {
            if (string.IsNullOrWhiteSpace(albumId) || string.IsNullOrWhiteSpace(artistDownloadClientId))
                return null;

            try
            {
                HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/album?artistId={artistDownloadClientId}");
                await response.ThrowIfNotSuccessfulAsync("Could not search album by Id", x => x.error);

                string jsonResponse = await response.Content.ReadAsStringAsync();
                List<JSONMusicAlbum> jsonAlbums = JsonConvert.DeserializeObject<List<JSONMusicAlbum>>(jsonResponse);

                return jsonAlbums?.FirstOrDefault(x => x.ForeignAlbumId.ToString().Equals(albumId, StringComparison.InvariantCultureIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred finding existing album by Id \"{albumId}\" with Lidarr: {ex.Message}");
            }

            return null;
        }

        private async Task<JSONMusicAlbum> FindAlbumByForeignIdAsync(string albumId)
        {
            if (string.IsNullOrWhiteSpace(albumId))
                return null;

            try
            {
                string searchTerm = Uri.EscapeDataString($"lidarr:{albumId}");
                HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/album/lookup?term={searchTerm}");
                await response.ThrowIfNotSuccessfulAsync("LidarrAlbumLookup failed", x => x.error);

                string jsonResponse = await response.Content.ReadAsStringAsync();
                List<JSONMusicAlbum> jsonAlbums = JsonConvert.DeserializeObject<List<JSONMusicAlbum>>(jsonResponse);

                return jsonAlbums?.FirstOrDefault(x => x.ForeignAlbumId.ToString().Equals(albumId, StringComparison.InvariantCultureIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred finding album by foreign Id \"{albumId}\" with Lidarr: {ex.Message}");
            }

            return null;
        }

        private async Task<JSONMusicAlbum> GetAlbumByIdAsync(int albumId)
        {
            try
            {
                HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/album/{albumId}");
                await response.ThrowIfNotSuccessfulAsync("LidarrGetAlbum failed", x => x.error);

                string jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<JSONMusicAlbum>(jsonResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while fetching album by id \"{albumId}\" with Lidarr: {ex.Message}");
            }

            return null;
        }

        private bool IsRequestedReleaseType(JSONMusicAlbum album, ReleaseFilters filters)
        {
            if (album == null)
                return false;

            if (!IsAllowedByMetadataProfileRules(album.AlbumType, album.SecondaryTypes, filters))
                return false;

            if (!HasAllowedReleaseStatus(album.Releases, filters))
                return false;

            string releaseType = DetermineReleaseType(album.AlbumType, album.SecondaryTypes);
            if (releaseType == null)
                return false;

            if (!filters.PrimaryTypes.Any())
                return true;

            return filters.PrimaryTypes.Contains(releaseType);
        }

        private bool IsRequestedReleaseType(MusicBrainzReleaseGroup releaseGroup, MusicArtist requestedArtist, ReleaseFilters filters)
        {
            if (releaseGroup == null)
                return false;

            if (!IsPrimaryArtistMatch(releaseGroup, requestedArtist))
                return false;

            if (!IsAllowedByMetadataProfileRules(releaseGroup.PrimaryType, releaseGroup.SecondaryTypes, filters))
                return false;

            string releaseType = DetermineReleaseType(releaseGroup.PrimaryType, releaseGroup.SecondaryTypes);
            if (releaseType == null)
                return false;

            if (!filters.PrimaryTypes.Any())
                return true;

            return filters.PrimaryTypes.Contains(releaseType);
        }

        private static bool IsPrimaryArtistMatch(MusicBrainzReleaseGroup releaseGroup, MusicArtist requestedArtist)
        {
            if (releaseGroup == null || requestedArtist == null)
                return false;

            MusicBrainzArtistCredit primaryCredit = releaseGroup.ArtistCredit?.FirstOrDefault();
            if (primaryCredit == null)
                return false;

            string primaryArtistId = primaryCredit.Artist?.Id;
            if (!string.IsNullOrWhiteSpace(primaryArtistId) && !string.IsNullOrWhiteSpace(requestedArtist.ArtistId))
                return primaryArtistId.Equals(requestedArtist.ArtistId, StringComparison.InvariantCultureIgnoreCase);

            if (!string.IsNullOrWhiteSpace(primaryCredit.Name) && !string.IsNullOrWhiteSpace(requestedArtist.ArtistName))
            {
                string normalizedPrimaryName = NormalizeArtistName(primaryCredit.Name);
                string normalizedRequestedName = NormalizeArtistName(requestedArtist.ArtistName);

                if (normalizedPrimaryName.Equals(normalizedRequestedName, StringComparison.InvariantCultureIgnoreCase))
                    return true;

                if (normalizedPrimaryName.Contains(normalizedRequestedName, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string NormalizeArtistName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var chars = value
                .Trim()
                .ToLowerInvariant()
                .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                .ToArray();

            return string.Join(" ", new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }




        public async Task<MusicRequestResult> RequestMusicAsync(MusicRequest request, MusicArtist music)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(music.DownloadClientId))
                    await CreateMusicInLidarr(request, music, true, _lidarrSettings.SearchNewRequests);
                else
                    await UpdateExistingMusic(request, music, true);

                return new MusicRequestResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error while requesting music \"{music.ArtistName}\" from Lidarr: " + ex.Message);
            }

            throw new Exception("An error occurred while requesting a music from Lidarr");
        }

        public async Task<MusicRequestResult> RequestMusicAlbumAsync(MusicRequest request, MusicArtist artist, MusicAlbum album)
        {
            try
            {
                if (album == null || artist == null)
                    throw new Exception("Invalid album or artist request.");

                MusicArtist existingArtist = await SearchMusicForArtistIdAsync(request, artist.ArtistId);

                if (string.IsNullOrWhiteSpace(existingArtist.DownloadClientId))
                {
                    await CreateMusicInLidarr(request, existingArtist, false, false);
                    existingArtist = await SearchMusicForArtistIdAsync(request, artist.ArtistId);
                }

                if (string.IsNullOrWhiteSpace(existingArtist?.DownloadClientId))
                    throw new Exception("Artist was not created in Lidarr.");

                await EnsureArtistMonitoredForAlbumRequestsAsync(existingArtist.DownloadClientId);

                JSONMusicAlbum existingAlbum = await EnsureAlbumExistsInLidarrAsync(album.AlbumId, existingArtist.DownloadClientId);

                if (existingAlbum == null || !existingAlbum.Id.HasValue)
                    throw new Exception("Album not found in Lidarr.");

                bool albumMonitored = await EnsureAlbumMonitoredAsync(existingAlbum.Id.Value);
                if (!albumMonitored)
                    throw new Exception("Lidarr did not persist album monitoring state.");

                if (_lidarrSettings.SearchNewRequests)
                {
                    HttpResponseMessage response = await HttpPostAsync($"{BaseURL}/command", JsonConvert.SerializeObject(new
                    {
                        name = "albumSearch",
                        albumIds = new[] { existingAlbum.Id.Value }
                    }));

                    await response.ThrowIfNotSuccessfulAsync("LidarrAlbumSearchCommand failed", x => x.error);
                }

                // Some Lidarr instances can flip album monitoring state during refresh/search cycles.
                // Enforce it once more before returning success.
                await Task.Delay(750);
                albumMonitored = await EnsureAlbumMonitoredAsync(existingAlbum.Id.Value);
                if (!albumMonitored)
                    throw new Exception("Album search started, but Lidarr did not keep album monitored.");

                return new MusicRequestResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error while requesting album \"{album?.AlbumTitle}\" from Lidarr: " + ex.Message);
            }

            throw new Exception("An error occurred while requesting a music album from Lidarr");
        }

        private async Task<bool> EnsureAlbumMonitoredAsync(int albumId)
        {
            // Attempt 1: update the full album payload (works on most Lidarr installs).
            HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/album/{albumId}");
            await response.ThrowIfNotSuccessfulAsync("LidarrGetAlbum failed", x => x.error);

            string albumJson = await response.Content.ReadAsStringAsync();
            JObject lidarrAlbum = JObject.Parse(albumJson);
            lidarrAlbum["monitored"] = true;

            response = await HttpPutAsync($"{BaseURL}/album/{albumId}", lidarrAlbum.ToString(Formatting.None));
            await response.ThrowIfNotSuccessfulAsync("LidarrUpdateAlbum failed", x => x.error);

            if (await IsAlbumMonitoredAsync(albumId))
                return true;

            // Attempt 2: use monitor endpoint for installs that ignore PUT monitor changes.
            try
            {
                response = await HttpPostAsync($"{BaseURL}/album/monitor", JsonConvert.SerializeObject(new
                {
                    albumIds = new[] { albumId },
                    monitored = true
                }));

                if (response.IsSuccessStatusCode && await IsAlbumMonitoredAsync(albumId))
                    return true;
            }
            catch { }

            // Attempt 3: bulk editor endpoint fallback.
            try
            {
                response = await HttpPutAsync($"{BaseURL}/album/editor", JsonConvert.SerializeObject(new
                {
                    albumIds = new[] { albumId },
                    monitored = true
                }));

                if (response.IsSuccessStatusCode && await IsAlbumMonitoredAsync(albumId))
                    return true;
            }
            catch { }

            return false;
        }

        private async Task<bool> IsAlbumMonitoredAsync(int albumId)
        {
            HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/album/{albumId}");
            await response.ThrowIfNotSuccessfulAsync("LidarrGetAlbum failed", x => x.error);

            string albumJson = await response.Content.ReadAsStringAsync();
            JObject refreshedAlbum = JObject.Parse(albumJson);

            return refreshedAlbum["monitored"]?.Value<bool>() == true;
        }

        private async Task EnsureArtistMonitoredForAlbumRequestsAsync(string artistDownloadClientId)
        {
            if (string.IsNullOrWhiteSpace(artistDownloadClientId))
                return;

            HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/artist/{artistDownloadClientId}");
            await response.ThrowIfNotSuccessfulAsync("LidarrGetMusic failed", x => x.error);

            string jsonResponse = await response.Content.ReadAsStringAsync();
            JObject lidarrMusic = JObject.Parse(jsonResponse);
            lidarrMusic["monitored"] = true;
            lidarrMusic["monitorNewItems"] = "none";

            response = await HttpPutAsync($"{BaseURL}/artist/{artistDownloadClientId}", lidarrMusic.ToString(Formatting.None));
            await response.ThrowIfNotSuccessfulAsync("LidarrUpdateMusic failed", x => x.error);
        }

        private async Task<JSONMusicAlbum> EnsureAlbumExistsInLidarrAsync(string albumId, string artistDownloadClientId)
        {
            JSONMusicAlbum existingAlbum = await FindExistingAlbumByMusicDbIdAsync(albumId, artistDownloadClientId);
            if (existingAlbum?.Id.HasValue == true)
                return existingAlbum;

            if (int.TryParse(artistDownloadClientId, out int artistId))
            {
                await RefreshArtistAsync(artistId);
                existingAlbum = await WaitForAlbumInArtistAsync(albumId, artistDownloadClientId, attempts: 8, delayMs: 1200);
                if (existingAlbum?.Id.HasValue == true)
                    return existingAlbum;
            }

            existingAlbum = await FindAlbumByForeignIdAsync(albumId);
            if (existingAlbum?.Id.HasValue == true)
                return existingAlbum;

            return null;
        }

        private async Task RefreshArtistAsync(int artistId)
        {
            HttpResponseMessage response = await HttpPostAsync($"{BaseURL}/command", JsonConvert.SerializeObject(new
            {
                name = "refreshArtist",
                artistId = artistId
            }));

            await response.ThrowIfNotSuccessfulAsync("LidarrRefreshArtistCommand failed", x => x.error);
        }

        private async Task<JSONMusicAlbum> WaitForAlbumInArtistAsync(string albumId, string artistDownloadClientId, int attempts, int delayMs)
        {
            for (int i = 0; i < attempts; i++)
            {
                JSONMusicAlbum existingAlbum = await FindExistingAlbumByMusicDbIdAsync(albumId, artistDownloadClientId);
                if (existingAlbum?.Id.HasValue == true)
                    return existingAlbum;

                await Task.Delay(delayMs);
            }

            return null;
        }



        private async Task CreateMusicInLidarr(MusicRequest request, MusicArtist music, bool monitorArtist, bool searchMissingAlbums)
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
                monitored = monitorArtist && _lidarrSettings.MonitorNewRequests,
                monitorNewItems = monitorArtist ? "all" : "none",
                tags = JToken.FromObject(category.Tags),
                rootFolderPath = category.RootFolder,
                addOptions = new
                {
                    searchForMissingAlbums = searchMissingAlbums && _lidarrSettings.SearchNewRequests
                }
            }));

            await response.ThrowIfNotSuccessfulAsync("LidarrMusicCreation failed", x => x.error);
        }


        private async Task UpdateExistingMusic(MusicRequest request, MusicArtist music, bool monitorArtist)
        {
            LidarrCategory category = null;
            int lidarrMusicId = int.Parse(music.DownloadClientId);
            HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/artist/{lidarrMusicId}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    await CreateMusicInLidarr(request, music, monitorArtist, false);

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
            lidarrMusic.monitored = monitorArtist && _lidarrSettings.MonitorNewRequests;

            response = await HttpPutAsync($"{BaseURL}/artist/{lidarrMusicId}", JsonConvert.SerializeObject(lidarrMusic));
            await response.ThrowIfNotSuccessfulAsync("LidarrUpdateMusic failed", x => x.error);

            if (monitorArtist && _lidarrSettings.SearchNewRequests)
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
                Requested = !jsonArtist.Monitored && (!string.IsNullOrWhiteSpace(downloadClientId) || _lidarrSettings.MonitorNewRequests) ? jsonArtist.Monitored : true,

                PlexUrl = string.Empty,
                EmbyUrl = string.Empty,
                PosterPath = GetPosterImageUrl(jsonArtist.Images)
            };
        }

        private MusicAlbum ConvertToAlbum(JSONMusicAlbum jsonAlbum, MusicArtist fallbackArtist)
        {
            string downloadClientAlbumId = jsonAlbum.Id?.ToString();
            var artistName = jsonAlbum.Artist?.ArtistName ?? fallbackArtist?.ArtistName;
            var artistId = jsonAlbum.Artist != null ? jsonAlbum.Artist.ForeignArtistId.ToString() : fallbackArtist?.ArtistId;

            bool available = (jsonAlbum.Statistics?.TrackFileCount ?? 0) > 0 || jsonAlbum.Grabbed;
            bool requested = jsonAlbum.Monitored;

            return new MusicAlbum
            {
                DownloadClientAlbumId = downloadClientAlbumId,
                AlbumId = jsonAlbum.ForeignAlbumId.ToString(),
                AlbumTitle = jsonAlbum.Title,
                Overview = jsonAlbum.Overview,

                ArtistId = artistId,
                ArtistName = artistName,
                ReleaseDate = jsonAlbum.ReleaseDate == default ? null : jsonAlbum.ReleaseDate,
                ReleaseType = DetermineReleaseType(jsonAlbum.AlbumType, jsonAlbum.SecondaryTypes),

                Available = available,
                Monitored = jsonAlbum.Monitored,
                Requested = requested,

                PosterPath = GetPosterImageUrl(jsonAlbum.Images)
            };
        }

        private MusicAlbum ConvertToAlbum(MusicBrainzReleaseGroup releaseGroup, MusicArtist fallbackArtist)
        {
            string artistName = releaseGroup.ArtistCredit?.FirstOrDefault()?.Name ?? fallbackArtist?.ArtistName;
            string artistId = releaseGroup.ArtistCredit?.FirstOrDefault()?.Artist?.Id ?? fallbackArtist?.ArtistId;

            return new MusicAlbum
            {
                DownloadClientAlbumId = null,
                AlbumId = releaseGroup.Id,
                AlbumTitle = releaseGroup.Title,
                Overview = string.Empty,

                ArtistId = artistId,
                ArtistName = artistName,
                ReleaseDate = ParseReleaseDate(releaseGroup.FirstReleaseDate),
                ReleaseType = DetermineReleaseType(releaseGroup.PrimaryType, releaseGroup.SecondaryTypes),

                Available = false,
                Monitored = false,
                Requested = false,

                PosterPath = string.Empty
            };
        }

        private DateTime? ParseReleaseDate(string releaseDate)
        {
            if (string.IsNullOrWhiteSpace(releaseDate))
                return null;

            string[] formats = { "yyyy-MM-dd", "yyyy-MM", "yyyy" };
            if (DateTime.TryParseExact(releaseDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                return parsed;

            if (DateTime.TryParse(releaseDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return parsed;

            return null;
        }

        private ReleaseFilters GetReleaseFilters(int categoryId)
        {
            var category = _lidarrSettings.Categories.FirstOrDefault(x => x.Id == categoryId);
            var configuredPrimaryTypes = category?.PrimaryTypes ?? Array.Empty<string>();

            var primaryTypes = configuredPrimaryTypes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(NormalizeReleaseType)
                .Where(x => x != null)
                .ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            var secondaryTypes = (category?.SecondaryTypes ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(NormalizeSecondaryTypeValue)
                .Where(x => x != null)
                .Where(x => AllowedSecondaryTypes.Contains(x, StringComparer.InvariantCultureIgnoreCase))
                .ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            var releaseStatuses = (category?.ReleaseStatuses ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(NormalizeReleaseStatusValue)
                .Where(x => x != null)
                .Where(x => AllowedReleaseStatuses.Contains(x, StringComparer.InvariantCultureIgnoreCase))
                .ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            return new ReleaseFilters(primaryTypes, secondaryTypes, releaseStatuses);
        }

        private static string DetermineReleaseType(string primaryType, IEnumerable<string> secondaryTypes)
        {
            var normalizedSecondaryTypes = secondaryTypes?.Select(NormalizeReleaseType).Where(x => x != null).ToArray() ?? Array.Empty<string>();

            if (normalizedSecondaryTypes.Any(x => x.Equals(ReleaseTypeSingle, StringComparison.InvariantCultureIgnoreCase)))
                return ReleaseTypeSingle;

            if (normalizedSecondaryTypes.Any(x => x.Equals(ReleaseTypeEp, StringComparison.InvariantCultureIgnoreCase)))
                return ReleaseTypeEp;

            var normalizedPrimaryType = NormalizeReleaseType(primaryType);
            if (normalizedPrimaryType != null)
                return normalizedPrimaryType;

            if (!string.IsNullOrWhiteSpace(primaryType))
                return null;

            return ReleaseTypeAlbum;
        }

        private static string NormalizeReleaseType(string releaseType)
        {
            if (string.IsNullOrWhiteSpace(releaseType))
                return null;

            if (releaseType.Equals("Album", StringComparison.InvariantCultureIgnoreCase))
                return ReleaseTypeAlbum;

            if (releaseType.Equals("EP", StringComparison.InvariantCultureIgnoreCase))
                return ReleaseTypeEp;

            if (releaseType.Equals("Single", StringComparison.InvariantCultureIgnoreCase))
                return ReleaseTypeSingle;

            if (releaseType.Equals("Broadcast", StringComparison.InvariantCultureIgnoreCase))
                return ReleaseTypeBroadcast;

            if (releaseType.Equals("Other", StringComparison.InvariantCultureIgnoreCase))
                return ReleaseTypeOther;

            return null;
        }

        private static string[] ExtractStringValues(JToken source, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                JToken token = source[propertyName];
                if (token == null)
                    continue;

                var values = new List<string>();

                if (token is JArray array)
                {
                    foreach (JToken item in array)
                    {
                        if (item == null)
                            continue;

                        if (item.Type == JTokenType.String)
                        {
                            string value = item.Value<string>();
                            if (!string.IsNullOrWhiteSpace(value))
                                values.Add(value.Trim());
                            continue;
                        }

                        if (item.Type == JTokenType.Object && !IsSelectedItem(item))
                            continue;

                        string[] objectValueCandidates = { "name", "value", "type", "id" };
                        foreach (string valueProperty in objectValueCandidates)
                        {
                            string value = item[valueProperty]?.Value<string>();
                            if (string.IsNullOrWhiteSpace(value) && item[valueProperty] is JObject nestedObject)
                            {
                                value = nestedObject["name"]?.Value<string>()
                                    ?? nestedObject["value"]?.Value<string>()
                                    ?? nestedObject["type"]?.Value<string>()
                                    ?? nestedObject["id"]?.Value<string>();
                            }

                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                values.Add(value.Trim());
                                break;
                            }
                        }
                    }
                }

                if (values.Any())
                    return values.Distinct(StringComparer.InvariantCultureIgnoreCase).ToArray();
            }

            return Array.Empty<string>();
        }

        private static string[] GetMetadataPrimaryTypes(JToken source)
        {
            var values = ExtractAllowedTypeNames(source, "primaryAlbumTypes", "albumType");
            if (values.Length == 0)
                values = ExtractStringValues(source, "primaryTypes", "primaryType", "primaryAlbumTypes", "allowedPrimaryTypes");
            if (values.Length == 0)
                values = ExtractStringValuesDeep(source);

            return values
                .Select(NormalizeMetadataPrimaryTypeValue)
                .Where(v => v != null)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .ToArray();
        }

        private static string[] GetMetadataSecondaryTypes(JToken source)
        {
            var values = ExtractAllowedTypeNames(source, "secondaryAlbumTypes", "albumType");
            if (values.Length == 0)
                values = ExtractStringValues(source, "secondaryTypes", "secondaryType", "secondaryAlbumTypes", "allowedSecondaryTypes");
            if (values.Length == 0)
                values = ExtractStringValuesDeep(source);

            return values
                .Select(NormalizeSecondaryTypeValue)
                .Where(v => v != null)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .ToArray();
        }

        private static string[] GetMetadataReleaseStatuses(JToken source)
        {
            var values = ExtractAllowedTypeNames(source, "releaseStatuses", "releaseStatus");
            if (values.Length == 0)
                values = ExtractStringValues(source, "releaseStatuses", "releaseStatus", "statuses", "allowedReleaseStatuses");
            if (values.Length == 0)
                values = ExtractStringValuesDeep(source);

            return values
                .Select(NormalizeReleaseStatusValue)
                .Where(v => v != null)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .ToArray();
        }

        private static string[] ExtractAllowedTypeNames(JToken source, string collectionPropertyName, string valueObjectPropertyName)
        {
            if (!(source[collectionPropertyName] is JArray collection))
                return Array.Empty<string>();

            return collection
                .Where(item => item != null && IsSelectedItem(item))
                .Select(item => item[valueObjectPropertyName]?["name"]?.Value<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .ToArray();
        }

        private static string[] ExtractStringValuesDeep(JToken source)
        {
            var values = new List<string>();

            IEnumerable<JToken> tokens = source is JContainer container
                ? container.DescendantsAndSelf()
                : new[] { source };

            foreach (var token in tokens)
            {
                if (token.Type == JTokenType.String)
                {
                    string value = token.Value<string>();
                    if (!string.IsNullOrWhiteSpace(value))
                        values.Add(value.Trim());
                    continue;
                }

            }

            return values.Distinct(StringComparer.InvariantCultureIgnoreCase).ToArray();
        }

        private static bool IsSelectedItem(JToken item)
        {
            string[] selectionFlags = { "allowed", "selected", "enabled", "isAllowed", "isSelected", "isEnabled" };
            foreach (string flag in selectionFlags)
            {
                JToken token = item[flag];
                if (token == null)
                    continue;

                if (token.Type == JTokenType.Boolean && token.Value<bool>() == false)
                    return false;
            }

            return true;
        }

        private static bool IsAllowedByMetadataProfileRules(string primaryType, IEnumerable<string> secondaryTypes, ReleaseFilters filters)
        {
            string normalizedPrimary = NormalizeReleaseType(primaryType);
            if (normalizedPrimary == null)
                return false;

            if (filters.PrimaryTypes.Any() && !filters.PrimaryTypes.Contains(normalizedPrimary))
                return false;

            if (secondaryTypes == null)
                return true;

            var normalizedSecondary = secondaryTypes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(NormalizeSecondaryTypeValue)
                .Where(x => x != null)
                .ToArray();

            if (normalizedSecondary.Length == 0)
                return true;

            if (!filters.SecondaryTypes.Any())
                return true;

            return normalizedSecondary.All(x => filters.SecondaryTypes.Contains(x));
        }

        private static bool HasAllowedReleaseStatus(IEnumerable<JSONReleases> releases, ReleaseFilters filters)
        {
            if (!filters.ReleaseStatuses.Any())
                return true;

            if (releases == null)
                return true;

            var releaseList = releases.ToArray();
            if (releaseList.Length == 0)
                return true;

            return releaseList.Any(x => !string.IsNullOrWhiteSpace(x.Status)
                && filters.ReleaseStatuses.Contains(NormalizeReleaseStatusValue(x.Status)));
        }

        private static string NormalizeSecondaryTypeValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string normalized = value.Trim();
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

        private static string NormalizeMetadataPrimaryTypeValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string normalized = value.Trim();
            if (normalized.Equals("Album", StringComparison.InvariantCultureIgnoreCase))
                return "Album";
            if (normalized.Equals("EP", StringComparison.InvariantCultureIgnoreCase))
                return "EP";
            if (normalized.Equals("Single", StringComparison.InvariantCultureIgnoreCase))
                return "Single";
            if (normalized.Equals("Broadcast", StringComparison.InvariantCultureIgnoreCase))
                return "Broadcast";
            if (normalized.Equals("Other", StringComparison.InvariantCultureIgnoreCase))
                return "Other";

            return null;
        }

        private static string NormalizeReleaseStatusValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string normalized = value.Trim();
            if (normalized.Equals("Pseudo Release", StringComparison.InvariantCultureIgnoreCase))
                return "Pseudo-Release";
            return normalized;
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

        public class JSONAlbumStatistics
        {
            [JsonProperty("trackFileCount")]
            public int TrackFileCount { get; set; }

            [JsonProperty("trackCount")]
            public int TrackCount { get; set; }

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
            [JsonProperty("id")]
            public int? Id { get; set; }

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

            [JsonProperty("statistics")]
            public JSONAlbumStatistics Statistics { get; set; }
        }

        private class MusicBrainzReleaseGroupResponse
        {
            [JsonProperty("release-groups")]
            public List<MusicBrainzReleaseGroup> ReleaseGroups { get; set; }

            [JsonProperty("release-group-count")]
            public int TotalCount { get; set; }

            [JsonProperty("release-group-offset")]
            public int Offset { get; set; }
        }

        private class MusicBrainzReleaseGroup
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("first-release-date")]
            public string FirstReleaseDate { get; set; }

            [JsonProperty("primary-type")]
            public string PrimaryType { get; set; }

            [JsonProperty("secondary-types")]
            public List<string> SecondaryTypes { get; set; }

            [JsonProperty("artist-credit")]
            public List<MusicBrainzArtistCredit> ArtistCredit { get; set; }
        }

        private class MusicBrainzArtistCredit
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("artist")]
            public MusicBrainzArtist Artist { get; set; }
        }

        private class MusicBrainzArtist
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }

        private const string ReleaseTypeAlbum = "Album";
        private const string ReleaseTypeEp = "EP";
        private const string ReleaseTypeSingle = "Single";
        private const string ReleaseTypeBroadcast = "Broadcast";
        private const string ReleaseTypeOther = "Other";
        private static readonly string[] AllowedSecondaryTypes =
        {
            "Studio",
            "Spokenword",
            "Soundtrack",
            "Remix",
            "Mixtape/Street",
            "Live",
            "Interview",
            "DJ-Mix",
            "Demo",
            "Compilation",
            "Audio drama"
        };
        private static readonly string[] AllowedReleaseStatuses = { "Pseudo-Release", "Promotion", "Official", "Bootleg" };

        private sealed class ReleaseFilters
        {
            public ReleaseFilters(HashSet<string> primaryTypes, HashSet<string> secondaryTypes, HashSet<string> releaseStatuses)
            {
                PrimaryTypes = primaryTypes;
                SecondaryTypes = secondaryTypes;
                ReleaseStatuses = releaseStatuses;
            }

            public HashSet<string> PrimaryTypes { get; }
            public HashSet<string> SecondaryTypes { get; }
            public HashSet<string> ReleaseStatuses { get; }
        }
    }
}
