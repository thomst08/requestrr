using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Music
{
    public class MusicRequestingWorkflow
    {
        private readonly int _categoryId;
        private readonly MusicUserRequester _user;
        private readonly IMusicSearcher _musicSearcher;
        private readonly IMusicRequester _requester;
        private readonly IMusicUserInterface _userInterface;
        private readonly string _restrictions;
        private readonly IMusicNotificationWorkflow _notificationWorkflow;


        public MusicRequestingWorkflow(
            MusicUserRequester user,
            int categoryId,
            IMusicSearcher searcher,
            IMusicRequester requester,
            IMusicUserInterface userInterface,
            string restrictions,
            IMusicNotificationWorkflow notificationWorkflow
        )
        {
            _categoryId = categoryId;
            _user = user;
            _musicSearcher = searcher;
            _requester = requester;
            _userInterface = userInterface;
            _restrictions = string.IsNullOrWhiteSpace(restrictions) ? MusicRestrictions.None : restrictions;
            _notificationWorkflow = notificationWorkflow;
        }


        public async Task SearchMusicForArtistAsync(string artistName)
        {
            IReadOnlyList<MusicArtist> musicList = await SearchMusicForArtistListAsync(artistName);

            if (musicList.Any())
            {
                if (musicList.Count > 1)
                {
                    await _userInterface.ShowMusicArtistSelection(new MusicRequest(_user, _categoryId), musicList);
                }
                else if (musicList.Count == 1)
                {
                    MusicArtist music = musicList.Single();
                    await HandleMusicSelectionAsync(music);
                }
            }
        }


        public async Task<IReadOnlyList<MusicArtist>> SearchMusicForArtistListAsync(string artistName)
        {
            IReadOnlyList<MusicArtist> music = Array.Empty<MusicArtist>();

            artistName = artistName.Replace(".", " ");
            music = await _musicSearcher.SearchMusicForArtistAsync(new MusicRequest(_user, _categoryId), artistName);

            if (!music.Any())
                await _userInterface.WarnNoMusicArtistFoundAsync(artistName);

            return music;
        }


        public async Task HandleMusicArtistSelectionAsync(string musicArtistId)
        {
            await HandleMusicSelectionAsync(await _musicSearcher.SearchMusicForArtistIdAsync(new MusicRequest(_user, _categoryId), musicArtistId));
        }

        public async Task HandleMusicAlbumSelectionAsync(string musicArtistId, string albumId, string releaseType = null)
        {
            MusicArtist musicArtist = await _musicSearcher.SearchMusicForArtistIdAsync(new MusicRequest(_user, _categoryId), musicArtistId);

            if (musicArtist == null)
            {
                await _userInterface.WarnNoMusicArtistFoundAsync(musicArtistId);
                return;
            }

            if (string.Equals(albumId, "all", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!CanRequestAllAlbumsForReleaseType(releaseType))
                {
                    await ShowMusicAlbumPageAsync(musicArtistId, 0, releaseType);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(releaseType))
                {
                    await _userInterface.DisplayAllAlbumsRequestConfirmAsync(new MusicRequest(_user, _categoryId), musicArtist, releaseType);
                    return;
                }

                await HandleMusicArtistRequestSelectionAsync(musicArtist);
                return;
            }

            IReadOnlyList<MusicAlbum> albums = await _musicSearcher.SearchMusicAlbumsForArtistAsync(new MusicRequest(_user, _categoryId), musicArtist);
            MusicAlbum selectedAlbum = albums.FirstOrDefault(x => x.AlbumId == albumId);

            if (selectedAlbum == null)
            {
                if (IsSingleSelectionRequired())
                {
                    await ShowMusicAlbumPageAsync(musicArtistId, 0, releaseType);
                    return;
                }

                await HandleMusicArtistRequestSelectionAsync(musicArtist);
                return;
            }

            if (CanAlbumBeRequested(selectedAlbum))
            {
                await _userInterface.DisplayMusicAlbumDetailsAsync(new MusicRequest(_user, _categoryId), musicArtist, selectedAlbum);
            }
            else if (selectedAlbum.Available)
            {
                await _userInterface.WarnMusicAlbumAlreadyAvailableAsync(musicArtist, selectedAlbum);
            }
            else
            {
                await _userInterface.WarnMusicAlbumAlreadyRequestedAsync(musicArtist, selectedAlbum);
            }
        }


        private async Task HandleMusicSelectionAsync(MusicArtist musicArtist)
        {
            IReadOnlyList<MusicAlbum> albums = await _musicSearcher.SearchMusicAlbumsForArtistAsync(new MusicRequest(_user, _categoryId), musicArtist);
            if (albums.Any())
            {
                await _userInterface.ShowMusicAlbumSelection(new MusicRequest(_user, _categoryId), musicArtist, albums, 0);
                return;
            }

            await HandleMusicArtistRequestSelectionAsync(musicArtist);
        }

        private async Task HandleMusicArtistRequestSelectionAsync(MusicArtist musicArtist)
        {
            if (CanBeRequested(musicArtist))
            {
                await _userInterface.DisplayMusicArtistDetailsAsync(new MusicRequest(_user, _categoryId), musicArtist);
            }
            else
            {
                if (musicArtist.Available)
                {
                    await _userInterface.WarnMusicArtistAlreadyAvailableAsync(musicArtist);
                }
                else
                {
                    await _notificationWorkflow.NotifyForExistingRequestAsync(_user.UserId, musicArtist);
                }
            }
        }



        /// <summary>
        /// Handles the request for an artist
        /// </summary>
        /// <param name="artistId"></param>
        /// <returns></returns>
        public async Task RequestMusicArtistAsync(string artistId)
        {
            MusicArtist musicArtist = await _musicSearcher.SearchMusicForArtistIdAsync(new MusicRequest(_user, _categoryId), artistId);
            MusicRequestResult result = await _requester.RequestMusicAsync(new MusicRequest(_user, _categoryId), musicArtist);

            if (result.WasDenied)
            {
                await _userInterface.DisplayArtistRequestDeniedAsync(musicArtist);
            }
            else
            {
                await _userInterface.DisplayArtistRequestSuccessAsync(musicArtist);
                await _notificationWorkflow.NotifyForNewRequestAsync(_user.UserId, musicArtist);
            }
        }

        public async Task RequestMusicAlbumAsync(string artistId, string albumId)
        {
            MusicArtist musicArtist = await _musicSearcher.SearchMusicForArtistIdAsync(new MusicRequest(_user, _categoryId), artistId);
            IReadOnlyList<MusicAlbum> albums = await _musicSearcher.SearchMusicAlbumsForArtistAsync(new MusicRequest(_user, _categoryId), musicArtist);
            MusicAlbum selectedAlbum = albums.FirstOrDefault(x => x.AlbumId == albumId);

            if (selectedAlbum == null)
            {
                return;
            }

            MusicRequestResult result = await _requester.RequestMusicAlbumAsync(new MusicRequest(_user, _categoryId), musicArtist, selectedAlbum);

            if (result.WasDenied)
            {
                await _userInterface.DisplayMusicAlbumRequestDeniedAsync(musicArtist, selectedAlbum);
            }
            else
            {
                await _userInterface.DisplayMusicAlbumRequestSuccessAsync(musicArtist, selectedAlbum);
            }
        }

        public async Task RequestAllAlbumsByTypeAsync(string artistId, string releaseType)
        {
            MusicArtist musicArtist = await _musicSearcher.SearchMusicForArtistIdAsync(new MusicRequest(_user, _categoryId), artistId);
            if (musicArtist == null)
            {
                await _userInterface.WarnNoMusicArtistFoundAsync(artistId);
                return;
            }

            await RequestAllAlbumsByReleaseTypeAsync(musicArtist, releaseType);
        }

        public async Task ShowMusicAlbumPageAsync(string artistId, int page, string releaseType = null)
        {
            MusicArtist musicArtist = await _musicSearcher.SearchMusicForArtistIdAsync(new MusicRequest(_user, _categoryId), artistId);
            if (musicArtist == null)
            {
                await _userInterface.WarnNoMusicArtistFoundAsync(artistId);
                return;
            }

            IReadOnlyList<MusicAlbum> albums = await _musicSearcher.SearchMusicAlbumsForArtistAsync(new MusicRequest(_user, _categoryId), musicArtist);
            if (!albums.Any())
            {
                await HandleMusicArtistRequestSelectionAsync(musicArtist);
                return;
            }

            await _userInterface.ShowMusicAlbumSelection(new MusicRequest(_user, _categoryId), musicArtist, albums, page, releaseType);
        }



        private static bool CanBeRequested(MusicArtist music)
        {
            return !music.Available && !music.Requested;
        }

        private static bool CanAlbumBeRequested(MusicAlbum album)
        {
            return !album.Available && !album.Requested;
        }

        private async Task RequestAllAlbumsByReleaseTypeAsync(MusicArtist musicArtist, string releaseType)
        {
            IReadOnlyList<MusicAlbum> albums = await _musicSearcher.SearchMusicAlbumsForArtistAsync(new MusicRequest(_user, _categoryId), musicArtist);
            MusicAlbum[] matchingAlbums = albums
                .Where(x => string.Equals(x.ReleaseType, releaseType, StringComparison.InvariantCultureIgnoreCase))
                .ToArray();

            MusicAlbum[] requestableAlbums = matchingAlbums
                .Where(CanAlbumBeRequested)
                .ToArray();

            if (!requestableAlbums.Any())
            {
                if (matchingAlbums.Any(x => x.Available))
                    await _userInterface.WarnMusicAlbumAlreadyAvailableAsync(musicArtist, matchingAlbums.First(x => x.Available));
                else if (matchingAlbums.Any(x => x.Requested))
                    await _userInterface.WarnMusicAlbumAlreadyRequestedAsync(musicArtist, matchingAlbums.First(x => x.Requested));
                else
                    await _userInterface.WarnNoMusicArtistFoundAsync(musicArtist.ArtistName);

                return;
            }

            bool anyDenied = false;
            MusicAlbum lastAlbum = requestableAlbums.Last();
            foreach (MusicAlbum album in requestableAlbums)
            {
                MusicRequestResult result = await _requester.RequestMusicAlbumAsync(new MusicRequest(_user, _categoryId), musicArtist, album);
                if (result.WasDenied)
                    anyDenied = true;
            }

            if (anyDenied)
                await _userInterface.DisplayMusicAlbumRequestDeniedAsync(musicArtist, lastAlbum);
            else
                await _userInterface.DisplayMusicAlbumRequestSuccessAsync(musicArtist, lastAlbum);
        }

        private bool CanRequestAllAlbumsForReleaseType(string releaseType)
        {
            if (string.IsNullOrWhiteSpace(releaseType))
                return IsNoneRestriction();

            if (IsNoneRestriction())
                return true;

            var restrictedTypes = GetRestrictedReleaseTypes();
            return !restrictedTypes.Contains(releaseType, StringComparer.InvariantCultureIgnoreCase);
        }

        private bool IsNoneRestriction()
        {
            return !ParseRestrictions().Any();
        }

        private bool IsSingleSelectionRequired()
        {
            return GetRestrictedReleaseTypes().Any();
        }

        private bool HasRestriction(string restriction)
        {
            return ParseRestrictions().Contains(restriction, StringComparer.InvariantCultureIgnoreCase);
        }

        private string[] GetRestrictedReleaseTypes()
        {
            return ParseRestrictions()
                .Select(MapRestrictionToReleaseType)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .ToArray();
        }

        private static string MapRestrictionToReleaseType(string restriction)
        {
            if (string.IsNullOrWhiteSpace(restriction))
                return null;

            string normalized = restriction.Trim();
            if (normalized.StartsWith(MusicRestrictions.SinglePrefix, StringComparison.InvariantCultureIgnoreCase))
                return normalized.Substring(MusicRestrictions.SinglePrefix.Length).Trim();

            if (normalized.Equals(MusicRestrictions.SingleAlbum, StringComparison.InvariantCultureIgnoreCase))
                return "Album";

            if (normalized.Equals(MusicRestrictions.SingleEP, StringComparison.InvariantCultureIgnoreCase))
                return "EP";

            if (normalized.Equals(MusicRestrictions.SingleSingle, StringComparison.InvariantCultureIgnoreCase))
                return "Single";

            return null;
        }

        private string[] ParseRestrictions()
        {
            if (string.IsNullOrWhiteSpace(_restrictions))
                return Array.Empty<string>();

            var parsed = _restrictions
                .Split(',')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .ToArray();

            if (parsed.Any(x => x.Equals(MusicRestrictions.None, StringComparison.InvariantCultureIgnoreCase)))
                return Array.Empty<string>();

            return parsed;
        }
    }
}
