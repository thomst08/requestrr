using Requestrr.WebApi.RequestrrBot.Movies;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Music
{
    public class DisabledMusicNotificationWorkflow : IMusicNotificationWorkflow
    {
        private readonly IMusicUserInterface _userInterface;

        public DisabledMusicNotificationWorkflow(IMusicUserInterface userInterface)
        {
            _userInterface = userInterface;
        }

        public Task<MusicArtist> AddNotificationArtistAsync(string userId, string musicArtistId)
        {
            return Task.FromResult<MusicArtist>(null);
        }

        public Task NotifyForExistingRequestAsync(string userId, MusicArtist musicArtist)
        {
            return _userInterface.WarnMusicArtistAlreadyAvailableAsync(musicArtist);
        }

        public Task NotifyForNewRequestAsync(string userId, MusicArtist musicArtist)
        {
            return Task.CompletedTask;
        }
    }
}
