using System;
using System.Linq;
using System.Reflection;

namespace Chino_chan.Models.Settings.Credentials
{
    public enum CredentialType
    {
        Discord,
        osu,
        WaifuCloud,
        Sankaku,
        Imgur,
        Twitch,
        Gelbooru
    }
    public struct Credentials
    {
        public DiscordCredentials Discord { get; set; }
        public osuCredentials osu { get; set; }
        public WaifuCloudCredentials WaifuCloud { get; set; }
        public SankakuCredentials Sankaku { get; set; }
        public ImgurCredentials Imgur { get; set; }
        public TwitchCredentials Twitch { get; set; }
        public GelbooruCredentials Gelbooru { get; set; }

        public bool IsEmpty(CredentialType CredentialType, params string[] ExceptionName)
        {
            object Credential = null;

            switch (CredentialType)
            {
                case CredentialType.Discord:
                    Credential = Discord;
                    break;
                case CredentialType.Imgur:
                    Credential = Imgur;
                    break;
                case CredentialType.osu:
                    Credential = osu;
                    break;
                case CredentialType.Sankaku:
                    Credential = Sankaku;
                    break;
                case CredentialType.WaifuCloud:
                    Credential = WaifuCloud;
                    break;
                case CredentialType.Twitch:
                    Credential = Twitch;
                    break;
                case CredentialType.Gelbooru:
                    Credential = Gelbooru;
                    break;
            }

            Type Type = Credential.GetType();

            PropertyInfo[] Properties = Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < Properties.Length; i++)
            {
                if (Properties[i].PropertyType == typeof(string) && !ExceptionName.Contains(Properties[i].Name))
                {
                    string Value = Properties[i].GetValue(Credential) as string;

                    if (string.IsNullOrWhiteSpace(Value))
                        return true;
                }
            }
            return false;
        }
    }
}
