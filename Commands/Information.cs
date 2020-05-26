using Chino_chan.Models;
using Chino_chan.Models.osuAPI;
using Chino_chan.Models.Privileges;
using Chino_chan.Models.Settings;
using Chino_chan.Modules;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chino_chan.Commands
{
    public class Information : ChinoContext
    {
        [Command("help"), Summary("Basic help command")]
        public async Task HelpAsync(params string[] args)
        {
            if (args.Length > 0)
            {
                var CommandNames = Global.CommandService.Commands.Select(t =>
                {
                    string Name = t.Name;
                    if (!string.IsNullOrWhiteSpace(t.Module.Group))
                        Name = t.Module.Group + (Name.Length > 0 ? " " : "") + Name;

                    return Name;
                });
                var Message = "";
                bool SendHelp = false;

                string arg = string.Join(" ", args).ToLower();

                if (!CommandNames.Contains(arg))
                {
                    SendHelp = true;
                }
                else
                {
                    Message += arg + ": ";
                    if (Language.Commands.ContainsKey(arg))
                    {
                        Message += GetHelp(arg) + "\n";
                    }
                    else
                    {
                        Message += GetEntry("NoHelp") + "\n";
                    }
                }

                if (SendHelp)
                    await SendHelpAsync();

                if (Message != "")
                {
                    await ReplyAsync(Message);
                }
            }
            else
            {
                await SendHelpAsync();
            }
        }

        [Command("ping"), Summary("Latency test owo")]
        public async Task PinkAsync(params string[] _)
        {
            var Message = await Context.Channel.SendMessageAsync("Pong!");
            await Message.ModifyAsync((MessageProps) =>
            {
                var Time = Message.Timestamp - Context.Message.Timestamp;
                var Seconds = Math.Truncate(Time.TotalMilliseconds / 1000);
                var Ms = Time.TotalMilliseconds - (Seconds * 1000);

                MessageProps.Content = $"Pong! `{ Seconds }s { Ms }ms`";
            });
        }

        [Command("git"), Summary("Sends the link to git repo owo")]
        public async Task SendGitAsync(params string[] _)
        {
            EmbedBuilder Builder = new EmbedBuilder()
            {
                Color = new Color(255, 0, 203)
            };

            string Name;
            string Avatar;

            if (Context.Guild != null)
            {
                var User = Global.Client.GetGuild(Context.Guild.Id).GetUser(Global.Client.CurrentUser.Id) as IGuildUser;

                Name = User.Nickname ?? User.Username;
                Avatar = User.GetAvatarUrl(size: 1024);
            }
            else
            {
                Name = Global.Client.CurrentUser.Username;
                Avatar = Global.Client.CurrentUser.GetAvatarUrl(size: 1024);
            }

            if (string.IsNullOrWhiteSpace(Global.Settings.GithubLink))
            {
                Builder.WithDescription(GetEntry("NoLink"));
            }
            else
            {
                Builder.WithDescription(GetEntry("ClickOnName"));
                Builder.WithAuthor(Name, Avatar, Global.Settings.GithubLink);
            }

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("info"), Summary("Sends a little information about me owo")]
        public async Task SendInfoAsync(params string[] _)
        {
            var CurrentProcess = Process.GetCurrentProcess();
            IUser Owner = Global.Client.GetUser(Global.Settings.OwnerId);
            if (Owner == null)
            {
                foreach (IGuild guild in Global.Client.Guilds)
                {
                    IUser u = guild.GetUserAsync(Global.Settings.OwnerId).Result;
                    if (u != null)
                    {
                        Owner = u;
                        break;
                    }
                }
            }
            List<ulong> userIds = new List<ulong>();
            foreach (SocketGuild guild in Global.Client.Guilds)
            {
                foreach (SocketGuildUser user in guild.Users)
                {
                    if (!userIds.Contains(user.Id))
                        userIds.Add(user.Id);
                }
            }
            int UserCount = userIds.Count;
            
            var Embed = new EmbedBuilder();

            Embed.WithAuthor(await Context.Guild.GetUserAsync(Context.Client.CurrentUser.Id));
            Embed.WithDescription($"**{ GetEntry("InfoAboutMe") }**\r\n");
            Embed.WithColor(255 << 16 | 050 << 8 | 230);

            Embed.AddField(new EmbedFieldBuilder()
            {
                IsInline = true,
                Name = GetEntry("MemUsage"),
                Value = (CurrentProcess.NonpagedSystemMemorySize64 + CurrentProcess.PagedMemorySize64) / 1048576 + "MB"
            });
            Embed.AddField(new EmbedFieldBuilder()
            {
                IsInline = true,
                Name = GetEntry("DCLib"),
                Value = DiscordConfig.Version
            });
            Embed.AddField(new EmbedFieldBuilder()
            {
                IsInline = true,
                Name = GetEntry("Creator"),
                Value = Owner.Username + "#" + Owner.Discriminator
            });
            Embed.AddField(new EmbedFieldBuilder()
            {
                IsInline = true,
                Name = GetEntry("Users"),
                Value = UserCount
            });
            Embed.AddField(new EmbedFieldBuilder()
            {
                IsInline = false,
                Name = GetEntry("Uptime"),
                Value = GetEntry("UptimeDesc", "D", Global.Uptime.Days.ToString(),
                                               "H", Global.Uptime.Hours.ToString(),
                                               "M", Global.Uptime.Minutes.ToString(),
                                               "S", Global.Uptime.Seconds.ToString()),
            });
            Embed.AddField(new EmbedFieldBuilder()
            {
                IsInline = false,
                Name = GetEntry("JoinedServers"),
                Value = GetEntry("JoinedServersDesc", "C", Global.Client.Guilds.Count.ToString())
            });

            await Context.Channel.SendMessageAsync("", embed: Embed.Build());
        }

        [Command("servers"), Summary("Displays which servers I'm on owo [10 / page]")]
        public async Task ServersAsync(int Page = 1)
        {
            string Description = "";

            int PageCount = (int)Math.Ceiling(Global.Client.Guilds.Count / 10.0f);

            if (Page < 1)
            {
                Description = GetEntry("NegativeOutOfRange");
                Page = 1;
            }
            else if (Page > PageCount)
            {
                Description = GetEntry("PositiveOutOfRange", "PC", PageCount.ToString());
                Page = PageCount;
            }

            int Index = ((Page - 1) * 10);
            
            for (int i = Index; i < Index + 10; i++)
            {
                if (i >= Global.Client.Guilds.Count)
                    break;

                SocketGuild Guild = Global.Client.Guilds.ElementAt(i);
                
                Description += "\n" + GetEntry("Line", "NAME", Guild.Name, "U", Guild.MemberCount.ToString(), "C", Guild.Channels.Count.ToString());
            }

            EmbedBuilder Builder = new EmbedBuilder()
            {
                Title = GetEntry("Servers"),
                Description = Description,
                Color = new Color(255, 0, 203),
                Footer = new EmbedFooterBuilder()
                {
                    Text = GetEntry("PageOf", "P", Page.ToString(), "PC", PageCount.ToString())
                }
            };
            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }
        
        [Command("vpsinfo"), Summary("Sends a little information about the vps owo")]
        public async Task VPSInfoAsync(params string[] _)
        {
            var Embed = new EmbedBuilder()
            {
                Color = Global.Pink,
                Title = GetEntry("ServerInfo")
            };

            var Os = Global.SysInfo.OS;
            var CPU = Global.SysInfo.CPU;
            var MemInfo = Global.SysInfo.MemInfo;
            var VideoCard = Global.SysInfo.VideoCardInfo;

            var MemFree = MemInfo.FreeMemory;
            var MemTotal = MemInfo.TotalMemory;
            var MemUsage = MemTotal - MemFree;
            
            Embed.AddField(new EmbedFieldBuilder()
            {
                Name = GetEntry("OS"),
                Value = GetEntry("OSDesc", "OS", Os.Name, "VER", Os.Version, "ARCH", Os.Architecture),
                IsInline = false
            });
            Embed.AddField(new EmbedFieldBuilder()
            {
                Name = GetEntry("CPU"),
                Value = GetEntry("CPUDesc", "CPU", CPU.Name, "SPEED", CPU.Speed.ToString(), "USAGE", "N/A"),
                IsInline = false
            });
            Embed.AddField(new EmbedFieldBuilder()
            {
                Name = GetEntry("MEM"),
                Value = GetEntry("MEMDesc", "USAGE", MemUsage.ToString(), "TOTAL", MemTotal.ToString(), "FREE", MemFree.ToString()),
                IsInline = false
            });
            string VcValue = GetEntry("NOGPU");
            if (VideoCard.VideoCards.Count != 0)
            {
                VcValue = string.Join("\n- ", VideoCard.VideoCards.Select(t => t.Name + " - " + t.RAM / 1024 / 1024 + "MB"));
            }
            Embed.AddField(new EmbedFieldBuilder()
            {
                Name = GetEntry("GPU"),
                Value = "- " + VcValue,
                IsInline = false
            });
            await Context.Channel.SendMessageAsync("", embed: Embed.Build());
        }

        [Command("invite"), Summary("Sends you my invitation link")]
        public async Task SendInviteAsync(params string[] _)
        {
            if (string.IsNullOrWhiteSpace(Global.Settings.InvitationLink))
            {
                await Context.Channel.SendMessageAsync(GetEntry("NoLink"));
            }
            else
            {
                EmbedBuilder Builder = new EmbedBuilder();

                var User = Global.Client.GetGuild(Context.Guild.Id).GetUser(Context.Client.CurrentUser.Id);
                Builder.WithAuthor(User.Nickname ?? User.Username, User.GetAvatarUrl(size: 1024), Global.Settings.InvitationLink);
                Builder.Description = GetEntry("ClickName");
                Builder.Color = new Color(0 << 16 | 255 << 8 | 255);

                await Context.Channel.SendMessageAsync("", embed: Builder.Build());
            }
        }

        [Command("chstats"), ServerCommand(), Summary("Gives you the channel message statistics by only channel name")]
        public async Task ChannelStatisticsAsync(params string[] Args)
        {
            string name = string.Join(" ", Args).ToLower();
            ITextChannel Channel = null;

            bool imgs = false;

            if (name == "")
            {
                Channel = Context.Channel as ITextChannel;
            }
            else if (name == "-c")
            {
                imgs = true;
                Channel = Context.Channel as ITextChannel;
            }
            else
            {
                IReadOnlyCollection<ITextChannel> channels = await Context.Guild.GetTextChannelsAsync();
                foreach (ITextChannel channel in channels)
                {
                    if (channel.Name.ToLower() == name || channel.Mention == name)
                    {
                        Channel = channel;
                        break ;
                    }
                }
            }
            
            if (Channel == null)
            {
                await Context.Channel.SendMessageAsync(GetEntry("NotFound"));
            }
            else
            {
                await Context.Channel.SendMessageAsync(GetEntry("Fetching", "CN", Channel.Name));

                Dictionary<ulong, int> Statistics = new Dictionary<ulong, int>();
                List<IMessage> Messages;
                IMessage Last = null;

                int Limit = 100;

                do
                {
                    if (Last == null)
                    {
                        Messages = await Channel.GetMessagesAsync(Limit).Flatten().ToListAsync();
                    }
                    else
                    {
                        Messages = await Channel.GetMessagesAsync(Last, Direction.Before, Limit).Flatten().ToListAsync();
                    }

                    Last = Messages[Messages.Count - 1];

                    for (int i = 0; i < Messages.Count; i++)
                    {
                        IMessage Current = Messages[i];

                        if (Current.Author.IsBot) continue;

                        int c = 1;

                        if (imgs)
                        {
                            c = 0;
                            if (Current.Attachments != null)
                            {
                                foreach (IAttachment attachment in Current.Attachments)
                                {
                                    string ext = Path.GetExtension(attachment.Filename);
                                    switch (ext)
                                    {
                                        case ".png":
                                        case ".gif":
                                        case ".jpeg":
                                        case ".jpg":
                                        case ".webp":
                                        case ".bmp":
                                            c++;
                                            break;
                                    }
                                }
                            }
                        }
                        if (c != 0)
                        {
                            if (Statistics.ContainsKey(Current.Author.Id))
                            {
                                Statistics[Current.Author.Id] += c;
                            }
                            else
                            {
                                Statistics.Add(Current.Author.Id, c);
                            }
                        }
                    }

                }
                while (Messages.Count == Limit);

                Statistics = Statistics.OrderByDescending(t => t.Value).ToDictionary(Pair => Pair.Key, Pair => Pair.Value);

                string Message = $"```css\n{ GetEntry("Most", "CN", Channel.Name) }:\n";
                for (int i = 0; i < 10; i++)
                {
                    KeyValuePair<ulong, int> Pair = Statistics.ElementAt(i);
                    Message += GetEntry("Line", "ID", (i + 1).ToString(), "U", Tools.GetName(Pair.Key, Context.Guild), "C", Pair.Value.ToString());

                    if (Statistics.Count - 1 == i) i = 10;
                }
                Message += "```";

                await Context.Channel.SendMessageAsync(Message);
            }
        }

        [Command("userinfo"), Summary("Gets information about you")]
        public async Task UserInfoAsync([Remainder]string args = "")
        {
            IUser User = Context.User;
            
            if (args.Length > 0)
            {
                User = Tools.ParseUser(args, false, Context);
            }
            if (User == null)
            {
                User = Context.User;
            }

            EmbedBuilder Builder = new EmbedBuilder()
            {
                Color = Global.Pink,
                Author = new EmbedAuthorBuilder()
                {
                    Name = GetEntry("InfoAbout", "U", User.Username),
                    IconUrl = User.GetAvatarUrl() ?? User.GetDefaultAvatarUrl(),
                    Url = User.GetAvatarUrl(size: 2048) ?? User.GetDefaultAvatarUrl()
                },
                Timestamp = DateTimeOffset.Now
            };

            Builder.ThumbnailUrl = Builder.Author.Url;

            Builder.AddField(GetEntry("UserInfo"), User.Username + "#" + User.Discriminator + "\n" + GetEntry("UserId") + User.Id, true);

            string Activity = GetEntry("Inactive");
            if (User.Activity != null)
                Activity = User.Activity.Type.ToString() + " " + User.Activity.Name;

            Builder.AddField(GetEntry("Activity"), Activity, true);
            Builder.AddField(GetEntry("Status"), User.Status.ToString(), true);
            Builder.AddField(GetEntry("UserCreated"), User.CreatedAt.ToString(), true);
            if (User is IGuildUser GuildUser)
            {
                Builder.AddField(GetEntry("JoinedAt"), GuildUser.JoinedAt.ToString(), true);
                Builder.AddField(GetEntry("Nickname"), GuildUser.Nickname ?? GetEntry("NoNickname"), true);
                string roleText = string.Join(", ", GuildUser.RoleIds.Where(t => t != Context.Guild.EveryoneRole.Id).Select(t => Context.Guild.GetRole(t).Name));
                if (string.IsNullOrWhiteSpace(roleText))
                {
                    roleText = GetEntry("NoRoles");
                }
                Builder.AddField(GetEntry("Roles"), roleText, true);
            }
            Builder.AddField(GetEntry("IsBot"), User.IsBot, true);
            Builder.AddField(GetEntry("FromWebhook"), User.IsWebhook, true);

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("serverinfo"), Summary("Gets information about the server"), ServerCommand()]
        public async Task ServerInfoAsync(params string[] _)
        {
            SocketGuild Guild = Global.Client.GetGuild(Context.Guild.Id);

            EmbedBuilder Builder = new EmbedBuilder()
            {
                Color = Global.Pink,
                Author = new EmbedAuthorBuilder()
                {
                    Name = GetEntry("InformationAbout", "SN", Context.Guild.Name),
                    IconUrl = Guild.IconUrl,
                    Url = Guild.IconUrl
                },
                Timestamp = DateTimeOffset.Now
            };

            Builder.ThumbnailUrl = Guild.IconUrl;
            Builder.AddField(GetEntry("Members"), Guild.MemberCount, true);
            Builder.AddField(GetEntry("AFK"), Guild.AFKChannel?.Name ?? GetEntry("NOAFK"), true);
            Builder.AddField(GetEntry("Main"), Guild.DefaultChannel.Name, true);
            Builder.AddField(GetEntry("Text"), Guild.TextChannels.Count, true);
            Builder.AddField(GetEntry("Voice"), Guild.VoiceChannels.Count, true);
            Builder.AddField(GetEntry("CreatedAt"), Guild.CreatedAt.ToString(), true);
            Builder.AddField(GetEntry("GuildId"), Guild.Id, true);
            Builder.AddField(GetEntry("Owner"), Tools.GetDisplayName(Guild.Owner), true);

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("contact"), Summary("Use this if you want to contact with the bot owner owo use: contact [message]")]
        public async Task ContactAsync(params string[] Args)
        {
            if (Args.Length != 0)
            {
                EmbedBuilder Builder = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.User.Username + "#" + Context.User.Discriminator + " contacted!",
                        IconUrl = Context.User.GetAvatarUrl(),
                        Url = Context.User.GetAvatarUrl(size: 2048)
                    },
                    Description = string.Join(" ", Args),
                    Color = Global.Pink
                };
                if (Global.Settings.DevServer.Id != 0)
                {
                    if (Global.Client.GetGuild(Global.Settings.DevServer.Id) is IGuild Guild)
                    {
                        List<ITextChannel> Channels = (await Guild.GetTextChannelsAsync()).ToList();
                        ITextChannel Feedback = Channels.Find(t => t.Name.ToLower() == "feedback");

                        if (Feedback == null)
                        {
                            try
                            {
                                Feedback = await Guild.CreateTextChannelAsync("feedback");
                            }
                            catch { } // Has no permission to create a text channel
                        }

                        if (Feedback != null)
                        {
                            try
                            {
                                await Feedback.SendMessageAsync("", embed: Builder.Build());
                                await Context.Channel.SendMessageAsync(GetEntry("Forwarded"));
                                return;
                            }
                            catch { } // Has no permission to send a message
                        }
                    }
                }
                if (Global.Settings.OwnerId != 0)
                {
                    IUser User = Global.Client.GetUser(Global.Settings.OwnerId);
                    if (User == null)
                    {
                        foreach (IGuild guild in Global.Client.Guilds)
                        {
                            IUser u = await guild.GetUserAsync(Global.Settings.OwnerId);
                            if (u != null)
                            {
                                User = u;
                                break;
                            }
                        }
                    }
                    if (User != null)
                    {
                        IDMChannel Dm = await User.GetOrCreateDMChannelAsync();

                        if (Dm != null)
                        {
                            try
                            {
                                await Dm.SendMessageAsync("", embed: Builder.Build());
                                await Context.Channel.SendMessageAsync(GetEntry("Forwarded"));
                                return;
                            }
                            catch { } // Has no permisson to send a message to the dm channel
                        }
                    }
                }

                await Context.Channel.SendMessageAsync(GetEntry("NoInfo"));
            }
        }

        [Command("joinmessage"), Alias("joinmsg"), Summary("Greets and user after joining the server, type joinmsg for more information"), ServerCommand(), ServerOwner()]
        public async Task JoinMessageAsync(params string[] Args)
        {
            bool Send = false;

            if (Args.Length == 0)
            {
                if (Settings.Greet == null)
                {
                    await Context.Channel.SendMessageAsync(GetEntry("NoJoinMsg") + "\n" + GetHelp("joinmessage"));
                }
                else
                {
                    Send = true;
                }
            }
            else
            {
                Greet Greet = Settings.Greet;
                bool Save = false;

                switch (Args[0].ToLower())
                {
                    case "embed":
                        if (Greet == null)
                        {
                            await Context.Channel.SendMessageAsync(GetEntry("SetMessage"));
                        }
                        else if (Args.Length == 1)
                        {
                            await Context.Channel.SendMessageAsync(GetHelp("joinmessage"));
                        }
                        else
                        {
                            switch (Args[1].ToLower())
                            {
                                case "enable":
                                    Greet.SendEmbed = true;
                                    if (Greet.Embed == null)
                                        Greet.Embed = new GreetEmbed();

                                    await Context.Channel.SendMessageAsync(GetEntry("EmbedEnabled"));
                                    Save = true;
                                    break;
                                case "disable":
                                    Greet.SendEmbed = true;
                                    await Context.Channel.SendMessageAsync(GetEntry("EmbedDisabled"));
                                    Save = true;
                                    break;
                                case "set":
                                    if (Greet.Embed == null)
                                        Greet.Embed = new GreetEmbed();

                                    List<string> Set = new List<string>();

                                    string Title = null;
                                    bool? Include = null;
                                    Color? Color = null;

                                    bool IsTitle = false;

                                    for (int i = 2; i < Args.Length; i++)
                                    {
                                        string Current = Args[i];

                                        if (Current.ToLower().StartsWith("title:"))
                                        {
                                            IsTitle = true;
                                            Title = Current.Substring(6);

                                            Set.Add(GetEntry("Title"));
                                        }
                                        else if (Current.ToLower().StartsWith("avatar:"))
                                        {
                                            IsTitle = false;
                                            string src = Current.Substring(7);

                                            if (bool.TryParse(src, out bool val))
                                            {
                                                Include = val;
                                            }
                                            Set.Add(GetEntry("Avatar"));
                                        }
                                        else if (Current.ToLower().StartsWith("color:"))
                                        {
                                            IsTitle = false;
                                            string[] src = Current.Substring(6).Split(',');

                                            if (src.Length == 3)
                                            {
                                                bool Success = byte.TryParse(src[0], out byte R);

                                                if (!Success || R < 0 || R > 255)
                                                    continue;

                                                Success = byte.TryParse(src[1], out byte G);

                                                if (!Success || G < 0 || G > 255)
                                                    continue;

                                                Success = byte.TryParse(src[2], out byte B);

                                                if (!Success || B < 0 || B > 255)
                                                    continue;
                                                
                                                Color = new Color(R, G, B);
                                                Set.Add(GetEntry("Color"));
                                            }
                                        }
                                        else if (IsTitle)
                                        {
                                            Title += (" " + Current);
                                        }
                                    }
                                    
                                    if (Set.Count == 0)
                                    {
                                        await Context.Channel.SendMessageAsync(GetEntry("NoValues"));
                                    }
                                    else
                                    {
                                        if (Title != null)
                                        {
                                            Greet.Embed.Title = Title;
                                        }
                                        if (Include.HasValue)
                                        {
                                            Greet.Embed.IncludeAvatar = Include.Value;
                                        }
                                        if (Color.HasValue)
                                        {
                                            Greet.Embed.Color = Color.Value;
                                        }

                                        await Context.Channel.SendMessageAsync(GetEntry("Set", "VALUES", string.Join(", ", Set.Distinct())));
                                        Save = true;
                                        Send = true;
                                    }
                                    break;
                            }
                        }
                        break;
                    case "enable":
                        Greet.Enabled = true;
                        await Context.Channel.SendMessageAsync(GetEntry("Enabled"));
                        Save = true;
                        break;
                    case "disable":
                        Greet.Enabled = false;
                        await Context.Channel.SendMessageAsync(GetEntry("Disabled"));
                        Save = true;
                        break;
                    case "channel":
                        if (Greet == null)
                        {
                            await Context.Channel.SendMessageAsync(GetEntry("NoJoinMsg") + "\n" + GetHelp("joinmessage"));
                        }
                        else
                        {
                            if (Args.Length == 0)
                            {
                                Greet.ChannelId = Context.Channel.Id;
                                Save = true;
                                await Context.Channel.SendMessageAsync(GetEntry("ChannelSet", "CH", Greet.ChannelId.ToString()));
                            }
                            else if (Context.Message.MentionedChannelIds.Count > 1)
                            {
                                await Context.Channel.SendMessageAsync(GetEntry("ChannelOnlyOneMention"));
                            }
                            else if (Context.Message.MentionedChannelIds.Count == 1)
                            {
                                Greet.ChannelId = Context.Message.MentionedChannelIds.First();
                                Save = true;
                                await Context.Channel.SendMessageAsync(GetEntry("ChannelSet", "CH", Greet.ChannelId.ToString()));
                            }
                            else
                            {
                                string input = string.Join(" ", Args);
                                List<ITextChannel> channels = new List<ITextChannel>(await Context.Guild.GetTextChannelsAsync());
                                ITextChannel partialMatch = null;
                                bool set = false;

                                foreach (ITextChannel channel in channels)
                                {
                                    if (channel.Name.ToLower() == input)
                                    {
                                        Greet.ChannelId = channel.Id;
                                        Save = true;
                                        set = true;
                                        await Context.Channel.SendMessageAsync(GetEntry("ChannelSet", "CH", Greet.ChannelId.ToString()));
                                        break;
                                    }
                                    else if (channel.Name.ToLower().Contains(input))
                                    {
                                        partialMatch = channel;
                                    }
                                }
                                if (!set)
                                {
                                    if (partialMatch == null)
                                    {
                                        await Context.Channel.SendMessageAsync(GetEntry("ChannelNotFound"));
                                    }
                                    else
                                    {
                                        Greet.ChannelId = partialMatch.Id;
                                        Save = true;
                                        await Context.Channel.SendMessageAsync(GetEntry("ChannelSet", "CH", Greet.ChannelId.ToString()));
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        string Arg = string.Join(" ", Args);
                        if (Greet == null)
                        {
                            Greet = new Greet(Arg, Context.Channel.Id);
                        }
                        else
                        {
                            Greet.Message = Arg;
                            Greet.ChannelId = Context.Channel.Id;
                        }

                        Save = true;
                        Send = true;
                        break;
                }
                
                if (Save)
                {
                    Global.GuildSettings.Modify(Context.Guild.Id, Settings =>
                    {
                        Settings.Greet = Greet;
                    });
                }
            }

            if (Send)
            {
                var Resp = Global.GreetMessage(Context.User as IGuildUser);
                await Context.Channel.SendMessageAsync(Resp.Item1, embed: Resp.Item2);
            }
        }

        private async Task SendHelpAsync()
        {
            string Message = "```css\n";

            var AvailableCommands = new Dictionary<string, List<CommandInfo>>();

            foreach (var Command in Global.CommandService.Commands)
            {
                var Result = await Command.CheckPreconditionsAsync(Context);
                if (Result.IsSuccess)
                {
                    if (AvailableCommands.TryGetValue(Command.Module.Name, out List<CommandInfo> Infos))
                    {
                        var List = new List<CommandInfo>(Infos)
                        {
                            Command
                        };
                        AvailableCommands[Command.Module.Name] = List;
                    }
                    else
                    {
                        AvailableCommands.Add(Command.Module.Name, new List<CommandInfo>() { Command });
                    }
                }
            }

            foreach (var AvailableCommand in AvailableCommands)
            {
                Message += ("#" + AvailableCommand.Key + "\n");
                Message += string.Join(", ", AvailableCommand.Value.Select(t =>
                {
                    string Name = t.Name;
                    if (!string.IsNullOrWhiteSpace(t.Module.Group))
                        Name = t.Module.Group + (Name.Length > 0 ? " " : "") + Name;

                    return Name;
                }));
                Message += "\n\n";
            }
            Message += $"\n{ GetEntry("FurtherHelp") }```";

            var DMChannel = await Context.User.GetOrCreateDMChannelAsync();
            try
            {
                await DMChannel.SendMessageAsync(Message);
                if (Context.Channel.Id != DMChannel.Id)
                    await ReplyAsync(GetEntry("CheckDMs", "MENTION", Context.User.Mention));
            }
            catch (HttpException)
            {
                await Context.Channel.SendMessageAsync(GetEntry("CouldNotSendDM"));
            }

        }
    }
}
