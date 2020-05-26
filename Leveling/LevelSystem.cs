using Chino_chan.Models.Settings;
using Chino_chan.Models.Settings.Language;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Leveling
{
    public class LevelSystem
    {
        public UserHandler Users { get; set; }

        public List<uint> Levels { get; set; }
        
        public LevelSystem(DiscordSocketClient Client)
        {
            Levels = new List<uint>()
            {
                100
            };

            Users = new UserHandler(this);

            Client.MessageReceived += async (Message) =>
            {
                if (!(Message is SocketUserMessage sMessage))
                    return;
                
                ICommandContext Context = new SocketCommandContext(Global.Client, sMessage);

                if (Context.Guild == null || Message.Author.IsBot)
                    return;

                GuildSetting Settings = Context.Guild.GetSettings();
                LanguageEntry Language = Settings.GetLanguage();

                if (!Settings.Leveling)
                    return;

                if (Settings.LevelingAvoidChannel.Contains(Context.Channel.Id))
                    return;

                if (Settings.LevelingWatchExtensions.Count != 0)
                {
                    if (Message.Attachments.Count == 0)
                    {
                        return;
                    }

                    foreach (IAttachment Attachment in Message.Attachments)
                    {
                        if (!Settings.LevelingWatchExtensions.Contains(Attachment.Url.Split('.').Last().ToLower()))
                        {
                            return;
                        }
                    }
                }

                User User;

                if (Users.ContainsKey(Message.Author.Id))
                {
                    User = Users[Message.Author.Id];
                }
                else
                {
                    User = new User()
                    {
                        UserId = Message.Author.Id
                    };
                }


                uint Xp = Global.Random.NextUInt(5, 7);

                uint LengthBonus = (uint)(Message.Content.Length / 40);
                if (LengthBonus > 5)
                    LengthBonus = 5;

                Xp += LengthBonus;

                if (!User.GuildXps.ContainsKey(Context.Guild.Id))
                {
                    User.GuildXps.Add(Context.Guild.Id, Xp);
                    User.GuildLevels.Add(Context.Guild.Id, 0);
                }
                else
                {
                    User.GuildXps[Context.Guild.Id] += Xp;
                }

                uint NextLevelExp = Levels[(int)User.GuildLevels[Context.Guild.Id]];
                if (User.GuildXps[Context.Guild.Id] > NextLevelExp)
                {
                    User.GuildLevels[Context.Guild.Id]++;
                    if (Levels.Count - 1 <= User.GuildLevels[Context.Guild.Id])
                        GenerateLevel(1);

                    IGuildUser gUser = Context.User as IGuildUser;

                    string Prefix = Context.Guild.GetSettings().Prefix;

                    IMessageChannel Channel = Message.Channel;

                    if (Settings.LevelupReport != 0)
                    {
                        ITextChannel ReportChannel = await Context.Guild.GetTextChannelAsync(Settings.LevelupReport);

                        if (ReportChannel != null)
                            Channel = ReportChannel;
                    }
                    EmbedBuilder Embed = new EmbedBuilder()
                    {
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = gUser.Nickname ?? gUser.Username,
                            IconUrl = gUser.GetAvatarUrl()
                        },
                        Description = Language.GetEntry("LevelSystem:Congratulation", "USER", gUser.Nickname ?? gUser.Username, "LEVEL", User.GuildLevels[Context.Guild.Id] + ""),
                        Footer = new EmbedFooterBuilder()
                        {
                            Text = Language.GetEntry("LevelSystem:LevelFooter", "PREFIX", Prefix)
                        },
                        Color = Color.DarkPurple
                    };
                    try
                    {
                        await Channel.SendMessageAsync("", embed: Embed.Build());
                    }
                    catch { } // Couldn't send the message, doesn't have permission or sth like that
                    
                    if (Settings.AssignRoleAtLevels.ContainsKey(User.GuildLevels[Context.Guild.Id]))
                    {
                        List<ulong> RoleIds = Settings.AssignRoleAtLevels[User.GuildLevels[Context.Guild.Id]];

                        foreach (ulong RoleId in RoleIds)
                        {
                            if (Context.Guild.GetRole(RoleId) is IRole Role)
                            {
                                if (!gUser.RoleIds.Contains(RoleId))
                                {
                                    await gUser.AddRoleAsync(Role);
                                }
                            }

                        }
                    }
                }

                Users.UpdateUser(User);
            };
        }
        public void GenerateLevelUntilXP(uint XP)
        {
            do
            {
                Levels.Add(CalculateXP());
            }
            while (Levels[Levels.Count - 1] < XP);
        }
        public uint GetLevelFromXP(uint XP)
        {
            for (int i = 0; i < Levels.Count; i++)
            {
                if (Levels[i] > XP)
                {
                    return (uint)i + 1;
                }

                if (Levels.Count - 1 == i)
                {
                    GenerateLevel(1);
                }
            }
            return 0;
        }
        
        public void GenerateLevel(uint Count)
        {
            for (uint i = 0; i < Count; i++)
            {
                Levels.Add(CalculateXP());
            }
        }
        

        uint CalculateXP()
        {
            uint LastXP = Levels[Levels.Count - 1];
            return LastXP + (uint)Math.Pow(Levels.Count, 2) * 64;
        }
    }
}
