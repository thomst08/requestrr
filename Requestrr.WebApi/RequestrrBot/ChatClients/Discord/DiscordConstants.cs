using DSharpPlus.Entities;

namespace Requestrr.WebApi.RequestrrBot.ChatClients.Discord
{
    public static class DiscordConstants
    {
        public const int MaxEmbedLength = 1000;
        public const int MaxMessageLength = 2000;

        // CRIOS palette — keep embeds visually consistent with the
        // followarr / crios-watchlist bot and the CRIOS web UI.
        public static readonly DiscordColor CriosAccent = new DiscordColor(0x00, 0xD4, 0xFF);
        public static readonly DiscordColor CriosSuccess = new DiscordColor(0x00, 0xE6, 0x76);
        public static readonly DiscordColor CriosDanger = new DiscordColor(0xFF, 0x5B, 0x5B);

        public const string CriosFooter = "CRIOS Request";
    }
}