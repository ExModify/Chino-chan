using System.Collections.Generic;
using Chino_chan.Models.Settings;
using Chino_chan.Models.Settings.Credentials;

namespace Chino_chan
{
    public class Settings
    {
        public ulong OwnerId { get; set; } = 193356184806227969;
        
        public List<ulong> GlobalAdminIds { get; set; } = new List<ulong>();
        public List<ulong> CanTargetExMo { get; set; } = new List<ulong>();

        public string WaifuCloudHostname { get; set; } = "Boltzmann";

        public DevServer DevServer { get; set; } = new DevServer();

        public string Game { get; set; } = "with ExMo";

        public List<BlockedUser> GloballyBlocked { get; set; } = new List<BlockedUser>();

        public string InvitationLink { get; set; } = "https://discordapp.com/oauth2/authorize?client_id=271658919443562506&scope=bot&permissions=0";

        public List<ulong> WaifuYes { get; set; } = new List<ulong>(){};

        public Credentials Credentials { get; set; } = new Credentials();

        public int OSUAPICallLimit { get; set; } = 60;
        public int SubTagSleepTime { get; set; } = 300000; // 5 min

        public int TwitchStreamUpdate { get; set; } = 10000;
        public int TwitchUserUpdate { get; set; } = 60 * 1000;

        public int WebServerPort { get; set; } = 2465;
        public int APIPort { get; set; } = 2053;

        public string WebsitePath { get; set; } = "D:/web/chino.exmodify.com/";

        public string SoundCloudClientId { get; set; } = "4dd97a35cf647de595b918944aa6915d";

        public Dictionary<ulong, int> osuDiscordUserDatabase { get; set; } = new Dictionary<ulong, int>();
        
        public string[] ImageExtensions { get; set; } = new string[]
        {
            "png",
            "jpg",
            "gif",
            "jpeg",
            "webp"
        };

        public List<Models.Settings.Image> ImagePaths { get; set; } = new List<Models.Settings.Image>();
        public List<string> ApiHttpReferrers { get; set; } = new List<string>();

        public Dictionary<ulong, SayPreferences> SayPreferences { get; set; } = new Dictionary<ulong, SayPreferences>();

        public string GithubLink { get; set; } = "";

        public string ApiUrl { get; set; } = "https://chino.exmodify.com/api/";
        public string ImageCDN { get; set; } = "https://chino.exmodify.com/images/";
        public string ApiKey { get; set; } = "";
    }
}
