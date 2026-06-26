using System.Threading.Tasks;
using Requestrr.WebApi.RequestrrBot.Logging;

namespace Requestrr.WebApi.RequestrrBot.TvShows.SeasonsRequestWorkflows
{
    public class NormalTvSeasonRequestingWorkflow
    {
        private readonly ITvShowSearcher _searcher;
        private readonly ITvShowRequester _requester;
        private readonly ITvShowUserInterface _userInterface;
        private readonly ITvShowNotificationWorkflow _tvShowNotificationWorkflow;
        private readonly IRequestLogger _requestLogger;

        public NormalTvSeasonRequestingWorkflow(
            ITvShowSearcher searcher,
            ITvShowRequester requester,
            ITvShowUserInterface userInterface,
            ITvShowNotificationWorkflow tvShowNotificationWorkflow,
            IRequestLogger requestLogger)
        {
            _searcher = searcher;
            _requester = requester;
            _userInterface = userInterface;
            _tvShowNotificationWorkflow = tvShowNotificationWorkflow;
            _requestLogger = requestLogger;
        }

        public async Task HandleSelectionAsync(TvShowRequest request, TvShow tvShow, NormalTvSeason selectedSeason)
        {
            if (selectedSeason.IsRequested == RequestedState.Full)
            {
                await RequestNotificationsForSeasonAsync(request, tvShow, selectedSeason);
            }
            else
            {
                await _userInterface.DisplayTvShowDetailsForSeasonAsync(request, tvShow, selectedSeason);
            }
        }

        public async Task RequestAsync(TvShowRequest request, TvShow tvShow, NormalTvSeason selectedSeason)
        {
            var result = await _requester.RequestTvShowAsync(request, tvShow, selectedSeason);

            if (result.WasDenied)
            {
                await _userInterface.DisplayRequestDeniedForSeasonAsync(tvShow, selectedSeason);
                await _requestLogger.LogTvShowRequestAsync(request.User.UserId, request.User.Username, tvShow.Title, tvShow.TheTvDbId, $"Season {selectedSeason.SeasonNumber}", request.CategoryName, false, "Request denied");
            }
            else
            {
                await _userInterface.DisplayRequestSuccessForSeasonAsync(tvShow, selectedSeason);
                await _tvShowNotificationWorkflow.NotifyForNewRequestAsync(request.User.UserId, tvShow, selectedSeason);
                await _requestLogger.LogTvShowRequestAsync(request.User.UserId, request.User.Username, tvShow.Title, tvShow.TheTvDbId, $"Season {selectedSeason.SeasonNumber}", request.CategoryName, true);
            }
        }

        private async Task RequestNotificationsForSeasonAsync(TvShowRequest request, TvShow tvShow, TvSeason selectedSeason)
        {
            if (selectedSeason.IsAvailable)
            {
                await _userInterface.WarnSeasonAlreadyAvailableAsync(tvShow, selectedSeason);
            }
            else
            {
                await _tvShowNotificationWorkflow.NotifyForExistingRequestAsync(request.User.UserId, tvShow, selectedSeason);
            }
        }
    }
}