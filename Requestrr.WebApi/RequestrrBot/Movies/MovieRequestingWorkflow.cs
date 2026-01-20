using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly IQualityProfileProvider _qualityProfileProvider;

        public MovieRequestingWorkflow(
            MovieUserRequester user,
            int categoryId,
            IMovieSearcher searcher,
            IMovieRequester requester,
            IMovieUserInterface userInterface,
            IMovieNotificationWorkflow movieNotificationWorkflow,
            IQualityProfileProvider qualityProfileProvider)
        {
            _categoryId = categoryId;
            _user = user;
            _searcher = searcher;
            _requester = requester;
            _userInterface = userInterface;
            _notificationWorkflow = movieNotificationWorkflow;
            _qualityProfileProvider = qualityProfileProvider;
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
                var qualityProfiles = await _qualityProfileProvider.GetQualityProfilesAsync();
                
                if (qualityProfiles.Count > 1)
                {
                    await _userInterface.DisplayQualitySelectionAsync(new MovieRequest(_user, _categoryId), movie, qualityProfiles);
                }
                else
                {
                    await _userInterface.DisplayMovieDetailsAsync(new MovieRequest(_user, _categoryId), movie);
                }
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

        public async Task HandleQualitySelectionAsync(int theMovieDbId, int qualityProfileId)
        {
            var movie = await _searcher.SearchMovieAsync(new MovieRequest(_user, _categoryId), theMovieDbId);
            var qualityProfiles = await _qualityProfileProvider.GetQualityProfilesAsync();
            var selectedProfile = qualityProfiles.FirstOrDefault(x => x.Id == qualityProfileId);

            var request = new MovieRequest(_user, _categoryId)
            {
                QualityProfileId = qualityProfileId,
                QualityProfileName = selectedProfile?.Name
            };

            await _userInterface.DisplayMovieDetailsAsync(request, movie);
        }

        public async Task RequestMovieAsync(int theMovieDbId, int? qualityProfileId = null)
        {
            var movie = await _searcher.SearchMovieAsync(new MovieRequest(_user, _categoryId), theMovieDbId);
            
            var request = new MovieRequest(_user, _categoryId);
            
            if (qualityProfileId.HasValue)
            {
                var qualityProfiles = await _qualityProfileProvider.GetQualityProfilesAsync();
                var selectedProfile = qualityProfiles.FirstOrDefault(x => x.Id == qualityProfileId.Value);
                request.QualityProfileId = qualityProfileId;
                request.QualityProfileName = selectedProfile?.Name;
            }

            var result = await _requester.RequestMovieAsync(request, movie);

            if (result.WasDenied)
            {
                await _userInterface.DisplayRequestDeniedAsync(request, movie);
            }
            else
            {
                await _userInterface.DisplayRequestSuccessAsync(request, movie);
                await _notificationWorkflow.NotifyForNewRequestAsync(_user.UserId, movie);
            }
        }

        private static bool CanBeRequested(Movie movie)
        {
            return !movie.Available && !movie.Requested;
        }
    }
}