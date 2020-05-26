using Chino_chan.Models;
using Chino_chan.Models.Privileges;
using Chino_chan.Models.Settings;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Commands
{
    public class Level : ChinoContext
    {
        [Command("level"), ServerCommand(), Summary("Displays your or other's level~")]
        public async Task LevelAsync([Remainder]string Arg = "")
        {
            IGuildUser User = Tools.ParseUser(Arg, false, Context);

            if (User == null)
            {
                if (Arg.Length == 0)
                {
                    User = Context.User as IGuildUser;
                }
                else
                {
                    await Context.Channel.SendMessageAsync(GetEntry("UserNotFound"));
                    return;
                }
            }

            Leveling.User usr = Global.Level.Users.GetUser(User.Id);

            bool Edited = false;

            if (!usr.GuildLevels.ContainsKey(Context.Guild.Id))
            {
                usr.GuildLevels.Add(Context.Guild.Id, 0);
                Edited = true;
            }
            if (!usr.GuildXps.ContainsKey(Context.Guild.Id))
            {
                usr.GuildXps.Add(Context.Guild.Id, 0);
                Edited = true;
            }
            if (Edited)
                Global.Level.Users.UpdateUser(usr);

            EmbedBuilder Builder = new EmbedBuilder()
            {
                Color = Color.DarkPurple,
                Author = new EmbedAuthorBuilder()
                {
                    Name = GetEntry("LevelOf", "USERNAME", User.Nickname ?? User.Username),
                    IconUrl = User.GetAvatarUrl()
                },
                Description = GetEntry("LevelDescription", "LEVEL", usr.GuildLevels[Context.Guild.Id].ToString("N0"),
                                                           "CURRENTXP", usr.GuildXps[Context.Guild.Id].ToString("N0"),
                                                           "NEXTLEVELXP", Global.Level.Levels[(int)usr.GuildLevels[Context.Guild.Id]].ToString("N0"))
            };

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("top"), ServerCommand(), Summary("Displays the top users of the server (from 1 to 20)")]
        public async Task TopAsync(int Top = 10)
        {
            if (Top < 1 || Top > 20)
            {
                await Context.Channel.SendMessageAsync(GetEntry("OutOfRange"));
            }
            else
            {
                var GuildUsers = (await Context.Guild.GetUsersAsync()).Where(t => !t.IsBot).Select(t => t.Id);
                
                List<Leveling.User> Users = new List<Leveling.User>();
                foreach (KeyValuePair<ulong, Leveling.User> User in Global.Level.Users)
                {
                    if (User.Value.GuildXps.ContainsKey(Context.Guild.Id))
                    {
                        Users.Add(User.Value);
                    }
                }
                Users = Users.OrderByDescending(t => t.GuildXps[Context.Guild.Id]).ToList();

                string Description = "";

                int Min = Math.Min(Top, Users.Count);

                for (int i = 0; i < Min; i++)
                {
                    Leveling.User User = Users[i];
                    IGuildUser gUser = await Context.Guild.GetUserAsync(User.UserId);
                    string Name = "";
                    if (gUser == null)
                    {
                        Name = GetEntry("UnknownUser");
                        Min++;
                    }
                    else
                    {
                        Name = gUser.Nickname;
                        if (Name != null)
                        {
                            Name += $" ({ gUser.Username })";
                        }
                        else
                        {
                            Name = gUser.Username;
                        }
                    }

                    Description += $"#{ i + 1 } - { Name }: lvl { User.GuildLevels[Context.Guild.Id] } ({ User.GuildXps[Context.Guild.Id] } xp)\r\n";
                }

                if (Top > Min)
                {
                    Description += GetEntry("OutOfUsers");
                }

                EmbedBuilder Builder = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = GetEntry("TopOf", "X", Min.ToString(), "SERVERNAME", Context.Guild.Name),
                        IconUrl = Context.Guild.IconUrl
                    },
                    Description = Description,
                    Color = Color.DarkPurple
                };

                await Context.Channel.SendMessageAsync("", embed: Builder.Build());
            }
        }
        
        [Command("leveling"), Admin(), Summary("Manage leveling system")]
        public async Task WatchAsync(params string[] Args)
        {
            Dictionary<string, string[]> Formats = new Dictionary<string, string[]>()
            {
                {
                    "image", new string[] { "jpg", "jpeg", "png", "gif", "gifv", "webp", "gif", "tiff", "svg" }
                },
                {
                    "video", new string[] { "webm", "flv", "mkv", "vob", "ogb", "gifv", "avi", "mp4", "wmv" }
                }
            };
            
            string Arg = string.Join(" ", Args).ToLower();
            
            EmbedBuilder Builder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = await Context.GetNicknameOrUsernameAsync(Context.Client.CurrentUser),
                    IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
                },
                Color = new Color(252, 117, 252)
            };

            bool Edited = false;

            if (Args.Length > 0)
            {
                if (Arg == "enable")
                {
                    if (!Settings.Leveling)
                    {
                        Global.GuildSettings.Modify(Settings.GuildId, (Settings) =>
                        {
                            Settings.Leveling = true;
                        });
                        Builder.Description = GetEntry("EnabledLeveling");
                    }
                    else
                    {
                        Builder.Description = GetEntry("AlreadyEnabled");
                    }
                }
                else if (Arg == "disable")
                {
                    if (Settings.Leveling)
                    {
                        Global.GuildSettings.Modify(Settings.GuildId, (Settings) =>
                        {
                            Settings.Leveling = false;
                        });
                        Builder.Description = GetEntry("DisabledLeveling");
                    }
                    else
                    {
                        Builder.Description = GetEntry("AlreadyDisabled");
                    }
                }
                else if (Args[0].ToLower() == "ext")
                {
                    if (Args.Length > 2)
                    {
                        string Option = Args[1].ToLower();
                        string[] Exts = Args.Skip(2).ToArray();
                        switch (Option)
                        {
                            case "add":
                                foreach (string Ext in Exts)
                                {
                                    if (!Settings.LevelingWatchExtensions.Contains(Ext.ToLower()))
                                    {
                                        Settings.LevelingWatchExtensions.Add(Ext.ToLower());
                                        Edited = true;
                                    }
                                }
                                break;
                            case "remove":
                                foreach (string Ext in Exts)
                                {
                                    if (Settings.LevelingWatchExtensions.Contains(Ext.ToLower()))
                                    {
                                        Settings.LevelingWatchExtensions.Remove(Ext.ToLower());
                                        Edited = true;
                                    }
                                }
                                break;
                            case "addbundle":
                                foreach (string Ext in Exts)
                                {
                                    if (Formats.ContainsKey(Ext.ToLower()))
                                    {
                                        foreach (string FormatExt in Formats[Ext.ToLower()])
                                        {
                                            if (!Settings.LevelingWatchExtensions.Contains(FormatExt))
                                            {
                                                Settings.LevelingWatchExtensions.Add(FormatExt);
                                                Edited = true;
                                            }
                                        }
                                    }
                                }
                                break;
                            case "removebundle":
                                foreach (string Ext in Exts)
                                {
                                    if (Formats.ContainsKey(Ext.ToLower()))
                                    {
                                        foreach (string FormatExt in Formats[Ext.ToLower()])
                                        {
                                            if (Settings.LevelingWatchExtensions.Contains(FormatExt))
                                            {
                                                Settings.LevelingWatchExtensions.Remove(FormatExt);
                                                Edited = true;
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
                else if (Args[0].ToLower() == "reset")
                {
                    if (Context.Message.MentionedUserIds.Count == 0)
                    {
                        await Context.Channel.SendMessageAsync(GetEntry("OnlyHighlight"));
                    }
                    else
                    {
                        foreach (ulong Id in Context.Message.MentionedUserIds)
                        {
                            Global.Level.Users[Id].GuildLevels[Context.Guild.Id] = 0;
                            Global.Level.Users[Id].GuildXps[Context.Guild.Id] = 0;

                            Global.Level.Users.UpdateUser(Global.Level.Users[Id]);
                        }
                        if (Context.Message.MentionedUserIds.Count > 1)
                        {
                            Builder.AddField(GetEntry("Reset"), GetEntry("PluralReset"));
                        }
                        else
                        {
                            Builder.AddField(GetEntry("Reset"), GetEntry("SingularReset"));
                        }
                    }
                }
            }

            if (Settings.Leveling)
            {
                Builder.AddField(GetEntry("Leveling"), GetEntry("Enabled"));
            }
            else
            {
                Builder.AddField(GetEntry("Leveling"), GetEntry("Disabled"));
            }

            if (Settings.LevelingWatchExtensions.Count == 0)
            {
                Builder.AddField(GetEntry("Extensions"), "No specific type of attachment, each message counts now");
            }
            else
            {
                Builder.AddField(GetEntry("Extensions"), string.Join(", ", Settings.LevelingWatchExtensions));
            }

            if (Edited)
            {
                Global.GuildSettings.Update(Settings);
            }

            Builder.AddField(GetEntry("Bundles"), "- " + string.Join("\n- ", Formats.Select(t => t.Key + $" ({ string.Join(", ", t.Value) })")));

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("bindreport"), ServerCommand(), Admin(), Summary("Binds a channel to send all the level-ups")]
        public async Task BindReportAsync(params string[] Args)
        {
            string Name = string.Join(" ", Args).ToLower();

            if (string.IsNullOrWhiteSpace(Name))
            {
                if (Settings.LevelupReport == 0)
                {
                    await Context.Channel.SendMessageAsync(GetEntry("NotBound"));
                }
                else
                {
                    Global.GuildSettings.Modify(Settings.GuildId, (Settings) =>
                    {
                        Settings.LevelupReport = 0;
                    });

                    await Context.Channel.SendMessageAsync(GetEntry("RemovedBinding"));
                }
            }
            else
            {
                List<ITextChannel> Channels = (await Context.Guild.GetTextChannelsAsync()).ToList();

                ITextChannel Binding = null;

                List<ITextChannel> Similars = new List<ITextChannel>();

                foreach (var Channel in Channels)
                {
                    if (Channel.Name.ToLower() == Name)
                    {
                        Binding = Channel;
                        break;
                    }
                    else if (Channel.Name.ToLower().Contains(Name))
                    {
                        Similars.Add(Channel);
                    }
                    else if (Channel.Mention == Name)
                    {
                        Binding = Channel;
                        break;
                    }
                }
                if (Binding == null)
                {
                    Similars = Similars.OrderBy(t => Math.Abs(t.Name.CompareTo(Name))).ToList();
                    if (Similars.Count != 0)
                        Binding = Similars[0];
                }

                if (Binding == null)
                {
                    await Context.Channel.SendMessageAsync(GetEntry("ChannelNotFound"));
                }
                else
                {
                    Global.GuildSettings.Modify(Settings.GuildId, (Settings) =>
                    {
                        Settings.LevelupReport = Binding.Id;
                    });

                    await Context.Channel.SendMessageAsync(GetEntry("BoundTo", "CN", Binding.Name));
                }
            }
        }

        [Command("levelchannel"), Admin(), Summary("Remove channels from tracking")]
        public async Task LevelAvoidChannelAsync(params string[] Args)
        {
            EmbedBuilder Builder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = await Context.GetNicknameOrUsernameAsync(Context.Client.CurrentUser),
                    IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
                },
                Color = new Color(252, 117, 252)
            };

            if (Settings.LevelingAvoidChannel.Contains(Context.Channel.Id))
            {
                Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                {
                    Settings.LevelingAvoidChannel.Remove(Context.Channel.Id);
                });
                Builder.Description = GetEntry("XPWillBeEarned");
            }
            else
            {
                Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                {
                    Settings.LevelingAvoidChannel.Add(Context.Channel.Id);
                });
                Builder.Description = GetEntry("XPWillNotBeEarned");
            }

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("roleat"), Admin(), Summary("Gives roles at specific levels: level rolename")]
        public async Task RoleAtAsync(uint Level, params string[] RoleNameParts)
        {
            if (RoleNameParts.Length == 0)
                return;

            EmbedBuilder Builder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = await Context.GetNicknameOrUsernameAsync(Context.Client.CurrentUser),
                    IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
                },
                Color = new Color(252, 117, 252)
            };

            string Rolename = string.Join(" ", RoleNameParts).ToLower();

            IRole Role = null;

            foreach (IRole GuildRole in Context.Guild.Roles)
            {
                if (GuildRole.Name.ToLower() == Rolename)
                {
                    Role = GuildRole;
                    break;
                }
            }

            if (Role == null)
            {
                Builder.Description = GetEntry("RoleNotFound");
            }
            else
            {
                if (Settings.AssignRoleAtLevels.ContainsKey(Level))
                {
                    if (Settings.AssignRoleAtLevels[Level].Contains(Role.Id))
                    {
                        Builder.Description = GetEntry("AlreadyAssigned");
                    }
                    else
                    {
                        Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                        {
                            Settings.AssignRoleAtLevels[Level].Add(Role.Id);
                        });
                    }
                }
                else
                {
                    Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                    {
                        Settings.AssignRoleAtLevels.Add(Level, new List<ulong>() { Role.Id });
                    });
                }
                if (string.IsNullOrWhiteSpace(Builder.Description))
                {
                    Builder.Description = GetEntry("WillBeGiven", "RN", Rolename, "LVL", Level.ToString());
                }
            }

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("listroleat"), Summary("Lists all the roles what will be given at specific levels")]
        public async Task ListRoleAtAsync(params string[] Args)
        {
            if (Settings.AssignRoleAtLevels.Count == 0)
            {
                await Context.Channel.SendMessageAsync(GetEntry("NoAssignRoleAt"));
            }
            else
            {
                string List = "";
                foreach (var item in Settings.AssignRoleAtLevels)
                {
                    string pre = GetEntry("AtLevel", "LVL", item.Key.ToString());
                    List += pre;
                    for (int i = 0; i < item.Value.Count; i++)
                    {
                        if (Context.Guild.GetRole(item.Value[i]) is IRole role)
                        {
                            if (i == 0)
                            {
                                List += "- " + role.Mention + "\n";
                            }
                            else
                            {
                                List += "- ".PadLeft(pre.Length + 2) + role.Mention + "\n";
                            }
                        }
                    }
                }
                await Context.Channel.SendMessageAsync(List);
            }
        }

        [Command("removeroleat"), Admin(), Summary("Remove a level - role assignment: level rolename")]
        public async Task RemoveRoleAtAsync(uint Level, params string[] RoleNameParts)
        {
            if (RoleNameParts.Length == 0)
                return;

            EmbedBuilder Builder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = await Context.GetNicknameOrUsernameAsync(Context.Client.CurrentUser),
                    IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
                },
                Color = new Color(252, 117, 252)
            };

            string Rolename = string.Join(" ", RoleNameParts).ToLower();

            IRole Role = null;

            foreach (IRole GuildRole in Context.Guild.Roles)
            {
                if (GuildRole.Name.ToLower() == Rolename)
                {
                    Role = GuildRole;
                    break;
                }
            }

            if (Role == null)
            {
                Builder.Description = GetEntry("RoleNotFound");
            }
            else
            {
                if (Settings.AssignRoleAtLevels.ContainsKey(Level))
                {
                    if (!Settings.AssignRoleAtLevels[Level].Contains(Role.Id))
                    {
                        Builder.Description = GetEntry("NotAssigned");
                    }
                    else
                    {
                        Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                        {
                            Settings.AssignRoleAtLevels[Level].Remove(Role.Id);
                        });
                    }
                }
                else
                {
                    Builder.Description = GetEntry("NotAssigned");
                }
                if (string.IsNullOrWhiteSpace(Builder.Description))
                {
                    Builder.Description = GetEntry("Removed", "RN", Role.Name, "LVL", Level.ToString());
                }
            }

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("purgeleft"), Admin(), Summary("Purges all the left users' xp")]
        public async Task PurgeLeftAsync(params string[] args)
        {
            int Purged = 0;
            for (int i = 0; i < Global.Level.Users.Count; i++)
            {
                KeyValuePair<ulong, Leveling.User> User = Global.Level.Users.ElementAt(i);

                if (User.Value.GuildXps.ContainsKey(Context.Guild.Id))
                {
                    IGuildUser usr = await Context.Guild.GetUserAsync(User.Value.UserId);

                    if (usr == null)
                    {
                        User.Value.GuildXps.Remove(Context.Guild.Id);
                        Global.Level.Users.UpdateUser(User.Value);
                        Purged++;
                    }
                }
            }
            await Context.Channel.SendMessageAsync(GetEntry("Purged", "X", Purged.ToString()));
        }
    }
}
