using Chino_chan.Models.Settings;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.API
{
    public struct Guild
    {
        public GuildSetting Settings { get; set; }
        public string Name { get; set; }
        public int MemberCount { get; set; }
        public string OwnerId { get; set; }
        public string IconUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string Id { get; set; }
        public UserAccess Access { get; set; }


        
        public Guild(SocketGuild Guild, ulong UserId)
        {
            Name = Guild.Name;
            MemberCount = Guild.MemberCount;
            OwnerId = Guild.OwnerId.ToString();
            IconUrl = Guild.IconUrl;
            CreatedAt = Guild.CreatedAt;
            Id = Guild.Id.ToString();
            Settings = Guild.GetSettings();

            Access = Tools.GetUserAccess(UserId, Settings, Guild.Id);
        }
    }
}
