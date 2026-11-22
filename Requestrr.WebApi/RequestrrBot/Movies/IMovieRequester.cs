using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Movies
{
    public interface IMovieRequester
    {
        Task<MovieRequestResult> RequestMovieAsync(MovieRequest request, Movie movie);
    }

    public class MovieRequestResult
    {
        public bool WasDenied { get; set; }
        public bool IsPending { get; set; }
        public int? RequestId { get; set; }
    }
}
