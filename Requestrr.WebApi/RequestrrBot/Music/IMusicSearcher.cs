using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Music
{
    public interface IMusicSearcher
    {
        Task<IReadOnlyList<MusicArtist>> SearchMusicForArtistAsync(MusicRequest request, string artistName);
        Task<MusicArtist> SearchMusicForArtistIdAsync(MusicRequest request, string artistId);
        Task<IReadOnlyList<MusicAlbum>> SearchMusicAlbumsForArtistAsync(MusicRequest request, MusicArtist artist);


        Task<Dictionary<string, MusicArtist>> SearchAvailableMusicArtistAsync(HashSet<string> artistIds, CancellationToken token);
    }
}
