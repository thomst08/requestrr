namespace Requestrr.WebApi.RequestrrBot.Music
{
    public class MusicSettingsProvider
    {
        public MusicSettings Provide()
        {
            dynamic settings = SettingsFile.Read();
            string restrictions = MusicRestrictions.None;

            try
            {
                restrictions = settings.Music.Restrictions;
            }
            catch
            {
                restrictions = MusicRestrictions.None;
            }

            if (string.IsNullOrWhiteSpace(restrictions))
                restrictions = MusicRestrictions.None;

            return new MusicSettings
            {
                Client = settings.Music.Client,
                Restrictions = restrictions
            };
        }
    }
}
