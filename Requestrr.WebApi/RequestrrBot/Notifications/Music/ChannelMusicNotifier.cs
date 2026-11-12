using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Requestrr.WebApi.RequestrrBot.ChatClients.Discord;
using Requestrr.WebApi.RequestrrBot.Locale;
using Requestrr.WebApi.RequestrrBot.Music;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Notifications.Music
{
    public class ChannelMusicNotifier : IMusicNotifier
    {
        private readonly DiscordClient _discordClient;
        private readonly ulong[] _channelIds;
        private readonly ILogger _logger;

        public ChannelMusicNotifier(
            DiscordClient discordClient,
            ulong[] channelIds,
            ILogger logger
        )
        {
            _discordClient = discordClient;
            _channelIds = channelIds;
            _logger = logger;
        }


        public async Task<HashSet<string>> NotifyArtistAsync(IReadOnlyCollection<string> userIds, MusicArtist musicArtist, CancellationToken token)
        {
            if (_discordClient.Guilds.Any())
            {
                HashSet<ulong> discordUserIds = new HashSet<ulong>(userIds.Select(x => ulong.Parse(x)));
                HashSet<ulong> userNotified = new HashSet<ulong>();

                DiscordChannel[] channels = GetNoificationChannels();
                HandleRemovedUsers(discordUserIds, userNotified, token);

                foreach (DiscordChannel channel in channels)
                {
                    if (token.IsCancellationRequested)
                        return new HashSet<string>(userNotified.Select(x => x.ToString()));

                    try
                    {
                        await NotifyUsersInChannelForArtist(musicArtist, discordUserIds, userNotified, channel);
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"An error occurred while sending a music artist notification to channel \"{channel?.Name}\" in server \"{channel?.Guild?.Name}\": " + ex.Message);
                    }
                }

                return new HashSet<string>(userNotified.Select(x => x.ToString()));
            }

            return new HashSet<string>();
        }


        private DiscordChannel[] GetNoificationChannels()
        {
            return _discordClient.Guilds
                .SelectMany(x => x.Value.Channels.Values)
                .Where(x => _channelIds.Contains(x.Id))
                .OfType<DiscordChannel>()
                .ToArray();
        }


        private void HandleRemovedUsers(HashSet<ulong> discordUserIds, HashSet<ulong> userNotified, CancellationToken token)
        {
            HashSet<ulong> currentMembers = new HashSet<ulong>(_discordClient.Guilds.SelectMany(x => x.Value.Members).Select(x => x.Value.Id));

            foreach (ulong missingUserIds in discordUserIds.Except(currentMembers))
            {
                if (token.IsCancellationRequested)
                    return;

                userNotified.Add(missingUserIds);
                _logger.LogWarning($"Removing music artist notification for user with ID {missingUserIds} as it could not be found in any of the guilds.");
            }
        }


        private static async Task NotifyUsersInChannelForArtist(MusicArtist musicArtist, HashSet<ulong> discordUserIds, HashSet<ulong> userNotified, DiscordChannel channel)
        {
            List<DiscordMember> usersToNotify = channel.Users
                .Where(x => discordUserIds.Contains(x.Id))
                .Where(x => !userNotified.Contains(x.Id))
                .ToList();

            if (usersToNotify.Any())
            {
                StringBuilder messageBuilder = new StringBuilder();
                messageBuilder.AppendLine(Language.Current.DiscordNotificationMusicArtistChannel.ReplaceTokens(musicArtist));

                foreach (DiscordMember user in usersToNotify)
                {
                    string userMentionText = $"{user.Mention} ";
                    if (messageBuilder.Length + userMentionText.Length < DiscordConstants.MaxMessageLength)
                        messageBuilder.Append(userMentionText);
                }

                await channel.SendMessageAsync(messageBuilder.ToString(), DiscordMusicUserInterface.GenerateMusicArtistDetails(musicArtist));

                foreach (DiscordMember user in usersToNotify)
                {
                    userNotified.Add(user.Id);
                }
            }
        }
    }
}
