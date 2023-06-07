using Chino_chan.Models;
using Chino_chan.Models.API;
using Chino_chan.Models.Privileges;
using Chino_chan.Models.Settings;
using Chino_chan.Models.Settings.Language;
using Chino_chan.Models.Twitch;
using Chino_chan.Modules;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Commands
{
    [RequireTwitch, ServerCommand, Admin]
    public class Twitch : ChinoContext
    {
        [Command("register"), Summary("Registers a user for checking whether his / her stream is up or not")]
        public async Task RegisterAsync(params string[] Args)
        {
            EmbedBuilder Builder = new EmbedBuilder()
            {
                Title = GetEntry("TwitchTrack"),
                Color = Color.Red
            };

            if (Settings.TwitchTrack.ChannelId == 0)
            {
                await Context.Channel.SendMessageAsync(GetEntry("SetReport", "PREFIX", Settings.Prefix));
                return;
            }

            if (Args.Length == 0)
            {
                Builder.Description = GetEntry("HowToAdd", "PREFIX", Settings.Prefix);
            }
            else if (Args.Length > 1)
            {
                Builder.Description = GetEntry("NoSpace");
            }
            else
            {
                RegisterStatus Status = Global.TwitchTracker.Register(Args[0]);

                UserResponse? User = null;
                if (Status != RegisterStatus.UserNotFound)
                    User = Global.TwitchTracker.GetUser(Args[0]);

                switch (Status)
                {
                    case RegisterStatus.FailedToStart:
                        Builder.Description = "Failed to get API key.";
                        break;
                    case RegisterStatus.UserNotFound:
                        Builder.Description = GetGlobalEntry("UserNotFound");
                        break;
                    default:
                        Builder.ThumbnailUrl = User.Value.Avatar;

                        if (Settings.TwitchTrack.UserIds.Contains(User.Value.Id))
                        {
                            Builder.Description = GetEntry("AlreadyTracked", "USER", $"{ User.Value.DisplayName }({ User.Value.LoginUsername })");
                            break;
                        }

                        Builder.Color = Global.Pink;
                        Builder.ThumbnailUrl = User.Value.Avatar;
                        Builder.Description = GetEntry("StartedTracking", "USER", $"{ User.Value.DisplayName }({ User.Value.LoginUsername })");

                        Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                        {
                            Settings.TwitchTrack.UserIds.Add(User.Value.Id);
                        });

                        break;
                }

            }

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("unregister"), Summary("Unregisters a user from being checked whether his / her stream is up or not")]
        public async Task UnregisterAsync(params string[] Args)
        {
            EmbedBuilder Builder = new EmbedBuilder()
            {
                Title = GetEntry("TwitchTrack"),
                Color = Color.Red
            };
            if (Args.Length == 0)
            {
                Builder.Description = GetEntry("HowToUnregister", "PREFIX", Settings.Prefix);
            }
            else if (Args.Length > 1)
            {
                Builder.Description = GetEntry("NoSpace");
            }
            else
            {
                UnregisterStatus Status = Global.TwitchTracker.Unregister(Args[0]);

                UserResponse? User = null;
                if (Status != UnregisterStatus.UserNotFound)
                    User = Global.TwitchTracker.GetUser(Args[0]);

                switch (Status)
                {
                    case UnregisterStatus.FailedToStart:
                        Builder.Description = "Failed to get API key.";
                        break;
                    case UnregisterStatus.UserNotFound:
                        Builder.Description = GetGlobalEntry("UserNotFound");
                        break;
                    default:
                        if (!Settings.TwitchTrack.UserIds.Contains(User.Value.Id))
                        {
                            Builder.ThumbnailUrl = User.Value.Avatar;
                            Builder.Description = GetEntry("NotTracked", "USER", $"{ User.Value.DisplayName }({ User.Value.LoginUsername })");
                            break;
                        }

                        Builder.Color = Global.Pink;
                        Builder.ThumbnailUrl = User.Value.Avatar;
                        Builder.Description = GetEntry("Unregistered", "USER", $"{ User.Value.DisplayName }({ User.Value.LoginUsername })");

                        Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                        {
                            Settings.TwitchTrack.UserIds.Remove(User.Value.Id);
                        });

                        break;
                }

            }

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("twitchup"), Summary("Turns on or off (whether you give true or false) the stream up message")]
        public async Task TwitchUpAsync(bool Up)
        {
            if (Settings.TwitchTrack.ChannelId == 0)
            {
                await Context.Channel.SendMessageAsync(GetEntry("SetReportChannel", "PREFIX", Settings.Prefix));
                return;
            }

            Global.GuildSettings.Modify(Settings.GuildId, Settings =>
            {
                Settings.TwitchTrack.SendStreamUp = Up;
            });
            
            if (Up)
            {
                await Context.Channel.SendMessageAsync(GetEntry("WillSend"));
            }
            else
            {
                await Context.Channel.SendMessageAsync(GetEntry("WillNotSend"));
            }
        }

        [Command("twitchdown"), Summary("Turns on or off (whether you give true or false) the stream down message")]
        public async Task TwitchDownAsync(bool Down)
        {
            if (Settings.TwitchTrack.ChannelId == 0)
            {
                await Context.Channel.SendMessageAsync(GetEntry("SetReportChannel", "PREFIX", Settings.Prefix));
                return;
            }

            Global.GuildSettings.Modify(Settings.GuildId, Settings =>
            {
                Settings.TwitchTrack.SendStreamDown = Down;
            });
            if (Down)
            {
                await Context.Channel.SendMessageAsync(GetEntry("WillSend"));
            }
            else
            {
                await Context.Channel.SendMessageAsync(GetEntry("WillNotSend"));
            }
        }

        [Command("twitchupmsg"), Summary("Sets the stream up message")]
        public async Task TwitchUpMessageAsync(params string[] args)
        {
            if (args.Length == 0)
                await Context.Channel.SendMessageAsync(GetEntry("Provide"));
            else
            {
                Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                {
                    Settings.TwitchTrack.StreamUpNotification = string.Join(" ", args);
                });

                await Global.SendTwitchUp(Context.Channel as ITextChannel, Settings.TwitchTrack, new StreamResponse()
                {
                    Title = GetEntry("TestStream"),
                    ThumbnailUrl = Context.User.GetAvatarUrl(size: 2048) ?? Context.User.GetDefaultAvatarUrl() + "?size=2048"
                }, new UserResponse()
                {
                    DisplayName = (Context.User as IGuildUser).Nickname ?? Context.User.Username,
                    LoginUsername = Context.User.Username
                }, true);
            }
        }

        [Command("twitchdownmsg"), Summary("Sets the stream down message")]
        public async Task TwitchDownMessageAsync(params string[] args)
        {
            if (args.Length == 0)
                await Context.Channel.SendMessageAsync(GetEntry("Provide"));
            else
            {
                Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                {
                    Settings.TwitchTrack.StreamDownNotification = string.Join(" ", args);
                });

                await Global.SendTwitchDown(Context.Channel as ITextChannel, Settings.TwitchTrack, new StreamResponse()
                {
                    Title = GetEntry("TestStream"),
                    ThumbnailUrl = Context.User.GetAvatarUrl(size: 2048) ?? Context.User.GetDefaultAvatarUrl() + "?size=2048"
                }, new UserResponse()
                {
                    DisplayName = (Context.User as IGuildUser).Nickname ?? Context.User.Username,
                    LoginUsername = Context.User.Username
                }, true);
            }
        }

        [Command("twitchembed"), Summary("Send embed instread of simple message")]
        public async Task TwitchSendEmbed(bool Send)
        {
            Global.GuildSettings.Modify(Settings.GuildId, Settings =>
            {
                Settings.TwitchTrack.SendEmbed = Send;
            });
            if (Send)
            {
                await Context.Channel.SendMessageAsync(GetEntry("WillSendEmbed"));
            }
            else
            {
                await Context.Channel.SendMessageAsync(GetEntry("WillSendSimpletext"));
            }

            await Global.SendTwitchUp(Context.Channel as ITextChannel, Settings.TwitchTrack, new StreamResponse()
            {
                Title = GetEntry("TestStream"),
                ThumbnailUrl = Context.User.GetAvatarUrl(size: 2048) ?? Context.User.GetDefaultAvatarUrl() + "?size=2048"
            }, new UserResponse()
            {
                DisplayName = (Context.User as IGuildUser).Nickname ?? Context.User.Username,
                LoginUsername = Context.User.Username,
                Avatar = Context.User.GetAvatarUrl(size: 512) ?? Context.User.GetDefaultAvatarUrl() + "?size=512"
            }, true);
        }

        [Command("twitchchannel"), Summary("Sets the channel where the Twitch stream up notifications should be sent (the channel where this command was typed)")]
        public async Task TwitchChannelAsync(params string[] args)
        {
            try
            {
                await Context.Channel.SendMessageAsync(GetEntry("Set"));
                Global.GuildSettings.Modify(Settings.GuildId, Settings =>
                {
                    Settings.TwitchTrack.ChannelId = Context.Channel.Id;
                });
            }
            catch
            {
                IDMChannel dm = await Context.User.CreateDMChannelAsync();
                await dm.SendMessageAsync(GetEntry("CannotSend"));
            }
        }
    }
}
