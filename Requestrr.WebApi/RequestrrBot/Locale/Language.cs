using Newtonsoft.Json;

namespace Requestrr.WebApi.RequestrrBot.Locale
{
    public static class LanguageTokens
    {
        public static string TvShowTitle = "[TvShowTitle]";
        public static string TvShowTVDBID = "[TvShowTVDBID]";
        public static string SeasonNumber = "[SeasonNumber]";
        public static string MovieTitle = "[MovieTitle]";
        public static string MusicArtistName = "[MusicArtistName]";
        public static string AuthorUsername = "[AuthorUsername]";
        public static string BotUsername = "[BotUsername]";
        public static string MovieTMDB = "[MovieTMDB]";
        public static string CommandPrefix = "[CommandPrefix]";

        public static string IssueLabel = "[Issue]";
        public static string IssueTitle = "[Title]";
        public static string IssueUsername = "[Username]";
        public static string RequestStatus = "[RequestStatus]";
        public static string ApproveEmoji = "[ApproveEmoji]";
        public static string DenyEmoji = "[DenyEmoji]";

        public static string FullCommandList = "[FullCommandList]";

        public static string MovieCommandTitle = "[MovieCommandTitle]";
        public static string MovieCommandIssue = "[MovieCommandIssue]";

        public static string IssueEnabledStart = "[IssueEnabledStart]";
        public static string IssueEnabledEnd = "[IssueEnabledEnd]";
    }

    public class Language
    {
        public static Language Current = null;

#if DEBUG
        public static string BuildVersion = "Development";
#else
        public static string BuildVersion = "v2.1.9";
#endif

        public string Error { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Help.Dropdown")]
        public string DiscordCommandMovieRequestHelpDropdown { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Help.Search.Dropdown")]
        public string DiscordCommandTvRequestHelpSearchDropdown { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Help.Seasons.Dropdown")]
        public string DiscordCommandTvRequestHelpSeasonsDropdown { get; set; }

        [JsonProperty("Discord.Command.Tv.Issue.Help.Episodes.Dropdown")]
        public string DiscordCommandTvIssueHelpEpisodesDropdown { get; set; }

        [JsonProperty("Discord.Command.Tv.Issue.Help.Episodes.FullSeason")]
        public string DiscordCommandTvIssueHelpEpisodesFullSeason { get; set; }

        [JsonProperty("Discord.Command.MissingRoles")]
        public string DiscordCommandMissingRoles { get; set; }

        [JsonProperty("Discord.Command.NotAvailableInChannel")]
        public string DiscordCommandNotAvailableInChannel { get; set; }

        [JsonProperty("Discord.Command.UnknownPrecondition")]
        public string DiscordCommandUnknownPrecondition { get; set; }

        [JsonProperty("Discord.Command.Sending")]
        public string DiscordCommandSending { get; set; }

        [JsonProperty("Discord.Command.Request.Group.Name")]
        public string DiscordCommandRequestGroupName { get; set; }

        [JsonProperty("Discord.Command.Request.Group.Description")]
        public string DiscordCommandRequestGroupDescription { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Title.Name")]
        public string DiscordCommandMovieRequestTitleName { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Title.Description")]
        public string DiscordCommandMovieRequestTitleDescription { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Title.Option.Name")]
        public string DiscordCommandMovieRequestTitleOptionName { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Title.Option.Description")]
        public string DiscordCommandMovieRequestTitleOptionDescription { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Tmbd.Name")]
        public string DiscordCommandMovieRequestTmbdName { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Tmbd.Description")]
        public string DiscordCommandMovieRequestTmbdDescription { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Tmbd.Option.Name")]
        public string DiscordCommandMovieRequestTmbdOptionName { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Tmbd.Option.Description")]
        public string DiscordCommandMovieRequestTmbdOptionDescription { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Title.Name")]
        public string DiscordCommandTvRequestTitleName { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Title.Description")]
        public string DiscordCommandTvRequestTitleDescription { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Title.Option.Name")]
        public string DiscordCommandTvRequestTitleOptionName { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Title.Option.Description")]
        public string DiscordCommandTvRequestTitleOptionDescription { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Tvdb.Name")]
        public string DiscordCommandTvRequestTvdbName { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Tvdb.Description")]
        public string DiscordCommandTvRequestTvdbDescription { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Tvdb.Option.Name")]
        public string DiscordCommandTvRequestTvdbOptionName { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Tvdb.Option.Description")]
        public string DiscordCommandTvRequestTvdbOptionDescription { get; set; }

        [JsonProperty("Discord.Command.Ping.Request.Name")]
        public string DiscordCommandPingRequestName { get; set; }

        [JsonProperty("Discord.Command.Ping.Request.Description")]
        public string DiscordCommandPingRequestDescription { get; set; }

        [JsonProperty("Discord.Command.Help.Request.Name")]
        public string DiscordCommandHelpRequestName { get; set; }

        [JsonProperty("Discord.Command.Help.Request.Description")]
        public string DiscordCommandHelpRequestDescription { get; set; }

        [JsonProperty("Discord.Notification.Tv.Channel.Season")]
        public string DiscordNotificationTvChannelSeason { get; set; }

        [JsonProperty("Discord.Notification.Tv.ChannelFirstEpisode")]
        public string DiscordNotificationTvChannelFirstEpisode { get; set; }

        [JsonProperty("Discord.Notification.Tv.DM.Season")]
        public string DiscordNotificationTvDMSeason { get; set; }

        [JsonProperty("Discord.Notification.Tv.DM.FirstEpisode")]
        public string DiscordNotificationTvDMFirstEpisode { get; set; }

        [JsonProperty("Discord.Notification.Movie.Channel")]
        public string DiscordNotificationMovieChannel { get; set; }

        [JsonProperty("Discord.Notification.Movie.DM")]
        public string DiscordNotificationMovieDM { get; set; }

        [JsonProperty("Discord.Command.Ping.Response")]
        public string DiscordCommandPingResponse { get; set; }

        [JsonProperty("Discord.Command.Help")]
        public string DiscordCommandHelp { get; set; }

        [JsonProperty("Discord.Command.Help.Message")]
        public string DiscordCommandHelpMessage { get; set; }

        [JsonProperty("Discord.Command.Movie.NotFound")]
        public string DiscordCommandMovieNotFound { get; set; }

        [JsonProperty("Discord.Command.Movie.NotFoundTMDB")]
        public string DiscordCommandMovieNotFoundTMDB { get; set; }

        [JsonProperty("Discord.Command.Movie.AlreadyAvailable")]
        public string DiscordCommandMovieAlreadyAvailable { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Success")]
        public string DiscordCommandMovieRequestSuccess { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Pending")]
        public string DiscordCommandMovieRequestPending { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Pending.Silent")]
        public string DiscordCommandMovieRequestPendingSilent { get; set; }
        [JsonProperty("Discord.Command.RequestButtonDenied")]
        public string DiscordCommandRequestButtonDenied { get; set; }

        [JsonProperty("Discord.Command.RequestButtonSuccess")]
        public string DiscordCommandRequestButtonSuccess { get; set; }

        [JsonProperty("Discord.Command.NotifyMe")]
        public string DiscordCommandNotifyMe { get; set; }

        [JsonProperty("Discord.Command.NotifyMeSuccess")]
        public string DiscordCommandNotifyMeSuccess { get; set; }

        [JsonProperty("Discord.Command.RequestedButton")]
        public string DiscordCommandRequestedButton { get; set; }

        [JsonProperty("Discord.Command.AvailableButton")]
        public string DiscordCommandAvailableButton { get; set; }

        [JsonProperty("Discord.Command.RequestButton")]
        public string DiscordCommandRequestButton { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Help")]
        public string DiscordCommandMovieRequestHelp { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Confirm")]
        public string DiscordCommandMovieRequestConfirm { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.AlreadyExist")]
        public string DiscordCommandMovieRequestAlreadyExist { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.AlreadyExistNotified")]
        public string DiscordCommandMovieRequestAlreadyExistNotified { get; set; }

        [JsonProperty("Discord.Command.Movie.Request.Denied")]
        public string DiscordCommandMovieRequestDenied { get; set; }

        [JsonProperty("Discord.Command.Movie.NotReleased")]
        public string DiscordCommandMovieNotReleased { get; set; }

        [JsonProperty("Discord.Command.Movie.Notification.Success")]
        public string DiscordCommandMovieNotificationSuccess { get; set; }

        [JsonProperty("Discord.Command.Movie.Notification.Request")]
        public string DiscordCommandMovieNotificationRequest { get; set; }

        [JsonProperty("Discord.Command.Movie.CancelCommand")]
        public string DiscordCommandMovieCancelCommand { get; set; }

        [JsonProperty("Discord.Embed.Movie.Search")]
        public string DiscordEmbedMovieSearch { get; set; }

        [JsonProperty("Discord.Embed.Movie.SearchResult")]
        public string DiscordEmbedMovieSearchResult { get; set; }

        [JsonProperty("Discord.Embed.Movie.Quality")]
        public string DiscordEmbedMovieQuality { get; set; }

        [JsonProperty("Discord.Embed.Movie.InTheaters")]
        public string DiscordEmbedMovieInTheaters { get; set; }

        [JsonProperty("Discord.Embed.Movie.Release")]
        public string DiscordEmbedMovieRelease { get; set; }

        [JsonProperty("Discord.Embed.Movie.WatchNow")]
        public string DiscordEmbedMovieWatchNow { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Help.Search")]
        public string DiscordCommandTvRequestHelpSearch { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Help.Seasons")]
        public string DiscordCommandTvRequestHelpSeasons { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Confirm.Season")]
        public string DiscordCommandTvRequestConfirmSeason { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Confirm.FutureSeasons")]
        public string DiscordCommandTvRequestConfirmFutureSeasons { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Confirm.AllSeasons")]
        public string DiscordCommandTvRequestConfirmAllSeasons { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Success.Season")]
        public string DiscordCommandTvRequestSuccessSeason { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Success.FutureSeasons")]
        public string DiscordCommandTvRequestSuccessFutureSeasons { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Success.AllSeasons")]
        public string DiscordCommandTvRequestSuccessAllSeasons { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Pending.Season")]
        public string DiscordCommandTvRequestPendingSeason { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Pending.FutureSeasons")]
        public string DiscordCommandTvRequestPendingFutureSeasons { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Pending.AllSeasons")]
        public string DiscordCommandTvRequestPendingAllSeasons { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Pending.Season.Silent")]
        public string DiscordCommandTvRequestPendingSeasonSilent { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Pending.FutureSeasons.Silent")]
        public string DiscordCommandTvRequestPendingFutureSeasonsSilent { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Pending.AllSeasons.Silent")]
        public string DiscordCommandTvRequestPendingAllSeasonsSilent { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Unsupported")]
        public string DiscordCommandTvRequestUnsupported { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyAvailable.Series")]
        public string DiscordCommandTvRequestAlreadyAvailableSeries { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyAvailable.Season")]
        public string DiscordCommandTvRequestAlreadyAvailableSeason { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyExist.Series")]
        public string DiscordCommandTvRequestAlreadyExistSeries { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyExist.Season")]
        public string DiscordCommandTvRequestAlreadyExistSeason { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyExist.FutureSeason.Found")]
        public string DiscordCommandTvRequestAlreadyExistFutureSeasonFound { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyExist.FutureSeason.Requested")]
        public string DiscordCommandTvRequestAlreadyExistFutureSeasonRequested { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyExist.FutureSeason.Available")]
        public string DiscordCommandTvRequestAlreadyExistFutureSeasonAvailable { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyExistNotified.Season")]
        public string DiscordCommandTvRequestAlreadyExistNotifiedSeason { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyExistNotified.FutureSeason.Found")]
        public string DiscordCommandTvRequestAlreadyExistNotifiedFutureSeasonFound { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyExistNotified.FutureSeason.Requested")]
        public string DiscordCommandTvRequestAlreadyExistNotifiedFutureSeasonRequested { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.AlreadyExistNotified.FutureSeason.Available")]
        public string DiscordCommandTvRequestAlreadyExistNotifiedFutureSeasonAvailable { get; set; }

        [JsonProperty("Discord.Command.Tv.Request.Denied")]
        public string DiscordCommandTvRequestDenied { get; set; }

        [JsonProperty("Discord.Command.Tv.NotFoundTVDBID")]
        public string DiscordCommandTvNotFoundTVDBID { get; set; }

        [JsonProperty("Discord.Command.Tv.NotFound")]
        public string DiscordCommandTvNotFound { get; set; }

        [JsonProperty("Discord.Command.Tv.Notification.Request.Season")]
        public string DiscordCommandTvNotificationRequestSeason { get; set; }

        [JsonProperty("Discord.Command.Tv.Notification.Request.FutureSeason.Missing")]
        public string DiscordCommandTvNotificationRequestFutureSeasonMissing { get; set; }

        [JsonProperty("Discord.Command.Tv.Notification.Request.FutureSeason.Requested")]
        public string DiscordCommandTvNotificationRequestFutureSeasonRequested { get; set; }

        [JsonProperty("Discord.Command.Tv.Notification.Request.FutureSeason.Available")]
        public string DiscordCommandTvNotificationRequestFutureSeasonAvailable { get; set; }

        [JsonProperty("Discord.Command.Tv.Notification.Success.FutureSeasons")]
        public string DiscordCommandTvNotificationSuccessFutureSeasons { get; set; }

        [JsonProperty("Discord.Command.Tv.Notification.Success.Season")]
        public string DiscordCommandTvNotificationSuccessSeason { get; set; }

        [JsonProperty("Discord.Command.Tv.CancelCommand")]
        public string DiscordCommandTvCancelCommand { get; set; }

        [JsonProperty("Discord.Embed.Tv.Search")]
        public string DiscordEmbedTvSearch { get; set; }

        [JsonProperty("Discord.Embed.Tv.SearchResult")]
        public string DiscordEmbedTvSearchResult { get; set; }

        [JsonProperty("Discord.Embed.Tv.Network")]
        public string DiscordEmbedTvNetwork { get; set; }

        [JsonProperty("Discord.Embed.Tv.Status")]
        public string DiscordEmbedTvStatus { get; set; }

        [JsonProperty("Discord.Embed.Tv.Quality")]
        public string DiscordEmbedTvQuality { get; set; }

        [JsonProperty("Discord.Embed.Tv.AllSeasons")]
        public string DiscordEmbedTvAllSeasons { get; set; }

        [JsonProperty("Discord.Embed.Tv.FutureSeasons")]
        public string DiscordEmbedTvFutureSeasons { get; set; }

        [JsonProperty("Discord.Embed.Tv.Season")]
        public string DiscordEmbedTvSeason { get; set; }

        [JsonProperty("Discord.Embed.Tv.FullyRequested")]
        public string DiscordEmbedTvFullyRequested { get; set; }

        [JsonProperty("Discord.Embed.Tv.PartiallyRequested")]
        public string DiscordEmbedTvPartiallyRequested { get; set; }

        [JsonProperty("Discord.Embed.Tv.Seasons")]
        public string DiscordEmbedTvSeasons { get; set; }

        [JsonProperty("Discord.Embed.Tv.WatchNow")]
        public string DiscordEmbedTvWatchNow { get; set; }



        [JsonProperty("Discord.Command.Movie.Issue.Select")]
        public string DiscordCommandMovieIssueSelect { get; set; }

        [JsonProperty("Discord.Command.Issue.CreatedBy")]
        public string DiscordCommandIssueCreatedBy { get; set; }

        [JsonProperty("Discord.Command.Issue.Help.Dropdown")]
        public string DiscordCommandIssueHelpDropdown { get; set; }

        [JsonProperty("Discord.Command.IssueButton")]
        public string DiscordCommandIssueButton { get; set; }

        [JsonProperty("Discord.Command.Movie.Issue.Success")]
        public string DiscordCommandMovieIssueSuccess { get; set; }

        [JsonProperty("Discord.Command.Movie.Issue.Failed")]
        public string DiscordCommandMovieIssueFailed { get; set; }



        [JsonProperty("Discord.Command.Tv.Issue.Select")]
        public string DiscordCommandTvIssueSelect { get; set; }

        [JsonProperty("Discord.Command.Tv.Issue.Success")]
        public string DiscordCommandTvIssueSuccess { get; set; }

        [JsonProperty("Discord.Command.Tv.Issue.Failed")]
        public string DiscordCommandTvIssueFailed { get; set; }



        [JsonProperty("Discord.Command.Issue.Name")]
        public string DiscordCommandIssueName { get; set; }
        [JsonProperty("Discord.Command.Issue.Description")]
        public string DiscordCommandIssueDescription { get; set; }

        [JsonProperty("Discord.Command.Issue.Interaction.Title")]
        public string DiscordCommandIssueInteractionTitle { get; set; }

        [JsonProperty("Discord.Command.Issue.Interaction.Label")]
        public string DiscordCommandIssueInteractionLabel { get; set; }

        [JsonProperty("Discord.Command.Issue.Interaction.Placeholder")]
        public string DiscordCommandIssueInteractionPlaceholder { get; set; }


        [JsonProperty("Discord.Command.Movie.Issue.Title.Name")]
        public string DiscordCommandMovieIssueTitleName { get; set; }
        [JsonProperty("Discord.Command.Movie.Issue.Title.Description")]
        public string DiscordCommandMovieIssueTitleDescription { get; set; }
        [JsonProperty("Discord.Command.Movie.Issue.Title.Option.Name")]
        public string DiscordCommandMovieIssueTitleOptionName { get; set; }
        [JsonProperty("Discord.Command.Movie.Issue.Title.Option.Description")]
        public string DiscordCommandMovieIssueTitleOptionDescription { get; set; }
        [JsonProperty("Discord.Command.Movie.Issue.Tmdb.Name")]
        public string DiscordCommandMovieIssueTmdbName { get; set; }
        [JsonProperty("Discord.Command.Movie.Issue.Tmdb.Description")]
        public string DiscordCommandMovieIssueTmdbDescription { get; set; }
        [JsonProperty("Discord.Command.Movie.Issue.Tmdb.Option.Name")]
        public string DiscordCommandMovieIssueTmdbOptionName { get; set; }
        [JsonProperty("Discord.Command.Movie.Issue.Tmdb.Option.Description")]
        public string DiscordCommandMovieIssueTmdbOptionDescription { get; set; }


        [JsonProperty("Discord.Command.Tv.Issue.Title.Name")]
        public string DiscordCommandTvIssueTitleName { get; set; }
        [JsonProperty("Discord.Command.Tv.Issue.Title.Description")]
        public string DiscordCommandTvIssueTitleDescription { get; set; }
        [JsonProperty("Discord.Command.Tv.Issue.Title.Option.Name")]
        public string DiscordCommandTvIssueTitleOptionName { get; set; }
        [JsonProperty("Discord.Command.Tv.Issue.Title.Option.Description")]
        public string DiscordCommandTvIssueTitleOptionDescription { get; set; }
        [JsonProperty("Discord.Command.Tv.Issue.Tvdb.Name")]
        public string DiscordCommandTvIssueTvdbName { get; set; }
        [JsonProperty("Discord.Command.Tv.Issue.Tvdb.Description")]
        public string DiscordCommandTvIssueTvdbDescription { get; set; }
        [JsonProperty("Discord.Command.Tv.Issue.Tvdb.Option.Name")]
        public string DiscordCommandTvIssueTvdbOptionName { get; set; }
        [JsonProperty("Discord.Command.Tv.Issue.Tvdb.Option.Description")]
        public string DiscordCommandTvIssueTvdbOptionDescription { get; set; }




        [JsonProperty("Discord.Command.Music.Request.Artist.Description")]
        public string DiscordCommandMusicRequestArtistDescription { get; set; }

        [JsonProperty("Discord.Command.Music.Request.Artist.Option.Name")]
        public string DiscordCommandMusicRequestArtistOptionName { get; set; }

        [JsonProperty("Discord.Command.Music.Request.Artist.Option.Description")]
        public string DiscordCommandMusicRequestArtistOptionDescription { get; set; }

        [JsonProperty("Discord.Notification.Music.Artist.Channel")]
        public string DiscordNotificationMusicArtistChannel { get; set; }

        [JsonProperty("Discord.Notification.Music.Artist.DM")]
        public string DiscordNotificationMusicArtistDM { get; set; }

        [JsonProperty("Discord.Command.Music.Artist.Request.Help")]
        public string DiscordCommandMusicArtistRequestHelp { get; set; }

        [JsonProperty("Discord.Command.Music.Artist.Request.Help.Dropdown")]
        public string DiscordCommandMusicArtistRequestHelpDropdown { get; set; }

        [JsonProperty("Discord.Command.Music.Artist.Request.Confirm")]
        public string DiscordCommandMusicArtistRequestConfirm { get; set; }

        [JsonProperty("Discord.Command.Music.Artist.AlreadyAvailable")]
        public string DiscordCommandMusicArtistAlreadyAvailable { get; set; }

        [JsonProperty("Discord.Command.Music.Artist.NotFound")]
        public string DiscordCommandMusicArtistNotFound { get; set; }

        [JsonProperty("Discord.Embed.Music.Quality")]
        public string DiscordEmbedMusicQuality { get; set; }

        [JsonProperty("Discord.Embed.Music.ListenNow")]
        public string DiscordEmbedMusicListenNow { get; set; }

        [JsonProperty("Discord.Command.Music.Artist.Request.Success")]
        public string DiscordCommandMusicArtistRequestSuccess { get; set; }

        [JsonProperty("Discord.Command.Music.Artist.Request.Denied")]
        public string DiscordCommandMusicArtistRequestDenied { get; set; }

        [JsonProperty("Discord.Command.Music.Artist.Request.AlreadyExistNotified")]
        public string DiscordCommandMusicArtistRequestAlreadyExistNotified { get; set; }

        [JsonProperty("Discord.Command.Music.Artist.Notification.Request")]
        public string DiscordCommandMusicArtistNotificationRequest { get; set; }

        [JsonProperty("Discord.Command.Music.Artist.Notification.Success")]
        public string DiscordCommandMusicArtistNotificationSuccess { get; set; }

        [JsonProperty("Discord.Command.Request.Approved")]
        public string DiscordCommandRequestApproved { get; set; }

        [JsonProperty("Discord.Command.Request.Denied")]
        public string DiscordCommandRequestDenied { get; set; }

        [JsonProperty("Discord.Command.Request.Pending.Admin")]
        public string DiscordCommandRequestPendingAdmin { get; set; }

        [JsonProperty("Discord.Command.Request.Admin.Summary")]
        public string DiscordCommandRequestAdminSummary { get; set; }
    }
}
