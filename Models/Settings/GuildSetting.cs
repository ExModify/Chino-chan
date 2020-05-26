using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Chino_chan.Models.Settings
{
    public class GuildSetting
    {
        public ulong GuildId { get; set; }
        public ulong LevelupReport { get; set; } = 0;

        public Dictionary<ulong, Mute> Mutes { get; set; } = new Dictionary<ulong, Mute>();

        public string Prefix { get; set; } = ";";
        public string Language { get; set; } = "en_US";

        public bool Leveling { get; set; } = false;
        
        public List<ulong> AdminIds { get; set; } = new List<ulong>();
        public List<BlockedUser> Blocked { get; set; } = new List<BlockedUser>();
        public List<AssignMessage> AssignMessages { get; set; } = new List<AssignMessage>();
        public Dictionary<int, List<Track>> Tracks { get; set; } = new Dictionary<int, List<Track>>();

        public List<ulong> AssignableRoles { get; set; } = new List<ulong>();
        public List<ulong> AvoidedChannels { get; set; } = new List<ulong>();
        public List<ulong> NsfwChannels { get; set; } = new List<ulong>();
        public List<ulong> NewMemberRoles { get; set; } = new List<ulong>();
        public List<ulong> BlockedRoles { get; set; } = new List<ulong>();
        public ulong ReactionAssignChannel { get; set; } = 0;
        public List<ulong> ReactionAssignChannels { get; set; } = new List<ulong>();
        public bool GlobalRecent { get; set; } = false;

        public Dictionary<string, List<string>> ImageHostImage { get; set; } = new Dictionary<string, List<string>>();

        public ulong MuteRoleId { get; set; } = 0;

        public bool AllowLoliContent { get; set; } = false;

        public TwitchTrack TwitchTrack { get; set; } = new TwitchTrack();

        public Greet Greet { get; set; } = null;

        public List<string> LevelingWatchExtensions { get; set; } = new List<string>();
        public List<ulong> LevelingAvoidChannel { get; set; } = new List<ulong>();
        public Dictionary<uint, List<ulong>> AssignRoleAtLevels { get; set; } = new Dictionary<uint, List<ulong>>();
    }
    public class Mute
    {
        public TimeSpan MuteTime { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }

        public List<ulong> RoleIds { get; private set; }
        public ulong MuteChannel { get; set; }
        public ulong MutedBy { get; private set; }
        public string Reason { get; private set; }

        [JsonIgnore]
        public CancellationTokenSource Cancel { get; private set; }

        [JsonIgnore]
        public CancellationToken Token { get; private set; }

        public Mute(int TimeInMinutes, ulong Channel, List<ulong> RoleIds, ulong MutedBy, string Reason)
        {
            Cancel = new CancellationTokenSource();
            Token = Cancel.Token;
            this.RoleIds = RoleIds;
            this.MutedBy = MutedBy;
            this.Reason = Reason;

            MuteChannel = Channel;

            MuteTime = TimeSpan.FromMinutes(TimeInMinutes);
            StartTime = DateTime.UtcNow;
            EndTime = StartTime + MuteTime;
        }
        
    }
    public class AssignMessage
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public ulong RoleId { get; set; }
    }
    public class Greet
    {
        public string Message { get; set; }
        public ulong ChannelId { get; set; }

        public GreetEmbed Embed { get; set; }

        public bool SendEmbed { get; set; }
        public bool Enabled { get; set; }

        public Greet(string Message, ulong Channel, GreetEmbed Embed = null, bool SendEmbed = false, bool Enabled = true)
        {
            this.Message = Message;
            this.ChannelId = Channel;
            this.Embed = Embed;
            this.SendEmbed = SendEmbed;
            this.Enabled = Enabled;
        }
    }

    public class GreetEmbed
    {
        public string Title { get; set; }
        public bool IncludeAvatar { get; set; }

        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        [JsonIgnore]
        public Discord.Color Color
        {
            get
            {
                return new Discord.Color(R, G, B);
            }
            set
            {
                R = value.R;
                G = value.G;
                B = value.B;
            }
        }

        public GreetEmbed(string Title = "New User", bool IncludeAvatar = true, Discord.Color? Color = null)
        {
            this.Title = Title;
            this.IncludeAvatar = IncludeAvatar;
            this.Color = Color ?? Global.Pink;
        }
    }

    public class TwitchTrack
    {
        public List<long> UserIds { get; set; } = new List<long>();

        public bool SendStreamUp { get; set; } = true;
        public bool SendStreamDown { get; set; } = false;

        public bool SendEmbed { get; set; } = false;
        
        public string StreamUpNotification { get; set; } = "<#display_name>(<#login_name>) started streaming: <#stream_name>!";
        public string StreamDownNotification { get; set; } = "<#display_name>(<#login_name>) stopped streaming: <#stream_name>!";

        public ulong ChannelId { get; set; } = 0;
    }
}
