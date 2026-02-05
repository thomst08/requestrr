using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Requestrr.WebApi.RequestrrBot.Approvals;
using Requestrr.WebApi.RequestrrBot.Locale;
using Requestrr.WebApi.RequestrrBot.TvShows;

namespace Requestrr.WebApi.RequestrrBot.ChatClients.Discord
{
    public class DiscordTvShowUserInterface : ITvShowUserInterface
    {
        private readonly DiscordInteraction _interactionContext;
        private readonly ITvShowIssueSearcher _tvShowIssue;
        private readonly DiscordSettingsProvider _settingsProvider;
        private readonly RequestApprovalRepository _approvalRepository;

        public DiscordTvShowUserInterface(DiscordInteraction interactionContext, DiscordSettingsProvider settingsProvider, RequestApprovalRepository approvalRepository, ITvShowIssueSearcher tvShowIssue = null)
        {
            _interactionContext = interactionContext;
            _settingsProvider = settingsProvider;
            _approvalRepository = approvalRepository;
            _tvShowIssue = tvShowIssue;
        }

        public static DiscordEmbed GenerateTvShowDetailsAsync(TvShow tvShow)
        {
            var title = tvShow.Title;

            if (!string.IsNullOrWhiteSpace(tvShow.FirstAired))
            {
                if (tvShow.FirstAired.Length >= 4 && !title.Contains(tvShow.FirstAired.Split("T")[0].Substring(0, 4), StringComparison.InvariantCultureIgnoreCase))
                {
                    title = $"{title} ({tvShow.FirstAired.Split("T")[0].Substring(0, 4)})";
                }
            }

            var embedBuilder = new DiscordEmbedBuilder()
                .WithTitle(title)
                .WithTimestamp(DateTime.Now)
                .WithThumbnail("https://thetvdb.com/images/logo.png")
                .WithFooter("Powered by Requestrr");

            if (!string.IsNullOrWhiteSpace(tvShow.Overview))
            {
                embedBuilder.WithDescription(tvShow.Overview.Substring(0, Math.Min(tvShow.Overview.Length, 255)) + "(...)");
            }

            if (!string.IsNullOrEmpty(tvShow.Banner) && tvShow.Banner.StartsWith("http", StringComparison.InvariantCultureIgnoreCase)) embedBuilder.WithImageUrl(tvShow.Banner);
            if (!string.IsNullOrWhiteSpace(tvShow.WebsiteUrl)) embedBuilder.WithUrl(tvShow.WebsiteUrl);
            if (!string.IsNullOrWhiteSpace(tvShow.Network)) embedBuilder.AddField($"__{Language.Current.DiscordEmbedTvNetwork}__", tvShow.Network, true);
            if (!string.IsNullOrWhiteSpace(tvShow.Status)) embedBuilder.AddField($"__{Language.Current.DiscordEmbedTvStatus}__", tvShow.Status, true);
            if (!string.IsNullOrWhiteSpace(tvShow.Quality)) embedBuilder.AddField($"__{Language.Current.DiscordEmbedTvQuality}__", $"{tvShow.Quality}p", true);
            if (!string.IsNullOrWhiteSpace(tvShow.PlexUrl)) embedBuilder.AddField($"__Plex__", $"[{Language.Current.DiscordEmbedTvWatchNow}]({tvShow.PlexUrl})", true);
            if (!string.IsNullOrWhiteSpace(tvShow.EmbyUrl)) embedBuilder.AddField($"__Emby__", $"[{Language.Current.DiscordEmbedTvWatchNow}]({tvShow.EmbyUrl})", true);
            if (!string.IsNullOrWhiteSpace(tvShow.MediaUrl)) embedBuilder.AddField($"__Media__", $"[{Language.Current.DiscordEmbedTvWatchNow}]({tvShow.MediaUrl})", true);

            return embedBuilder.Build();
        }

        public async Task DisplayMultiSeasonSelectionAsync(TvShowRequest request, TvShow tvShow, TvSeason[] tvSeasons)
        {
            var embed = GenerateTvShowDetailsAsync(tvShow);
            var options = tvSeasons.Select(x => new DiscordSelectComponentOption(GetFormattedSeasonName(tvShow, x), $"{request.CategoryId}/{tvShow.TheTvDbId.ToString()}/{x.SeasonNumber.ToString()}")).ToList();
            var seasonSelector = new DiscordSelectComponent($"TSS/{_interactionContext.User.Id}/{request.CategoryId}", LimitStringSize(Language.Current.DiscordCommandTvRequestHelpSeasonsDropdown), options);

            var builder = (await AddPreviousDropdownsAsync(tvShow, new DiscordWebhookBuilder().AddEmbed(embed))).AddComponents(seasonSelector).WithContent(Language.Current.DiscordCommandTvRequestHelpSeasons);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task DisplayNotificationSuccessForSeasonAsync(TvShow tvShow, TvSeason requestedSeason)
        {
            var embed = GenerateTvShowDetailsAsync(tvShow);
            var successButton = new DiscordButtonComponent(ButtonStyle.Success, $"0/1/0", Language.Current.DiscordCommandNotifyMeSuccess);

            if (requestedSeason is FutureTvSeasons)
            {
                var builder = (await AddPreviousDropdownsAsync(tvShow, new DiscordWebhookBuilder().AddEmbed(embed))).AddComponents(successButton).WithContent(Language.Current.DiscordCommandTvNotificationSuccessFutureSeasons.ReplaceTokens(tvShow));
                await _interactionContext.EditOriginalResponseAsync(builder);
            }
            else
            {
                var builder = (await AddPreviousDropdownsAsync(tvShow, new DiscordWebhookBuilder().AddEmbed(embed))).AddComponents(successButton).WithContent(Language.Current.DiscordCommandTvNotificationSuccessSeason.ReplaceTokens(tvShow, requestedSeason.SeasonNumber));
                await _interactionContext.EditOriginalResponseAsync(builder);
            }
        }

        public async Task AskForSeasonNotificationRequestAsync(TvShow tvShow, TvSeason selectedSeason)
        {
            var message = Language.Current.DiscordCommandTvNotificationRequestSeason.ReplaceTokens(tvShow, selectedSeason.SeasonNumber);

            if (selectedSeason is FutureTvSeasons)
            {
                if (tvShow.AllSeasonsAvailable())
                {
                    message = Language.Current.DiscordCommandTvNotificationRequestFutureSeasonAvailable;
                }
                else if (tvShow.AllSeasonsFullyRequested())
                {
                    message = Language.Current.DiscordCommandTvNotificationRequestFutureSeasonRequested;
                }
                else
                {
                    message = Language.Current.DiscordCommandTvNotificationRequestFutureSeasonMissing;
                }
            }

            var requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"TNR/{_interactionContext.User.Id}/{tvShow.TheTvDbId}/{selectedSeason.GetType().Name.First()}/{selectedSeason.SeasonNumber}", Language.Current.DiscordCommandNotifyMe, false, new DiscordComponentEmoji(DiscordEmoji.FromUnicode("🔔")));

            var embed = GenerateTvShowDetailsAsync(tvShow);
            var builder = (await AddPreviousDropdownsAsync(tvShow, new DiscordWebhookBuilder().AddEmbed(embed))).AddComponents(requestButton).WithContent(message);

            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task DisplayRequestDeniedForSeasonAsync(TvShow tvShow, TvSeason selectedSeason)
        {
            var embed = GenerateTvShowDetailsAsync(tvShow);
            var deniedButton = new DiscordButtonComponent(ButtonStyle.Danger, $"0/1/0", Language.Current.DiscordCommandRequestButtonDenied);
            var builder = (await AddPreviousDropdownsAsync(tvShow, new DiscordWebhookBuilder().AddEmbed(embed))).AddComponents(deniedButton).WithContent(Language.Current.DiscordCommandTvRequestDenied);

            await _interactionContext.EditOriginalResponseAsync(builder);
            await SendAdminRequestMessageAsync(tvShow, embed, Language.Current.DiscordCommandRequestDenied);
        }

        public async Task DisplayRequestSuccessForSeasonAsync(TvShow tvShow, TvSeason requestedSeason)
        {
            var embed = GenerateTvShowDetailsAsync(tvShow);

            var message = requestedSeason is AllTvSeasons
                ? Language.Current.DiscordCommandTvRequestSuccessAllSeasons.ReplaceTokens(tvShow, requestedSeason.SeasonNumber)
                : requestedSeason is FutureTvSeasons
                    ? Language.Current.DiscordCommandTvRequestSuccessFutureSeasons.ReplaceTokens(tvShow, requestedSeason.SeasonNumber)
                    : Language.Current.DiscordCommandTvRequestSuccessSeason.ReplaceTokens(tvShow, requestedSeason.SeasonNumber);

            var successButton = new DiscordButtonComponent(ButtonStyle.Success, $"0/1/0", Language.Current.DiscordCommandRequestButtonSuccess);
            var builder = (await AddPreviousDropdownsAsync(tvShow, new DiscordWebhookBuilder().AddEmbed(embed))).AddComponents(successButton).WithContent(message);

            await _interactionContext.EditOriginalResponseAsync(builder);
            await SendAdminRequestMessageAsync(tvShow, embed, Language.Current.DiscordCommandRequestApproved);
        }

        public async Task DisplayRequestPendingForSeasonAsync(TvShow tvShow, TvSeason requestedSeason, int requestId)
        {
            var settings = _settingsProvider.Provide();
            var approveEmoji = string.IsNullOrWhiteSpace(settings.ApprovalEmojiApprove) ? "✅" : settings.ApprovalEmojiApprove.Trim();
            var denyEmoji = string.IsNullOrWhiteSpace(settings.ApprovalEmojiDeny) ? "❌" : settings.ApprovalEmojiDeny.Trim();
            var baseEmbed = GenerateTvShowDetailsAsync(tvShow);
            var footerText = string.IsNullOrWhiteSpace(baseEmbed.Footer?.Text)
                ? $"{DiscordConstants.OverseerrRequestIdMarker} {requestId}"
                : $"{baseEmbed.Footer.Text} | {DiscordConstants.OverseerrRequestIdMarker} {requestId}";
            var embed = new DiscordEmbedBuilder(baseEmbed)
                .WithFooter(footerText)
                .Build();
            var message = requestedSeason is AllTvSeasons
                ? settings.AutomaticallyPurgeCommandMessages
                    ? Language.Current.DiscordCommandTvRequestPendingAllSeasonsSilent.ReplaceTokens(tvShow, requestedSeason.SeasonNumber, new Dictionary<string, string>
                    {
                        { LanguageTokens.ApproveEmoji, approveEmoji },
                        { LanguageTokens.DenyEmoji, denyEmoji }
                    })
                    : Language.Current.DiscordCommandTvRequestPendingAllSeasons.ReplaceTokens(tvShow, requestedSeason.SeasonNumber, new Dictionary<string, string>
                    {
                        { LanguageTokens.ApproveEmoji, approveEmoji },
                        { LanguageTokens.DenyEmoji, denyEmoji }
                    })
                : requestedSeason is FutureTvSeasons
                    ? settings.AutomaticallyPurgeCommandMessages
                        ? Language.Current.DiscordCommandTvRequestPendingFutureSeasonsSilent.ReplaceTokens(tvShow, requestedSeason.SeasonNumber, new Dictionary<string, string>
                        {
                            { LanguageTokens.ApproveEmoji, approveEmoji },
                            { LanguageTokens.DenyEmoji, denyEmoji }
                        })
                        : Language.Current.DiscordCommandTvRequestPendingFutureSeasons.ReplaceTokens(tvShow, requestedSeason.SeasonNumber, new Dictionary<string, string>
                        {
                            { LanguageTokens.ApproveEmoji, approveEmoji },
                            { LanguageTokens.DenyEmoji, denyEmoji }
                        })
                    : settings.AutomaticallyPurgeCommandMessages
                        ? Language.Current.DiscordCommandTvRequestPendingSeasonSilent.ReplaceTokens(tvShow, requestedSeason.SeasonNumber, new Dictionary<string, string>
                        {
                            { LanguageTokens.ApproveEmoji, approveEmoji },
                            { LanguageTokens.DenyEmoji, denyEmoji }
                        })
                        : Language.Current.DiscordCommandTvRequestPendingSeason.ReplaceTokens(tvShow, requestedSeason.SeasonNumber, new Dictionary<string, string>
                        {
                            { LanguageTokens.ApproveEmoji, approveEmoji },
                            { LanguageTokens.DenyEmoji, denyEmoji }
                        });

            var builder = (await AddPreviousDropdownsAsync(tvShow, new DiscordWebhookBuilder().AddEmbed(embed)))
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
            await SendAdminPendingMessageAsync(tvShow, embed, requestId, approveEmoji, denyEmoji);
        }

        public async Task DisplayTvShowDetailsForSeasonAsync(TvShowRequest request, TvShow tvShow, TvSeason season)
        {
            var message = season is AllTvSeasons
                ? Language.Current.DiscordCommandTvRequestConfirmAllSeasons
                : season is FutureTvSeasons
                    ? Language.Current.DiscordCommandTvRequestConfirmFutureSeasons
                    : Language.Current.DiscordCommandTvRequestConfirmSeason.ReplaceTokens(LanguageTokens.SeasonNumber, season.SeasonNumber.ToString());

            var requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"TRC/{_interactionContext.User.Id}/{request.CategoryId}/{tvShow.TheTvDbId}/{season.SeasonNumber}", Language.Current.DiscordCommandRequestButton);

            var embed = GenerateTvShowDetailsAsync(tvShow);
            var builder = (await AddPreviousDropdownsAsync(tvShow, new DiscordWebhookBuilder().AddEmbed(embed))).AddComponents(requestButton).WithContent(message);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }



        /// <summary>
        /// Handle the responce to the user when requesting submitting an issue
        /// </summary>
        /// <param name="request"></param>
        /// <param name="tvShow"></param>
        /// <param name="issue">String of issue</param>
        /// <param name="seasonNumber">Season number being reported</param>
        /// <param name="episodeNumber">Episode number being reported</param>
        /// <returns></returns>
        public async Task DisplayTvShowIssueDetailsAsync(TvShowRequest request, TvShow tvShow, string issue, int? seasonNumber, int? episodeNumber)
        {
            //Validate that the TV issue searcher is not null
            if (_tvShowIssue == null)
            {
                await WarnNoTvShowFoundAsync(tvShow.Title);
                return;
            }

            var message = Language.Current.DiscordCommandTvIssueSelect;
            var embed = GenerateTvShowDetailsAsync(tvShow);
            var builder = new DiscordWebhookBuilder().AddEmbed(embed);

            var currentSelectors = (await _interactionContext.GetOriginalResponseAsync()).FilterComponents<DiscordSelectComponent>().ToList();
            var previousTvSelector = currentSelectors.FirstOrDefault(x => IsIssueTvShowSelector(x));
            if (previousTvSelector != null)
            {
                var tvSelector = new DiscordSelectComponent(previousTvSelector.CustomId, GetFormatedTvShowTitle(tvShow), previousTvSelector.Options);
                builder.AddComponents(tvSelector);
            }

            var selectedSeasonNumber = seasonNumber ?? 0;
            var selectedEpisodeNumber = episodeNumber ?? -1;
            var issueValue = string.IsNullOrWhiteSpace(issue) ? "0" : issue;

            var seasonOptions = BuildIssueSeasonOptions(tvShow, selectedSeasonNumber);
            if (seasonOptions.Any())
            {
                var seasonSelect = new DiscordSelectComponent(
                    $"TIRSS/{_interactionContext.User.Id}/{request.CategoryId}/{tvShow.TheTvDbId}/{issueValue}/{selectedEpisodeNumber}",
                    LimitStringSize(Language.Current.DiscordCommandTvRequestHelpSeasonsDropdown),
                    seasonOptions
                );
                builder.AddComponents(seasonSelect);
            }

            if (selectedSeasonNumber > 0)
            {
                var episodeOptions = BuildIssueEpisodeOptions(tvShow, selectedSeasonNumber, selectedEpisodeNumber);
                if (episodeOptions.Any())
                {
                    var episodeSelect = new DiscordSelectComponent(
                        $"TIRSE/{_interactionContext.User.Id}/{request.CategoryId}/{tvShow.TheTvDbId}/{issueValue}/{selectedSeasonNumber}",
                        LimitStringSize(Language.Current.DiscordCommandTvIssueHelpEpisodesDropdown),
                        episodeOptions
                    );
                    builder.AddComponents(episodeSelect);
                }
            }

            var issueOptions = _tvShowIssue.IssueTypes
                .Select(x => new DiscordSelectComponentOption(LimitStringSize(x.Key), x.Value.ToString(), null, x.Value.ToString() == issue))
                .ToList();
            var issueSelect = new DiscordSelectComponent(
                $"TIRS/{_interactionContext.User.Id}/{request.CategoryId}/{tvShow.TheTvDbId}/{selectedSeasonNumber}/{selectedEpisodeNumber}",
                LimitStringSize(Language.Current.DiscordCommandIssueHelpDropdown),
                issueOptions
            );

            builder.AddComponents(issueSelect).WithContent(message);

            //If issue has been selected, add submit issue button to prompt for description
            if (!string.IsNullOrWhiteSpace(issue))
            {
                var button = new DiscordButtonComponent(
                    ButtonStyle.Primary,
                    $"TIRB/{_interactionContext.User.Id}/{request.CategoryId}/{tvShow.TheTvDbId}/{issue}/{selectedSeasonNumber}/{selectedEpisodeNumber}/Modal",
                    Language.Current.DiscordCommandIssueButton,
                    false,
                    null
                );
                builder.AddComponents(button);
            }

            await _interactionContext.EditOriginalResponseAsync(builder);
        }


        private string CreateInteractionString(string message, string split, string insert, int size)
        {
            List<string> tempString = message.Split(split).ToList();
            string join = LimitStringSize(insert, size - string.Join("", tempString).Count());
            return LimitStringSize(string.Join(join, tempString), size);
        }

        /// <summary>
        /// Used to handle the submitting of a Modal back to the user when an issue is being submitted
        /// </summary>
        /// <param name="request"></param>
        /// <param name="tvShow"></param>
        /// <param name="issue">String of issue</param>
        /// <param name="seasonNumber">Season number being reported</param>
        /// <param name="episodeNumber">Episode number being reported</param>
        /// <returns></returns>
        public async Task DisplayTvShowIssueModalAsync(TvShowRequest request, TvShow tvShow, string issue, int? seasonNumber, int? episodeNumber) //MovieRequest request, Movie movie, string issue)
        {
            DiscordInteractionResponseBuilder builder = new DiscordInteractionResponseBuilder();

            var locationSuffix = GetIssueLocationSuffix(seasonNumber, episodeNumber);
            string label = CreateInteractionString(
                Language.Current.DiscordCommandIssueInteractionLabel,
                LanguageTokens.IssueLabel,
                _tvShowIssue.IssueTypes.Where(x => x.Value.ToString() == issue).FirstOrDefault().Key,
                45
            );
            string placeholder = LimitStringSize(Language.Current.DiscordCommandIssueInteractionPlaceholder);
            string title = CreateInteractionString(
                Language.Current.DiscordCommandIssueInteractionTitle,
                LanguageTokens.IssueTitle,
                $"{tvShow.Title}{locationSuffix}",
                45
            );

            var seasonValue = seasonNumber ?? -1;
            var episodeValue = episodeNumber ?? -1;
            TextInputComponent textBox = new TextInputComponent(
                label,
                $"TIRC/{_interactionContext.User.Id}/{request.CategoryId}/{tvShow.TheTvDbId}/{issue}/{seasonValue}/{episodeValue}",
                placeholder,
                string.Empty,
                true,
                TextInputStyle.Paragraph,
                0,
                null
            );

            builder.AddComponents(textBox);
            builder.WithCustomId("TIRC");
            builder.WithTitle(title);

            await _interactionContext.CreateResponseAsync(InteractionResponseType.Modal, builder);
        }


        /// <summary>
        /// Handle the responce message to a modal for issue being submitted
        /// </summary>
        /// <param name="movie"></param>
        /// <param name="success"></param>
        /// <returns></returns>
        public async Task CompleteTvShowIssueModalRequestAsync(TvShow tvShow, bool success)
        {
            await _interactionContext.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder()
                .WithContent(success ? Language.Current.DiscordCommandTvIssueSuccess.ReplaceTokens(tvShow) : Language.Current.DiscordCommandTvIssueFailed.ReplaceTokens(tvShow))
            );
        }


        public async Task ShowTvShowSelection(TvShowRequest request, IReadOnlyList<SearchedTvShow> searchedTvShows)
        {
            await TvShowSelection("TRS", request, searchedTvShows);
        }
        
        
        /// <summary>
        /// Handles the message for issues of a TV Show
        /// </summary>
        /// <param name="request"></param>
        /// <param name="searchedTvShows"></param>
        /// <returns></returns>
        public async Task ShowTvShowIssueSelection(TvShowRequest request, IReadOnlyList<SearchedTvShow> searchedTvShows)
        {
            await TvShowSelection("TIRS", request, searchedTvShows);
        }


        private async Task TvShowSelection(string customId, TvShowRequest request, IReadOnlyList<SearchedTvShow> searchedTvShows)
        {
            var options = searchedTvShows.Take(15).Select(x => new DiscordSelectComponentOption(GetFormatedTvShowTitle(x), $"{request.CategoryId}/{x.TheTvDbId.ToString()}")).ToList();
            var select = new DiscordSelectComponent($"{customId}/{_interactionContext.User.Id}/{request.CategoryId}", LimitStringSize(Language.Current.DiscordCommandTvRequestHelpSearchDropdown), options);

            await _interactionContext.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddComponents(select).WithContent(Language.Current.DiscordCommandTvRequestHelpSearch));
        }
        

        public async Task WarnAlreadyNotifiedForSeasonsAsync(TvShow tvShow, TvSeason requestedSeason)
        {
            var messageContent = Language.Current.DiscordCommandTvRequestAlreadyExistNotifiedSeason.ReplaceTokens(tvShow, requestedSeason.SeasonNumber);
            var buttonMessage = Language.Current.DiscordCommandRequestedButton;

            if (requestedSeason is FutureTvSeasons)
            {
                if (tvShow.AllSeasonsAvailable())
                {
                    messageContent = Language.Current.DiscordCommandTvRequestAlreadyExistNotifiedFutureSeasonAvailable;
                    buttonMessage = Language.Current.DiscordCommandAvailableButton;
                }
                else if (tvShow.AllSeasonsFullyRequested())
                {
                    messageContent = Language.Current.DiscordCommandTvRequestAlreadyExistNotifiedFutureSeasonRequested;
                }
                else
                {
                    messageContent = Language.Current.DiscordCommandTvRequestAlreadyExistNotifiedFutureSeasonFound;
                }
            }

            var embed = GenerateTvShowDetailsAsync(tvShow);
            var requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"TTT/{_interactionContext.User.Id}/{tvShow.TheTvDbId}/999", buttonMessage, true);
            var builder = (await AddPreviousDropdownsAsync(tvShow, new DiscordWebhookBuilder().AddEmbed(embed))).WithContent(messageContent).AddComponents(requestButton);

            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task WarnAlreadySeasonAlreadyRequestedAsync(TvShow tvShow, TvSeason requestedSeason)
        {
            var messageContent = Language.Current.DiscordCommandTvRequestAlreadyExistSeason.ReplaceTokens(tvShow, requestedSeason.SeasonNumber);
            var buttonMessage = Language.Current.DiscordCommandRequestedButton;

            if (requestedSeason is FutureTvSeasons)
            {
                if (tvShow.AllSeasonsAvailable())
                {
                    messageContent = Language.Current.DiscordCommandTvRequestAlreadyExistFutureSeasonAvailable;
                    buttonMessage = Language.Current.DiscordCommandAvailableButton;
                }
                else if (tvShow.AllSeasonsFullyRequested())
                {
                    messageContent = Language.Current.DiscordCommandTvRequestAlreadyExistFutureSeasonRequested;
                }
                else
                {
                    messageContent = Language.Current.DiscordCommandTvRequestAlreadyExistFutureSeasonFound;
                }
            }

            var embed = GenerateTvShowDetailsAsync(tvShow);
            var requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"TTT/{_interactionContext.User.Id}/{tvShow.TheTvDbId}/999", buttonMessage, true);
            var builder = (await AddPreviousDropdownsAsync(tvShow, new DiscordWebhookBuilder().AddEmbed(embed))).WithContent(messageContent).AddComponents(requestButton);

            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task WarnNoTvShowFoundAsync(string tvShowName)
        {
            await _interactionContext.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent(Language.Current.DiscordCommandTvNotFound.ReplaceTokens(LanguageTokens.TvShowTitle, tvShowName)));
        }

        public async Task WarnNoTvShowFoundByTvDbIdAsync(int tvDbId)
        {
            await _interactionContext.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent(Language.Current.DiscordCommandTvNotFoundTVDBID.ReplaceTokens(LanguageTokens.TvShowTVDBID, tvDbId.ToString())));
        }

        public async Task WarnSeasonAlreadyAvailableAsync(TvShow tvShow, TvSeason selectedSeason)
        {
            var embed = GenerateTvShowDetailsAsync(tvShow);
            var requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"TTT/{_interactionContext.User.Id}/{tvShow.TheTvDbId}/999", Language.Current.DiscordCommandAvailableButton, true);
            var builder = (await AddPreviousDropdownsAsync(tvShow, new DiscordWebhookBuilder().AddEmbed(embed))).WithContent(Language.Current.DiscordCommandTvRequestAlreadyAvailableSeason.ReplaceTokens(LanguageTokens.SeasonNumber, selectedSeason.SeasonNumber.ToString())).AddComponents(requestButton);

            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task WarnShowCannotBeRequestedAsync(TvShow tvShow)
        {
            var embed = GenerateTvShowDetailsAsync(tvShow);
            var requestButton = new DiscordButtonComponent(ButtonStyle.Danger, $"TTT/{_interactionContext.User.Id}/{tvShow.TheTvDbId}/999", Language.Current.DiscordCommandRequestButton, true);
            var builder = (await AddPreviousDropdownsAsync(tvShow, new DiscordWebhookBuilder().AddEmbed(embed))).WithContent(Language.Current.DiscordCommandTvRequestUnsupported).AddComponents(requestButton);

            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task WarnShowHasEndedAsync(TvShow tvShow)
        {
            var embed = GenerateTvShowDetailsAsync(tvShow);

            if (tvShow.AllSeasonsAvailable())
            {
                var requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"TTT/{_interactionContext.User.Id}/{tvShow.TheTvDbId}/999", Language.Current.DiscordCommandAvailableButton, true);
                var builder = (await AddPreviousDropdownsAsync(tvShow, new DiscordWebhookBuilder().AddEmbed(embed))).WithContent(Language.Current.DiscordCommandTvRequestAlreadyAvailableSeries).AddComponents(requestButton);
                await _interactionContext.EditOriginalResponseAsync(builder);
            }
            else
            {
                var requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"TTT/{_interactionContext.User.Id}/{tvShow.TheTvDbId}/999", Language.Current.DiscordCommandRequestedButton, true);
                var builder = (await AddPreviousDropdownsAsync(tvShow, new DiscordWebhookBuilder().AddEmbed(embed))).AddComponents(requestButton).WithContent(Language.Current.DiscordCommandTvRequestAlreadyExistSeries);
                await _interactionContext.EditOriginalResponseAsync(builder);
            }
        }

        private async Task<DiscordWebhookBuilder> AddPreviousDropdownsAsync(TvShow tvShow, DiscordWebhookBuilder builder)
        {
            var selectors = (await _interactionContext.GetOriginalResponseAsync()).FilterComponents<DiscordSelectComponent>();
            DiscordSelectComponent previousTvSelector = selectors.FirstOrDefault(x => x.CustomId.StartsWith("TRS", true, null));

            if (previousTvSelector != null)
            {
                var tvSelector = new DiscordSelectComponent(previousTvSelector.CustomId, GetFormatedTvShowTitle(tvShow), previousTvSelector.Options);
                builder.AddComponents(tvSelector);
            }

            DiscordSelectComponent previousSeasonSelector = selectors.FirstOrDefault(x => x.CustomId.StartsWith("TSS", true, null));

            if (previousSeasonSelector != null)
            {
                if (!tvShow.AllSeasonsAvailable() && previousSeasonSelector != null && previousSeasonSelector.Options.Any(x => x.Value.Contains(tvShow.TheTvDbId.ToString(), StringComparison.OrdinalIgnoreCase)))
                {
                    if (!_interactionContext.Data.CustomId.StartsWith("TRS", true, null))
                    {
                        var newOptions = tvShow.Seasons.Select(x => new DiscordSelectComponentOption(GetFormattedSeasonName(tvShow, x), $"{tvShow.TheTvDbId.ToString()}/{x.SeasonNumber.ToString()}")).ToDictionary(x => x.Value, x => x);
                        var oldOptions = previousSeasonSelector.Options;

                        var currentOptions = oldOptions.Select(x => new DiscordSelectComponentOption(newOptions.ContainsKey(x.Value) ? LimitStringSize(newOptions[x.Value].Label) : LimitStringSize(x.Label), x.Value)).ToList();

                        string defaultSelectedValue = currentOptions.First().Label;

                        try
                        {
                            defaultSelectedValue = _interactionContext.Data.Values.Any()
                           ? currentOptions.Single(x => x.Value == _interactionContext.Data.Values.Single()).Label
                           : currentOptions.Single(x => x.Value == string.Join("/", _interactionContext.Data.CustomId.Split("/").Skip(2))).Label;
                        }
                        catch { }

                        var seasonSelector = new DiscordSelectComponent(previousSeasonSelector.CustomId, LimitStringSize(defaultSelectedValue), currentOptions);
                        builder.AddComponents(seasonSelector);
                    }
                }
            }

            return builder;
        }

        private async Task SendAdminPendingMessageAsync(TvShow tvShow, DiscordEmbed embed, int requestId, string approveEmoji, string denyEmoji)
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

        private async Task SendAdminRequestMessageAsync(TvShow tvShow, DiscordEmbed embed, string statusMessage)
        {
            var settings = _settingsProvider.Provide();
            if (!settings.AdminChannelAllRequests || settings.AdminChannelIds == null || !settings.AdminChannelIds.Any())
            {
                return;
            }

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

        private string GetFormatedTvShowTitle(SearchedTvShow tvShow)
        {
            return GetFormatedTvShowTitle(tvShow.Title, tvShow.FirstAired);
        }

        private string GetFormatedTvShowTitle(TvShow tvShow)
        {
            return GetFormatedTvShowTitle(tvShow.Title, tvShow.FirstAired);
        }

        private string GetFormatedTvShowTitle(string title, string firstAired)
        {
            var releaseYear = !string.IsNullOrWhiteSpace(firstAired) && firstAired.Length >= 4 ? firstAired.Substring(0, 4) : null;

            if (releaseYear != null)
            {
                if (!title.Contains(releaseYear, StringComparison.InvariantCultureIgnoreCase))
                {
                    return $"{LimitStringSize(title, 93)} ({releaseYear})";
                }
            }

            return LimitStringSize(title);
        }


        private string LimitStringSize(string value, int limit = 100)
        {
            return value.Count() > limit ? value[..(limit - 3)] + "..." : value;
        }

        private bool IsIssueTvShowSelector(DiscordSelectComponent selector)
        {
            if (!selector.CustomId.StartsWith("TIRS", true, null))
            {
                return false;
            }

            return selector.Options.Any(x => x.Value.Split("/").Length == 2);
        }

        private List<DiscordSelectComponentOption> BuildIssueSeasonOptions(TvShow tvShow, int selectedSeasonNumber)
        {
            var options = new List<DiscordSelectComponentOption>();
            var seasons = tvShow.Seasons ?? Array.Empty<TvSeason>();

            if (!seasons.Any(x => x.SeasonNumber == 0))
            {
                options.Add(new DiscordSelectComponentOption(LimitStringSize(Language.Current.DiscordEmbedTvAllSeasons), "0", null, selectedSeasonNumber == 0));
            }

            options.AddRange(seasons.Select(x => new DiscordSelectComponentOption(
                GetFormattedSeasonName(tvShow, x),
                x.SeasonNumber.ToString(),
                null,
                x.SeasonNumber == selectedSeasonNumber
            )));

            return options;
        }

        private List<DiscordSelectComponentOption> BuildIssueEpisodeOptions(TvShow tvShow, int selectedSeasonNumber, int selectedEpisodeNumber)
        {
            var seasons = tvShow.Seasons ?? Array.Empty<TvSeason>();
            var season = seasons.FirstOrDefault(x => x.SeasonNumber == selectedSeasonNumber);
            var episodes = season?.Episodes ?? Array.Empty<TvEpisode>();

            if (!episodes.Any())
            {
                return new List<DiscordSelectComponentOption>();
            }

            var options = new List<DiscordSelectComponentOption>
            {
                new DiscordSelectComponentOption(LimitStringSize(Language.Current.DiscordCommandTvIssueHelpEpisodesFullSeason), "0", null, selectedEpisodeNumber <= 0)
            };

            foreach (var episode in episodes.OrderBy(x => x.EpisodeNumber).Take(24))
            {
                options.Add(new DiscordSelectComponentOption($"Episode {episode.EpisodeNumber}", episode.EpisodeNumber.ToString(), null, episode.EpisodeNumber == selectedEpisodeNumber));
            }

            return options;
        }

        private string GetIssueLocationSuffix(int? seasonNumber, int? episodeNumber)
        {
            if (seasonNumber == null || seasonNumber < 0)
            {
                return string.Empty;
            }

            if (seasonNumber == 0)
            {
                return " - All Seasons";
            }

            if (episodeNumber != null && episodeNumber > 0)
            {
                return $" - S{seasonNumber}E{episodeNumber}";
            }

            return $" - S{seasonNumber}";
        }

        private string GetFormattedSeasonName(TvShow tvShow, TvSeason season)
        {
            var seasonName = season is AllTvSeasons
                ? $"{Language.Current.DiscordEmbedTvAllSeasons}"
                : season is FutureTvSeasons
                    ? $"{Language.Current.DiscordEmbedTvFutureSeasons}"
                    : $"{Language.Current.DiscordEmbedTvSeason.ReplaceTokens(LanguageTokens.SeasonNumber, season.SeasonNumber.ToString())}";

            if (season is AllTvSeasons)
            {
                seasonName += tvShow.AllSeasonsFullyRequested() ? $" ({Language.Current.DiscordEmbedTvFullyRequested})" : season.IsRequested == RequestedState.Partial ? string.Empty : string.Empty;
            }
            else
            {
                seasonName += season.IsRequested == RequestedState.Full ? $" ({Language.Current.DiscordEmbedTvFullyRequested})" : season.IsRequested == RequestedState.Partial ? $" ({Language.Current.DiscordEmbedTvPartiallyRequested})" : string.Empty;
            }

            return seasonName;
        }
    }
}
