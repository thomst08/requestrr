namespace Requestrr.WebApi.RequestrrBot.TvShows
{
    public class TvShowRequest
    {
        public int CategoryId { get; }
        public TvShowUserRequester User { get; }
        public int? QualityProfileId { get; set; }
        public string QualityProfileName { get; set; }

        public TvShowRequest(TvShowUserRequester user, int categoryId)
        {
            User = user;
            CategoryId = categoryId;
        }
    }
}