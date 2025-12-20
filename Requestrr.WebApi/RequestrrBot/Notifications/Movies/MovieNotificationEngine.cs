using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Requestrr.WebApi.RequestrrBot.Logging;
using Requestrr.WebApi.RequestrrBot.Movies;

namespace Requestrr.WebApi.RequestrrBot.Notifications.Movies
{
    public class MovieNotificationEngine
    {
        private object _lock = new object();
        private readonly IMovieSearcher _movieSearcher;
        private readonly IMovieNotifier _notifier;
        private readonly ILogger _logger;
        private readonly MovieNotificationsRepository _notificationRequestRepository;
        private Task _notificationTask = null;
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public MovieNotificationEngine(
            IMovieSearcher movieSearcher,
            IMovieNotifier notifier,
            ILogger logger,
            MovieNotificationsRepository notificationRequestRepository
        )
        {
            _movieSearcher = movieSearcher;
            _notifier = notifier;
            _logger = logger;
            _notificationRequestRepository = notificationRequestRepository;
        }

        public void Start()
        {
            _notificationTask = Task.Run(async () =>
            {
                while (!_tokenSource.IsCancellationRequested)
                {
                    var currentRequests = new Dictionary<int, HashSet<string>>();
                    var cycleStopwatch = Stopwatch.StartNew();
                    var notifiedCount = 0;

                    try
                    {
                        currentRequests = _notificationRequestRepository.GetAllMovieNotifications();
                        _logger.LogNotificationStart(Program.DiagnosticsSettings, "Movie", currentRequests.Count);

                        var availableMovies = await _movieSearcher.SearchAvailableMoviesAsync(new HashSet<int>(currentRequests.Keys), _tokenSource.Token);

                        foreach (var request in currentRequests.Where(x => availableMovies.ContainsKey(x.Key)))
                        {
                            if (_tokenSource.IsCancellationRequested)
                                return;

                            try
                            {
                                var userNotified = await _notifier.NotifyAsync(request.Value.ToArray(), availableMovies[request.Key], _tokenSource.Token);
                                notifiedCount += userNotified.Count;

                                foreach (var userId in userNotified)
                                {
                                    _notificationRequestRepository.RemoveNotification(userId, request.Key);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "An error occurred while processing movie notifications: " + ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while retrieving all movie notification: " + ex.Message);
                        if (ex.Message.Contains("An error occurred while searching for a movie by tmdbId"))
                        {
                            try
                            {
                                string tmdbId = ex.Message.Split("An error occurred while searching for a movie by tmdbId \"")[1].Split("\"")[0];
                                _logger.LogWarning($"Removing notifications for Movie {tmdbId}");
                                int id = int.Parse(tmdbId);
                                
                                foreach (var userId in currentRequests[id])
                                {
                                    _notificationRequestRepository.RemoveNotification(userId, id);
                                }
                            }
                            catch
                            {
                                _logger.LogWarning("Removing notification failed...");
                            }
                        }
                    }

                    cycleStopwatch.Stop();
                    _logger.LogNotificationCycle(Program.DiagnosticsSettings, "Movie", currentRequests.Count, currentRequests.Count, notifiedCount, cycleStopwatch.ElapsedMilliseconds);

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
            catch (System.Exception)
            {
                _tokenSource.Dispose();
                _tokenSource = new CancellationTokenSource();
            }
        }
    }
}