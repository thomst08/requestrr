using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Requestrr.WebApi.RequestrrBot.DownloadClients
{
    public static class RequesterTagHelper
    {
        private const string TagPrefix = "requestrr-";
        private static readonly ConcurrentDictionary<string, int> _tagCache = new ConcurrentDictionary<string, int>();

        public static string GetTagLabel(string username)
        {
            var sanitized = System.Text.RegularExpressions.Regex.Replace(username.ToLowerInvariant(), @"[^a-z0-9-]", "-");
            return $"{TagPrefix}{sanitized}";
        }

        public static async Task<int[]> GetTagsWithRequester<TTag>(
            int[] categoryTags,
            string username,
            string baseUrl,
            Func<Task<IList<TTag>>> getTags,
            Func<string, Task<TTag>> createTag,
            Func<TTag, string> getLabel,
            Func<TTag, int> getId,
            ILogger logger)
        {
            var label = GetTagLabel(username);
            var cacheKey = $"{baseUrl}:{label}";

            if (_tagCache.TryGetValue(cacheKey, out var cachedTagId))
            {
                return categoryTags.Concat(new[] { cachedTagId }).Distinct().ToArray();
            }

            var existingTags = await getTags();
            var matchingTag = existingTags.FirstOrDefault(t => string.Equals(getLabel(t), label, StringComparison.OrdinalIgnoreCase));

            int tagId;
            if (matchingTag != null)
            {
                tagId = getId(matchingTag);
            }
            else
            {
                var newTag = await createTag(label);
                tagId = getId(newTag);
            }

            _tagCache[cacheKey] = tagId;
            return categoryTags.Concat(new[] { tagId }).Distinct().ToArray();
        }
    }
}
