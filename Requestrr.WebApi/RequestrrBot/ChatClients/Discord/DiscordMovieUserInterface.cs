using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Options;
using Requestrr.WebApi.RequestrrBot.Approvals;
using Requestrr.WebApi.RequestrrBot.Locale;
using Requestrr.WebApi.RequestrrBot.Movies;

namespace Requestrr.WebApi.RequestrrBot.ChatClients.Discord
{
    public class DiscordMovieUserInterface : IMovieUserInterface
    {
        private readonly DiscordInteraction _interactionContext;
        private readonly IMovieSearcher _movieSearcher;
        private readonly DiscordSettingsProvider _settingsProvider;
        private readonly RequestApprovalRepository _approvalRepository;

        public DiscordMovieUserInterface(
            DiscordInteraction interactionContext,
            IMovieSearcher movieSearcher,
            DiscordSettingsProvider settingsProvider,
            RequestApprovalRepository approvalRepository)
        {
            _interactionContext = interactionContext;
            _movieSearcher = movieSearcher;
            _settingsProvider = settingsProvider;
            _approvalRepository = approvalRepository;
        }

        public async Task ShowMovieSelection(MovieRequest request, IReadOnlyList<Movie> movies)
        {
            await MovieSelection("MRS", request, movies);
        }


        public async Task ShowMovieIssueSelection(MovieRequest request, IReadOnlyList<Movie> movies)
        {
            await MovieSelection("MIRS", request, movies);
        }


        private async Task MovieSelection(string customId, MovieRequest request, IReadOnlyList<Movie> movies)
        {
            var options = movies.Take(15).Select(x => new DiscordSelectComponentOption(GetFormatedMovieTitle(x), $"{request.CategoryId}/{x.TheMovieDbId}")).ToList();
            var select = new DiscordSelectComponent($"{customId}/{_interactionContext.User.Id}/{request.CategoryId}", LimitStringSize(Language.Current.DiscordCommandMovieRequestHelpDropdown), options);

            await _interactionContext.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddComponents(select).WithContent(Language.Current.DiscordCommandMovieRequestHelp));
        }


        private string GetFormatedMovieTitle(Movie movie)
        {
            var releaseDate = !string.IsNullOrWhiteSpace(movie.ReleaseDate) && movie.ReleaseDate.Length >= 4 ? movie.ReleaseDate.Substring(0, 4) : null;

            if (releaseDate != null)
            {
                return $"{LimitStringSize(movie.Title, 93)} ({releaseDate})";
            }

            return LimitStringSize(movie.Title);
        }


        private string LimitStringSize(string value, int limit = 100)
        {
            return value.Count() > limit ? value[..(limit - 3)] + "..." : value;
        }

        public async Task WarnNoMovieFoundAsync(string movieName)
        {
            await _interactionContext.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent(Language.Current.DiscordCommandMovieNotFound.ReplaceTokens(LanguageTokens.MovieTitle, movieName)));
        }

        public async Task WarnNoMovieFoundByTheMovieDbIdAsync(string theMovieDbIdTextValue)
        {
            await _interactionContext.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent(Language.Current.DiscordCommandMovieNotFoundTMDB.ReplaceTokens(LanguageTokens.MovieTMDB, theMovieDbIdTextValue)));
        }

        public async Task DisplayMovieDetailsAsync(MovieRequest request, Movie movie)
        {
            var message = Language.Current.DiscordCommandMovieRequestConfirm;

            if (DateTime.TryParse(movie.ReleaseDate, out var releaseDate))
            {
                if (releaseDate > DateTime.UtcNow)
                {
                    message = Language.Current.DiscordCommandMovieNotReleased;
                }
                else
                {
                    message = Language.Current.DiscordCommandMovieRequestConfirm;
                }
            }

            var requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MRC/{_interactionContext.User.Id}/{request.CategoryId}/{movie.TheMovieDbId}", Language.Current.DiscordCommandRequestButton);

            var builder = (await AddPreviousDropdownsAsync(movie, new DiscordWebhookBuilder().AddEmbed(await GenerateMovieDetailsAsync(movie, _movieSearcher)))).AddComponents(requestButton).WithContent(message);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }


        /// <summary>
        /// Used to generate response for user to fill in the issues of the movie
        /// </summary>
        /// <param name="request"></param>
        /// <param name="movie"></param>
        /// <returns></returns>
        public async Task DisplayMovieIssueDetailsAsync(MovieRequest request, Movie movie, string issue = "")
        {
            //Validate that the movie searcher allow for issues to be reported
            if (_movieSearcher is not IMovieIssueSearcher)
            {
                await WarnNoMovieFoundAsync(movie.Title);
                return;
            }

            string message = Language.Current.DiscordCommandMovieIssueSelect;

            //Setup the dropdown of issues
            var options = ((IMovieIssueSearcher)_movieSearcher).IssueTypes.Select(x => new DiscordSelectComponentOption(LimitStringSize(x.Key), $"{request.CategoryId}/{movie.TheMovieDbId}/{x.Value}", null, x.Value.ToString() == issue)).ToList();
            DiscordSelectComponent select = new DiscordSelectComponent($"MIRS/{_interactionContext.User.Id}/{request.CategoryId}/{movie.TheMovieDbId}", LimitStringSize(Language.Current.DiscordCommandIssueHelpDropdown), options);

            DiscordWebhookBuilder builder = new DiscordWebhookBuilder().AddEmbed(await GenerateMovieDetailsAsync(movie, _movieSearcher));
            if ((await _interactionContext.GetOriginalResponseAsync()).FilterComponents<DiscordSelectComponent>().ToList().Count > 1)
            {
                builder = await AddPreviousDropdownsAsync(movie, builder);
            }

            builder.AddComponents(select);

            //If issue has been selected, add submit issue button to propmt for description
            if (issue != string.Empty)
            {
                DiscordButtonComponent button = new DiscordButtonComponent(ButtonStyle.Primary, $"MIRB/{_interactionContext.User.Id}/{request.CategoryId}/{movie.TheMovieDbId}/{issue}/Modal", Language.Current.DiscordCommandIssueButton, false, null);
                builder.AddComponents(button);
            }

            builder.WithContent(message);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        private string CreateInteractionString(string message, string split, string insert, int size)
        {
            List<string> tempString = message.Split(split).ToList();
            string join = LimitStringSize(insert, size - string.Join("", tempString).Count());
            return LimitStringSize(string.Join(join, tempString), size);
        }

        public async Task DisplayMovieIssueModalAsync(MovieRequest request, Movie movie, string issue)
        {
            DiscordInteractionResponseBuilder builder = new DiscordInteractionResponseBuilder();

            
            string label = CreateInteractionString(
                Language.Current.DiscordCommandIssueInteractionLabel,
                LanguageTokens.IssueLabel,
                ((IMovieIssueSearcher)_movieSearcher).IssueTypes.Where(x => x.Value.ToString() == issue).FirstOrDefault().Key,
                45
            );
            string placeholder = LimitStringSize(Language.Current.DiscordCommandIssueInteractionPlaceholder);
            string title = CreateInteractionString(
                Language.Current.DiscordCommandIssueInteractionTitle,
                LanguageTokens.IssueTitle,
                movie.Title,
                45
            );

            TextInputComponent textBox = new TextInputComponent(
                label,
                $"MIRC/{_interactionContext.User.Id}/{request.CategoryId}/{movie.TheMovieDbId}/{issue}",
                placeholder,
                string.Empty,
                true,
                TextInputStyle.Paragraph,
                0,
                null
            );

            builder.AddComponents(textBox);
            builder.WithCustomId("MIRC");
            builder.WithTitle(title);

            await _interactionContext.CreateResponseAsync(InteractionResponseType.Modal, builder);
        }


        public async Task CompleteMovieIssueModalRequestAsync(Movie movie, bool success)
        {
            await _interactionContext.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder()
                .WithContent(success ? Language.Current.DiscordCommandMovieIssueSuccess.ReplaceTokens(movie) : Language.Current.DiscordCommandMovieIssueFailed.ReplaceTokens(movie))
            );
        }



        public static async Task<DiscordEmbed> GenerateMovieDetailsAsync(Movie movie, IMovieSearcher movieSearcher = null)
        {
            var embedBuilder = new DiscordEmbedBuilder()
                .WithTitle($"{movie.Title} {(!string.IsNullOrWhiteSpace(movie.ReleaseDate) && movie.ReleaseDate.Length >= 4 ? $"({movie.ReleaseDate.Split("T")[0].Substring(0, 4)})" : string.Empty)}")
                .WithTimestamp(DateTime.Now)
                .WithUrl($"https://www.themoviedb.org/movie/{movie.TheMovieDbId}")
                .WithThumbnail("https://i.imgur.com/44ueTES.png")
                .WithFooter("Powered by Requestrr");

            if (!string.IsNullOrWhiteSpace(movie.Overview))
            {
                embedBuilder.WithDescription(movie.Overview.Substring(0, Math.Min(movie.Overview.Length, 255)) + "(...)");
            }

            if (!string.IsNullOrEmpty(movie.PosterPath) && movie.PosterPath.StartsWith("http", StringComparison.InvariantCultureIgnoreCase)) embedBuilder.WithImageUrl(movie.PosterPath);
            if (!string.IsNullOrWhiteSpace(movie.Quality)) embedBuilder.AddField($"__{Language.Current.DiscordEmbedMovieQuality}__", $"{movie.Quality}p", true);

            if (movieSearcher != null)
            {
                try
                {
                    var details = await movieSearcher.GetMovieDetails(new MovieRequest(null, int.MinValue), movie.TheMovieDbId);

                    if (!string.IsNullOrWhiteSpace(details.InTheatersDate))
                    {
                        embedBuilder.AddField($"__{Language.Current.DiscordEmbedMovieInTheaters}__", $"{details.InTheatersDate}", true);
                    }
                    else if (!string.IsNullOrWhiteSpace(movie.ReleaseDate))
                    {
                        embedBuilder.AddField($"__{Language.Current.DiscordEmbedMovieInTheaters}__", $"{DateTime.Parse(movie.ReleaseDate).ToString("MMMM dd yyyy", DateTimeFormatInfo.InvariantInfo)}", true);
                    }

                    if (!string.IsNullOrWhiteSpace(details.PhysicalReleaseName) && !string.IsNullOrWhiteSpace(details.PhysicalReleaseDate))
                    {
                        embedBuilder.AddField($"__{details.PhysicalReleaseName} {Language.Current.DiscordEmbedMovieRelease}__", $"{details.PhysicalReleaseDate}", true);
                    }
                }
                catch
                {
                    // Ignore
                }
            }

            if (!string.IsNullOrWhiteSpace(movie.PlexUrl)) embedBuilder.AddField($"__Plex__", $"[{Language.Current.DiscordEmbedMovieWatchNow}]({movie.PlexUrl})", true);
            if (!string.IsNullOrWhiteSpace(movie.EmbyUrl)) embedBuilder.AddField($"__Emby__", $"[{Language.Current.DiscordEmbedMovieWatchNow}]({movie.EmbyUrl})", true);
            if (!string.IsNullOrWhiteSpace(movie.MediaUrl)) embedBuilder.AddField($"__Media__", $"[{Language.Current.DiscordEmbedMovieWatchNow}]({movie.MediaUrl})", true);

            return embedBuilder.Build();
        }

        public async Task WarnMovieAlreadyAvailableAsync(Movie movie)
        {
            var requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MMM/{_interactionContext.User.Id}/{movie.TheMovieDbId}", Language.Current.DiscordCommandAvailableButton, true);
            var builder = (await AddPreviousDropdownsAsync(movie, new DiscordWebhookBuilder().AddEmbed(await GenerateMovieDetailsAsync(movie, _movieSearcher)))).AddComponents(requestButton).WithContent(Language.Current.DiscordCommandMovieAlreadyAvailable);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task WarnMovieAlreadyRequestedAsync(Movie movie)
        {
            var requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MMM/{_interactionContext.User.Id}/{movie.TheMovieDbId}", Language.Current.DiscordCommandRequestedButton, true);
            var builder = (await AddPreviousDropdownsAsync(movie, new DiscordWebhookBuilder().AddEmbed(await GenerateMovieDetailsAsync(movie, _movieSearcher)))).AddComponents(requestButton).WithContent(Language.Current.DiscordCommandMovieRequestAlreadyExist);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task DisplayRequestSuccessAsync(Movie movie)
        {
            var successButton = new DiscordButtonComponent(ButtonStyle.Success, $"0/1/0", Language.Current.DiscordCommandRequestButtonSuccess);

            var details = await GenerateMovieDetailsAsync(movie, _movieSearcher);
            var builder = (await AddPreviousDropdownsAsync(movie, new DiscordWebhookBuilder().AddEmbed(details))).AddComponents(successButton).WithContent(Language.Current.DiscordCommandMovieRequestSuccess.ReplaceTokens(movie));
            await _interactionContext.EditOriginalResponseAsync(builder);
            await SendAdminRequestMessageAsync(movie, details, Language.Current.DiscordCommandRequestApproved);
        }

        public async Task DisplayRequestPendingAsync(Movie movie, int requestId)
        {
            var settings = _settingsProvider.Provide();
            var approveEmoji = string.IsNullOrWhiteSpace(settings.ApprovalEmojiApprove) ? "✅" : settings.ApprovalEmojiApprove.Trim();
            var denyEmoji = string.IsNullOrWhiteSpace(settings.ApprovalEmojiDeny) ? "❌" : settings.ApprovalEmojiDeny.Trim();
            var message = settings.AutomaticallyPurgeCommandMessages
                ? Language.Current.DiscordCommandMovieRequestPendingSilent.ReplaceTokens(movie, new Dictionary<string, string>
                {
                    { LanguageTokens.ApproveEmoji, approveEmoji },
                    { LanguageTokens.DenyEmoji, denyEmoji }
                })
                : Language.Current.DiscordCommandMovieRequestPending.ReplaceTokens(movie, new Dictionary<string, string>
                {
                    { LanguageTokens.ApproveEmoji, approveEmoji },
                    { LanguageTokens.DenyEmoji, denyEmoji }
                });
            var baseEmbed = await GenerateMovieDetailsAsync(movie, _movieSearcher);
            var footerText = string.IsNullOrWhiteSpace(baseEmbed.Footer?.Text)
                ? $"{DiscordConstants.OverseerrRequestIdMarker} {requestId}"
                : $"{baseEmbed.Footer.Text} | {DiscordConstants.OverseerrRequestIdMarker} {requestId}";
            var embed = new DiscordEmbedBuilder(baseEmbed)
                .WithFooter(footerText)
                .Build();
            var builder = (await AddPreviousDropdownsAsync(movie, new DiscordWebhookBuilder().AddEmbed(embed)))
                .WithContent(message);

            await _interactionContext.EditOriginalResponseAsync(builder);
            var originalMessage = await _interactionContext.GetOriginalResponseAsync();
            var isDirectMessage = originalMessage.Channel != null && originalMessage.Channel.Type == ChannelType.Private;
            _approvalRepository.AddMessage(requestId, _interactionContext.User.Username, _interactionContext.User.Id, originalMessage.ChannelId, originalMessage.Id, false, isDirectMessage);
            if (!settings.AutomaticallyPurgeCommandMessages)
            {
                try
                {
                    await originalMessage.CreateReactionAsync(DiscordEmoji.FromUnicode(approveEmoji));
                    await originalMessage.CreateReactionAsync(DiscordEmoji.FromUnicode(denyEmoji));
                }
                catch
                {
                    // Ignore reaction failures
                }
            }
            await SendAdminPendingMessageAsync(movie, embed, requestId, approveEmoji, denyEmoji);
        }

        public async Task AskForNotificationRequestAsync(Movie movie)
        {
            var notifyButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MNR/{_interactionContext.User.Id}/{movie.TheMovieDbId}", Language.Current.DiscordCommandNotifyMe, false, new DiscordComponentEmoji(DiscordEmoji.FromUnicode("🔔")));

            var builder = (await AddPreviousDropdownsAsync(movie, new DiscordWebhookBuilder().AddEmbed(await GenerateMovieDetailsAsync(movie, _movieSearcher)))).AddComponents(notifyButton).WithContent(Language.Current.DiscordCommandMovieNotificationRequest);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task DisplayNotificationSuccessAsync(Movie movie)
        {
            var successButton = new DiscordButtonComponent(ButtonStyle.Success, $"0/1/0", Language.Current.DiscordCommandNotifyMeSuccess);

            var builder = (await AddPreviousDropdownsAsync(movie, new DiscordWebhookBuilder().AddEmbed(await GenerateMovieDetailsAsync(movie, _movieSearcher)))).AddComponents(successButton).WithContent(Language.Current.DiscordCommandMovieNotificationSuccess.ReplaceTokens(movie));
            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task DisplayRequestDeniedAsync(Movie movie)
        {
            var deniedButton = new DiscordButtonComponent(ButtonStyle.Danger, $"0/1/0", Language.Current.DiscordCommandRequestButtonDenied);

            var details = await GenerateMovieDetailsAsync(movie, _movieSearcher);
            var builder = (await AddPreviousDropdownsAsync(movie, new DiscordWebhookBuilder().AddEmbed(details))).AddComponents(deniedButton).WithContent(Language.Current.DiscordCommandMovieRequestDenied);
            await _interactionContext.EditOriginalResponseAsync(builder);
            await SendAdminRequestMessageAsync(movie, details, Language.Current.DiscordCommandRequestDenied);
        }

        public async Task WarnMovieUnavailableAndAlreadyHasNotificationAsync(Movie movie)
        {
            var requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MMM/{_interactionContext.User.Id}/{movie.TheMovieDbId}", Language.Current.DiscordCommandRequestedButton, true);

            var builder = (await AddPreviousDropdownsAsync(movie, new DiscordWebhookBuilder().AddEmbed(await GenerateMovieDetailsAsync(movie, _movieSearcher)))).AddComponents(requestButton).WithContent(Language.Current.DiscordCommandMovieRequestAlreadyExistNotified);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        private async Task<DiscordWebhookBuilder> AddPreviousDropdownsAsync(Movie movie, DiscordWebhookBuilder builder)
        {
            var previousMovieSelector = (DiscordSelectComponent)(await _interactionContext.GetOriginalResponseAsync()).FilterComponents<DiscordSelectComponent>().FirstOrDefault();

            if (previousMovieSelector != null)
            {
                var movieSelector = new DiscordSelectComponent(previousMovieSelector.CustomId, GetFormatedMovieTitle(movie), previousMovieSelector.Options);
                builder.AddComponents(movieSelector);
            }

            return builder;
        }

        private async Task SendAdminPendingMessageAsync(Movie movie, DiscordEmbed embed, int requestId, string approveEmoji, string denyEmoji)
        {
            var settings = _settingsProvider.Provide();
            if (settings.AdminChannelIds == null || !settings.AdminChannelIds.Any())
            {
                return;
            }

            var adminPrompt = Language.Current.DiscordCommandRequestPendingAdmin
                .ReplaceTokens(LanguageTokens.AuthorUsername, _interactionContext.User.Username)
                .ReplaceTokens(LanguageTokens.ApproveEmoji, approveEmoji)
                .ReplaceTokens(LanguageTokens.DenyEmoji, denyEmoji);

            var builder = new DiscordMessageBuilder()
                .WithContent(adminPrompt)
                .AddEmbed(embed);

            foreach (var channelId in settings.AdminChannelIds)
            {
                if (!ulong.TryParse(channelId, out var parsedChannelId))
                {
                    continue;
                }

                var channel = _interactionContext.Guild?.GetChannel(parsedChannelId);
                if (channel != null)
                {
                    var adminRequestMessage = await channel.SendMessageAsync(builder);
                    _approvalRepository.AddMessage(requestId, _interactionContext.User.Username, _interactionContext.User.Id, adminRequestMessage.ChannelId, adminRequestMessage.Id, true, false);
                    try
                    {
                        await adminRequestMessage.CreateReactionAsync(DiscordEmoji.FromUnicode(approveEmoji));
                        await adminRequestMessage.CreateReactionAsync(DiscordEmoji.FromUnicode(denyEmoji));
                    }
                    catch
                    {
                        // Ignore reaction failures
                    }
                }
            }
        }

        private async Task SendAdminRequestMessageAsync(Movie movie, DiscordEmbed baseEmbed, string statusMessage)
        {
            var settings = _settingsProvider.Provide();
            if (!settings.AdminChannelAllRequests || settings.AdminChannelIds == null || !settings.AdminChannelIds.Any())
            {
                return;
            }

            var embed = new DiscordEmbedBuilder(baseEmbed).Build();
            var adminMessage = Language.Current.DiscordCommandRequestAdminSummary
                .ReplaceTokens(LanguageTokens.AuthorUsername, _interactionContext.User.Username)
                .ReplaceTokens(LanguageTokens.RequestStatus, statusMessage);

            var builder = new DiscordMessageBuilder()
                .WithContent(adminMessage)
                .AddEmbed(embed);

            foreach (var channelId in settings.AdminChannelIds)
            {
                if (!ulong.TryParse(channelId, out var parsedChannelId))
                {
                    continue;
                }

                var channel = _interactionContext.Guild?.GetChannel(parsedChannelId);
                if (channel != null)
                {
                    await channel.SendMessageAsync(builder);
                }
            }
        }

    }
}
