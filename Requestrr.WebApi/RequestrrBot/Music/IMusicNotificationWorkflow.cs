using Requestrr.WebApi.RequestrrBot.Movies;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Music
{
    public interface IMusicNotificationWorkflow
    {
        Task NotifyForNewRequestAsync(string userId, MusicArtist musicArtist);
        Task NotifyForExistingRequestAsync(string userId, MusicArtist musicArtist);
        Task<MusicArtist> AddNotificationArtistAsync(string userId, string musicArtistId);
    }
}
