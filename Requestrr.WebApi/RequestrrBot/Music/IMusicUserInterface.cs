using System.Collections.Generic;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Music
{
    public interface IMusicUserInterface
    {
        Task ShowMusicArtistSelection(MusicRequest request, IReadOnlyList<MusicArtist> music);
        Task ShowMusicAlbumSelection(MusicRequest request, MusicArtist musicArtist, IReadOnlyList<MusicAlbum> albums, int page, string selectedReleaseType = null);
        Task WarnNoMusicArtistFoundAsync(string musicName);

        Task DisplayMusicArtistDetailsAsync(MusicRequest request, MusicArtist music);
        Task DisplayAllAlbumsRequestConfirmAsync(MusicRequest request, MusicArtist artist, string releaseType);
        Task DisplayMusicAlbumDetailsAsync(MusicRequest request, MusicArtist artist, MusicAlbum album);
        Task DisplayArtistRequestDeniedAsync(MusicArtist music);
        Task DisplayArtistRequestSuccessAsync(MusicArtist music);
        Task DisplayMusicAlbumRequestDeniedAsync(MusicArtist artist, MusicAlbum album);
        Task DisplayMusicAlbumRequestSuccessAsync(MusicArtist artist, MusicAlbum album);

        Task WarnMusicArtistAlreadyAvailableAsync(MusicArtist music);
        Task WarnMusicAlbumAlreadyAvailableAsync(MusicArtist artist, MusicAlbum album);
        Task WarnMusicAlbumAlreadyRequestedAsync(MusicArtist artist, MusicAlbum album);

        Task WarnMusicArtistUnavailableAndAlreadyHasNotificationAsync(MusicArtist music);
        Task AskForNotificationArtistRequestAsync(MusicArtist music);
        Task DisplayNotificationArtistSuccessAsync(MusicArtist music);
    }
}
