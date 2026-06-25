using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Logging
{
    public interface IRequestLogger
    {
        Task LogMovieRequestAsync(string userId, string username, string movieTitle, int theMovieDbId, string category, bool success, string reason = null);
        Task LogTvShowRequestAsync(string userId, string username, string tvShowTitle, int tvDbId, string seasons, string category, bool success, string reason = null);
        Task LogMusicRequestAsync(string userId, string username, string artistName, string artistId, bool success, string reason = null);
        Task LogMovieIssueAsync(string userId, string username, string movieTitle, int theMovieDbId, string issueDescription);
        Task LogTvShowIssueAsync(string userId, string username, string tvShowTitle, int tvDbId, string issueDescription);
        Task LogMovieNotificationAsync(string userId, string username, string movieTitle, int theMovieDbId);
        Task LogTvShowNotificationAsync(string userId, string username, string tvShowTitle, int tvDbId, string seasons);
        Task LogMusicNotificationAsync(string userId, string username, string artistName, string artistId);
    }
}
