using Chino_chan.Models;
using Chino_chan.Models.Poll;
using Chino_chan.Models.Privileges;
using Chino_chan.Models.Settings;
using Chino_chan.Models.Settings.Language;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Chino_chan.Commands
{
    public class Management : ChinoContext
    {
        [Command("purgedm"), Summary("Deletes all of my messages from our direct message channel")]
        public async Task PurgeDMAsync(params string[] _)
        {
            IDMChannel DmChannel = await Context.User.CreateDMChannelAsync();
            if (Context.Channel.Id == DmChannel.Id)
            {
                int MessageCount = 0;
                int Limit = 100;
                ulong LastMessageId = 0;
                IEnumerable<IMessage> Messages;
                do
                {
                    do
                    {
                        if (LastMessageId != 0)
                            Messages = await DmChannel.GetMessagesAsync(LastMessageId, Direction.Before, Limit).Flatten().ToListAsync();
                        else
                            Messages = await DmChannel.GetMessagesAsync(Limit).Flatten().ToListAsync();

                        if (LastMessageId == Messages.Last().Id)
                        {
                            Limit = 0;
                            break;
                        }
                        LastMessageId = Messages.Last().Id;
                    }
                    while (!Messages.Select(t => t.Author.Id).Contains(Context.Client.CurrentUser.Id));

                    if (Limit == 0)
                        break;

                    MessageCount = Messages.Count();

                    foreach (IMessage Message in Messages)
                    {
                        if (Message.Author.Id == Context.Client.CurrentUser.Id)
                        {
                            await Message.DeleteAsync();
                        }
                        else
                        {
                            LastMessageId = Message.Id;
                        }
                    }
                }
                while (Limit == MessageCount);
            }
        }

        [Command("prefix"), Admin(), Summary("Changes my prefix owo")]
        public async Task ChangePrefixAsync(params string[] Prefix)
        {
            if (Prefix.Length == 0)
            {
                await Context.Channel.SendMessageAsync(GetEntry("CurrentPrefix"));
            }
            else
            {
                Global.GuildSettings.Modify(Settings.GuildId, Modi =>
                {
                    Modi.Prefix = string.Join(" ", Prefix);
                });
                CommandName = "prefix";
                string text = GetEntry("PrefixChanged");
                await Context.Channel.SendMessageAsync(text);
            }
        }
        
        [Command("language"), Alias("lang"), Admin, Summary("Changes server language")]
        public async Task ChangeLanguageAsync([Remainder]string Language = "")
        {
            EmbedBuilder builder = new EmbedBuilder()
            {
                Color = Color.Green,
                Title = GetEntry("Language")
            };

            if (Language == "")
            {
                builder.Description = GetEntry("CurrentLanguage", "L", "\n- " + string.Join("\n- ", Global.Languages.Languages.Select(t =>
                {
                    IUser user = Global.Client.GetUser(t.Value.By);
                    if (user == null)
                    {
                        foreach (IGuild guild in Global.Client.Guilds)
                        {
                            IUser u = guild.GetUserAsync(t.Value.By).Result;
                            if (u != null)
                            {
                                user = u;
                                break;
                            }
                        }
                    }
                    return $"ID: { t.Key } | { t.Value.NameTranslated } ({ t.Value.Name }) - { user.Username }";
                })));
            }
            else
            {
                LanguageEntry lang = Global.Languages.GetLanguageNullDefault(Language);
                if (lang == null)
                {
                    builder.Description = GetEntry("LanguageNotFound");
                }
                else
                {
                    Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                    {
                        Settings.Language = Language;
                    });
                    builder.Title = lang.GetEntry("language:Language");
                    builder.Description = lang.GetEntry("language:LanguageChanged");
                }
            }

            await Context.Channel.SendMessageAsync("", embed: builder.Build());
        }

        [Command("purge"), ServerOwner(), Summary("Purges the current channel (give no | don't | false to avoid the \"This channel has been purged~\" text)")]
        public async Task PurgeChannelAsync(params string[] _)
        {
            var DMChannel = await Context.User.CreateDMChannelAsync();
            if (DMChannel.Id == Context.Channel.Id)
            {
                await Context.Channel.SendMessageAsync(GetEntry("NotForDMs"));
            }
            else
            {
                List<IMessage> Messages;
                int Limit = 100;
                List<IMessage> Bulkable = new List<IMessage>();

                do
                {
                    Messages = await Context.Channel.GetMessagesAsync(Limit).Flatten().ToListAsync();
                    
                    foreach (IMessage Message in Messages)
                    {
                        if (Message.CreatedAt > DateTime.Now.AddDays(-13))
                        {
                            Bulkable.Add(Message);
                        }
                        else
                        {
                            await Message.DeleteAsync();
                            await Task.Delay(500);
                        }
                    }
                    
                    if (Bulkable.Count > 0)
                        await (Context.Channel as ITextChannel).DeleteMessagesAsync(Bulkable);
                }
                while (Messages.Count == Limit);
                
                IMessage message = await Context.Channel.SendMessageAsync(GetEntry("Purged"));
                await Task.Delay(5000);
                try
                {
                    await message.DeleteAsync();
                }
                catch { } // message already deleted
            }
        }

        [Command("ban"), Admin(), ServerCommand(), Summary("Bans a user, usage: ban (id, username, nickname or highlihgt) [reason] [prune messages from the past 0-7 days]\nIf you use username or nickname, and it contains space, please consider using \"\" otherwise it may ban the wrong person")]
        public async Task BanAsync(params string[] Args)
        {
            IGuildUser User = null;
            string Reason = GetEntry("NoReasons");
            int Prune = 0;

            string Message = "";

            if (Args.Length == 1)
                User = Tools.ParseUser(Args[0], false, Context) as IGuildUser;
            else if (Args.Length > 1)
            {
                User = Tools.ParseUser(Args[0], false, Context);

                Args = Args.Skip(1).ToArray();

                if (!int.TryParse(Args[Args.Length - 1], out Prune))
                    Prune = 0;
                else
                {
                    if (Prune > 7)
                    {
                        Message = GetEntry("PruneOutOfRange");
                        Prune = 7;
                    }
                    else if (Prune < 0)
                    {
                        Message = GetEntry("PruneOutOfRange");
                        Prune = 0;
                    }
                    Args = Args.Take(Args.Length - 1).ToArray();
                }
                if (Args.Length > 0)
                    Reason = string.Join(" ", Args);
            }

            if (User == null)
            {
                await Context.Channel.SendMessageAsync(GetGlobalEntry("UserNotFound"));
                return;
            }
            else
            {
                try
                {
                    EmbedBuilder Builder = new EmbedBuilder()
                    {
                        Author = new EmbedAuthorBuilder()
                        {
                            IconUrl = User.GetAvatarUrl(),
                            Name = GetEntry("New"),
                            Url = User.GetAvatarUrl(size: 2048)
                        },
                        Color = Color.DarkRed
                    };

                    string Name = User.Nickname;

                    if (Name == null)
                        Name = User.Username;
                    else
                        Name += $" ({ User.Username })";

                    Builder.AddField(GetEntry("BannedUser"), string.Join("#", Name, User.Discriminator));
                    Builder.AddField(GetEntry("Reason"), Reason, true);
                    Builder.AddField(GetEntry("UserId"), User.Id, true);
                    if (Prune != 0)
                        Builder.AddField(GetEntry("Prune"), GetEntry("BackTo", "DAYS", Prune.ToString()), true);

                    await Context.Guild.AddBanAsync(User, Prune, Reason);

                    await Context.Channel.SendMessageAsync(Message, embed: Builder.Build());
                }
                catch
                {
                    await Context.Channel.SendMessageAsync(GetEntry("CouldNotBan", "USERNAME", User.Nickname ?? User.Username));
                }
            }
        }

        [Command("unban"), Admin(), ServerCommand(), Summary("Revokes a ban assoicated with the specific user: unban user_highlight")]
        public async Task UnbanAsync(params string[] Args)
        {
            if (Args.Length == 0)
            {
                await Context.Channel.SendMessageAsync(GetGlobalEntry("UserNotFound"));
            }
            else
            {
                List<IBan> bans = new List<IBan>(await Context.Guild.GetBansAsync().FlattenAsync());

                bool revoke = false;
                string input = string.Join(" ", Args).ToLower();

                foreach (IBan ban in bans)
                {
                    if (!ulong.TryParse(input, out ulong Id))
                        Id = 0;

                    if (ban.User.Username.ToLower() == input || ban.User.Mention.ToLower() == input || Id == ban.User.Id)
                    {
                        revoke = true;
                        try
                        {
                            await Context.Guild.RemoveBanAsync(ban.User);
                            await Context.Channel.SendMessageAsync(GetEntry("BanRevoked"));
                        }
                        catch
                        {
                            await Context.Channel.SendMessageAsync(GetEntry("NoPermission"));
                        }

                        break;
                    }
                }

                if (!revoke)
                {
                    await Context.Channel.SendMessageAsync(GetEntry("NoBanFound"));
                }
            }
        }

        [Command("joinrole"), Admin(), ServerCommand(), Summary("When a user joins, these roles will be given to them")]
        public async Task JoinRoleAsync(params string[] Args)
        {
            if (Args.Length > 1)
            {
                string Mode = Args[0].ToLower();
                string RoleName = "";

                if (Mode != "add" && Mode != "remove")
                {
                    RoleName = string.Join(" ", Args).ToLower();
                }
                else
                {
                    RoleName = string.Join(" ", Args.Skip(1)).ToLower();
                }


                IRole Role = null;
                foreach (IRole GuildRole in Context.Guild.Roles)
                {
                    if (GuildRole.Id.ToString() == RoleName || GuildRole.Name.ToLower() == RoleName || GuildRole.Mention.ToLower() == RoleName)
                    {
                        Role = GuildRole;
                        break;
                    }
                }

                if (Role == null)
                {
                    await Context.Channel.SendMessageAsync(GetEntry("RoleNotFound"));
                    return;
                }
                
                if (Mode == "add")
                {
                    if (Settings.NewMemberRoles.Contains(Role.Id))
                    {
                        await Context.Channel.SendMessageAsync(GetEntry("AlreadyAdded"));
                    }
                    else
                    {
                        Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                        {
                            Settings.NewMemberRoles.Add(Role.Id);
                        });
                        await Context.Channel.SendMessageAsync(GetEntry("Added", "ROLENAME", Role.Name));
                    }
                }
                else if (Mode == "remove")
                {
                    if (!Settings.NewMemberRoles.Contains(Role.Id))
                    {
                        await Context.Channel.SendMessageAsync(GetEntry("NotAssigned"));
                    }
                    else
                    {
                        Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                        {
                            Settings.NewMemberRoles.Remove(Role.Id);
                        });
                        await Context.Channel.SendMessageAsync(GetEntry("Removed", "ROLENAME", Role.Name));
                    }
                }
                else
                {
                    if (Settings.NewMemberRoles.Contains(Role.Id))
                    {
                        await Context.Channel.SendMessageAsync(GetEntry("Assigned"));
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync(GetEntry("NotAssigned"));
                    }
                }

                return;
            }

            List<ulong> Ids = Settings.NewMemberRoles;

            string Message = "";

            if (Ids.Count > 0)
            {
                List<ulong> RemoveIds = new List<ulong>();
                Message = GetEntry("ListAutoAssigned") + "\n```css\n";

                foreach (ulong Id in Ids)
                {
                    IRole Role = Context.Guild.GetRole(Id);

                    if (Role == null)
                        RemoveIds.Add(Id);
                    else
                        Message += Role.Name + "\n";
                }
                Message += "```";
                if (RemoveIds.Count > 0)
                {
                    Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                    {
                        foreach (ulong Id in RemoveIds)
                        {
                            Settings.NewMemberRoles.Remove(Id);
                        }
                    });
                }
            }
            else
            {
                Message = GetEntry("NoAutoAssigned");
            }

            await Context.Channel.SendMessageAsync(Message);
        }

        [Command("kick"), Admin(), ServerCommand(), Summary("Kicks a user")]
        public async Task KickAsync(params string[] Args)
        {
            IGuildUser User = null;
            string Reason = GetEntry("NoReason");

            string Message = "";

            if (Args.Length == 1)
                User = Tools.ParseUser(Args[0], false, Context) as IGuildUser;
            else if (Args.Length > 1)
            {
                User = Tools.ParseUser(Args[0], false, Context);
                Reason = string.Join(" ", Args, 1, Args.Length - 1);
            }

            if (User == null)
            {
                await Context.Channel.SendMessageAsync(GetGlobalEntry("UserNotFound"));
                return;
            }
            else
            {
                try
                {
                    EmbedBuilder Builder = new EmbedBuilder()
                    {
                        Author = new EmbedAuthorBuilder()
                        {
                            IconUrl = User.GetAvatarUrl(),
                            Name = GetEntry("NewKick"),
                            Url = User.GetAvatarUrl(size: 2048)
                        },
                        Color = Color.DarkMagenta
                    };

                    string Name = User.Nickname;

                    if (Name == null)
                        Name = User.Username;
                    else
                        Name += $" ({ User.Username })";

                    Builder.AddField(GetEntry("KickedUser"), string.Join("#", Name, User.Discriminator));
                    Builder.AddField(GetEntry("Reason"), Reason);

                    await User.KickAsync(Reason);

                    await Context.Channel.SendMessageAsync(Message, embed: Builder.Build());
                }
                catch
                {
                    await Context.Channel.SendMessageAsync(GetEntry("CouldNotKick", "NAME", Tools.GetDisplayName(User)));
                }
            }
        }
        
        [Command("block"), Summary("Basic block management"), Admin, ServerCommand]
        public async Task BlockAsync(params string[] Args)
        {
            bool IsGlobalAdmin = Global.IsGlobalAdminOrHigher(Context.User.Id);
            
            if (Context.Message.MentionedRoleIds.Count == 0 && Context.Message.MentionedUserIds.Count == 0)
            {
                await Context.Channel.SendMessageAsync(GetEntry("MissingMention"));
                return;
            }
            List<string> args = new List<string>(Args);
            foreach (string arg in Args)
            {
                foreach (ulong id in Context.Message.MentionedUserIds.Concat(Context.Message.MentionedRoleIds))
                {
                    if (arg.Contains(id.ToString()))
                    {
                        args.Remove(arg);
                        break;
                    }
                }
            }

            EmbedBuilder builder = new EmbedBuilder()
            {
                Title = "Block",
                Color = new Color(0, 255, 255)
            };
            if (args.Count == 0)
            {
                foreach (IRole Role in Context.Message.MentionedRoleIds.Select(t => Context.Guild.GetRole(t)))
                {
                    EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder()
                    {
                        Name = Role.Name
                    };
                    if (Settings.BlockedRoles.Contains(Role.Id))
                    {
                        fieldBuilder.Value = GetEntry("Blocked", "NAME", Role.Name);
                    }
                    else
                    {
                        fieldBuilder.Value = GetEntry("NotBlocked", "NAME", Role.Name);
                    }
                    builder.AddField(fieldBuilder);
                }
                foreach (IGuildUser User in Context.Message.MentionedUserIds.Select(t => Global.Client.GetGuild(Context.Guild.Id).GetUser(t)))
                {
                    EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder()
                    {
                        Name = User.Nickname ?? User.Username
                    };
                    if (Global.IsBlocked(User.Id) || Global.IsBlocked(User.Id, Context.Guild.Id))
                    {
                        fieldBuilder.Value = GetEntry("Blocked", "NAME", User.Nickname ?? User.Username);
                    }
                    else
                    {
                        fieldBuilder.Value = GetEntry("NotBlocked", "NAME", User.Nickname ?? User.Username);
                    }
                    builder.AddField(fieldBuilder);
                }
            }
            else
            {
                bool save = false;
                switch (args[0].ToLower())
                {
                    case "add":
                        foreach (IRole Role in Context.Message.MentionedRoleIds.Select(t => Context.Guild.GetRole(t)))
                        {
                            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder()
                            {
                                Name = Role.Name
                            };
                            if (Settings.BlockedRoles.Contains(Role.Id))
                            {
                                fieldBuilder.Value = GetEntry("AlreadyBlocked", "NAME", Role.Name);
                            }
                            else
                            {
                                Settings.BlockedRoles.Add(Role.Id);
                                fieldBuilder.Value = GetEntry("Blocked", "NAME", Role.Name);
                                save = true;
                            }
                            builder.AddField(fieldBuilder);
                        }
                        foreach (IGuildUser User in Context.Message.MentionedUserIds.Select(t => Global.Client.GetGuild(Context.Guild.Id).GetUser(t)))
                        {
                            string name = User.Nickname ?? User.Username;
                            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder()
                            {
                                Name = name
                            };

                            if (Global.IsBlocked(User.Id, User.GuildId))
                            {
                                fieldBuilder.Value = GetEntry("AlreadyBlocked", "NAME", name);
                            }
                            else
                            {
                                Settings.Blocked.Add(new BlockedUser()
                                {
                                    Id = User.Id,
                                    Who = Context.User.Id,
                                    Reason = string.Join(" ", args.Skip(1))
                                });
                                save = true;

                                fieldBuilder.Value = GetEntry("Blocked", "NAME", name);
                            }
                            builder.AddField(fieldBuilder);
                        }

                        if (save)
                        {
                            Global.GuildSettings.Save();
                        }
                        break;
                    case "addglobal":
                        if (!IsGlobalAdmin)
                        {
                            builder.Description = GetEntry("OnlyGlobalAdmin");
                            break;
                        }
                        if (Context.Message.MentionedRoleIds.Count > 0)
                        {
                            builder.Description = GetEntry("GlobalOnlyUser");
                        }
                        foreach (IGuildUser User in Context.Message.MentionedUserIds.Select(t => Global.Client.GetGuild(Context.Guild.Id).GetUser(t)))
                        {
                            string name = User.Nickname ?? User.Username;
                            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder()
                            {
                                Name = name
                            };

                            if (Global.IsBlocked(User.Id))
                            {
                                fieldBuilder.Value = GetEntry("AlreadyBlocked", "NAME", name);
                            }
                            else
                            {
                                Global.Settings.GloballyBlocked.Add(new BlockedUser()
                                {
                                    Id = User.Id,
                                    Who = Context.User.Id,
                                    Reason = string.Join(" ", args.Skip(1))
                                });
                                save = true;

                                fieldBuilder.Value = GetEntry("Blocked", "NAME", name);
                            }
                            builder.AddField(fieldBuilder);
                        }

                        if (save)
                        {
                            Global.SaveSettings();
                        }
                        break;
                    case "remove":
                        foreach (IRole Role in Context.Message.MentionedRoleIds.Select(t => Context.Guild.GetRole(t)))
                        {
                            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder()
                            {
                                Name = Role.Name
                            };
                            if (Settings.BlockedRoles.Contains(Role.Id))
                            {
                                Settings.BlockedRoles.Remove(Role.Id);
                                fieldBuilder.Value = GetEntry("RemovedBlock", "NAME", Role.Name);
                                save = true;
                            }
                            else
                            {
                                fieldBuilder.Value = GetEntry("NotBlocked", "NAME", Role.Name);
                            }
                            builder.AddField(fieldBuilder);
                        }
                        foreach (IGuildUser User in Context.Message.MentionedUserIds.Select(t => Global.Client.GetGuild(Context.Guild.Id).GetUser(t)))
                        {
                            string name = User.Nickname ?? User.Username;
                            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder()
                            {
                                Name = name
                            };

                            if (Global.IsBlocked(User.Id, User.GuildId))
                            {
                                Settings.Blocked.RemoveAll(t => t.Id == User.Id);
                                save = true;
                                fieldBuilder.Value = GetEntry("RemovedBlock", "NAME", name);
                            }
                            else
                            {
                                fieldBuilder.Value = GetEntry("NotBlocked", "NAME", name);
                            }
                            builder.AddField(fieldBuilder);
                        }

                        if (save)
                        {
                            Global.GuildSettings.Save();
                        }
                        break;
                    case "removeglobal":
                        if (!IsGlobalAdmin)
                        {
                            builder.Description = GetEntry("OnlyGlobalAdmin");
                            break;
                        }
                        if (Context.Message.MentionedRoleIds.Count > 0)
                        {
                            builder.Description = GetEntry("GlobalOnlyUser");
                        }
                        foreach (IGuildUser User in Context.Message.MentionedUserIds.Select(t => Global.Client.GetGuild(Context.Guild.Id).GetUser(t)))
                        {
                            string name = User.Nickname ?? User.Username;
                            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder()
                            {
                                Name = name
                            };

                            if (Global.IsBlocked(User.Id))
                            {
                                Global.Settings.GloballyBlocked.RemoveAll(t => t.Id == User.Id);
                                save = true;
                                fieldBuilder.Value = GetEntry("RemovedBlock", "NAME", name);
                            }
                            else
                            {
                                fieldBuilder.Value = GetEntry("NotBlocked", "NAME", name);
                            }
                            builder.AddField(fieldBuilder);
                        }

                        if (save)
                        {
                            Global.SaveSettings();
                        }
                        break;
                    default:
                        builder.Description = GetEntry("CheckHelp");
                        break;
                }
            }

            await Context.Channel.SendMessageAsync("", embed: builder.Build());
        }

        [Command("delete"), ServerCommand(), Admin(), Summary("Deletes messages owo")]
        public async Task DeleteAsync(params string[] args)
        {
            if (args.Length == 0)
            {
                await Context.Channel.SendMessageAsync(GetHelp("delete"));
            }
            else
            {
                int count = 0;
                List<ulong> messageIds = new List<ulong>(); // very max can be 110 ids, because 2000 character limit
                bool notify = true;


                for (int i = 0; i < args.Length; i++)
                {
                    if (int.TryParse(args[i], out int parsed) && parsed > 0)
                    {
                        count += parsed;
                    }
                    else if (args[i].ToLower().StartsWith("ids:"))
                    {
                        string[] split = args[i].Substring(4).Split(',');
                        foreach (string item in split)
                        {
                            if (ulong.TryParse(item, out ulong id))
                            {
                                if (!messageIds.Contains(id))
                                    messageIds.Add(id);
                            }
                        }
                    }
                    else if (args[i].ToLower().StartsWith("count:"))
                    {
                        await Context.Channel.SendMessageAsync(GetEntry("UsageChanged"));
                        return;
                    }
                    else if (args[i].ToLower() == "nonotify")
                    {
                        notify = false;
                    }
                }

                count++;

                while (count != 0)
                {
                    int currentLimit = count;
                    if (currentLimit > 100)
                    {
                        currentLimit = 100;
                    }

                    List<IUserMessage> messages = await Context.Channel.GetMessagesAsync(currentLimit).Flatten().Select(t => t as IUserMessage).ToListAsync();

                    if (messages.Count < currentLimit)
                    {
                        count = 0;
                    }
                    else
                    {
                        count -= currentLimit;
                    }
                    for (int i = 0; i < messages.Count; i++)
                    {
                        if (messages[i].Timestamp.AddDays(13) < DateTimeOffset.Now)
                        {
                            await messages[i].DeleteAsync();
                            messages.RemoveAt(i);
                            i--;
                        }
                    }

                    if (messages.Count > 0)
                    {
                        await (Context.Channel as ITextChannel).DeleteMessagesAsync(messages);
                    }
                }

                List<IUserMessage> bulkable = new List<IUserMessage>();
                for (int i = 0; i < messageIds.Count; i++)
                {
                    IMessage message = await Context.Channel.GetMessageAsync(messageIds[i]);
                    if (message is IUserMessage uMessage)
                    {
                        if (uMessage.Timestamp.AddDays(13) < DateTimeOffset.Now)
                        {
                            await uMessage.DeleteAsync();
                        }
                        else
                        {
                            bulkable.Add(uMessage);
                        }
                    }
                }
                for (int j = 0; j < bulkable.Count; j += 100)
                {
                    await (Context.Channel as ITextChannel).DeleteMessagesAsync(bulkable.Skip(j).Take(Math.Min(100, bulkable.Count - j)));
                }
                if (notify)
                {
                    await Context.Channel.SendMessageAsync(GetEntry("Deleted")).ContinueWith(async t =>
                    {
                        await Task.Delay(5000);
                        try
                        {
                            await t.Result.DeleteAsync();
                        }
                        catch { } // prolly the message has been already deleted!
                    });
                }
            }
        }
        
        [Command("highlight"), ServerCommand(), Summary("Highlights a message, type without parameters to see the longer help owo")]
        public async Task HighlightAsync(params string[] args)
        {
            if (args.Length == 0)
            {
                await Context.Channel.SendMessageAsync(GetHelp("highlight"));
            }
            else
            {
                List<string> Args = args.ToList();
                List<IMessage> UserMessages = new List<IMessage>();

                IGuildUser User = null;
                IMessageChannel channel = Context.Channel;

                if (Context.Message.MentionedChannelIds.Count > 0)
                {
                    channel = await Context.Guild.GetTextChannelAsync(Context.Message.MentionedChannelIds.ElementAt(0));

                    foreach (ulong ChannelId in Context.Message.MentionedChannelIds)
                    {
                        for (int i = 0; i < Args.Count; i++)
                        {
                            if (Args[i] == $"<#{ ChannelId }>")
                            {
                                Args.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                }

                string Arg = string.Join(" ", Args);
                if (ulong.TryParse(Arg, out ulong Id))
                {
                    var Message = await channel.GetMessageAsync(Id);
                    if (Message == null)
                    {
                        User = Tools.ParseUser(Arg, false, Context);
                        if (User == null)
                        {
                            await Context.Channel.SendMessageAsync(GetHelp("highlight"));
                            return;
                        }
                    }
                    else
                    {
                        UserMessages.Add(Message);
                    }
                }
                else
                {
                    User = Tools.ParseUser(Arg, false, Context);
                }
                
                if (User != null)
                {
                    List<IMessage> Messages = null;
                    ulong LastId = 0;
                    do
                    {
                        if (LastId != 0)
                            Messages = await channel.GetMessagesAsync(LastId, Direction.Before, 100).Flatten().ToListAsync();
                        else
                            Messages = await channel.GetMessagesAsync(100).Flatten().ToListAsync();

                        bool broke = false;

                        for (int i = 0; i < Messages.Count; i++)
                        {
                            var Message = Messages[i];
                            if (Message.Author.Id == User.Id)
                            {
                                UserMessages.Add(Message);
                            }
                            else if (UserMessages.Count > 0)
                            {
                                broke = true;
                                break;
                            }
                        }
                        if (broke)
                        {
                            break;
                        }
                        else
                        {
                            LastId = Messages[Messages.Count - 1].Id;
                        }
                    }
                    while (Messages.Count == 100);
                }

                if (UserMessages.Count == 0)
                {
                    if (User != null)
                    {
                        await Context.Channel.SendMessageAsync(GetEntry("NoMessagesFound"));
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync(GetGlobalEntry("UserNotFound"));
                    }
                }
                else
                {
                    User = Global.Client.GetGuild(Context.Guild.Id).GetUser(UserMessages[0].Author.Id);
                    if (UserMessages.Count == 1)
                    {
                        IMessage Message = UserMessages[0];

                        EmbedBuilder builder = new EmbedBuilder();
                        builder.WithAuthor(User.Nickname ?? User.Username, User.GetAvatarUrl(ImageFormat.Png, 2048));
                        builder.WithColor(Tools.GetHighestRoleColor(User));

                        builder.WithDescription(Message.Content);
                        for (int i = 0; i < Message.Embeds.Count; i++)
                        {
                            IEmbed embed = Message.Embeds.ElementAt(i);

                            if (builder.Description.Trim() != "")
                                builder.Description += "\n\n";

                            var attachURL = "";

                            if (string.IsNullOrWhiteSpace(builder.ImageUrl))
                            {
                                if (embed.Image.HasValue)
                                {
                                    builder.WithImageUrl(embed.Image.Value.Url);
                                }
                            }
                            if (string.IsNullOrWhiteSpace(builder.ThumbnailUrl))
                            {
                                if (embed.Thumbnail.HasValue)
                                {
                                    builder.WithThumbnailUrl(embed.Thumbnail.Value.Url);
                                }
                            }
                            if (string.IsNullOrWhiteSpace(builder.Url))
                            {
                                if (!string.IsNullOrWhiteSpace(embed.Url))
                                {
                                    builder.WithUrl(embed.Url);
                                }
                            }

                            foreach (EmbedField field in embed.Fields)
                            {
                                builder.AddField(field.Name, field.Value, field.Inline);
                            }

                            if (!string.IsNullOrWhiteSpace(embed.Title) || !string.IsNullOrWhiteSpace(embed.Description))
                            {
                                string first = "", second = "", third = "";

                                if (!string.IsNullOrWhiteSpace(embed.Title))
                                {
                                    string Title = attachURL == "" ? embed.Title : $"[{ embed.Title }]({ attachURL })";
                                    first = $"\n{ GetEntry("Title") }: { Title }";
                                }
                                if (!string.IsNullOrWhiteSpace(embed.Description))
                                {
                                    second = $"\n{ GetEntry("Description") }: { embed.Description }";
                                }
                                if (embed.Footer.HasValue)
                                {
                                    third = $"\n{ GetEntry("Footer") }: { embed.Footer.Value.Text }";
                                }

                                if (Message.Embeds.Count == 1)
                                {
                                    builder.Description += first + second + third;
                                }
                                else
                                {
                                    builder.Description += $"#{ i + 1 } Embed:{ first }{ second }{ third }";

                                }
                            }
                        }
                        for (int i = 0; i < Message.Attachments.Count; i++)
                        {
                            IAttachment Attachment = Message.Attachments.ElementAt(i);
                            if (string.IsNullOrWhiteSpace(builder.ImageUrl))
                            {
                                string Extension = Attachment.Filename.Split('.').Last();
                                if (Global.Settings.ImageExtensions.Contains(Extension.ToLower()))
                                {
                                    builder.WithImageUrl(Attachment.Url);
                                    continue;
                                }
                            }
                            if (builder.Description.Trim() != "")
                                builder.Description += "\n";

                            builder.Description += $"#{ i + 1 }: [{ Attachment.Filename }]({ Attachment.Url }) ({ Attachment.Size / 1024 }KB)";
                        }

                        builder.WithFooter("#" + Message.Channel.Name);
                        builder.WithTimestamp(Message.CreatedAt);

                        await Context.Channel.SendMessageAsync("", embed: builder.Build());
                    }
                    else
                    {
                        List<IMessage> Messages = UserMessages.OrderBy(t => t.CreatedAt).ToList();

                        EmbedBuilder builder = new EmbedBuilder();
                        builder.WithAuthor(User.Nickname ?? User.Username, User.GetAvatarUrl(ImageFormat.Png, 2048));
                        builder.WithColor(Tools.GetHighestRoleColor(User));

                        foreach (var Message in Messages)
                        {
                            builder.Description += Message.Content;
                            for (int i = 0; i < Message.Attachments.Count; i++)
                            {
                                var Attachment = Message.Attachments.ElementAt(i);
                                if (string.IsNullOrWhiteSpace(builder.ImageUrl))
                                {
                                    var Extension = Attachment.Filename.Split('.').Last();
                                    if (Global.Settings.ImageExtensions.Contains(Extension.ToLower()))
                                    {
                                        builder.WithImageUrl(Attachment.Url);
                                        continue;
                                    }
                                }
                                if (builder.Description.Trim() != "")
                                    builder.Description += "\n";

                                builder.Description += $"#{ i + 1 }: [{ Attachment.Filename }]({ Attachment.Url }) ({ Attachment.Size / 1024 }KB)";
                            }
                            builder.Description += "\n";
                        }

                        IMessage Last = Messages.Last();

                        builder.WithFooter("#" + Last.Channel.Name);
                        builder.WithTimestamp(Last.Timestamp);


                        await Context.Channel.SendMessageAsync("", embed: builder.Build());
                    }
                }
            }
        }

        [Command("setnsfw"), Admin(), ServerCommand(), Summary("A channel can be added to the nsfw channels without changing the channel properties, but cannot be excluded from the nsfw channels if the channel has the nsfw switch turned on in the channel properties")]
        public async Task SetNsfwAsync(params string[] Args)
        {
            if (Context.Guild == null)
            {
                await Context.Channel.SendMessageAsync(GetEntry("DMNSFW"));
                return;
            }
            if (Args.Length == 1 && Global.IsAdminOrHigher(Context.User.Id, Context.Guild, Settings))
            {
                bool SetToNsfw = false;
                
                var CheckableText = Args[0].ToLower();
                if (CheckableText == "1"
                 || CheckableText == "yes"
                 || CheckableText == "true"
                 || CheckableText == "ya")
                    SetToNsfw = true;

                Global.GuildSettings.Modify(Settings.GuildId, Modify =>
                {
                    if (SetToNsfw)
                    {
                        if (!Modify.NsfwChannels.Contains(Context.Channel.Id))
                        {
                            Modify.NsfwChannels.Add(Context.Channel.Id);
                        }
                    }
                    else
                    {
                        if (Modify.NsfwChannels.Contains(Context.Channel.Id))
                        {
                            Modify.NsfwChannels.Remove(Context.Channel.Id);
                        }
                    }
                });

                await Context.Channel.SendMessageAsync(GetEntry(SetToNsfw ? "ChangedToNSFW" : "RemovedForcing"));
            }
            else
            {
                bool IsNSFW = Settings.NsfwChannels.Contains(Context.Channel.Id);
                await Context.Channel.SendMessageAsync(GetEntry(IsNSFW ? "CurrentlyForced" : "CurrentlyNormal"));
            }
        }

        [Command("say"), Admin(), Summary("Type the command without paramteres to get the longer help -w-")]
        public async Task SayAsync([Remainder]string Arg = "")
        {
            if (Arg.Length > 0)
            {
                string[] Args = Arg.Split(' ', '\n');

                var Option = Args[0].ToLower();
                var Value = "";

                if (Args.Length > 1)
                    Value = string.Join(" ", Arg.Substring(Args[0].Length + 1));

                string Message = null;
                ITextChannel txtChannel = Context.Channel as ITextChannel;

                if (Option == "listen")
                {
                    if (ulong.TryParse(Value, out ulong ChannelId))
                    {
                        if (Global.Client.GetChannel(ChannelId) is ITextChannel Channel)
                        {
                            if (Global.Settings.SayPreferences.ContainsKey(Context.User.Id))
                            {
                                Global.Settings.SayPreferences[Context.User.Id].Listening.Add(Context.Channel.Id, ChannelId);
                            }
                            else
                            {
                                Global.Settings.SayPreferences.Add(Context.User.Id, new SayPreferences()
                                {
                                    UserId = Context.User.Id,
                                    Listening = new Dictionary<ulong, ulong>()
                                    {
                                        { Context.Channel.Id, ChannelId }
                                    }
                                });
                            }

                            await Context.Channel.SendMessageAsync(GetEntry("ListenStart", "CN", Channel.Name));

                            Global.SaveSettings();
                            return;
                        }
                    }
                    else if (Value == "stop")
                    {
                        if (Global.Settings.SayPreferences.ContainsKey(Context.User.Id))
                        {
                            if (Global.Settings.SayPreferences[Context.User.Id].Listening.ContainsKey(Context.Channel.Id))
                            {
                                Global.Settings.SayPreferences[Context.User.Id].Listening.Remove(Context.Channel.Id);
                                await Context.Channel.SendMessageAsync(GetEntry("ListenStop"));
                                return;
                            }
                        }
                    }
                }
                else if (Option == "autodel")
                {
                    Value = Value.ToLower();
                    if (Value == "true" || Value == "yes" || Value == "ya" || Value == "1")
                    {
                        if (Global.Settings.SayPreferences.ContainsKey(Context.User.Id))
                        {
                            Global.Settings.SayPreferences[Context.User.Id].AutoDel = true;
                        }
                        else
                        {
                            Global.Settings.SayPreferences.Add(Context.User.Id, new SayPreferences()
                            {
                                AutoDel = true,
                                UserId = Context.User.Id
                            });
                        }
                        await Context.Channel.SendMessageAsync(GetEntry("AutoDelOn"));
                        Global.SaveSettings();
                        return;
                    }
                    else if (Value == "false" || Value == "no" || Value == "nai" || Value == "0")
                    {
                        if (Global.Settings.SayPreferences.ContainsKey(Context.User.Id))
                        {
                            Global.Settings.SayPreferences[Context.User.Id].AutoDel = false;
                        }
                        else
                        {
                            Global.Settings.SayPreferences.Add(Context.User.Id, new SayPreferences()
                            {
                                AutoDel = false,
                                UserId = Context.User.Id
                            });
                        }
                        await Context.Channel.SendMessageAsync(GetEntry("AutoDelOff"));
                        Global.SaveSettings();
                        return;
                    }
                }
                else if (ulong.TryParse(Option, out ulong Id))
                {
                    if (Global.Client.GetChannel(Id) is ITextChannel Channel)
                    {
                        txtChannel = Channel;
                        Message = string.Join(" ", Args.Skip(1));
                    }
                    else
                    {
                        Message = Arg;
                    }
                }
                else
                {
                    Message = Arg;
                }

                if (Message != null)
                {
                    await txtChannel.SendMessageAsync(Global.ProcessEmotes(Message));

                    if (Global.Settings.SayPreferences.ContainsKey(Context.User.Id))
                    {
                        if (Global.Settings.SayPreferences[Context.User.Id].AutoDel)
                        {
                            var Dm = await Context.User.CreateDMChannelAsync();
                            if (Dm.Id != Context.Channel.Id)
                            {
                                await Context.Message.DeleteAsync();
                            }
                        }
                    }

                    return;
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync(GetHelp("say"));
            }
        }

        [Command("cdisable"), ServerCommand(), Admin(), Summary("Disables triggering commands in the specific channel")]
        public async Task ChannelDisableAsync(params string[] _)
        {
            if (Settings.AvoidedChannels.Contains(Context.Channel.Id))
            {
                await Context.Channel.SendMessageAsync(GetEntry("AlreadyDisabled"));
            }
            else
            {
                Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                {
                    Settings.AvoidedChannels.Add(Context.Channel.Id);
                });

                await Context.Channel.SendMessageAsync(GetEntry("Disabled"));
            }
        }

        [Command("cdisableall"), ServerCommand(), Admin(), Summary("Disables all of the channels")]
        public async Task ChannelDisableAllAsync(params string[] _)
        {
            List<ITextChannel> TextChannels = new List<ITextChannel>(await Context.Guild.GetTextChannelsAsync());
            Global.GuildSettings.Modify(Settings.GuildId, Settings =>
            {
                foreach (ITextChannel Channel in TextChannels)
                {
                    if (!Settings.AvoidedChannels.Contains(Channel.Id))
                    {
                        Settings.AvoidedChannels.Add(Channel.Id);
                    }
                }
            });

            await Context.Channel.SendMessageAsync(GetEntry("Disabled"));
        }

        [Command("cenable"), ServerCommand(), Admin(), Summary("Enables triggering commands in the specific channel")]
        public async Task ChannelEnableAsync(params string[] _)
        {
            if (!Settings.AvoidedChannels.Contains(Context.Channel.Id))
            {
                await Context.Channel.SendMessageAsync(GetEntry("AlreadyEnabled"));
            }
            else
            {
                Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                {
                    Settings.AvoidedChannels.Remove(Context.Channel.Id);
                });

                await Context.Channel.SendMessageAsync(GetEntry("Enabled"));
            }
        }

        [Command("cenableall"), ServerCommand(), Admin(), Summary("Enables all of the channels")]
        public async Task ChannelEnableAllAsync(params string[] _)
        {
            List<ITextChannel> TextChannels = new List<ITextChannel>(await Context.Guild.GetTextChannelsAsync());
            Global.GuildSettings.Modify(Settings.GuildId, Settings =>
            {
                foreach (ITextChannel Channel in TextChannels)
                {
                    Settings.AvoidedChannels.Remove(Channel.Id);
                }
            });

            await Context.Channel.SendMessageAsync(GetEntry("Enabled"));
        }
        /*
        [Command("mute"), Admin, ServerCommand, Summary("You can mute a person by: `{PREFIX}mute @UserMention [minutes]` - Default is 10 minutes, but you can go up to 2,147,483,647 minutes :^)")]
        public async Task MuteAsync(IGuildUser User = null, int Time = 10)
        {
            if (User == null)
            {
                await ReplyAsync(GetEntry("NoMention"));
            }
            else if (Time <= 0)
            {
                await ReplyAsync(GetEntry("InvalidTime"));
            }
            else
            {
                if (Settings.MutedPeople.ContainsKey(User.Id))
                {
                    await ReplyAsync(GetEntry("AlreadyMuted"));
                }
                else
                {
                    IReadOnlyCollection<ITextChannel> tc = await User.Guild.GetTextChannelsAsync();
                    IReadOnlyCollection<IVoiceChannel> vc = await User.Guild.GetVoiceChannelsAsync();

                    Dictionary<ulong, PermValue?> tcPermValues = new Dictionary<ulong, PermValue?>();
                    Dictionary<ulong, PermValue?> vcPermValues = new Dictionary<ulong, PermValue?>();

                    foreach (ITextChannel  t in tc)
                    {
                        try
                        {
                            OverwritePermissions? op = t.GetPermissionOverwrite(User);
                            if (op.HasValue)
                            {
                                tcPermValues.Add(t.Id, op.Value.SendMessages);
                                op.Value.Modify(sendMessages: PermValue.Deny);
                            }
                            else
                            {
                                tcPermValues.Add(t.Id, null);
                                await t.AddPermissionOverwriteAsync(User, new OverwritePermissions(sendMessages: PermValue.Deny));
                            }
                        }
                        catch
                        {
                            await ReplyAsync(GetEntry("PermModFail", "CN", t.Name));
                        }
                    }
                    foreach (IVoiceChannel v in vc)
                    {
                        try
                        {
                            OverwritePermissions? op = v.GetPermissionOverwrite(User);
                            if (op.HasValue)
                            {
                                vcPermValues.Add(v.Id, op.Value.Connect);
                                op.Value.Modify(connect: PermValue.Deny);
                            }
                            else
                            {
                                vcPermValues.Add(v.Id, null);
                                await v.AddPermissionOverwriteAsync(User, new OverwritePermissions(connect: PermValue.Deny));
                            }
                        }
                        catch
                        {
                            await ReplyAsync(GetEntry("PermModFail", "CN", v.Name));
                        }
                    }
                    Mute mute = new Mute(Time, Context.Channel.Id, tcPermValues, vcPermValues);
                    Global.GuildSettings.Modify(Context.Guild.Id, settings =>
                    {
                        settings.MutedPeople.Add(User.Id, mute);
                    });
                    Global.HandleMute(Context.Guild.Id, User.Id, mute);

                    await ReplyAsync(GetEntry("Muted", "USER", User.Mention, "M", Time.ToString("N0")));
                }
            }
        }
        */

        [Command("mute"), Admin, ServerCommand(), Summary("You can mute a person by: `{PREFIX}mute @UserMention [minutes] (Reason)` - Default is 10 minutes, but you can go up to 2,147,483,647 minutes :^) You don't have to define a reason to mute, it's optional.")]
        public async Task MuteAsync(string _ = null, int Time = 10, [Remainder]string Reason = null)
        {
            if (Context.Message.MentionedRoleIds.Count != 0)
            {
                if (Context.Message.MentionedRoleIds.Count > 1)
                {
                    await ReplyAsync(GetEntry("MuteRoleOnlyOne"));
                }
                else
                {
                    ulong id = Context.Message.MentionedRoleIds.First();
                    Global.GuildSettings.Modify(Settings.GuildId, t =>
                    {
                        t.MuteRoleId = id;
                    });
                    IRole role = Context.Guild.GetRole(id);
                    await ReplyAsync(GetEntry("MuteRoleChanged", "MRN", role.Name));
                }
            }
            else if (Context.Message.MentionedUserIds.Count == 0)
            {
                await ReplyAsync(GetEntry("NoMention"));
            }
            else if (Time <= 0)
            {
                await ReplyAsync(GetEntry("InvalidTime"));
            }
            else
            {
                var User = await Context.Guild.GetUserAsync(Context.Message.MentionedUserIds.ElementAt(0));
                if (Settings.Mutes.ContainsKey(User.Id))
                {
                    await ReplyAsync(GetEntry("AlreadyMuted"));
                    return;
                }
                List<IRole> roles = (await Context.Guild.GetCurrentUserAsync()).RoleIds.Select(t => Context.Guild.GetRole(t)).ToList();

                if (roles.Count == 0)
                {
                    await ReplyAsync(GetEntry("NoPermission"));
                    return;
                }

                bool canManageRoles = roles.Find(t =>
                                t.Permissions.Has(GuildPermission.Administrator) ||
                                t.Permissions.Has(GuildPermission.ManageRoles)) != null;

                if (!canManageRoles)
                {
                    await ReplyAsync(GetEntry("NoPermission", "USR", User.Mention));
                    return;
                }

                IRole highestRole = roles.OrderBy(t => t.Position).Last();
                List<IRole> userRoles = User.RoleIds.Select(t => Context.Guild.GetRole(t)).Where(t => t.Id != Context.Guild.EveryoneRole.Id).ToList();
                IRole userHighestRole = userRoles.OrderBy(t => t.Position).Last();

                if (userHighestRole != null && (highestRole == null || highestRole.Position <= userHighestRole.Position))
                {
                    await ReplyAsync(GetEntry("NoPermission", "USR", User.Mention));
                    return;
                }

                IRole muteRole = null;
                if ((muteRole = Context.Guild.GetRole(Settings.MuteRoleId)) == null)
                {
                    foreach (IRole role in Context.Guild.Roles)
                    {
                        if (role.Name == "Muted")
                        {
                            muteRole = role;

                            Global.GuildSettings.Modify(Settings.GuildId, t =>
                            {
                                t.MuteRoleId = role.Id;
                            });

                            break;
                        }
                    }

                    if (muteRole == null && roles.Find(t => t.Permissions.ManageRoles) == null)
                    {
                        await ReplyAsync(GetEntry("CannotCreateRole"));
                        return;
                    }
                    else
                    {
                        muteRole = await Context.Guild.CreateRoleAsync("Muted", new GuildPermissions(sendMessages: false, connect: false, speak: false), null, false, false, null);

                        Global.GuildSettings.Modify(Settings.GuildId, t =>
                        {
                            t.MuteRoleId = muteRole.Id;
                        });

                    }
                }
                if (muteRole == null)
                {
                    await ReplyAsync(GetEntry("UnknownError"));
                }
                else
                {
                    if (muteRole.Position >= highestRole.Position)
                    {
                        await ReplyAsync(GetEntry("MuteRoleHighPos"));
                        return;
                    }
                    Mute mute = new Mute(Time, Context.Channel.Id, User.RoleIds.ToList(), Context.User.Id, Reason);
                    await User.RemoveRolesAsync(userRoles);
                    await User.AddRoleAsync(muteRole);
                    Global.GuildSettings.Modify(Settings.GuildId, t =>
                    {
                        t.Mutes.Add(User.Id, mute);
                    });
                    Global.HandleMute(Context.Guild.Id, User.Id, mute);
                    await ReplyAsync(GetEntry("Muted", "USER", User.Mention, "M", Time.ToString("N0"), "REASON", Reason ?? GetEntry("NoReason")));
                }
            }
        }

        [Command("unmute"), Admin, ServerCommand, Summary("You can unmute a person by: `{PREFIX}unmute @UserMention`")]
        public async Task UnmuteAsync(IGuildUser User = null)
        {
            if (User == null)
            {
                await ReplyAsync(GetEntry("NoMention"));
            }
            else
            {
                if (Settings.Mutes.ContainsKey(User.Id))
                {
                    Settings.Mutes[User.Id].MuteChannel = Context.Channel.Id;
                    Settings.Mutes[User.Id].Cancel.Cancel();

                }
                else
                {
                    await ReplyAsync(GetEntry("NotMuted"));
                }
            }
        }


        [Command("poll"), ServerCommand, Summary("Makes a poll, write the command without paramteres so I'll write down how to use it ~")]
        public async Task PollAsync(params string[] Args)
        {
            if (Args.Length == 0)
            {
                EmbedBuilder builder = new EmbedBuilder()
                {
                    Title = GetEntry("HelpTitle"),
                    Color = Color.Green,
                    Description = GetEntry("HelpDescription"),
                };
                builder.Fields = new List<EmbedFieldBuilder>()
                {
                    new EmbedFieldBuilder()
                    {
                        Name = GetEntry("MakePollTitle"),
                        Value = GetEntry("MakePollDesc"),
                        IsInline = false
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = GetEntry("OptionalParamsTitle"),
                        Value = GetEntry("OptionalParamsDesc"),
                        IsInline = false
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = GetEntry("CancelPollTitle"),
                        Value = GetEntry("CancelPollDesc"),
                        IsInline = false
                    },
                };

                await Context.Channel.SendMessageAsync("", embed: builder.Build());
                return;
            }
            if (Args[0].ToLower() == "cancel")
            {
                if (Args.Length != 2)
                {
                    await Context.Channel.SendMessageAsync(GetEntry("CancelWrongUsage"));
                    return;
                }

                if (!int.TryParse(Args[1], out int PollId))
                {
                    await Context.Channel.SendMessageAsync(GetEntry("CancelIncorrectId"));
                    return;
                }

                if (!Global.Polls.CancelPoll(Context.User.Id, PollId))
                {
                    await Context.Channel.SendMessageAsync(GetEntry("CancelPollNotFound"));
                }
            }
            else
            {
                string Question = "";
                string[] Options = new string[2] { GetEntry("Yes"), GetEntry("No") };
                TimeSpan Time = TimeSpan.FromMinutes(5);

                Regex Daygex = new Regex("(\\d)+d");
                Regex Hourgex = new Regex("(\\d)+h");
                Regex Mingex = new Regex("(\\d)+m");
                Regex Secgex = new Regex("(\\d)+s");

                for (int i = 0; i < Args.Length; i++)
                {
                    if (Args[i].ToLower().StartsWith("-time="))
                    {
                        string timeString = Args[i].ToLower().Substring(6);
                        TimeSpan newTime = TimeSpan.Zero;
                        foreach (Match match in Daygex.Matches(timeString))
                        {
                            double value = double.Parse(match.Groups[1].Value);
                            if (value >= 0)
                            {
                                newTime = newTime.Add(TimeSpan.FromDays(value));
                            }
                        }
                        foreach (Match match in Hourgex.Matches(timeString))
                        {
                            double value = double.Parse(match.Groups[1].Value);
                            if (value >= 0)
                            {
                                newTime = newTime.Add(TimeSpan.FromHours(value));
                            }
                        }
                        foreach (Match match in Mingex.Matches(timeString))
                        {
                            double value = double.Parse(match.Groups[1].Value);
                            if (value >= 0)
                            {
                                newTime = newTime.Add(TimeSpan.FromMinutes(value));
                            }
                        }
                        foreach (Match match in Secgex.Matches(timeString))
                        {
                            double value = double.Parse(match.Groups[1].Value);
                            if (value >= 0)
                            {
                                newTime = newTime.Add(TimeSpan.FromSeconds(value));
                            }
                        }

                        if (newTime > TimeSpan.Zero)
                        {
                            Time = newTime;
                        }
                    }
                    else if (Args[i].ToLower().StartsWith("-options="))
                    {
                        string[] options = Args[i].ToLower().Substring(9).Split(',');
                        if (options.Length != 0)
                        {
                            Options = options;
                        }
                    }
                    else
                    {
                        Question += Args[i] + " ";
                    }
                }

                if (Question == "")
                {
                    await Context.Channel.SendMessageAsync(GetEntry("InvalidUsage"));
                }
                else
                {
                    Poll poll = new Poll()
                    {
                        ChannelId = Context.Channel.Id,
                        Duration = Time,
                        GuildId = Context.Guild.Id,
                        PollCreatedAt = TimeSpan.FromTicks(DateTime.Now.Ticks),
                        PollCreator = Context.User.Id,
                        PollId = Global.Polls.CreatePollId(Context.User.Id),
                        PollText = Question.Substring(0, Question.Length - 1)
                    };
                    poll.SetOptions(Options);

                    await Global.Polls.RegisterPollAsync(poll);
                }
            }
        }
    
        [Command("globalrecent"), ServerCommand(), Admin]
        public async Task GlobalRecentAsync(bool State)
        {
            Global.GuildSettings.Modify(Settings.GuildId, Setting =>
            {
                Setting.GlobalRecent = State;
            });

            await ReplyAsync(GetEntry(State.ToString()));
        }
        
    }

    [Group("admin"), Name("Management"), ServerCommand()]
    public class AdminCommands : ChinoContext
    {
        [Command(""), Summary("Admin summary")]
        public async Task DefaultAsync()
        {
            List<ulong> adminIds = new List<ulong>(Settings.AdminIds);
            Dictionary<ulong, IRole> roleCache = new Dictionary<ulong, IRole>();

            foreach (IGuildUser user in await Context.Guild.GetUsersAsync())
            {
                foreach (ulong roleId in user.RoleIds)
                {
                    IRole role;
                    if (roleCache.ContainsKey(roleId))
                        role = roleCache[roleId];
                    else
                    {
                        role = Context.Guild.GetRole(roleId);
                        roleCache.Add(roleId, role);
                    }

                    if (role.Permissions.Administrator)
                    {
                        if (!adminIds.Contains(user.Id))
                        {
                            adminIds.Add(user.Id);
                            break;
                        }
                    }
                }
            }

            List<string> Admins = await GetNamesAsync(adminIds);

            if (Admins.Count == 0)
                Admins.Add(GetEntry("NoAdmins"));
            List<string> GlobalAdmins = await GetNamesAsync(Global.Settings.GlobalAdminIds);
            if (GlobalAdmins.Count == 0)
                GlobalAdmins.Add(GetEntry("NoGlobalAdmins"));

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

            EmbedBuilder Builder = new EmbedBuilder()
            {
                Fields = new List<EmbedFieldBuilder>()
                    {
                        new EmbedFieldBuilder()
                        {
                            IsInline = true,
                            Name = GetEntry("Owner"),
                            Value = Tools.GetDisplayName(Owner)
                        },
                        new EmbedFieldBuilder()
                        {
                            IsInline = true,
                            Name = GetEntry("GlobalAdmins"),
                            Value = string.Join(", ", GlobalAdmins)
                        },
                        new EmbedFieldBuilder()
                        {
                            IsInline = true,
                            Name = GetEntry("ServerOwner"),
                            Value = Tools.GetDisplayName(await Context.Guild.GetOwnerAsync())
                        },
                        new EmbedFieldBuilder()
                        {
                            IsInline = true,
                            Name = GetEntry("Admins"),
                            Value = string.Join(", ", Admins)
                        }
                    },
                Color = Global.Pink,
                Title = GetEntry("Admin")
            };

            if (Global.IsServerOwnerOrHigher(Context.User.Id, Context.Guild))
            {
                Builder.AddField(GetEntry("GlobalAdminAndServerOwner"), GetEntry("AdminAddHelp"), false);
            }
            if (Global.IsOwner(Context.User.Id))
            {
                Builder.AddField(GetEntry("Owner"), GetEntry("GlobalAdminAddHelp"), false);
            }

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("add"), ServerOwner(), Summary("Adds a user to the admins")]
        public async Task AddAsync([Remainder]string Args = "")
        {
            IGuildUser User = null;
            if (Context.Message.MentionedUserIds.Count > 1)
            {
                await Context.Channel.SendMessageAsync(GetEntry("OnlyOneMention"));
                return;
            }
            else
            {
                User = Tools.ParseUser(Args, false, Context);
            }
            EmbedBuilder Builder = new EmbedBuilder()
            {
                Title = GetEntry("Admin"),
            };

            if (User == null)
            {
                Builder.Color = Color.DarkRed;
                Builder.Description = GetEntry("UserNotFound");
            }
            else
            {
                ulong Id = User.Id;
                Builder.ThumbnailUrl = User.GetAvatarUrl(size: 256);

                bool AlreadyAdmin = false;
                
                AlreadyAdmin = Global.IsAdminOrHigher(Id, Context.Guild, Settings);

                if (AlreadyAdmin)
                {
                    Builder.Color = Global.Pink;
                    Builder.AddField(GetEntry("AlreadyAdmin"), Tools.GetDisplayName(User));
                }
                else
                {
                    Builder.Color = new Color(46, 242, 72);
                    Builder.AddField(GetEntry("NewAdmin"), Tools.GetDisplayName(User));

                    Global.GuildSettings.Modify(Context.Guild.Id, Setting =>
                    {
                        Setting.AdminIds.Add(Id);
                    });
                }
            }

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("addglobal"), Models.Privileges.Owner(), Summary("Adds a user to the global admins")]
        public async Task AddGlobalAsync([Remainder]string Args = "")
        {
            IGuildUser User = null;
            if (Context.Message.MentionedUserIds.Count > 1)
            {
                await Context.Channel.SendMessageAsync(GetEntry("OnlyOneMention"));
                return;
            }
            else
            {
                User = Tools.ParseUser(Args, false, Context);
            }

            EmbedBuilder Builder = new EmbedBuilder()
            {
                Title = GetEntry("GlobalAdmins"),
            };

            if (User == null)
            {
                Builder.Color = Color.DarkRed;
                Builder.Description = GetEntry("UserNotFound");
            }
            else
            {
                ulong Id = User.Id;
                Builder.ThumbnailUrl = User.GetAvatarUrl(size: 256);
                bool AlreadyAdmin = false;
                
                if (Global.IsAdminOrHigher(Id, Context.Guild, Settings))
                {
                    if (Global.IsGlobalAdminOrHigher(Id))
                    {
                        AlreadyAdmin = true;
                    }
                    else
                    {
                        Global.GuildSettings.Modify(Context.Guild.Id, Setting =>
                        {
                            var Index = Setting.AdminIds.FindIndex(t => t == Id);
                            if (Index > -1)
                            {
                                Setting.AdminIds.RemoveAt(Index);
                            }
                        });
                    }
                }


                if (AlreadyAdmin)
                {
                    Builder.Color = Global.Pink;
                    Builder.AddField(GetEntry("AlreadyGlobalAdmin"), Tools.GetDisplayName(User));
                }
                else
                {
                    Builder.Color = new Color(46, 242, 72);
                    Builder.AddField(GetEntry("NewGlobalAdmin"), Tools.GetDisplayName(User));

                    Global.Settings.GlobalAdminIds.Add(Id);
                    Global.SaveSettings();
                }
            }
            
            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("remove"), ServerOwner(), Summary("Removes an admin")]
        public async Task RemoveAsync([Remainder]string Args = "")
        {
            IGuildUser User = null;
            if (Context.Message.MentionedUserIds.Count > 1)
            {
                await Context.Channel.SendMessageAsync(GetEntry("OnlyOneMention"));
                return;
            }
            else
            {
                User = Tools.ParseUser(Args, false, Context);
            }


            EmbedBuilder Builder = new EmbedBuilder()
            {
                Title = GetEntry("RemoveAdmin")
            };

            if (User == null)
            {
                if (ulong.TryParse(Args, out ulong UserId))
                {
                    bool NotAdmin = false;

                    if (Global.IsAdmin(UserId, Settings))
                    {
                        Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                        {
                            for (int j = 0; j < Settings.AdminIds.Count; j++)
                            {
                                if (Settings.AdminIds[j] == UserId)
                                {
                                    Settings.AdminIds.RemoveAt(j);
                                    break;
                                }
                            }
                        });
                    }
                    else
                    {
                        NotAdmin = true;
                    }

                    if (NotAdmin)
                    {
                        Builder.Color = new Color(252, 252, 40);
                        Builder.AddField(GetEntry("NotAdmin"), Args);
                    }
                    else
                    {
                        Builder.Color = new Color(247, 37, 9);
                        Builder.AddField(GetEntry("Removed"), Args);
                    }
                }
                else
                {
                    Builder.Color = Color.DarkRed;
                    Builder.Description = GetEntry("UserNotFound");
                }
            }
            else
            {
                Builder.ThumbnailUrl = User.GetAvatarUrl(size: 256);
                bool NotAdmin = false;
                ulong Id = User.Id;

                if (Global.IsAdmin(Id, Settings))
                {
                    Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                    {
                        for (int j = 0; j < Settings.AdminIds.Count; j++)
                        {
                            if (Settings.AdminIds[j] == Id)
                            {
                                Settings.AdminIds.RemoveAt(j);
                                break;
                            }
                        }
                    });
                }
                else
                {
                    NotAdmin = true;
                }

                if (NotAdmin)
                {
                    Builder.Color = new Color(252, 252, 40);
                    Builder.AddField(GetEntry("NotAdmin"), Tools.GetDisplayName(User));
                }
                else
                {
                    Builder.Color = new Color(247, 37, 9);
                    Builder.AddField(GetEntry("Removed"), Tools.GetDisplayName(User));
                }
            }

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("removeglobal"), ServerOwner(), Summary("Removes a global admin")]
        public async Task RemoveGlobalAsync(IGuildUser User)
        {
            EmbedBuilder Builder = new EmbedBuilder()
            {
                Title = GetEntry("RemoveAdmin"),
                ThumbnailUrl = User.GetAvatarUrl(size: 256)
            };

            bool NotAdmin = false;
            ulong Id = User.Id;

            if (Global.IsGlobalAdmin(Id))
            {
                Global.Settings.GlobalAdminIds.RemoveAll(t => t == Id);
            }
            else
            {
                NotAdmin = true;
            }

            if (NotAdmin)
            {
                Builder.Color = new Color(252, 252, 40);
                Builder.AddField(GetEntry("NotAdmin"), Tools.GetDisplayName(User));
            }
            else
            {
                Builder.Color = new Color(247, 37, 9);
                Builder.AddField(GetEntry("Removed"), Tools.GetDisplayName(User));
            }

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }
        
        private async Task<List<string>> GetNamesAsync(List<ulong> Ids)
        {
            List<string> List = new List<string>();

            foreach (ulong AdminId in Ids)
            {
                IGuildUser gUser = await Context.Guild.GetUserAsync(AdminId);
                if (gUser != null)
                {
                    List.Add(Tools.GetDisplayName(gUser));
                }
                else
                {
                    IUser usr = Global.Client.GetUser(AdminId);

                    if (usr == null)
                    {
                        foreach (IGuild guild in Global.Client.Guilds)
                        {
                            IUser u = guild.GetUserAsync(AdminId).Result;
                            if (u != null)
                            {
                                usr = u;
                                break;
                            }
                        }
                    }

                    if (usr != null)
                        List.Add(Tools.GetDisplayName(usr));
                }
            }

            return List;
        }
    }

}
