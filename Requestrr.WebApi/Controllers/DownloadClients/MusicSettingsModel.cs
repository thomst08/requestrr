using Requestrr.WebApi.Controllers.DownloadClients.Lidarr;
using System.ComponentModel.DataAnnotations;

namespace Requestrr.WebApi.Controllers.DownloadClients
{
    public class MusicSettingsModel
    {
        [Required]
        public string Client { get; set; }

        [Required]
        public string Restrictions { get; set; }

        [Required]
        public LidarrSettingsModel Lidarr { get; set; }

        [Required]
        public string[] OtherCategories { get; set; }
    }
}
