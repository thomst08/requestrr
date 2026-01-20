using System.Collections.Generic;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot
{
    public class QualityProfile
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public interface IQualityProfileProvider
    {
        Task<IReadOnlyList<QualityProfile>> GetQualityProfilesAsync();
    }

    public class NullQualityProfileProvider : IQualityProfileProvider
    {
        public Task<IReadOnlyList<QualityProfile>> GetQualityProfilesAsync()
        {
            return Task.FromResult<IReadOnlyList<QualityProfile>>(new List<QualityProfile>());
        }
    }
}
