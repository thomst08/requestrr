using System;
using System.ComponentModel.DataAnnotations;

namespace Requestrr.WebApi.Controllers.DownloadClients.Overseerr
{
    public class OverseerrSettingsModel
    {
        [Required]
        public string Hostname { get; set; }
        [Required]
        public int Port { get; set; }
        [Required]
        public string ApiKey { get; set; }
        [Required]
        public bool UseSSL { get; set; }

        [Required]
        public string Version { get; set; }

        public OverseerrInstanceModel[] Instances { get; set; } = Array.Empty<OverseerrInstanceModel>();
    }

    public class OverseerrInstanceModel
    {
        public int InstanceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public int Port { get; set; } = 5055;
        public bool UseSSL { get; set; } = false;
        public string ApiKey { get; set; } = string.Empty;
        public string Version { get; set; } = "1";
    }
}