using System;

namespace Requestrr.WebApi.RequestrrBot.Music
{
    public class MusicAlbum
    {
        public string DownloadClientAlbumId { get; set; }
        public string AlbumId { get; set; }
        public string AlbumTitle { get; set; }
        public string Overview { get; set; }

        public string ArtistId { get; set; }
        public string ArtistName { get; set; }

        public DateTime? ReleaseDate { get; set; }
        public string ReleaseType { get; set; }

        public bool Available { get; set; }
        public bool Monitored { get; set; }
        public bool Requested { get; set; }

        public string PosterPath { get; set; }
    }
}
