

using Requestrr.WebApi.RequestrrBot.Logging;
using Requestrr.WebApi.RequestrrBot.Movies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.TvShows
{
    public class TvShowIssueWorkflow
    {
        private readonly TvShowUserRequester _user;
        private readonly int _categoryId;
        private readonly string _categoryName;
        private readonly ITvShowSearcher _searcher;
        private readonly ITvShowRequester _requester;
        private readonly ITvShowUserInterface _userInterface;
        private readonly ITvShowNotificationWorkflow _tvShowNotificationWorkflow;
        private readonly TvShowsSettings _settings;
        private readonly IRequestLogger _requestLogger;

        public TvShowIssueWorkflow(
            TvShowUserRequester user,
            int categoryId,
            string categoryName,
            ITvShowSearcher searcher,
            ITvShowRequester requester,
            ITvShowUserInterface userInterface,
            ITvShowNotificationWorkflow tvShowNotificationWorkflow,
            TvShowsSettings settings,
            IRequestLogger requestLogger)
        {
            _user = user;
            _categoryId = categoryId;
            _categoryName = categoryName;
            _searcher = searcher;
            _requester = requester;
            _userInterface = userInterface;
            _tvShowNotificationWorkflow = tvShowNotificationWorkflow;
            _settings = settings;
            _requestLogger = requestLogger;
        }
        
        
        /// <summary>
        /// Searchs for Tv Shows in library and responces
        /// </summary>
        /// <param name="tvShowName">Name of the TV Show</param>
        /// <returns></returns>
        public async Task SearchTvShowLibraryAsync(string tvShowName)
        {
            var searchedTvShows = await SearchTvShowLibraryByNameAsync(tvShowName);

            if (searchedTvShows.Any())
            {
                if (searchedTvShows.Count > 1)
                {
                    await _userInterface.ShowTvShowIssueSelection(new TvShowRequest(_user, _categoryId, _categoryName), searchedTvShows);
                }
                else if (searchedTvShows.Count == 1)
                {
                    var selection = searchedTvShows.Single();
                    await HandleTvShowSelectionAsync(selection.TheTvDbId);
                }
            }
        }



        /// <summary>
        /// Handles the fetching of a TV show by TVDB ID in your library
        /// </summary>
        /// <param name="tvDbId"></param>
        /// <returns></returns>
        public async Task SearchTvShowLibraryAsync(int tvDbId)
        {
            try
            {
                //Check if searcher supports issues
                if (_searcher is not ITvShowIssueSearcher)
                {
                    await _userInterface.WarnNoTvShowFoundByTvDbIdAsync(tvDbId);
                    return;
                }

                var searchedTvShow = await ((ITvShowIssueSearcher)_searcher).SearchTvShowLibraryAsync(new TvShowRequest(_user, _categoryId, _categoryName), tvDbId);
                if (searchedTvShow == null)
                {
                    await _userInterface.WarnNoTvShowFoundByTvDbIdAsync(tvDbId);
                }
                else
                {
                    await HandleTvShowSelectionAsync(searchedTvShow.TheTvDbId);
                }
            }
            catch
            {
                await _userInterface.WarnNoTvShowFoundByTvDbIdAsync(tvDbId);
            }
        }



        /// <summary>
        /// This handles the checking of the TV Show in the library
        /// </summary>
        /// <param name="tvShowName"></param>
        /// <returns></returns>
        private async Task<IReadOnlyList<SearchedTvShow>> SearchTvShowLibraryByNameAsync(string tvShowName)
        {
            IReadOnlyList<SearchedTvShow> searchedTvShows = Array.Empty<SearchedTvShow>();
            
            //Check if searcher supports issues
            if (_searcher is not ITvShowIssueSearcher)
            {
                await _userInterface.WarnNoTvShowFoundAsync(tvShowName);
                return searchedTvShows;
            }

            tvShowName = tvShowName.Replace(".", " ");
            searchedTvShows = await ((ITvShowIssueSearcher)_searcher).SearchTvShowLibraryAsync(new TvShowRequest(_user, _categoryId, _categoryName), tvShowName);

            if (!searchedTvShows.Any())
            {
                await _userInterface.WarnNoTvShowFoundAsync(tvShowName);
            }

            return searchedTvShows;
        }
        
        
        
        
        public async Task HandleTvShowSelectionAsync(int tvDbId)
        {
            var tvShow = await GetTvShowAsync(tvDbId);
            await _userInterface.DisplayTvShowIssueDetailsAsync(new TvShowRequest(_user, _categoryId, _categoryName), tvShow, string.Empty, null, null);
        }
        
        
        
        private async Task<TvShow> GetTvShowAsync(int tvDbId)
        {
            var tvShow = await _searcher.GetTvShowDetailsAsync(new TvShowRequest(_user, _categoryId, _categoryName), tvDbId);

            if (tvShow.IsMultiSeasons())
            {
                if (_settings.Restrictions == TvShowsRestrictions.AllSeasons || tvShow.Seasons.Length <= 24)
                {
                    tvShow.Seasons = tvShow.Seasons.Prepend(new AllTvSeasons
                    {
                        SeasonNumber = 0,
                        IsAvailable = false,
                        IsRequested = RequestedState.None,
                    }).ToArray();
                }
                else
                {
                    tvShow.Seasons = tvShow.Seasons.TakeLast(25).ToArray();
                }
            }

            return tvShow;
        }



        /// <summary>
        /// Handle the converting of a TVDM ID to the title name 
        /// </summary>
        /// <param name="theTvDbId">TV Show TVDB Id</param>
        /// <param name="issue">String of issue</param>
        /// <param name="seasonNumber">Season number being reported</param>
        /// <param name="episodeNumber">Episode number being reported</param>
        /// <returns></returns>
        public async Task HandleIssueTvSelectionAsync(int theTvDbId, string issue = "", int? seasonNumber = null, int? episodeNumber = null)
        {
            var tvShow = await GetTvShowAsync(theTvDbId);
            tvShow = await EnsureSeasonEpisodesAsync(tvShow, seasonNumber);
            await HandleIssueTvSelectionAsync(tvShow, issue, seasonNumber, episodeNumber);
        }


        /// <summary>
        /// Handle responce to submit issues for a TV Show
        /// </summary>
        /// <param name="tvShow">TV Show media data</param>
        /// <param name="issue">String of issue</param>
        /// <param name="seasonNumber">Season number being reported</param>
        /// <param name="episodeNumber">Episode number being reported</param>
        /// <returns></returns>
        private async Task HandleIssueTvSelectionAsync(TvShow tvShow, string issue = "", int? seasonNumber = null, int? episodeNumber = null)
        {
            await _userInterface.DisplayTvShowIssueDetailsAsync(new TvShowRequest(_user, _categoryId, _categoryName), tvShow, issue, seasonNumber, episodeNumber);
        }


        /// <summary>
        /// Handles responce to Discord responce service to handle creating Modal
        /// </summary>
        /// <param name="theTvDbId">TV Show TVDB Id</param>
        /// <param name="issue">String of issue</param>
        /// <param name="seasonNumber">Season number being reported</param>
        /// <param name="episodeNumber">Episode number being reported</param>
        /// <returns></returns>
        public async Task HandleIssueTvShowSendModalAsync(int theTvDbId, string issue, int? seasonNumber = null, int? episodeNumber = null) //TvShow tvShow, string issue = "")
        {
            var tvShow = await GetTvShowAsync(theTvDbId);
            tvShow = await EnsureSeasonEpisodesAsync(tvShow, seasonNumber);
            await _userInterface.DisplayTvShowIssueModalAsync(
                new TvShowRequest(_user, _categoryId, _categoryName),
                tvShow,
                issue,
                seasonNumber,
                episodeNumber
            );
        }



        /// <summary>
        /// Submit an issue
        /// </summary>
        /// <param name="textBox">Holds the details sent back form the user</param>
        /// <returns></returns>
        public async Task SubmitIssueTvShowModalReadAsync(KeyValuePair<string, string> textBox)
        {
            string[] values = textBox.Key.Split("/");

            int tvShowId = int.Parse(values[3]);
            string issue = values[4];
            int seasonNumber = values.Length > 5 ? int.Parse(values[5]) : -1;
            int episodeNumber = values.Length > 6 ? int.Parse(values[6]) : -1;

            bool result = false;

            var request = new TvShowRequest(_user, _categoryId, _categoryName);
            var issueDescription = PrependIssueLocation(textBox.Value, seasonNumber, episodeNumber);

            //Check if searcher supports issues
            if (_searcher is ITvShowIssueRequester)
            {
                result = await ((ITvShowIssueRequester)_requester).SubmitTvShowIssueAsync(request, tvShowId, issue, issueDescription);
            }

            var tvShow = await _searcher.GetTvShowDetailsAsync(request, tvShowId);
            await _userInterface.CompleteTvShowIssueModalRequestAsync(tvShow, result);

            if (result)
            {
                await _requestLogger.LogTvShowIssueAsync(_user.UserId, _user.Username, tvShow.Title, tvShowId, issueDescription);
            }
        }

        private string PrependIssueLocation(string description, int seasonNumber, int episodeNumber)
        {
            if (seasonNumber <= -1 && episodeNumber <= -1)
            {
                return description;
            }

            string location;

            if (seasonNumber == 0)
            {
                location = "All Seasons";
            }
            else if (episodeNumber > 0)
            {
                location = $"Season {seasonNumber} Episode {episodeNumber}";
            }
            else
            {
                location = $"Season {seasonNumber}";
            }

            return $"{location}\n{description}";
        }

        private async Task<TvShow> EnsureSeasonEpisodesAsync(TvShow tvShow, int? seasonNumber)
        {
            if (tvShow == null || seasonNumber == null || seasonNumber <= 0)
            {
                return tvShow;
            }

            var season = tvShow.Seasons.OfType<NormalTvSeason>().FirstOrDefault(x => x.SeasonNumber == seasonNumber);
            if (season == null || (season.Episodes?.Any() ?? false))
            {
                return tvShow;
            }

            if (_searcher is DownloadClients.Overseerr.OverseerrClient overseerrClient)
            {
                season.Episodes = await overseerrClient.GetSeasonEpisodesAsync(tvShow.TheTvDbId, seasonNumber.Value);
            }

            return tvShow;
        }
    }
}
