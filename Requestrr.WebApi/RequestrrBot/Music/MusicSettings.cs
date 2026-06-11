namespace Requestrr.WebApi.RequestrrBot.Music
{
    public class MusicSettings
    {
        public string Client { get; set; }
        public string Restrictions { get; set; } = MusicRestrictions.None;
    }
}
