using System.Threading.Tasks;
using Requestrr.WebApi.RequestrrBot.Logging;

namespace Requestrr.WebApi.RequestrrBot.TvShows.SeasonsRequestWorkflows
{
    public class FutureSeasonsRequestingWorkflow
    {
        private readonly ITvShowSearcher _searcher;
        private readonly ITvShowRequester _requester;
        private readonly ITvShowUserInterface _userInterface;
        private readonly ITvShowNotificationWorkflow _tvShowNotificationWorkflow;
        private readonly IRequestLogger _requestLogger;

        public FutureSeasonsRequestingWorkflow(
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

        public async Task HandleSelectionAsync(TvShowRequest request, TvShow tvShow, FutureTvSeasons selectedSeason)
        {
            if (tvShow.IsRequested)
            {
                await _tvShowNotificationWorkflow.NotifyForExistingRequestAsync(request.User.UserId, tvShow, selectedSeason);
            }
            else
            {
                await _userInterface.DisplayTvShowDetailsForSeasonAsync(request, tvShow, selectedSeason);
            }
        }

        public async Task RequestAsync(TvShowRequest request, TvShow tvShow, FutureTvSeasons selectedSeason)
        {
            var result = await _requester.RequestTvShowAsync(request, tvShow, selectedSeason);

            if (result.WasDenied)
            {
                await _userInterface.DisplayRequestDeniedForSeasonAsync(tvShow, selectedSeason);
                await _requestLogger.LogTvShowRequestAsync(request.User.UserId, request.User.Username, tvShow.Title, tvShow.TheTvDbId, "Future Seasons", request.CategoryName, false, "Request denied");
            }
            else
            {
                await _userInterface.DisplayRequestSuccessForSeasonAsync(tvShow, selectedSeason);
                await _tvShowNotificationWorkflow.NotifyForNewRequestAsync(request.User.UserId, tvShow, new FutureTvSeasons());
                await _requestLogger.LogTvShowRequestAsync(request.User.UserId, request.User.Username, tvShow.Title, tvShow.TheTvDbId, "Future Seasons", request.CategoryName, true);
            }
        }
    }
}