using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Requestrr.WebApi.RequestrrBot.Logging;

namespace Requestrr.WebApi.RequestrrBot.Movies
{
    public class MovieRequestingWorkflow
    {
        private readonly int _categoryId;
        private readonly MovieUserRequester _user;
        private readonly IMovieSearcher _searcher;
        private readonly IMovieRequester _requester;
        private readonly IMovieUserInterface _userInterface;
        private readonly IMovieNotificationWorkflow _notificationWorkflow;
        private readonly ILogger _logger;

        public MovieRequestingWorkflow(
            MovieUserRequester user,
            int categoryId,
            IMovieSearcher searcher,
            IMovieRequester requester,
            IMovieUserInterface userInterface,
            IMovieNotificationWorkflow movieNotificationWorkflow,
            ILogger logger)
        {
            _categoryId = categoryId;
            _user = user;
            _searcher = searcher;
            _requester = requester;
            _userInterface = userInterface;
            _notificationWorkflow = movieNotificationWorkflow;
            _logger = logger;
        }

        public async Task SearchMovieAsync(string movieName)
        {
            var movies = await SearchMoviesAsync(movieName);

            if (movies.Any())
            {
                if (movies.Count > 1)
                {
                    await _userInterface.ShowMovieSelection(new MovieRequest(_user, _categoryId), movies);
                }
                else if (movies.Count == 1)
                {
                    var movie = movies.Single();
                    await HandleMovieSelectionAsync(movie);
                }
            }
        }



        public async Task SearchMovieAsync(int theMovieDbId)
        {
            try
            {
                var movie = await _searcher.SearchMovieAsync(new MovieRequest(_user, _categoryId), theMovieDbId);
                await HandleMovieSelectionAsync(movie);
            }
            catch
            {
                await _userInterface.WarnNoMovieFoundByTheMovieDbIdAsync(theMovieDbId.ToString());
            }
        }

        private async Task<IReadOnlyList<Movie>> SearchMoviesAsync(string movieName)
        {
            IReadOnlyList<Movie> movies = Array.Empty<Movie>();

            movieName = movieName.Replace(".", " ");
            _logger.LogWorkflowStart("Movie", _user.UserId, movieName);

            var stopwatch = Stopwatch.StartNew();
            movies = await _searcher.SearchMovieAsync(new MovieRequest(_user, _categoryId), movieName);
            stopwatch.Stop();

            if (!movies.Any())
            {
                _logger.LogInformation($"Movie search completed in {stopwatch.ElapsedMilliseconds}ms, found 0 results for '{movieName}'");
                await _userInterface.WarnNoMovieFoundAsync(movieName);
            }
            else
            {
                _logger.LogWorkflowComplete("Movie search", stopwatch.ElapsedMilliseconds, movies.Count);
            }

            return movies;
        }



        public async Task HandleMovieSelectionAsync(int theMovieDbId)
        {
            await HandleMovieSelectionAsync(await _searcher.SearchMovieAsync(new MovieRequest(_user, _categoryId), theMovieDbId));
        }

        private async Task HandleMovieSelectionAsync(Movie movie)
        {
            if (CanBeRequested(movie))
            {
                await _userInterface.DisplayMovieDetailsAsync(new MovieRequest(_user, _categoryId), movie);
            }
            else
            {
                if (movie.Available)
                {
                    await _userInterface.WarnMovieAlreadyAvailableAsync(movie);
                }
                else
                {
                    await _notificationWorkflow.NotifyForExistingRequestAsync(_user.UserId, movie);
                }
            }
        }

        public async Task RequestMovieAsync(int theMovieDbId)
        {
            var movie = await _searcher.SearchMovieAsync(new MovieRequest(_user, _categoryId), theMovieDbId);

            _logger.LogInformation($"User {_user.UserId} requesting movie: '{movie.Title}' (TMDB: {theMovieDbId})");
            var stopwatch = Stopwatch.StartNew();
            var result = await _requester.RequestMovieAsync(new MovieRequest(_user, _categoryId), movie);
            stopwatch.Stop();

            if (result.WasDenied)
            {
                _logger.LogWarning($"Movie request denied for '{movie.Title}' in {stopwatch.ElapsedMilliseconds}ms");
                await _userInterface.DisplayRequestDeniedAsync(movie);
            }
            else
            {
                _logger.LogInformation($"Movie request submitted successfully for '{movie.Title}' in {stopwatch.ElapsedMilliseconds}ms");
                await _userInterface.DisplayRequestSuccessAsync(movie);
                await _notificationWorkflow.NotifyForNewRequestAsync(_user.UserId, movie);
            }
        }

        private static bool CanBeRequested(Movie movie)
        {
            return !movie.Available && !movie.Requested;
        }
    }
}