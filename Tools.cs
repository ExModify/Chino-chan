using Chino_chan.Models.API;
using Chino_chan.Models.Settings;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Chino_chan
{
    public static class Tools
    {
        public static string ConvertHighlightsBack(string Input)
        {
            var Base = Input;
            var Regex = GetMentionFinderRegex();
            foreach (Match Match in Regex.Matches(Input))
            {
                var Name = GetName(GetId(Match.Value));
                Base = Base.Replace(Match.Value, Match.Value.Substring(1, 1) + Name);
            }
            return Base;
        }

        public static string GetName(ulong Id)
        {
            if (Global.Client.GetChannel(Id) is IGuildChannel Channel)
            {
                return Channel.Name;
            }
            else if (Global.Client.GetUser(Id) is IUser User)
            {
                if (User is IGuildUser GuildUser)
                {
                    return GuildUser.Nickname ?? GuildUser.Username;
                }
                else
                {
                    return User.Username;
                }
            }
            foreach (IGuild guild in Global.Client.Guilds)
            {
                IUser u = guild.GetUserAsync(Id).Result;
                if (u != null)
                {
                    return u.Username;
                }
            }
            if (Global.Client.GetGuild(Id) is IGuild Guild)
            {
                return Guild.Name;
            }
            return "<#" + Id.ToString() + ">";
        }
        public static string GetName(ulong Id, IGuild Guild)
        {
            if (Guild.GetChannelAsync(Id).Result is IGuildChannel Channel)
            {
                return Channel.Name;
            }
            else if (Guild.GetUserAsync(Id).Result is IUser User)
            {
                if (User is IGuildUser GuildUser)
                {
                    return GuildUser.Nickname ?? GuildUser.Username;
                }
                else
                {
                    return User.Username;
                }
            }
            return "<#" + Id.ToString() + ">";
        }

        public static ulong GetId(string Section)
        {
            return ulong.Parse(Section.Substring(2, Section.Length - 3));
        }

        public static Regex GetMentionFinderRegex()
        {
            return new Regex(@"<[@|#]\d*>");
        }
        
        public static IGuildUser ParseUser(string Input, bool SearchGlobal = false, ICommandContext Context = null)
        {
            if (Input.Trim() == "")
                if (Context == null)
                    return null;
                else return Context.Guild.GetUserAsync(Context.User.Id).Result;

            if (Context.Message.MentionedUserIds.Count > 0 && Context.Channel is IGuildChannel guildChannel)
            {
                return guildChannel.GetUserAsync(Context.Message.MentionedUserIds.First()).Result;
            }

            if (ulong.TryParse(Input, out ulong parsedId) || (parsedId = GetHlId(Input)) != 0)
            {
                return Context.Guild.GetUserAsync(parsedId).Result;
            }
            else
            {
                Input = Input.ToLower();
                if (Context?.Guild == null)
                {
                    return null;
                }
                else
                {
                    List<IGuildUser> users = new List<IGuildUser>();
                    if (SearchGlobal)
                    {
                        foreach (SocketGuild guild in Global.Client.Guilds)
                        {
                            users.AddRange(guild.SearchUsersAsync(Input).Result);
                        }
                    }
                    else
                    {
                        users.AddRange(Context.Guild.SearchUsersAsync(Input).Result);
                    }

                    for (int i = 0; i < users.Count; i++)
                    {
                        IGuildUser User = users[i];

                        if (User.Username.ToLower() == Input || (User.Nickname ?? "").ToLower() == Input)
                            return users[i];
                    }
                    
                    if (users.Count > 0)
                        return users.OrderBy(t => Math.Abs(t.Username.ToLower().CompareTo(Input))).ElementAt(0);
                    return null;

                }
            }
        }

        public static uint GetHighestRoleColor(IGuildUser User)
        {
            uint RawColor = Color.Default.RawValue;

            foreach (var RoleId in User.RoleIds.Reverse())
            {
                var Role = User.Guild.GetRole(RoleId);

                if (Role.Color.RawValue != Color.Default.RawValue)
                {
                    RawColor = Role.Color.RawValue;
                    break;
                }
            }

            return RawColor;
        }

        private static ulong GetHlId(string Highlight)
        {
            var Search = new Regex("<@!?(\\d*)>");
            foreach (Match Match in Search.Matches(Highlight))
            {
                return ulong.Parse(Match.Groups[1].Value);
            }
            return 0;
        }

        public static async Task<string> GetNicknameOrUsernameAsync(this ICommandContext Context, IUser User)
        {
            if (Context.Guild != null)
            {
                if ((await Context.Guild.GetUserAsync(User.Id)) is IGuildUser GuildUser)
                {
                    if (GuildUser.Nickname != null)
                        return GuildUser.Nickname;
                }
            }

            return User.Username;
        }
        public static string GetDisplayName(IUser User)
        {
            string Name = User.Username;
            if (User.Id == Global.Settings.OwnerId)
                Name += "#" + User.Discriminator;

            if (User is IGuildUser GuildUser)
            {
                if (GuildUser.Nickname != null)
                {
                    Name = (GuildUser.Nickname + " (" + Name + ")");
                }
            }

            return Name;
        }
        
        public static UserAccess GetUserAccess(ulong UserId, ulong GuildId)
        {
            if (UserId == Global.Settings.OwnerId)
                return UserAccess.Owner;
            else if (Global.IsGlobalAdmin(UserId))
                return UserAccess.GlobalAdmin;
            else if (Global.Client.GetGuild(GuildId).OwnerId == UserId)
                return UserAccess.ServerOwner;
            else if (Global.GuildSettings.GetSettings(GuildId).AdminIds.Contains(UserId))
                return UserAccess.Admin;

            return UserAccess.Common;
        }
        public static UserAccess GetUserAccess(ulong UserId, GuildSetting Settings, ulong GuildId)
        {
            if (UserId == Global.Settings.OwnerId)
                return UserAccess.Owner;
            else if (Global.IsGlobalAdmin(UserId))
                return UserAccess.GlobalAdmin;
            else if (Global.Client.GetGuild(GuildId).OwnerId == UserId)
                return UserAccess.ServerOwner;
            else if (Settings.AdminIds.Contains(UserId))
                return UserAccess.Admin;

            return UserAccess.Common;
        }

        public static string GetMaxResUrl(this IReadOnlyList<YoutubeExplode.Common.Thumbnail> thumbnails)
        {
            return thumbnails.Aggregate((curr, next) => curr.Resolution.Area > next.Resolution.Area ? curr : next).Url;
        }
    }
}
