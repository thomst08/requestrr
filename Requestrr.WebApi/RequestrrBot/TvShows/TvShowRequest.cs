namespace Requestrr.WebApi.RequestrrBot.TvShows
{
    public class TvShowRequest
    {
        public int CategoryId { get; }
        public string CategoryName { get; }
        public TvShowUserRequester User { get; }

        public TvShowRequest(TvShowUserRequester user, int categoryId, string categoryName)
        {
            User = user;
            CategoryId = categoryId;
            CategoryName = categoryName;
        }
    }
}