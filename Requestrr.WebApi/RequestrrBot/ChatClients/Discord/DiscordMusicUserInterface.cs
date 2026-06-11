using DSharpPlus;
using DSharpPlus.Entities;
using Requestrr.WebApi.RequestrrBot.Locale;
using Requestrr.WebApi.RequestrrBot.Music;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Requestrr.WebApi.RequestrrBot.ChatClients.Discord
{
    public class DiscordMusicUserInterface : IMusicUserInterface
    {
        private readonly DiscordInteraction _interactionContext;
        private readonly IMusicSearcher _musicSearcher;
        private readonly string _restrictions;

        public DiscordMusicUserInterface(
            DiscordInteraction interactionContext,
            IMusicSearcher musicSearcher,
            string restrictions)
        {
            _interactionContext = interactionContext;
            _musicSearcher = musicSearcher;
            _restrictions = string.IsNullOrWhiteSpace(restrictions) ? MusicRestrictions.None : restrictions;
        }


        public async Task ShowMusicArtistSelection(MusicRequest request, IReadOnlyList<MusicArtist> music)
        {
            List<DiscordSelectComponentOption> options = music.Take(15).Select(x => new DiscordSelectComponentOption(GetFormattedMusicArtistName(x), $"{request.CategoryId}/{x.ArtistId}")).ToList();
            DiscordSelectComponent select = new DiscordSelectComponent($"MuRSA/{_interactionContext.User.Id}/{request.CategoryId}", LimitStringSize(Language.Current.DiscordCommandMusicArtistRequestHelpDropdown), options);

            await _interactionContext.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddComponents(select).WithContent(Language.Current.DiscordCommandMusicArtistRequestHelp));
        }

        public async Task ShowMusicAlbumSelection(MusicRequest request, MusicArtist musicArtist, IReadOnlyList<MusicAlbum> albums, int page, string selectedReleaseType = null)
        {
            var availableReleaseTypes = albums
                .Select(x => x.ReleaseType)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .OrderBy(x => GetReleaseTypeSortOrder(x))
                .ToArray();

            if (!availableReleaseTypes.Any())
                availableReleaseTypes = new[] { "Album" };

            string effectiveReleaseType = string.IsNullOrWhiteSpace(selectedReleaseType) ? "Album" : selectedReleaseType;
            if (!availableReleaseTypes.Contains(effectiveReleaseType, StringComparer.InvariantCultureIgnoreCase))
                effectiveReleaseType = availableReleaseTypes.First();

            var releaseTypeOptions = availableReleaseTypes
                .Select(x => new DiscordSelectComponentOption(x, $"{request.CategoryId}/{musicArtist.ArtistId}/{x}", null, x.Equals(effectiveReleaseType, StringComparison.InvariantCultureIgnoreCase)))
                .ToList();

            DiscordSelectComponent releaseTypeSelect = new DiscordSelectComponent(
                $"MuRLT/{_interactionContext.User.Id}/{request.CategoryId}/{musicArtist.ArtistId}",
                effectiveReleaseType,
                releaseTypeOptions);

            var filteredAlbums = albums
                .Where(x => string.Equals(x.ReleaseType, effectiveReleaseType, StringComparison.InvariantCultureIgnoreCase))
                .ToArray();

            const int pageSize = 15;
            int totalPages = (int)Math.Ceiling(filteredAlbums.Length / (double)pageSize);
            int currentPage = Math.Max(0, Math.Min(page, Math.Max(totalPages - 1, 0)));
            string allLabel = GetAllReleaseTypeLabel(effectiveReleaseType);

            var options = new List<DiscordSelectComponentOption>();

            if (CanRequestAllAlbumsForReleaseType(effectiveReleaseType))
            {
                options.Add(new DiscordSelectComponentOption(allLabel, $"{request.CategoryId}/{musicArtist.ArtistId}/all/{effectiveReleaseType}", null, true));
            }

            options.AddRange(filteredAlbums
                .Skip(currentPage * pageSize)
                .Take(pageSize)
                .Select(x => new DiscordSelectComponentOption(GetFormattedMusicAlbumName(x), $"{request.CategoryId}/{musicArtist.ArtistId}/{x.AlbumId}/{effectiveReleaseType}")));

            string placeholder = totalPages > 1
                ? $"{Language.Current.DiscordCommandMusicAlbumRequestHelpDropdown} ({currentPage + 1}/{totalPages})"
                : Language.Current.DiscordCommandMusicAlbumRequestHelpDropdown;

            DiscordSelectComponent select = new DiscordSelectComponent($"MuRLA/{_interactionContext.User.Id}/{request.CategoryId}/{musicArtist.ArtistId}/{currentPage}/{effectiveReleaseType}", LimitStringSize(placeholder), options);

            var albumBuilder = new DiscordWebhookBuilder()
                .AddEmbed(GenerateMusicArtistDetails(musicArtist))
                .AddComponents(releaseTypeSelect)
                .AddComponents(select)
                .WithContent(Language.Current.DiscordCommandMusicAlbumRequestHelp);

            if (totalPages > 1)
            {
                var prevButton = new DiscordButtonComponent(
                    ButtonStyle.Secondary,
                    $"MuRLP/{_interactionContext.User.Id}/{request.CategoryId}/{musicArtist.ArtistId}/{Math.Max(currentPage - 1, 0)}/{effectiveReleaseType}",
                    "◀",
                    currentPage == 0
                );
                var nextButton = new DiscordButtonComponent(
                    ButtonStyle.Secondary,
                    $"MuRLP/{_interactionContext.User.Id}/{request.CategoryId}/{musicArtist.ArtistId}/{Math.Min(currentPage + 1, totalPages - 1)}/{effectiveReleaseType}",
                    "▶",
                    currentPage >= totalPages - 1
                );

                if (CanRequestAllAlbumsForReleaseType(effectiveReleaseType))
                {
                    var requestAllButton = new DiscordButtonComponent(
                        ButtonStyle.Primary,
                        $"MuRLRA/{_interactionContext.User.Id}/{request.CategoryId}/{CompactGuid(musicArtist.ArtistId)}/{effectiveReleaseType}",
                        allLabel
                    );
                    albumBuilder.AddComponents(prevButton, nextButton, requestAllButton);
                }
                else
                {
                    albumBuilder.AddComponents(prevButton, nextButton);
                }
            }
            else if (CanRequestAllAlbumsForReleaseType(effectiveReleaseType))
            {
                var requestAllButton = new DiscordButtonComponent(
                    ButtonStyle.Primary,
                    $"MuRLRA/{_interactionContext.User.Id}/{request.CategoryId}/{CompactGuid(musicArtist.ArtistId)}/{effectiveReleaseType}",
                    allLabel
                );
                albumBuilder.AddComponents(requestAllButton);
            }

            await _interactionContext.EditOriginalResponseAsync(albumBuilder);
        }


        public async Task DisplayMusicArtistDetailsAsync(MusicRequest request, MusicArtist musicArtist)
        {
            string message = Language.Current.DiscordCommandMusicArtistRequestConfirm;
            DiscordButtonComponent requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MuRCA/{_interactionContext.User.Id}/{request.CategoryId}/{musicArtist.ArtistId}", Language.Current.DiscordCommandRequestButton);

            var builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(requestButton).WithContent(message);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task DisplayAllAlbumsRequestConfirmAsync(MusicRequest request, MusicArtist artist, string releaseType)
        {
            string message = Language.Current.DiscordCommandMusicArtistRequestConfirm;
            DiscordButtonComponent requestButton = new DiscordButtonComponent(
                ButtonStyle.Primary,
                $"MuRLRA/{_interactionContext.User.Id}/{request.CategoryId}/{CompactGuid(artist.ArtistId)}/{releaseType}",
                Language.Current.DiscordCommandRequestButton);

            var builder = (await AddPreviousDropdownsAsync(artist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(artist)), true, "all", true, releaseType)).AddComponents(requestButton).WithContent(message);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task DisplayMusicAlbumDetailsAsync(MusicRequest request, MusicArtist artist, MusicAlbum album)
        {
            string message = Language.Current.DiscordCommandMusicAlbumRequestConfirm.ReplaceTokens(album, artist);
            DiscordButtonComponent requestButton = new DiscordButtonComponent(
                ButtonStyle.Primary,
                $"MuRLC/{_interactionContext.User.Id}/{request.CategoryId}/{CompactGuid(artist.ArtistId)}/{CompactGuid(album.AlbumId)}",
                Language.Current.DiscordCommandRequestButton);

            var builder = (await AddPreviousDropdownsAsync(artist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicAlbumDetails(artist, album)), true, album.AlbumId)).AddComponents(requestButton).WithContent(message);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }


        public async Task WarnMusicArtistAlreadyAvailableAsync(MusicArtist musicArtist)
        {
            var requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MMU/{_interactionContext.User.Id}/{musicArtist.ArtistId}", Language.Current.DiscordCommandAvailableButton, true);
            var builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(requestButton).WithContent(Language.Current.DiscordCommandMusicArtistAlreadyAvailable);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task WarnMusicAlbumAlreadyAvailableAsync(MusicArtist artist, MusicAlbum album)
        {
            var requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MMU/{_interactionContext.User.Id}/{artist.ArtistId}", Language.Current.DiscordCommandAvailableButton, true);
            var builder = (await AddPreviousDropdownsAsync(artist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicAlbumDetails(artist, album)), true, album.AlbumId)).AddComponents(requestButton).WithContent(Language.Current.DiscordCommandMusicAlbumAlreadyAvailable.ReplaceTokens(album, artist));
            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task WarnMusicAlbumAlreadyRequestedAsync(MusicArtist artist, MusicAlbum album)
        {
            var requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MMU/{_interactionContext.User.Id}/{artist.ArtistId}", Language.Current.DiscordCommandRequestedButton, true);
            var builder = (await AddPreviousDropdownsAsync(artist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicAlbumDetails(artist, album)), true, album.AlbumId)).AddComponents(requestButton).WithContent(Language.Current.DiscordCommandMusicAlbumRequestAlreadyExist.ReplaceTokens(album, artist));
            await _interactionContext.EditOriginalResponseAsync(builder);
        }


        public async Task WarnNoMusicArtistFoundAsync(string musicArtistName)
        {
            await _interactionContext.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent(Language.Current.DiscordCommandMusicArtistNotFound.ReplaceTokens(LanguageTokens.MusicArtistName, musicArtistName)));
        }



        public static DiscordEmbed GenerateMusicArtistDetails(MusicArtist musicArtist)
        {
            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithTitle(musicArtist.ArtistName)
                .WithTimestamp(DateTime.Now)
                .WithUrl($"https://musicbrainz.org/artist/{musicArtist.ArtistId}")
                .WithFooter("Powered by Requestrr");

            if (!string.IsNullOrWhiteSpace(musicArtist.Overview))
                embedBuilder.WithDescription(musicArtist.Overview.Substring(0, Math.Min(musicArtist.Overview.Length, 255)) + "(...)");

            if (!string.IsNullOrWhiteSpace(musicArtist.PosterPath) && musicArtist.PosterPath.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                embedBuilder.WithImageUrl(musicArtist.PosterPath);

            if (!string.IsNullOrWhiteSpace(musicArtist.Quality))
                embedBuilder.AddField($"__{Language.Current.DiscordEmbedMusicQuality}__", $"{musicArtist.Quality}", true);

            if (!string.IsNullOrWhiteSpace(musicArtist.PlexUrl))
                embedBuilder.AddField($"__Plex__", $"[{Language.Current.DiscordEmbedMusicListenNow}]({musicArtist.PlexUrl})", true);

            if (!string.IsNullOrWhiteSpace(musicArtist.EmbyUrl))
                embedBuilder.AddField($"__Emby__", $"[{Language.Current.DiscordEmbedMusicListenNow}]({musicArtist.EmbyUrl})", true);

            return embedBuilder.Build();
        }

        public static DiscordEmbed GenerateMusicAlbumDetails(MusicArtist artist, MusicAlbum album)
        {
            var title = album.AlbumTitle;
            if (album.ReleaseDate.HasValue && !title.Contains(album.ReleaseDate.Value.Year.ToString(), StringComparison.InvariantCultureIgnoreCase))
                title = $"{title} ({album.ReleaseDate.Value.Year})";

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithTitle($"{title} — {artist.ArtistName}")
                .WithTimestamp(DateTime.Now)
                .WithFooter("Powered by Requestrr");

            if (!string.IsNullOrWhiteSpace(album.AlbumId))
                embedBuilder.WithUrl($"https://musicbrainz.org/release-group/{album.AlbumId}");

            if (!string.IsNullOrWhiteSpace(album.Overview))
                embedBuilder.WithDescription(album.Overview.Substring(0, Math.Min(album.Overview.Length, 255)) + "(...)");

            if (!string.IsNullOrWhiteSpace(album.PosterPath) && album.PosterPath.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                embedBuilder.WithImageUrl(album.PosterPath);

            return embedBuilder.Build();
        }


        public async Task DisplayArtistRequestSuccessAsync(MusicArtist musicArtist)
        {
            DiscordButtonComponent successButton = new DiscordButtonComponent(ButtonStyle.Success, $"0/1/0", Language.Current.DiscordCommandRequestButtonSuccess);
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(successButton).WithContent(Language.Current.DiscordCommandMusicArtistRequestSuccess.ReplaceTokens(musicArtist));            
            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task DisplayMusicAlbumRequestSuccessAsync(MusicArtist artist, MusicAlbum album)
        {
            DiscordButtonComponent successButton = new DiscordButtonComponent(ButtonStyle.Success, $"0/1/0", Language.Current.DiscordCommandRequestButtonSuccess);
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(artist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicAlbumDetails(artist, album)), true, album.AlbumId)).AddComponents(successButton).WithContent(Language.Current.DiscordCommandMusicAlbumRequestSuccess.ReplaceTokens(album, artist));
            await _interactionContext.EditOriginalResponseAsync(builder);
        }



        public async Task DisplayArtistRequestDeniedAsync(MusicArtist musicArtist)
        {
            DiscordButtonComponent deniedButton = new DiscordButtonComponent(ButtonStyle.Danger, $"0/1/0", Language.Current.DiscordCommandRequestButtonDenied);
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(deniedButton).WithContent(Language.Current.DiscordCommandMusicArtistRequestDenied);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task DisplayMusicAlbumRequestDeniedAsync(MusicArtist artist, MusicAlbum album)
        {
            DiscordButtonComponent deniedButton = new DiscordButtonComponent(ButtonStyle.Danger, $"0/1/0", Language.Current.DiscordCommandRequestButtonDenied);
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(artist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicAlbumDetails(artist, album)), true, album.AlbumId)).AddComponents(deniedButton).WithContent(Language.Current.DiscordCommandMusicAlbumRequestDenied.ReplaceTokens(album, artist));
            await _interactionContext.EditOriginalResponseAsync(builder);
        }



        public async Task WarnMusicArtistUnavailableAndAlreadyHasNotificationAsync(MusicArtist musicArtist)
        {
            DiscordButtonComponent requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MMU/{_interactionContext.User.Id}/{musicArtist.ArtistId}", Language.Current.DiscordCommandRequestButton, true);
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(requestButton).WithContent(Language.Current.DiscordCommandMusicArtistRequestAlreadyExistNotified);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }



        public async Task AskForNotificationArtistRequestAsync(MusicArtist musicArtist)
        {
            var notificationButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MuNR/{_interactionContext.User.Id}/{musicArtist.ArtistId}", Language.Current.DiscordCommandRequestButton, false, new DiscordComponentEmoji(DiscordEmoji.FromUnicode("🔔")));
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(notificationButton).WithContent(Language.Current.DiscordCommandMusicArtistNotificationRequest);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }


        public async Task DisplayNotificationArtistSuccessAsync(MusicArtist musicArtist)
        {
            DiscordButtonComponent successButton = new DiscordButtonComponent(ButtonStyle.Success, $"0/1/0", Language.Current.DiscordCommandNotifyMeSuccess);
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(successButton).WithContent(Language.Current.DiscordCommandMusicArtistNotificationSuccess.ReplaceTokens(musicArtist));
            await _interactionContext.EditOriginalResponseAsync(builder);
        }





        private string GetFormattedMusicArtistName(MusicArtist music)
        {
            return LimitStringSize(music.ArtistName);
        }

        private string GetFormattedMusicAlbumName(MusicAlbum album)
        {
            return LimitStringSize(album.AlbumTitle);
        }
        private string LimitStringSize(string value, int limit = 100)
        {
            return value.Count() > limit ? value[..(limit - 3)] + "..." : value;
        }


        private async Task<DiscordWebhookBuilder> AddPreviousDropdownsAsync(
            MusicArtist music,
            DiscordWebhookBuilder builder,
            bool includeAlbumSelector = true,
            string selectedAlbumId = null,
            bool includeReleaseTypeSelector = true,
            string selectedReleaseType = null)
        {
            var previousMessage = await _interactionContext.GetOriginalResponseAsync();
            var components = previousMessage.FilterComponents<DiscordSelectComponent>().ToArray();
            DiscordSelectComponent previousMusicSelector = components.FirstOrDefault(x => x.CustomId.StartsWith("MuRSA", true, null));
            if (previousMusicSelector != null)
            {
                DiscordSelectComponent musicSelector = new DiscordSelectComponent(previousMusicSelector.CustomId, GetFormattedMusicArtistName(music), previousMusicSelector.Options);
                builder.AddComponents(musicSelector);
            }

            if (includeReleaseTypeSelector)
            {
                DiscordSelectComponent previousReleaseTypeSelector = components.FirstOrDefault(x => x.CustomId.StartsWith("MuRLT", true, null));
                if (previousReleaseTypeSelector != null)
                {
                    IReadOnlyList<DiscordSelectComponentOption> releaseTypeOptions = previousReleaseTypeSelector.Options;
                    string releaseTypePlaceholder = string.IsNullOrWhiteSpace(previousReleaseTypeSelector.Placeholder)
                        ? "Release Type"
                        : previousReleaseTypeSelector.Placeholder;

                    if (!string.IsNullOrWhiteSpace(selectedReleaseType))
                    {
                        releaseTypeOptions = previousReleaseTypeSelector.Options
                            .Select(x =>
                            {
                                bool isSelected = x.Value.EndsWith($"/{selectedReleaseType}", StringComparison.InvariantCultureIgnoreCase);
                                if (isSelected)
                                    releaseTypePlaceholder = x.Label;

                                return new DiscordSelectComponentOption(x.Label, x.Value, x.Description, isSelected);
                            })
                            .ToList();
                    }

                    DiscordSelectComponent releaseTypeSelector = new DiscordSelectComponent(previousReleaseTypeSelector.CustomId, LimitStringSize(releaseTypePlaceholder), releaseTypeOptions);
                    builder.AddComponents(releaseTypeSelector);
                }
            }

            if (includeAlbumSelector)
            {
                DiscordSelectComponent previousAlbumSelector = components.FirstOrDefault(x => x.CustomId.StartsWith("MuRLA", true, null));
                if (previousAlbumSelector != null)
                {
                    IReadOnlyList<DiscordSelectComponentOption> albumOptions = previousAlbumSelector.Options;
                    string placeholder = string.IsNullOrWhiteSpace(previousAlbumSelector.Placeholder)
                        ? Language.Current.DiscordCommandMusicAlbumRequestHelpDropdown
                        : previousAlbumSelector.Placeholder;

                    if (!string.IsNullOrWhiteSpace(selectedAlbumId))
                    {
                        albumOptions = previousAlbumSelector.Options
                            .Select(x =>
                            {
                                string[] values = x.Value.Split("/");
                                bool isSelected = values.Length >= 3 && values[2].Equals(selectedAlbumId, StringComparison.InvariantCultureIgnoreCase);

                                if (isSelected)
                                    placeholder = x.Label;

                                return new DiscordSelectComponentOption(x.Label, x.Value, x.Description, isSelected);
                            })
                            .ToList();
                    }

                    DiscordSelectComponent albumSelector = new DiscordSelectComponent(previousAlbumSelector.CustomId, LimitStringSize(placeholder), albumOptions);
                    builder.AddComponents(albumSelector);
                }
            }

            var paginationButtons = previousMessage.FilterComponents<DiscordButtonComponent>()
                .Where(x => x.CustomId.StartsWith("MuRLP", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (paginationButtons.Length > 0)
                builder.AddComponents(paginationButtons);

            return builder;
        }

        private static string GetAllReleaseTypeLabel(string releaseType)
        {
            if (string.Equals(releaseType, "Album", StringComparison.InvariantCultureIgnoreCase))
                return "All Albums";

            if (string.Equals(releaseType, "EP", StringComparison.InvariantCultureIgnoreCase))
                return "All EPs";

            if (string.Equals(releaseType, "Broadcast", StringComparison.InvariantCultureIgnoreCase))
                return "All Broadcasts";

            if (string.Equals(releaseType, "Other", StringComparison.InvariantCultureIgnoreCase))
                return "All Other";

            if (string.Equals(releaseType, "Single", StringComparison.InvariantCultureIgnoreCase))
                return "All Singles";

            return "All";
        }

        private static int GetReleaseTypeSortOrder(string releaseType)
        {
            if (string.Equals(releaseType, "Album", StringComparison.InvariantCultureIgnoreCase)) return 0;
            if (string.Equals(releaseType, "EP", StringComparison.InvariantCultureIgnoreCase)) return 1;
            if (string.Equals(releaseType, "Single", StringComparison.InvariantCultureIgnoreCase)) return 2;
            if (string.Equals(releaseType, "Broadcast", StringComparison.InvariantCultureIgnoreCase)) return 3;
            if (string.Equals(releaseType, "Other", StringComparison.InvariantCultureIgnoreCase)) return 4;
            return 100;
        }

        private static string CompactGuid(string value)
        {
            if (Guid.TryParse(value, out Guid guid))
                return guid.ToString("N");

            return value;
        }

        private bool CanRequestAllAlbumsForReleaseType(string releaseType)
        {
            if (IsNoneRestriction())
                return true;

            var restrictedTypes = GetRestrictedReleaseTypes();
            return !restrictedTypes.Contains(releaseType, StringComparer.InvariantCultureIgnoreCase);
        }

        private bool IsNoneRestriction()
        {
            return !ParseRestrictions().Any();
        }

        private bool HasRestriction(string restriction)
        {
            return ParseRestrictions().Contains(restriction, StringComparer.InvariantCultureIgnoreCase);
        }

        private string[] GetRestrictedReleaseTypes()
        {
            return ParseRestrictions()
                .Select(MapRestrictionToReleaseType)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .ToArray();
        }

        private static string MapRestrictionToReleaseType(string restriction)
        {
            if (string.IsNullOrWhiteSpace(restriction))
                return null;

            string normalized = restriction.Trim();
            if (normalized.StartsWith(MusicRestrictions.SinglePrefix, StringComparison.InvariantCultureIgnoreCase))
                return normalized.Substring(MusicRestrictions.SinglePrefix.Length).Trim();

            if (normalized.Equals(MusicRestrictions.SingleAlbum, StringComparison.InvariantCultureIgnoreCase))
                return "Album";

            if (normalized.Equals(MusicRestrictions.SingleEP, StringComparison.InvariantCultureIgnoreCase))
                return "EP";

            if (normalized.Equals(MusicRestrictions.SingleSingle, StringComparison.InvariantCultureIgnoreCase))
                return "Single";

            return null;
        }

        private string[] ParseRestrictions()
        {
            if (string.IsNullOrWhiteSpace(_restrictions))
                return Array.Empty<string>();

            var parsed = _restrictions
                .Split(',')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .ToArray();

            if (parsed.Any(x => x.Equals(MusicRestrictions.None, StringComparison.InvariantCultureIgnoreCase)))
                return Array.Empty<string>();

            return parsed;
        }
    }
}
