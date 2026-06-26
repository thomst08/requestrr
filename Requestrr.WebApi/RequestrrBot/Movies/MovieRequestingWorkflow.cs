using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Requestrr.WebApi.RequestrrBot.Logging;

namespace Requestrr.WebApi.RequestrrBot.Movies
{
    public class MovieRequestingWorkflow
    {
        private readonly int _categoryId;
        private readonly string _categoryName;
        private readonly MovieUserRequester _user;
        private readonly IMovieSearcher _searcher;
        private readonly IMovieRequester _requester;
        private readonly IMovieUserInterface _userInterface;
        private readonly IMovieNotificationWorkflow _notificationWorkflow;
        private readonly IRequestLogger _requestLogger;

        public MovieRequestingWorkflow(
            MovieUserRequester user,
            int categoryId,
            string categoryName,
            IMovieSearcher searcher,
            IMovieRequester requester,
            IMovieUserInterface userInterface,
            IMovieNotificationWorkflow movieNotificationWorkflow,
            IRequestLogger requestLogger)
        {
            _categoryId = categoryId;
            _categoryName = categoryName;
            _user = user;
            _searcher = searcher;
            _requester = requester;
            _userInterface = userInterface;
            _notificationWorkflow = movieNotificationWorkflow;
            _requestLogger = requestLogger;
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
            movies = await _searcher.SearchMovieAsync(new MovieRequest(_user, _categoryId), movieName);

            if (!movies.Any())
            {
                await _userInterface.WarnNoMovieFoundAsync(movieName);
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
            var result = await _requester.RequestMovieAsync(new MovieRequest(_user, _categoryId), movie);

            if (result.WasDenied)
            {
                await _userInterface.DisplayRequestDeniedAsync(movie);
                await _requestLogger.LogMovieRequestAsync(_user.UserId, _user.Username, movie.Title, theMovieDbId, _categoryName, false, "Request denied");
            }
            else
            {
                await _userInterface.DisplayRequestSuccessAsync(movie);
                await _notificationWorkflow.NotifyForNewRequestAsync(_user.UserId, movie);
                await _requestLogger.LogMovieRequestAsync(_user.UserId, _user.Username, movie.Title, theMovieDbId, _categoryName, true);
            }
        }

        private static bool CanBeRequested(Movie movie)
        {
            return !movie.Available && !movie.Requested;
        }
    }
}