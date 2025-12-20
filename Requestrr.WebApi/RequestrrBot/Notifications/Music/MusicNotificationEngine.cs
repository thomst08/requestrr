using Microsoft.Extensions.Logging;
using Requestrr.WebApi.RequestrrBot.Logging;
using Requestrr.WebApi.RequestrrBot.Music;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Notifications.Music
{
    public class MusicNotificationEngine
    {
        private object _lock = new object();
        private readonly IMusicSearcher _musicSearcher;
        private readonly IMusicNotifier _notifier;
        private readonly ILogger _logger;
        private readonly MusicNotificationsRepository _notificationsRepository;
        private Task _notificationTask = null;
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public MusicNotificationEngine(
            IMusicSearcher musicSearcher,
            IMusicNotifier notifier,
            ILogger logger,
            MusicNotificationsRepository musicNotificationsRepository
        )
        {
            _musicSearcher = musicSearcher;
            _notifier = notifier;
            _logger = logger;
            _notificationsRepository = musicNotificationsRepository;
        }


        public void Start()
        {
            _notificationTask = Task.Run(async () =>
            {
                while (!_tokenSource.IsCancellationRequested)
                {
                    Dictionary<string, HashSet<string>> currentRequests = new Dictionary<string, HashSet<string>>();
                    var cycleStopwatch = Stopwatch.StartNew();
                    var notifiedCount = 0;

                    try
                    {
                        currentRequests = _notificationsRepository.GetAllMusicNotifications();
                        _logger.LogNotificationStart(Program.DiagnosticsSettings, "Music", currentRequests.Count);
                        Dictionary<string, MusicArtist> availableMusicArtists = await _musicSearcher.SearchAvailableMusicArtistAsync(new HashSet<string>(currentRequests.Keys), _tokenSource.Token);

                        foreach (KeyValuePair<string, HashSet<string>> request in currentRequests.Where(x => availableMusicArtists.ContainsKey(x.Key)))
                        {
                            if (_tokenSource.IsCancellationRequested)
                                return;

                            try
                            {
                                HashSet<string> userNotified = await _notifier.NotifyArtistAsync(request.Value.ToArray(), availableMusicArtists[request.Key], _tokenSource.Token);
                                notifiedCount += userNotified.Count;

                                foreach (string userId in userNotified)
                                {
                                    _notificationsRepository.RemoveNotification(userId, request.Key);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "An error occurred while processing music artist notifications: " + ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while retrieving all music notification: " + ex.Message);
                    }

                    cycleStopwatch.Stop();
                    _logger.LogNotificationCycle(Program.DiagnosticsSettings, "Music", currentRequests.Count, currentRequests.Count, notifiedCount, cycleStopwatch.ElapsedMilliseconds);

                    await Task.Delay(TimeSpan.FromMinutes(1), _tokenSource.Token);
                }
            }, _tokenSource.Token);
        }


        public async Task StopAsync()
        {
            try
            {
                _tokenSource.Cancel();
                await _notificationTask;
            }
            catch
            {
                _tokenSource.Dispose();
                _tokenSource = new CancellationTokenSource();
            }
        }
    }
}
