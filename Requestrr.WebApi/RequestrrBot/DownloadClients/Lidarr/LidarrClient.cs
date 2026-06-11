using Microsoft.Extensions.Logging;
using Requestrr.WebApi.RequestrrBot.Music;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.DownloadClients.Lidarr
{
    public class LidarrClient : IMusicSearcher, IMusicRequester
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LidarrClient> _logger;
        private readonly LidarrSettingsProvider _settingsProvider;


        public LidarrClient(IHttpClientFactory httpClientFactory, ILogger<LidarrClient> logger, LidarrSettingsProvider settingsProvider)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _settingsProvider = settingsProvider;
        }


        /// <summary>
        /// Used to test a connection to Lidarr
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static Task TestConnectionAsync(HttpClient httpClient, ILogger<LidarrClient> logger, LidarrSettings settings)
        {
            return LidarrClientV1.TestConnectionAsync(httpClient, logger, settings);
        }



        /// <summary>
        /// Returns all paths setup in Lidarr where media is stored
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <returns>Returns a list of JSONRootPath objects</returns>
        public static Task<IList<JSONRootPath>> GetRootPaths(HttpClient httpClient, ILogger<LidarrClient> logger, LidarrSettings settings)
        {
            return LidarrClientV1.GetRootPaths(httpClient, logger, settings);
        }



        /// <summary>
        /// Returns all profiles setup in Lidarr
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <returns>Returns a list of JSONProfile objects</returns>
        public static Task<IList<JSONProfile>> GetProfiles(HttpClient httpClient, ILogger<LidarrClient> logger, LidarrSettings settings)
        {
            return LidarrClientV1.GetProfiles(httpClient, logger, settings);
        }



        /// <summary>
        /// Returns all metadata profiles setup in Lidarr
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <returns>Returns a list of JSONProfile objects</returns>
        public static Task<IList<JSONProfile>> GetMetadataProfiles(HttpClient httpClient, ILogger<LidarrClient> logger, LidarrSettings settings)
        {
            return LidarrClientV1.GetMetadataProfiles(httpClient, logger, settings);
        }

        public static Task<IList<JSONMetadataProfile>> GetMetadataProfilesDetailed(HttpClient httpClient, ILogger<LidarrClient> logger, LidarrSettings settings)
        {
            return LidarrClientV1.GetMetadataProfilesDetailed(httpClient, logger, settings);
        }




        /// <summary>
        /// Returns all tags setup in Lidarr
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <returns>Returns a list of JSONTag objects</returns>
        public static Task<IList<JSONTag>> GetTags(HttpClient httpClient, ILogger<LidarrClient> logger, LidarrSettings settings)
        {
            return LidarrClientV1.GetTags(httpClient, logger, settings);
        }


        /// <summary>
        /// Handles the passing of a Music name into the Muisc client
        /// </summary>
        /// <param name="request"></param>
        /// <param name="musicName"></param>
        /// <returns></returns>
        public Task<MusicArtist> SearchMusicForArtistIdAsync(MusicRequest request, string artistId)
        {
            return CreateInstance<IMusicSearcher>().SearchMusicForArtistIdAsync(request, artistId);
        }

        public Task<IReadOnlyList<MusicArtist>> SearchMusicForArtistAsync(MusicRequest request, string artistName)
        {
            return CreateInstance<IMusicSearcher>().SearchMusicForArtistAsync(request, artistName);
        }

        public Task<IReadOnlyList<MusicAlbum>> SearchMusicAlbumsForArtistAsync(MusicRequest request, MusicArtist artist)
        {
            return CreateInstance<IMusicSearcher>().SearchMusicAlbumsForArtistAsync(request, artist);
        }


        //-----------------------------


        //public Task<MovieDetails> GetMovieDetails(MovieRequest request, string theMovieDbId)
        //{
        //    return CreateInstance<IMovieSearcher>().GetMovieDetails(request, theMovieDbId);
        //}

        public Task<Dictionary<string, MusicArtist>> SearchAvailableMusicArtistAsync(HashSet<string> artistIds, CancellationToken token)
        {
            return CreateInstance<IMusicSearcher>().SearchAvailableMusicArtistAsync(artistIds, token);
        }


        public Task<MusicRequestResult> RequestMusicAsync(MusicRequest request, MusicArtist music)
        {
            return CreateInstance<IMusicRequester>().RequestMusicAsync(request, music);
        }

        public Task<MusicRequestResult> RequestMusicAlbumAsync(MusicRequest request, MusicArtist artist, MusicAlbum album)
        {
            return CreateInstance<IMusicRequester>().RequestMusicAlbumAsync(request, artist, album);
        }


        private T CreateInstance<T>() where T : class
        {
            return new LidarrClientV1(_httpClientFactory, _logger, _settingsProvider) as T;
        }

        //-----------------------------


        public class JSONRootPath
        {
            public string path { get; set; }
            public int id { get; set; }
        }

        public class JSONProfile
        {
            public string name { get; set; }
            public int id { get; set; }
        }


        public class JSONTag
        {
            public string label { get; set; }
            public int id { get; set; }
        }

        public class JSONMetadataProfile
        {
            public int id { get; set; }
            public string name { get; set; }
            public string[] primaryTypes { get; set; } = Array.Empty<string>();
            public string[] secondaryTypes { get; set; } = Array.Empty<string>();
            public string[] releaseStatuses { get; set; } = Array.Empty<string>();
        }
    }
}
