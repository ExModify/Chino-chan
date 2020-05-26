using Chino_chan.Models;
using Chino_chan.Models.Privileges;
using Chino_chan.Models.Settings;
using Chino_chan.Modules;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Commands
{
    public class Role : ChinoContext
    {
        [Command("assignable"), ServerCommand(), Admin(), Summary("Makes a role assignable or non-assignable by me~")]
        public async Task AssignableAsync(params string[] Args)
        {
            string RoleName = string.Join(" ", Args).ToLower();

            if (string.IsNullOrWhiteSpace(RoleName))
            {
                await Context.Channel.SendMessageAsync(GetEntry("DefineARole"));
                return;
            }

            List<IRole> Roles = Context.Guild.Roles.ToList();

            IRole Assignable = null;
            
            foreach (IRole Role in Roles)
            {
                if (Role.Name.ToLower() == RoleName)
                {
                    Assignable = Role;
                    break;
                }
            }

            if (Assignable == null)
            {
                await Context.Channel.SendMessageAsync(GetEntry("RoleNotFound"));
                return;
            }
            else
            {
                if (Settings.AssignableRoles.Contains(Assignable.Id))
                {
                    Global.GuildSettings.Modify(Settings.GuildId, (Settings) =>
                    {
                        Settings.AssignableRoles.Remove(Assignable.Id);
                    });

                    await Context.Channel.SendMessageAsync(GetEntry("MadeNonAssignable"));
                }
                else
                {
                    Global.GuildSettings.Modify(Settings.GuildId, (Settings) =>
                    {
                        Settings.AssignableRoles.Add(Assignable.Id);
                    });

                    await Context.Channel.SendMessageAsync(GetEntry("MadeAssignable", "ROLENAME", Assignable.Name));
                }
            }
        }

        [Command("role"), ServerCommand(), Summary("Assigns or removes a role from you which is assignable~")]
        public async Task AssignAsync(params string[] Args)
        {
            string RoleName = string.Join(" ", Args).ToLower();

            if (string.IsNullOrWhiteSpace(RoleName))
            {
                await Context.Channel.SendMessageAsync(GetEntry("DefineARole"));
                return;
            }

            List<IRole> Roles = Context.Guild.Roles.ToList();

            IRole Assign = null;

            foreach (IRole Role in Roles)
            {
                if (Role.Name.ToLower().Contains(RoleName))
                {
                    Assign = Role;
                    break;
                }
            }

            if (Assign == null)
            {
                await Context.Channel.SendMessageAsync(GetEntry("RoleNotFound"));
                return;
            }
            else
            {
                if (Settings.AssignableRoles.Contains(Assign.Id))
                {
                    IGuildUser User = Context.User as IGuildUser;

                    if (User.RoleIds.Contains(Assign.Id))
                    {
                        try
                        {
                            await User.RemoveRoleAsync(Assign);
                            await Context.Channel.SendMessageAsync(GetEntry("RoleRemoved", "MENTION", User.Mention, "ROLENAME", Assign.Name));
                        }
                        catch
                        {
                            await Context.Channel.SendMessageAsync(GetEntry("RoleCannotBeRemoved"));
                        }
                    }
                    else
                    {
                        try
                        {
                            await User.AddRoleAsync(Assign);
                            await Context.Channel.SendMessageAsync(GetEntry("RoleAdded", "MENTION", User.Mention, "ROLENAME", Assign.Name));
                        }
                        catch
                        {
                            await Context.Channel.SendMessageAsync(GetEntry("RoleCannotBeAdded"));
                        }
                    }

                }
                else
                {
                    await Context.Channel.SendMessageAsync(GetEntry("RoleNotAssignable"));
                }
            }
        }

        [Command("assignchannel"), ServerCommand(), Admin(), Summary("Sets a channel where you can assign roles by reacting a message with green tick")]
        public async Task AssignChannelAsync(params string[] Args)
        {
            if (Args.Length > 0)
            {
                string Sub = Args[0].ToLower();
                if (Sub == "set")
                {
                    string Name = string.Join(" ", Args.Skip(1));

                    IReadOnlyCollection<ITextChannel> Channels = await Context.Guild.GetTextChannelsAsync();
                    ITextChannel SetChannel = null;

                    if (ulong.TryParse(Name, out ulong Id))
                    {
                        foreach (ITextChannel Channel in Channels)
                        {
                            if (Channel.Id == Id)
                            {
                                SetChannel = Channel;
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (ITextChannel Channel in Channels)
                        {
                            if (Channel.Name.ToLower() == Name)
                            {
                                SetChannel = Channel;
                                break;
                            }
                        }
                    }

                    if (SetChannel == null)
                    {
                        await Context.Channel.SendMessageAsync(GetEntry("ChannelNotFound"));
                    }
                    else if (Settings.ReactionAssignChannels.Contains(SetChannel.Id))
                    {
                        await Context.Channel.SendMessageAsync(GetEntry("ChannelAlreadyAdded"));
                    }
                    else
                    {
                        Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                        {
                            Settings.ReactionAssignChannels.Add(SetChannel.Id);
                        });

                        await Context.Channel.SendMessageAsync(GetEntry("ChannelSet", "CHANNELNAME", SetChannel.Name));
                    }

                    return;
                }
                else if (Sub == "remove")
                {
                    if (Settings.ReactionAssignChannels.Count == 0)
                    {
                        await Context.Channel.SendMessageAsync(GetEntry("NoChannel"));
                    }
                    else
                    {
                        string Name = string.Join(" ", Args.Skip(1));

                        IReadOnlyCollection<ITextChannel> Channels = await Context.Guild.GetTextChannelsAsync();
                        ITextChannel SetChannel = null;

                        if (ulong.TryParse(Name, out ulong Id))
                        {
                            foreach (ITextChannel Channel in Channels)
                            {
                                if (Channel.Id == Id)
                                {
                                    SetChannel = Channel;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            foreach (ITextChannel Channel in Channels)
                            {
                                if (Channel.Name.ToLower() == Name)
                                {
                                    SetChannel = Channel;
                                    break;
                                }
                            }
                        }
                        if (SetChannel == null)
                        {
                            await Context.Channel.SendMessageAsync(GetEntry("ChannelNotFound"));
                        }
                        else if (!Settings.ReactionAssignChannels.Contains(SetChannel.Id))
                        {
                            await Context.Channel.SendMessageAsync(GetEntry("ChannelNotSet"));
                        }
                        else
                        {
                            Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                            {
                                Settings.ReactionAssignChannels.Remove(SetChannel.Id);
                            });

                            await Context.Channel.SendMessageAsync(GetEntry("ChannelRemoved"));
                        }
                    }

                    return;
                }
                else if (Sub == "clear")
                {
                    if (Settings.ReactionAssignChannels.Count == 0)
                    {
                        await Context.Channel.SendMessageAsync(GetEntry("NoChannel"));
                    }
                    else
                    {
                        Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                        {
                            Settings.ReactionAssignChannels.Clear();
                        });

                        await Context.Channel.SendMessageAsync(GetEntry("ChannelsCleared"));
                    }

                    return;
                }
            }

            string Message = "";

            if (Settings.ReactionAssignChannels.Count == 0)
            {
                Message += GetEntry("NoChannel", "PREFIX", Settings.Prefix);
            }
            else
            {
                List<string> names = new List<string>();

                foreach (ulong id in Settings.ReactionAssignChannels)
                {
                    ITextChannel Channel = await Context.Guild.GetTextChannelAsync(id);

                    if (Channel != null)
                    {
                        names.Add(Channel.Name);
                    }
                }

                if (names.Count == 0)
                {
                    Message += GetEntry("SetButRemoved");
                }
                else
                {
                    Message += GetEntry("AssignChannel", "CHANNELNAME", string.Join(", ", names));
                }
            }
            
            await Context.Channel.SendMessageAsync(Message);
        }

        [Command("assignmessage"), ServerCommand(), Admin(), Summary("")]
        public async Task AssignMessageAsync(ulong MessageId, IRole role)
        {
            if (Context.Channel.GetMessageAsync(MessageId).Result is IUserMessage message)
            {
                try
                {
                    if (message.Reactions.Select(t => t.Value.IsMe && t.Key.Name == "✅").Count() == 0)
                    {
                        await message.AddReactionAsync(new Emoji("✅"));
                    }

                    int index = -1;
                    AssignMessage assign = new AssignMessage()
                    {
                        ChannelId = Context.Channel.Id,
                        GuildId = Context.Guild.Id,
                        RoleId = role.Id,
                        MessageId = MessageId
                    };

                    if ((index = Settings.AssignMessages.FindIndex(t => t.MessageId == message.Id && t.GuildId == Context.Guild.Id && t.ChannelId == Context.Channel.Id)) > -1)
                    {
                        Global.GuildSettings.Modify(Context.Guild.Id, t =>
                        {
                            t.AssignMessages[index] = assign;
                        });
                        await Context.Channel.SendMessageAsync(GetEntry("Rewritten"));
                    }
                    else
                    {
                        Global.GuildSettings.Modify(Context.Guild.Id, t =>
                        {
                            t.AssignMessages.Add(assign);
                        });
                        await Context.Channel.SendMessageAsync(GetEntry("Set"));
                    }
                }
                catch
                {
                    await Context.Channel.SendMessageAsync(GetEntry("CannotReact"));
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync(GetEntry("NotFound"));
            }
        }

        [Command("multirole"), ServerCommand(), Admin(), Summary("")]
        public async Task MultiRoleAsync([Remainder]string Args = "")
        {
            if (Args.Equals("help", StringComparison.InvariantCultureIgnoreCase))
            {
                await ReplyAsync(GetHelp("multirole"));
            }
            else
            {
                if (Global.MultiRoleHandler.StartInformationFetching(Context))
                {
                    Global.MultiRoleHandler.AddToDelete(Context, await ReplyAsync(GetEntry("SendMessage")));
                }
                else
                {
                    Global.MultiRoleHandler.AddToDelete(Context, await ReplyAsync(GetEntry("AlreadyInProgress")));
                }
            }
        }

        [Command("managemulti"), Alias("mmr"), ServerCommand(), Admin(), Summary("")]
        public async Task ManageMultiRoleAsync(int Id = 0, string Option = null, string Value = null)
        {
            List<MultiRoleEntry> entries = Global.MultiRoleHandler.Entries.Where(t => t.GuildId == Context.Guild.Id).ToList();
            MultiRoleEntry entry = null;
            EmbedBuilder b = new EmbedBuilder()
            {
                Title = GetEntry("Title"),
                Color = Color.Green,
                Description = ""
            };

            bool paginate = false;

            if (entries.Count == 0)
            {
                b.Description = GetEntry("NoMultiRole");
            }
            else if (Id < 1 || Id > entries.Count)
            {
                int limit = Math.Min(entries.Count, 10);
                int pages = entries.Count / 10 + 1;

                for (int i = 0; i < limit; i++)
                {
                    entry = entries[i];
                    b.Description += GetEntry("Row", "ID", (i + 1).ToString(), "CM", $"<#{ entry.ChannelId }>", "MID", entry.MessageId.ToString());
                }

                b.Footer = new EmbedFooterBuilder()
                {
                    Text = GetEntry("Page", "C", "1", "M", pages.ToString())
                };

                if (pages > 1)
                    paginate = true;
            }
            else if (Option == null)
            {
                entry = entries[Id - 1];
                string content, roles, options;

                ITextChannel ch = await Context.Guild.GetTextChannelAsync(entry.ChannelId);
                if (ch != null)
                {
                    IMessage message = await ch.GetMessageAsync(entry.MessageId);

                    if (message != null)
                    {
                        content = message.Content;
                        roles = string.Join("\n", entry.EmoteRolePairs.Select(t => $"{ t.Key } - <@{ t.Value }>"));
                        options = $"OnlyOne: { entry.OnlyOne }";
                    }
                    else
                    {
                        content = GetEntry("ChannelRemoved");
                        roles = content;
                        options = content;
                    }
                }
                else
                {
                    content = GetEntry("ChannelRemoved");
                    roles = content;
                    options = content;
                }
                


                b.AddField(GetEntry("ContentFieldTitle"), content);
                b.AddField(GetEntry("RoleFieldTitle"), roles);
                b.AddField(GetEntry("OptionsFieldTitle"), options);
            }
            else
            {
                entry = entries[Id - 1];

                string exclude = "GuildId;ChannelId;MessageId;EmoteRolePairs";

                PropertyInfo[] infos = typeof(MultiRoleEntry).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                PropertyInfo info = infos.First(t => t.Name.ToLower() == Option.ToLower() && !exclude.Contains(t.Name));

                if (info == null)
                {
                    b.Description = GetEntry("OptionNotFound");
                }
                else if (Value == null)
                {
                    b.Description = GetEntry("OptionValue", "O", info.Name, "V", info.GetValue(entry).ToString());
                }
                else
                {
                    Type pType = info.PropertyType;
                    switch (pType.Name)
                    {
                        case "Boolean":
                            bool? boolVal = null;
                            switch (Value.ToLower())
                            {
                                case "true":
                                case "1":
                                    boolVal = true;
                                    break;
                                case "false":
                                case "0":
                                    boolVal = false;
                                    break;
                            }

                            if (boolVal == null)
                            {
                                b.Description = GetEntry("IncorrectValue");
                            }
                            else
                            {
                                info.SetValue(entries[Id - 1], boolVal.Value);
                                b.Description = GetEntry("OptionChanged");

                                Global.MultiRoleHandler.Save();
                            }

                            break;
                    }
                }
            }

            IUserMessage msg = await ReplyAsync("", false, b.Build());

            if (paginate)
            {
                //await msg.AddReactionsAsync(new Emoji[2] { new Emoji("◀️"), new Emoji("▶️") });
            }
        }
    }
}
