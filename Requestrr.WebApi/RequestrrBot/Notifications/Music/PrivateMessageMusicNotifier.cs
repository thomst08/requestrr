using System;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;
using Requestrr.WebApi.RequestrrBot.ChatClients.Discord;
using Requestrr.WebApi.RequestrrBot.Locale;
using Requestrr.WebApi.RequestrrBot.Music;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Notifications.Music
{
    public class PrivateMessageMusicNotifier : IMusicNotifier
    {
        private readonly DiscordClient _discordClient;
        private readonly ILogger _logger;

        public PrivateMessageMusicNotifier(DiscordClient discordClient, ILogger logger)
        {
            _discordClient = discordClient;
            _logger = logger;
        }


        public async Task<HashSet<string>> NotifyArtistAsync(IReadOnlyCollection<string> userIds, MusicArtist musicArtist, CancellationToken token)
        {
            HashSet<string> userNotified = new HashSet<string>();

            if (_discordClient.Guilds.Any())
            {
                foreach (string userId in userIds)
                {
                    if (token.IsCancellationRequested)
                        return userNotified;

                    try
                    {
                        DiscordMember user = null;
                        foreach (DiscordGuild guild in _discordClient.Guilds.Values)
                        {
                            try
                            {
                                user = await guild.GetMemberAsync(ulong.Parse(userId));
                                break;
                            }
                            catch { }
                        }

                        if (user != null)
                        {
                            DiscordDmChannel channel = await user.CreateDmChannelAsync();
                            await channel.SendMessageAsync(Language.Current.DiscordNotificationMusicArtistDM.ReplaceTokens(musicArtist), DiscordMusicUserInterface.GenerateMusicArtistDetails(musicArtist));
                            await Task.Delay(TimeSpan.FromSeconds(1));
                        }
                        else
                        {
                             _logger.LogWarning($"Removing music artist notification for user with ID {userId} as it could not be found in any of the guilds.");
                        }

                        userNotified.Add(userId);
                    }
                    catch (UnauthorizedException ex)
                    {
                        userNotified.Add(userId);
                        _logger.LogWarning($"Removing music artist notification [{musicArtist.ArtistName}] for user [{userId}] as we are missing permissions to do so [Unauthorized].");
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while sending a private message music artist notification to a specific user: " + ex.Message);
                    }
                }
            }

            return userNotified;
        }
    }
}
