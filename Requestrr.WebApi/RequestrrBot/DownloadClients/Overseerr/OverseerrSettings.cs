using System;
using System.Linq;

namespace Requestrr.WebApi.RequestrrBot.DownloadClients.Overseerr
{
    public class RadarrServiceSettings
    {
        public RadarrService[] RadarrServices { get; set; } = Array.Empty<RadarrService>();
    }

    public class RadarrService
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ServiceOption[] Profiles { get; set; } = Array.Empty<ServiceOption>();
        public ServiceOption[] RootPaths { get; set; } = Array.Empty<ServiceOption>();
        public ServiceOption[] Tags { get; set; } = Array.Empty<ServiceOption>();
    }

    public class SonarrServiceSettings
    {
        public SonarrService[] SonarrServices { get; set; } = Array.Empty<SonarrService>();
    }

    public class SonarrService
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ServiceOption[] Profiles { get; set; } = Array.Empty<ServiceOption>();
        public ServiceOption[]? LanguageProfiles { get; set; }
        public ServiceOption[] RootPaths { get; set; } = Array.Empty<ServiceOption>();
        public ServiceOption[] Tags { get; set; } = Array.Empty<ServiceOption>();
    }

    public class ServiceOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// Represents one extra Overseerr / Jellyseerr instance. Instance 0 is
    /// always the legacy top-level connection on <see cref="OverseerrSettings"/>.
    /// </summary>
    public class OverseerrInstance
    {
        public int InstanceId { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public int Port { get; set; } = 5055;
        public bool UseSSL { get; set; } = false;
        public string ApiKey { get; set; } = string.Empty;
        public string Version { get; set; } = "1";
    }

    public class OverseerrSettings
    {
        public string Hostname { get; set; } = string.Empty;
        public int Port { get; set; } = 5055;
        public bool UseSSL { get; set; } = false;

        public bool UseMovieIssue { get; set; } = false;
        public bool UseTVIssue { get; set; } = false;

        public string ApiKey { get; set; } = string.Empty;
        public OverseerrMovieSettings Movies { get; set; } = new OverseerrMovieSettings();
        public OverseerrTvShowSettings TvShows { get; set; } = new OverseerrTvShowSettings();
        public string Version { get; set; } = "1";

        /// <summary>
        /// Additional Overseerr/Jellyseerr instances. The top-level
        /// connection fields are always treated as instance 0; everything
        /// in <see cref="Instances"/> must have InstanceId &gt;= 1.
        /// </summary>
        public OverseerrInstance[] Instances { get; set; } = Array.Empty<OverseerrInstance>();

        /// <summary>
        /// Resolve an instance by id. Falls back to instance 0 (the legacy
        /// top-level fields) when the id is 0 or unknown.
        /// </summary>
        public OverseerrInstance GetInstance(int instanceId)
        {
            if (instanceId > 0 && Instances != null)
            {
                var extra = Instances.FirstOrDefault(i => i.InstanceId == instanceId);
                if (extra != null)
                {
                    return extra;
                }
            }

            return new OverseerrInstance
            {
                InstanceId = 0,
                Name = "Default",
                Hostname = Hostname,
                Port = Port,
                UseSSL = UseSSL,
                ApiKey = ApiKey,
                Version = Version,
            };
        }
    }

    public class OverseerrMovieSettings
    {
        public string DefaultApiUserId { get; set; }
        public OverseerrMovieCategory[] Categories { get; set; } = Array.Empty<OverseerrMovieCategory>();
    }

    public class OverseerrMovieCategory
    {
        public int Id { get; set; } = -1;
        public bool Is4K { get; set; } = false;
        public string Name { get; set; } = string.Empty;
        public int ServiceId { get; set; } = -1;
        public int ProfileId { get; set; } = -1;
        public string RootFolder { get; set; } = string.Empty;
        public int[] Tags { get; set; } = Array.Empty<int>();
        public int InstanceId { get; set; } = 0;
    }

    public class OverseerrTvShowSettings
    {
        public string DefaultApiUserId { get; set; }
        public OverseerrTvShowCategory[] Categories { get; set; } = Array.Empty<OverseerrTvShowCategory>();
    }

    public class OverseerrTvShowCategory
    {
        public int Id { get; set; } = -1;
        public bool Is4K { get; set; } = false;
        public string Name { get; set; } = string.Empty;
        public int ServiceId { get; set; } = -1;
        public int ProfileId { get; set; } = -1;
        public int LanguageProfileId { get; set; } = -1;
        public string RootFolder { get; set; } = string.Empty;
        public int[] Tags { get; set; } = Array.Empty<int>();
        public int InstanceId { get; set; } = 0;
    }
}
