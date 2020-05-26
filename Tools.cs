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
        
        public static IGuildUser ParseUser(string Input, bool SearchGlobally, ICommandContext Context = null)
        {
            if (Input.Trim() == "")
                return null;

            if (Context.Message.MentionedUserIds.Count > 0 && Context.Channel is IGuildChannel guildChannel)
            {
                return guildChannel.GetUserAsync(Context.Message.MentionedUserIds.First()).Result;
            }

            Input = Input.ToLower();

            var ReturnedUsers = new List<IGuildUser>();
            var Users = new List<IGuildUser>();
            
            if (SearchGlobally)
            {
                ulong Id = 0;
                if (Context?.Guild != null)
                {
                    Id = Context.Guild.Id;
                    SocketGuild guild = Global.Client.GetGuild(Id);
                    guild.DownloadUsersAsync().GetAwaiter().GetResult();
                    Users.AddRange(Global.Client.GetGuild(Id).Users);
                }
                foreach (var Guild in Global.Client.Guilds)
                {
                    if (Id == Guild.Id) continue;
                    Guild.DownloadUsersAsync().GetAwaiter().GetResult();
                    Users.AddRange(Guild.Users);
                }
            }
            else
            {
                if (Context?.Guild != null)
                {
                    Users.AddRange(Context.Guild.GetUsersAsync().Result);
                }
            }
            bool checkId = ulong.TryParse(Input, out ulong ParsedId);
            if (!checkId)
            {
                ParsedId = GetHlId(Input);
                checkId = ParsedId != 0;
            }
            for (int i = 0; i < Users.Count; i++)
            {
                IGuildUser User = Users[i];

                if (checkId)
                {
                    if (User.Id == ParsedId)
                    {
                        ReturnedUsers.Add(User);
                        break;
                    }
                }
                if (User.Username.ToLower().Contains(Input) || (User.Nickname ?? "").ToLower().Contains(Input))
                {
                    ReturnedUsers.Add(User);
                }
            }

            for (int i = 0; i < ReturnedUsers.Count; i++)
            {
                IGuildUser User = ReturnedUsers[i];

                if (User.Username.ToLower() == Input || (User.Nickname ?? "").ToLower() == Input)
                    return ReturnedUsers[i];
            }

            if (ReturnedUsers.Count > 0)
                return ReturnedUsers.OrderBy(t => Math.Abs(t.Username.ToLower().CompareTo(Input))).ElementAt(0);
            else return null;
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
    }
}
