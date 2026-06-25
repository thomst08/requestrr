using Requestrr.WebApi.RequestrrBot.Notifications.Music;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Music
{
    public class MusicNotificationWorkflow : IMusicNotificationWorkflow
    {
        private readonly MusicNotificationsRepository _notificationsRepository;
        private readonly IMusicUserInterface _userInterface;
        private readonly IMusicSearcher _musicSearcher;
        private readonly bool _automaticNotificationForNewRequests;


        public MusicNotificationWorkflow(
            MusicNotificationsRepository musicNotificationsRepository,
            IMusicUserInterface userInterface,
            IMusicSearcher musicSearcher,
            bool automaticNotificationForNewRequests
        )
        {
            _notificationsRepository = musicNotificationsRepository;
            _userInterface = userInterface;
            _musicSearcher = musicSearcher;
            _automaticNotificationForNewRequests = automaticNotificationForNewRequests;
        }


        public Task NotifyForNewRequestAsync(string userId, MusicArtist musicArtist)
        {
            if (_automaticNotificationForNewRequests)
            {
                _notificationsRepository.AddNotification(userId, musicArtist.ArtistId);
            }

            return Task.CompletedTask;
        }


        public async Task NotifyForExistingRequestAsync(string userId, MusicArtist musicArtist)
        {
            if (IsAlreadyNotified(userId, musicArtist))
            {
                await _userInterface.WarnMusicArtistUnavailableAndAlreadyHasNotificationAsync(musicArtist);
            }
            else
            {
                await _userInterface.AskForNotificationArtistRequestAsync(musicArtist);
            }
        }


        public async Task<MusicArtist> AddNotificationArtistAsync(string userId, string musicArtistId)
        {
            MusicArtist musicArtist = await _musicSearcher.SearchMusicForArtistIdAsync(new MusicRequest(null, int.MinValue), musicArtistId);
            _notificationsRepository.AddNotification(userId, musicArtist.ArtistId);
            await _userInterface.DisplayNotificationArtistSuccessAsync(musicArtist);
            return musicArtist;
        }


        private bool IsAlreadyNotified(string userId, MusicArtist musicArtist)
        {
            return _notificationsRepository.HasNotification(userId, musicArtist.ArtistId);
        }
    }
}
