using System;
using System.ComponentModel.DataAnnotations;

namespace Requestrr.WebApi.Controllers.DownloadClients.Lidarr
{
    public class LidarrSettingsModel : TestLidarrSettingsModel
    {
        [Required]
        public LidarrSettingsCategory[] Categories { get; set; } = Array.Empty<LidarrSettingsCategory>();

        public bool SearchNewRequests { get; set; }
        public bool MonitorNewRequests { get; set; }
        public bool AllowBulkAlbumRequests { get; set; } = true;
        public string Restrictions { get; set; }
    }

    public class LidarrSettingsCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ProfileId { get; set; }
        public int MetadataProfileId { get; set; }
        public string RootFolder { get; set; }
        public int[] Tags { get; set; } = Array.Empty<int>();
        // Legacy field kept for backward compatibility with older settings files.
        public string[] ReleaseTypes { get; set; } = Array.Empty<string>();
        public string[] PrimaryTypes { get; set; } = Array.Empty<string>();
        public string[] SecondaryTypes { get; set; } = Array.Empty<string>();
        public string[] ReleaseStatuses { get; set; } = Array.Empty<string>();
    }
}
