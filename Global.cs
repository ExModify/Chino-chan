using Chino_chan.Image;
using Chino_chan.Modules;
using Chino_chan.Models.Settings;
using Chino_chan.Models.Image;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Troschuetz.Random;
using Troschuetz.Random.Generators;
using System.Net;
using System.IO.Compression;
using Chino_chan.Models.Settings.Credentials;
using System.Net.Http;
using System.Text.RegularExpressions;
using Chino_chan.Leveling;
using Chino_chan.Models.Twitch;
using Chino_chan.Models.Settings.Language;
using osuBeatmapUtilities;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Chino_chan
{
    public static class Global
    {
        #region Variables
        private static string SettingsPath = "Settings";
        private static TimeSpan StartedTime;

        public static Color Pink { get; } = new Color(255, 168, 235);
        public static TimeSpan Uptime
        {
            get
            {
                return new TimeSpan(DateTime.Now.Ticks) - StartedTime;
            }
        }

        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        public static bool osuAPIEnabled
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Settings.Credentials.osu.Token) && Settings.OSUAPICallLimit > 0;
            }
        }
        public static Settings Settings { get; private set; }
        public static GuildSettings GuildSettings { get; private set; }
        public static LanguageHandler Languages { get; private set; }
        public static DiscordSocketClient Client { get; private set; }
        public static CommandService CommandService { get; private set; }
        public static TRandom Random { get; private set; }
        public static ImageHandler Images { get; private set; }
        public static Sankaku Sankaku { get; private set; }
        public static ImageFetcher DanbooruFetcher { get; private set; }
        public static ImageFetcher GelbooruFetcher { get; private set; }
        public static ImageFetcher YandereFetcher { get; private set; }
        public static SubTagHandler SubTagHandler { get; private set; }
        public static PollManager Polls { get; private set; }
        public static Image.Imgur Imgur { get; private set; }
        public static SysInfo SysInfo { get; private set; }
        public static SocketTextChannel JunkChannel
        {
            get
            {
                var Channel = Client.GetChannel(Settings.DevServer.JunkChannelId);

                if (Channel != null)
                    return Channel as SocketTextChannel;

                return null;
            }
        }
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        public static osuApi osuAPI { get; private set; }
        public static LevelSystem Level { get; private set; }
        public static ChinoAPI ChinoAPI { get; private set; }
        public static TwitchTracker TwitchTracker { get; private set; }
        public static SoundCloud SoundCloud { get; private set; }
        public static MusicHandler MusicHandler { get; private set; }
        public static WelcomeBannerManager WelcomeBannerManager { get; private set; }
        public static BeatmapManager Beatmaps { get; private set; }
        public static MultiRoleReactionHandler MultiRoleHandler { get; private set; }
        public static osuTracker Tracker { get; private set; }

        public static bool Running { get; private set; }
        public static int CommandsUnderExecution { get; set; } = 0;
        public static bool ReadyFired { get; private set; } = false;
        #endregion

        public static void Setup()
        {
            StartedTime = new TimeSpan(DateTime.Now.Ticks);
            Random = new TRandom(new NR3Generator(TMath.Seed()));
            Logger.Setup();
            Logger.Log(LogType.Info, ConsoleColor.Magenta, null, "Welcome to Chino-chan!");

            Languages = new LanguageHandler();
            CheckExternalLibs();

            LoadSettings();
            LoadImageFetchers();

            PPCalculator.Init();

            SubTagHandler = new SubTagHandler();
            
            if (!Settings.Credentials.IsEmpty(CredentialType.Imgur))
            {
                Imgur = new Image.Imgur();
            }

            GuildSettings = new GuildSettings();

            if (!Settings.Credentials.IsEmpty(CredentialType.Twitch)
                && Settings.TwitchStreamUpdate > 0)
            {
                TwitchTracker = new TwitchTracker();

                TwitchTracker.OnStreamUp += async (Stream, User) =>
                {
                    foreach (var Settings in GuildSettings.Settings)
                    {
                        if (Settings.Value.TwitchTrack.UserIds.Contains(User.Id))
                        {
                            if (!Settings.Value.TwitchTrack.SendStreamUp)
                                continue;

                            SocketGuild Guild = Client.GetGuild(Settings.Key);

                            if (Guild != null)
                            {
                                SocketTextChannel Channel = Guild.GetTextChannel(Settings.Value.TwitchTrack.ChannelId);
                                
                                if (Channel == null)
                                {
                                    Channel = Guild.DefaultChannel;
                                }

                                if (Channel != null)
                                {
                                    await SendTwitchUp(Channel, Settings.Value.TwitchTrack, Stream, User);
                                }
                            }
                        }
                    }
                };
                TwitchTracker.OnStreamDown += async (Stream, User) =>
                {
                    foreach (var Settings in GuildSettings.Settings)
                    {
                        if (Settings.Value.TwitchTrack.UserIds.Contains(User.Id))
                        {
                            if (!Settings.Value.TwitchTrack.SendStreamDown)
                                continue;

                            SocketGuild Guild = Client.GetGuild(Settings.Key);

                            if (Guild != null)
                            {
                                SocketTextChannel Channel = Guild.GetTextChannel(Settings.Value.TwitchTrack.ChannelId);

                                if (Channel == null)
                                {
                                    Channel = Guild.DefaultChannel;
                                }

                                if (Channel != null)
                                {
                                    await SendTwitchDown(Channel, Settings.Value.TwitchTrack, Stream, User);
                                }
                            }
                        }
                    }
                };
            }

            Client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                AlwaysDownloadUsers = true,
                DefaultRetryMode = RetryMode.AlwaysRetry,
                LargeThreshold = 250,
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 200,
                ExclusiveBulkDelete = false
            });
            Level = new LevelSystem(Client);
            WelcomeBannerManager = new WelcomeBannerManager();

            Client.Log += (Log) =>
            {
                Logger.Log(LogType.Discord, ConsoleColor.White, Log.Severity.ToString(), Log.Message);
                if (Log.Exception != null)
                {
                    Logger.Log(LogType.Discord, ConsoleColor.Red, "Discord Exception",
                        "Message: " + Log.Exception.Message + "\r\nStack Trace: " + Log.Exception.StackTrace + "\r\nSource: " + Log.Exception.Source);
                }
                return Task.CompletedTask;
            };
            Client.Ready += () =>
            {
                if (!ReadyFired)
                {
                    MusicHandler = new MusicHandler();
                    
                    SetupDiscordLogger().GetAwaiter().GetResult();
                    Tracker = new osuTracker(osuAPI);
                    Tracker.OnNewScore += (user, top, beatmap_rank, tryCounter, score, usr, oldPP) =>
                    {
                        for (int i = 0; i < GuildSettings.Settings.Values.Count; i++)
                        {
                            GuildSetting setting = GuildSettings.Settings.Values.ElementAt(i);
                            if (setting.Tracks.ContainsKey(user.UserId))
                            {
                                Track track = setting.Tracks[user.UserId].Find(t =>
                                {
                                    if (t.Mode == user.Mode)
                                    {
                                        if (t.Both)
                                        {
                                            if (t.Rank > 0 && t.Top > 0)
                                            {
                                                return t.Rank > beatmap_rank && t.Top > top;
                                            }
                                            else if (t.Rank > 0)
                                            {
                                                return t.Rank > beatmap_rank;
                                            }
                                            else if (t.Top > 0)
                                            {
                                                return t.Top > top;
                                            }
                                            else return true;
                                        }
                                        else
                                        {
                                            if (t.Rank > 0 && t.Top > 0)
                                            {
                                                return t.Rank > beatmap_rank || t.Top > top;
                                            }
                                            else if (t.Rank > 0)
                                            {
                                                return t.Rank > beatmap_rank;
                                            }
                                            else if (t.Top > 0)
                                            {
                                                return t.Top > top;
                                            }
                                            else return true;
                                        }
                                    }
                                    return false;
                                });
                                if (track != null)
                                {
                                    var ppCount = PPCalculator.CountStd(score.BeatmapId, score.MaxCombo, score.Count100, score.Count50, score.Misses, score.Accuracy, score.Mods);

                                    if (track.MinPP > ppCount.PP)
                                        continue;

                                    Models.osuAPI.Beatmap beatmap = osuAPI.GetBeatmapAsync(score.BeatmapId, track.Mode, null).Result;

                                    float ppChange = usr.PP - oldPP;

                                    string ppText = (ppChange < 0 ? "" : "+") + ppChange.ToString("N2");
                                    var maxPP = PPCalculator.CountStd(score.BeatmapId, EnabledMods: score.Mods);
                                    string title = $"{ beatmap.Artist } - { beatmap.Title } [{ beatmap.DifficultyName }]";

                                    EmbedBuilder builder = new EmbedBuilder()
                                    {
                                        Author = new EmbedAuthorBuilder()
                                        {
                                            Name = $"{ usr.UserName } ({ user.PP:N2}pp ({ ppText }pp) #{ usr.Rank:N0} - { usr.CountryFlag }: #{ usr.CountryRank:N0})",
                                            IconUrl = "https://a.ppy.sh/" + usr.UserId,
                                            Url = "https://osu.ppy.sh/users/" + usr.UserId
                                        },
                                        ThumbnailUrl = "https://b.ppy.sh/thumb/" + beatmap.BeatmapSetId + "l.jpg",
                                        Description = "",
                                        Footer = new EmbedFooterBuilder()
                                        {
                                            IconUrl = "https://a.ppy.sh/" + beatmap.CreatorId,
                                            Text = $"Mapped by: { beatmap.Creator } | played"
                                        },
                                        Timestamp = score.Date.AddHours((DateTime.Now - DateTime.UtcNow).TotalHours),
                                    };
                                    if ((top > 0 && top < 6) || (beatmap_rank > 0 && beatmap_rank < 6))
                                    {
                                        builder.Color = Color.Gold;
                                    }
                                    else
                                    {
                                        builder.Color = GetAverageColorAsync("https://a.ppy.sh/" + usr.UserId).Result;
                                    }
                                    if (top > 0 && top < 101)
                                    {
                                        builder.Description = $"**New personal best #{ top }!**";
                                    }
                                    if (beatmap_rank > 0 && beatmap_rank < 101)
                                    {
                                        if (top > 0)
                                        {
                                            builder.Description += " - ";
                                        }
                                        builder.Description += $"**Beatmap rank: #{ beatmap_rank }!**";
                                    }
                                    string fcPP = "";

                                    if (score.MaxCombo != beatmap.MaxCombo)
                                    {
                                        fcPP = $" ({ ppCount.SameAccFCPP:N2}pp for FC)";
                                    }
                                    string mods = "";

                                    if (score.Mods != 0)
                                    {
                                        mods = " +" + Commands.osu.GetShortMods(score.Mods);
                                    }


                                    builder.AddField("Play information",
                                        $"[{ title }](https://osu.ppy.sh/b/{ score.BeatmapId })**{ mods } | { ppCount.Stars:N2}☆**\n" +
                                        $"Score: { score.Score:N0} | Accuracy: **{ score.Accuracy:N2}%** | Rank: { score.Rank }\n" +
                                        $"Combo: **{ score.MaxCombo }x** / { beatmap.MaxCombo }x - [ { score.Count300:N0} / { score.Count100:N0} / { score.Count50:N0} / { score.Misses:N0} ]\n" +
                                        $"pp: **{ ppCount.PP:N2}pp** / { maxPP.MaxPP:N2}pp{ fcPP }", false);

                                    SocketGuild g = Client.GetGuild(track.ServerId);
                                    bool sent = false;
                                    if (g != null)
                                    {
                                        SocketTextChannel ch = g.GetTextChannel(track.ChannelId);
                                        if (ch != null)
                                        {
                                            ch.SendMessageAsync(tryCounter == 0 ? "" : "Try #" + tryCounter.ToString(), embed: builder.Build()).GetAwaiter().GetResult();
                                            sent = true;
                                        }
                                    }
                                    if (!sent)
                                    {
                                        setting.Tracks[user.UserId].Remove(track);
                                        if (setting.Tracks[user.UserId].Count == 0)
                                        {
                                            setting.Tracks.Remove(user.UserId);
                                            Tracker.Check(track.UserId, track.Mode);
                                            GuildSettings.Save();
                                        }
                                    }
                                }
                            }
                        }
                    };


                    if (TwitchTracker != null)
                    {
                        TwitchTracker.StartTrack();
                    }

                    Task.Run(async () =>
                    {
                        if (!string.IsNullOrWhiteSpace(Settings.SoundCloudClientId))
                        {
                            try
                            {
                                SoundCloud = new SoundCloud(Settings.SoundCloudClientId);
                            }
                            catch (Exception e)
                            {
                                Logger.Log(LogType.SoundCloud, ConsoleColor.DarkRed, "Error", "Invalid client id, or SoundCloud is not accessible!");
                                if (Settings.DevServer.ErrorReportChannelId != 0)
                                {
                                    ITextChannel channel = Client.GetChannel(Settings.DevServer.ErrorReportChannelId) as ITextChannel;
                                    string message = $"Error happened while declaring the SoundCloud client\n```\n{ e }\n```";
                                    await SendMessageAsync(message, channel);
                                }
                            }
                        }
                        ChinoAPI = new ChinoAPI();
                        Polls = new PollManager(Client);
                        SubTagHandler.Start();

                        foreach (var setting in GuildSettings.Settings)
                        {
                            foreach (var mute in setting.Value.Mutes)
                            {
                                HandleMute(setting.Key, mute.Key, mute.Value);
                            }
                        }
                        if (Sankaku != null)
                        {
                            try
                            {
                                Logger.Log(LogType.Sankaku, ConsoleColor.DarkYellow, "Login", "Logging in...");

                                var LoggedIn = Sankaku.Login(out bool TooManyRequests);
                                if (LoggedIn)
                                {
                                    Logger.Log(LogType.Sankaku, ConsoleColor.DarkYellow, "Login", "Logged in!");
                                }
                                else
                                {
                                    Logger.Log(LogType.Sankaku, ConsoleColor.Red, "Login", "Couldn't log in due to " + (TooManyRequests ? "rate limitation!" : "wrong credentials!"));
                                    Sankaku = null;
                                }
                            }
                            catch
                            {
                                Logger.Log(LogType.Sankaku, ConsoleColor.Red, "Login", "Couldn't reach the Sankaku server!");
                                Sankaku = null;
                            }
                        }


                        Polls.Load();

                    });

                    Task.Run(() =>
                    {
                        if (!Directory.Exists("Data/osu_cache"))
                            Directory.CreateDirectory("Data/osu_cache");

                        Beatmaps = new BeatmapManager(Settings.Credentials.osu.Token, "Data/osu_cache");
                        Logger.Log(LogType.BeatmapManager, ConsoleColor.Green, null, "Loading downloaded beatmaps!");
                        Beatmaps.Load(Entrance.CancellationToken);
                        Logger.Log(LogType.BeatmapManager, ConsoleColor.Green, null, "Loading complete!");

                        PPCalculator.Init();
                    });
                }
                ReadyFired = true;
                Task.Run(async () =>
                {
                    await MusicHandler.RestoreConnectionsAsync();
                });

                return Task.CompletedTask;
            };
            MultiRoleHandler = new MultiRoleReactionHandler(Client);

            CommandService = new CommandService(new CommandServiceConfig()
            {
                CaseSensitiveCommands = false,
                LogLevel = LogSeverity.Verbose,
                DefaultRunMode = RunMode.Async
            });
            CommandService.CommandExecuted += (CommandInfo, Context, Result) =>
            {
                CommandsUnderExecution--;
                return Task.CompletedTask;
            };
            Logger.Log(LogType.Commands, ConsoleColor.Blue, null, "Loading Commands...");

            CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), null).ContinueWith((ModuleInfo) =>
            {
                Logger.Log(LogType.Commands, ConsoleColor.Yellow, null, "Loaded Commands!");
            });
            Client.MessageReceived += async (ReceivedMessage) =>
            {
                if (!(ReceivedMessage is SocketUserMessage Message))
                {
                    return;
                }
                if (Message.Author.IsBot) return;
                
                var Context = new CommandContext(Client, Message);

                if (IsBlocked(Message.Author.Id, 0)) return;
                GuildSetting Settings = GuildSettings.GetSettings(Context.Guild != null ? Context.Guild.Id : Context.User.Id);
                if (IsBlocked(Message.Author.Id, Settings.GuildId)) return;

                for (int i = 0; i < MusicHandler.RequestUserChoice.Count; i++)
                {
                    Choice choice = MusicHandler.RequestUserChoice[i];

                    if (choice.ChannelId == Context.Channel.Id)
                    {
                        if (choice.UserId == Context.User.Id)
                        {
                            if (int.TryParse(Context.Message.Content, out int Number) 
                                && Number.ToString() == Context.Message.Content)
                            {
                                if (Number > 0 && choice.MaxNumber >= Number)
                                {
                                    choice.Selected(Number - 1);
                                    try
                                    {
                                        await Message.DeleteAsync();
                                    }
                                    catch { }
                                }
                                break;
                            }
                            else if (Context.Message.Content.ToLower() == "cancel")
                            {
                                choice.Cancel();
                                try
                                {
                                    await Message.DeleteAsync();
                                }
                                catch { }
                                break;
                            }
                        }
                    }
                }
                
                LanguageEntry Language = Languages.GetLanguage(Settings.Language);
                if (Global.Settings.SayPreferences.ContainsKey(Context.User.Id) && !Message.Content.StartsWith(Settings.Prefix + "say"))
                {
                    var Prefs = Global.Settings.SayPreferences[Context.User.Id];
                    if (Prefs.Listening.ContainsKey(Context.Channel.Id))
                    {
                        if (Client.GetChannel(Prefs.Listening[Context.Channel.Id]) is ITextChannel Channel)
                        {
                            await Channel.SendMessageAsync(ProcessEmotes(Message.Content));

                            if (Prefs.AutoDel)
                            {
                                var Dm = await Context.User.GetOrCreateDMChannelAsync();
                                if (Dm.Id != Context.Channel.Id)
                                {
                                    await Message.DeleteAsync();
                                }
                            }
                        }
                        else
                        {
                            Global.Settings.SayPreferences[Context.User.Id].Listening.Remove(Context.Channel.Id);
                            SaveSettings();
                        }
                    }
                }
                
                if (Message.Content == "/gamerescape" || Message.Content == "/shrug")
                {
                    var Name = Message.Author.Username;
                    if (!Context.IsPrivate)
                    {
                        await Message.DeleteAsync();
                        Name = (await Context.Guild.GetUserAsync(Message.Author.Id)).Nickname ?? Name;
                    }
                    await Message.Channel.SendMessageAsync($"{ Name } ¯\\_(ツ)_/¯");
                    return;
                }
                else if (Message.Content == "/lenny")
                {
                    var Name = Message.Author.Username;
                    if (!Context.IsPrivate)
                    {
                        await Message.DeleteAsync();
                        Name = (await Context.Guild.GetUserAsync(Message.Author.Id)).Nickname ?? Name;
                    }
                    await Message.Channel.SendMessageAsync($"{ Name } ( ͡° ͜ʖ ͡°)");
                    return;
                }

                int Position = 0;

                if (!(Message.HasStringPrefix(Settings.Prefix, ref Position)
                    || Message.HasMentionPrefix(Client.CurrentUser, ref Position)))
                    return;

                if (Settings.AvoidedChannels.Contains(Context.Channel.Id) && !IsAdminOrHigher(Context.User.Id, Settings.GuildId)) return;

                var MessageCommand = Message.Content.Substring(Position).ToLower();

                CommandsUnderExecution++;
                if (Images.Images.ContainsKey(MessageCommand))
                {
                    new Task(async () =>
                    {
                        var Pair = Images.Images[MessageCommand];
                        if (Pair.IsNsfw && !(Context.IsPrivate || IsNsfwChannel(Settings, Message.Channel.Id)))
                        {
                            await Message.Channel.SendMessageAsync(Language.GetEntry("Global:OnlyNSFWChannels"));
                            return;
                        }
                        bool Success = false;
                        var File = "";
                        do
                        {
                            if (!string.IsNullOrWhiteSpace(File))
                            {
                                Images.Images[MessageCommand].Files.Remove(File);

                                if (Images.Images[MessageCommand].Files.Count == 0)
                                {
                                    await Message.Channel.SendMessageAsync(Language.GetEntry("Global:FolderNoImage"));
                                    break;
                                }
                            }
                            File = Pair.RandomFile();
                            Success = await SendImageAsync(File, Context.Channel, Pair.TitleIncludeName ? Pair.Name : null);
                        }
                        while (!Success);

                        CommandsUnderExecution--;
                    }).Start();
                    return;
                }
                
                var Result = await CommandService.ExecuteAsync(Context, Position, null);

                if (!Result.IsSuccess)
                {
                    switch (Result.Error)
                    {
                        case CommandError.BadArgCount:
                        case CommandError.ParseFailed:
                            await Context.Channel.SendMessageAsync(Language.GetEntry("Global:CheckParameters"));
                            break;
                        case CommandError.UnmetPrecondition:
                            switch (Result.ErrorReason)
                            {
                                case "Owner":
                                case "ServerSide":
                                case "Twitch":
                                case "osuAPI":
                                    await Context.Channel.SendMessageAsync(Language.GetEntry("Preconditions:" + Result.ErrorReason));
                                    break;
                                default:
                                    await Context.Channel.SendMessageAsync(Language.GetEntry("Preconditions:Default"));
                                    break;
                            }
                            break;
                        case CommandError.UnknownCommand:
                            if (IsOwner(Context.User.Id))
                            {
                                var Command = Tools.ConvertHighlightsBack(Message.Content.Substring(Position));

                                new Thread(() => Entrance.HandleCommand(Command, Context.Channel as ITextChannel)).Start();
                            }
                            break;
                        default:
                            if (Global.Settings.DevServer.ErrorReportChannelId != 0)
                            {
                                if (Client.GetChannel(Global.Settings.DevServer.ErrorReportChannelId) is ITextChannel Channel)
                                {
                                    string message = $"```css\nError type: { Result.Error }\nReason: { Result.ErrorReason }```";
                                    await SendMessageAsync(message, Channel);
                                }
                            }
                            await Context.Channel.SendMessageAsync(Language.GetEntry("Global:UnknownErrorOccured"));
                            break;
                    }
                }
                else
                {
                    Logger.Log(LogType.Commands, ConsoleColor.DarkRed, Context.Guild != null ? Context.Guild.Name : Context.Channel.Name, $"#{ Context.Channel.Name } { Context.User.Username } executed { Context.Message.Content }");
                }
            };

            #region Role assign
            Client.ReactionAdded += async (Cache, Channel, Reaction) =>
            {
                if (Channel is IGuildChannel GuildChannel)
                {
                    GuildSetting Setting = GuildChannel.Guild.GetSettings();
                    
                    if (Setting.ReactionAssignChannels.Contains(Channel.Id) && Reaction.Emote.Name == "✅")
                    {
                        IUserMessage Message = await Cache.DownloadAsync();

                        List<string> RoleNames = new List<string>(GuildChannel.Guild.Roles.Select(t => t.Name.ToLower()));

                        int Index = -1;
                        
                        if ((Index = RoleNames.IndexOf(Message.Content.ToLower())) > -1)
                        {
                            IGuildUser User = await GuildChannel.Guild.GetUserAsync(Reaction.UserId);
                            
                            IRole Role = GuildChannel.Guild.Roles.ElementAt(Index);

                            if (!User.RoleIds.Contains(Role.Id))
                                await User.AddRoleAsync(Role);
                        }
                    }
                }
            };
            Client.ReactionRemoved += async (Cache, Channel, Reaction) =>
            {
                if (Channel is IGuildChannel GuildChannel)
                {
                    GuildSetting Setting = GuildChannel.Guild.GetSettings();

                    if (Setting.ReactionAssignChannels.Contains(Channel.Id) && Reaction.Emote.Name == "✅")
                    {
                        IUserMessage Message = await Cache.DownloadAsync();

                        List<string> RoleNames = new List<string>(GuildChannel.Guild.Roles.Select(t => t.Name.ToLower()));

                        int Index = -1;

                        if ((Index = RoleNames.IndexOf(Message.Content.ToLower())) > -1)
                        {
                            IGuildUser User = await GuildChannel.Guild.GetUserAsync(Reaction.UserId);

                            IRole Role = GuildChannel.Guild.Roles.ElementAt(Index);

                            if (User.RoleIds.Contains(Role.Id))
                                await User.RemoveRoleAsync(Role);
                        }
                    }
                }
            };
            Client.MessageReceived += async Message =>
            {
                if (!(Message is SocketUserMessage SocketMessage))
                {
                    return;
                }

                if (Message.Channel is IGuildChannel Channel)
                {
                    GuildSetting Settings = Channel.Guild.GetSettings();

                    if (Settings.ReactionAssignChannels.Contains(Channel.Id))
                    {
                        List<string> RoleNames = new List<string>(Channel.Guild.Roles.Select(t => t.Name.ToLower()));
                        
                        if (RoleNames.Contains(Message.Content.ToLower()))
                        {
                            await SocketMessage.AddReactionAsync(new Emoji("✅"));
                        }
                    }
                }
            };
            Client.ReactionAdded += async (Cache, Channel, Reaction) =>
            {
                if (Reaction.Emote.Name == "✅")
                {
                    if (Channel is IGuildChannel channel)
                    {
                        GuildSetting settings = channel.Guild.GetSettings();
                        if (settings.AssignMessages.Find(t => t.MessageId == Reaction.MessageId && t.ChannelId == channel.Id && t.GuildId == t.GuildId) is AssignMessage message)
                        {
                            IRole role = channel.Guild.GetRole(message.RoleId);
                            IGuildUser user = await channel.Guild.GetUserAsync(Reaction.UserId);
                            if (!user.RoleIds.Contains(role.Id))
                            {
                                await user.AddRoleAsync(role);
                            }
                        }
                    }
                }
            };
            Client.ReactionRemoved += async (Cache, Channel, Reaction) =>
            {
                if (Reaction.Emote.Name == "✅")
                {
                    if (Channel is IGuildChannel channel)
                    {
                        GuildSetting settings = channel.Guild.GetSettings();
                        if (settings.AssignMessages.Find(t => t.MessageId == Reaction.MessageId && t.ChannelId == channel.Id && t.GuildId == t.GuildId) is AssignMessage message)
                        {
                            IRole role = channel.Guild.GetRole(message.RoleId);
                            IGuildUser user = await channel.Guild.GetUserAsync(Reaction.UserId);
                            if (user.RoleIds.Contains(role.Id))
                            {
                                await user.RemoveRoleAsync(role);
                            }
                        }
                    }
                }
            };
            #endregion
            #region Global recent
            Client.MessageUpdated += async (Cacheable, Message, Channel) =>
            {
                if (!(Message is SocketUserMessage SocketMessage) || !SocketMessage.Author.IsBot)
                {
                    return;
                }
                await ProcessosuMessageAsync(SocketMessage);
            };
            Client.MessageReceived += async Message =>
            {
                if (!(Message is SocketUserMessage SocketMessage) || !SocketMessage.Author.IsBot || SocketMessage.Author.Id == Client.CurrentUser.Id)
                {
                    return;
                }
                await ProcessosuMessageAsync(SocketMessage);
            };

            #endregion

            #region Join
            Client.UserJoined += async (User) =>
            {
                GuildSetting Settings = User.Guild.GetSettings();

                if (Settings.NewMemberRoles.Count > 0)
                {
                    List<ulong> RemoveRoles = new List<ulong>();

                    foreach (ulong Id in Settings.NewMemberRoles)
                    {
                        IRole Role = User.Guild.GetRole(Id);

                        if (Role == null)
                        {
                            RemoveRoles.Add(Id);
                        }
                        else
                        {
                            try
                            {
                                await User.AddRoleAsync(Role);
                            }
                            catch { }
                        }
                    }

                    if (RemoveRoles.Count > 0)
                    {
                        GuildSettings.Modify(Settings.GuildId, settings =>
                        {
                            foreach (ulong Id in RemoveRoles)
                                settings.NewMemberRoles.Remove(Id);
                        });
                    }
                }
            };

            Client.UserJoined += async User =>
            {
                GuildSetting Settings = User.Guild.GetSettings();

                if (Settings.Greet != null)
                {
                    if (Settings.Greet.Enabled)
                    {
                        ITextChannel Channel = User.Guild.GetTextChannel(Settings.Greet.ChannelId);

                        if (Channel != null)
                        {
                            var Resp = GreetMessage(User);
                            await Channel.SendMessageAsync(Resp.Item1, embed: Resp.Item2);
                        }
                    }
                }
            };
            #endregion
            
            SysInfo = new SysInfo();
            SysInfo.Load();

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        public static async Task StartAsync()
        {
            await Client.LoginAsync(TokenType.Bot, Settings.Credentials.Discord.Token);
            await Client.StartAsync();

            Running = true;
        }
        public static void Stop()
        {
            Entrance.CancellationTokenSource.Cancel();
            Client.StopAsync();

            Running = false;
            SubTagHandler.Stop();
        }
        public static async Task<bool> SendImageAsync(string File, IMessageChannel Channel, string Title = null, string Description = null)
        {
            EmbedBuilder Builder = new EmbedBuilder
            {
                Color = new Color(0, 255, 255),
                Title = string.IsNullOrWhiteSpace(Title) ? "" : Title,
                Description = string.IsNullOrWhiteSpace(Description) ? "" : Description
            };

            var Stream = new FileStream(File, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            string Url;

            if (Stream.Length / 1024 / 1024 > 8192)
            {
                if (Imgur == null)
                {
                    return false;
                }
                else
                {
                    Url = await Imgur.UploadImage(Stream);
                }
            }
            else
            {
                var Message = await JunkChannel.SendFileAsync(Stream, Path.GetFileName(File), "");
                Url = Message.Attachments.ElementAt(0).Url;
            }

            Builder.WithImageUrl(Url);

            await Channel.SendMessageAsync("", embed: Builder.Build());

            return true;
        }
        private static async Task SetupDiscordLogger()
        {
            SocketGuild dev = Client.GetGuild(Settings.DevServer.Id);

            ITextChannel watchChannel = dev.GetTextChannel(Settings.DevServer.WatchChannelId);

            Logger.StartDiscordLogging(Client);

            if (osuAPIEnabled)
            {
                try
                {
                    osuAPI = new osuApi();

                    float avg = 0;
                    int count = 0, min = int.MaxValue, max = 0;
                    string scheme = "`---osu api calls---`\nAverage: `{0}`\nMinimum: `{1}`\nMaximum: `{2}`\nCurrent: `{3}`\nCount: `{4}`";


                    List<IMessage> messages = await watchChannel.GetMessagesAsync().Flatten().ToListAsync();
                    IUserMessage msg = null;
                    if (messages.Count > 0)
                    {
                        foreach (IMessage m in messages)
                        {
                            if (m.Author.Id == Client.CurrentUser.Id)
                            {
                                msg = m as IUserMessage;
                                break;
                            }
                        }
                    }

                    if (msg != null)
                    {

                        Regex parser = new Regex(Regex.Replace(scheme, "{\\d}", @"(\d*\.?\d*)"));

                        GroupCollection m = parser.Match(msg.Content.Replace(",", "")).Groups;
                        count = int.Parse(m[5].Value);
                        avg = float.Parse(m[1].Value) * count;

                        min = int.Parse(m[2].Value);
                        max = int.Parse(m[3].Value);
                    }
                    else
                    {
                        msg = await watchChannel.SendMessageAsync(string.Format(scheme, 0, 0, 0, 0, 0));
                    }

                    osuAPI.OnResetBegin += async calls =>
                    {
                        avg += calls;
                        if (calls < min)
                        {
                            min = calls;
                        }
                        if (calls > max)
                        {
                            max = calls;
                        }
                        count++;
                        await msg.ModifyAsync(t => t.Content = string.Format(scheme,
                            (avg / count).ToString("N2", CultureInfo.GetCultureInfo(1033)), min.ToString("N0"), max.ToString("N0"), calls.ToString("N0"), count.ToString("N0")));
                    };
                }
                catch
                {
                    Logger.Log(LogType.osuApi, ConsoleColor.Red, "Error", "Invalid API key!");
                }
            }
        }
        private static void LoadImageFetchers()
        {
            DanbooruFetcher = new ImageFetcher(new FetcherOptions("https://danbooru.donmai.us/posts.json", new Dictionary<string, string>()
            {
                { "limit", "10" },
                { "random", "true" }
            }, "large_file_url"));
            GelbooruFetcher = new ImageFetcher(new FetcherOptions("https://gelbooru.com/index.php", new Dictionary<string, string>()
            {
                { "page", "dapi" },
                { "s", "post" },
                { "q", "index" },
                { "limit", "24" },
                { "json", "1" },
            }, PageQuery: "pid"), new ImageFetcher.OutAction<string, int, List<string>>((string Tags, int Limit, out List<string> Images) =>
            {
                Images = new List<string>();

                string[] extensions = new string[]
                {
                    "png",
                    "gif",
                    "mp4",
                    "webm"
                };

                string Endpoint = "https://gelbooru.com/index.php?page=post&s=list&tags=" + Tags; // + page

                try
                {
                    HttpClientHandler handler = new HttpClientHandler();
                    CookieContainer container = new CookieContainer();
                    container.Add(new Cookie("fringeBenefits", "yup", "/", "gelbooru.com"));
                    handler.CookieContainer = container;
                    HttpClient Client = new HttpClient(handler);
                    HttpResponseMessage Response = Client.GetAsync(Endpoint).Result;
                    string Content = Response.Content.ReadAsStringAsync().Result;

                    Regex Regex = new Regex("<img src=\"https:\\/\\/gelbooru\\.com\\/thumbnails([^\"]*)\" alt=\"Image: \\d*\" title=\"[^\"]*\" class=\"preview\\s([^\"]*)\"\\/>");
                    foreach (Match Match in Regex.Matches(Content))
                    {
                        string imgLink = "https://img2.gelbooru.com/images/" + Match.Groups[1].Value.Replace("thumbnail_", "");

                        if (Match.Groups[2].Value.Length != 0)
                        {
                            imgLink = imgLink.Substring(0, imgLink.Length - 3) + Match.Groups[2].Value;
                        }

                        if (Client.GetAsync(imgLink).Result.StatusCode == HttpStatusCode.NotFound)
                        {
                            foreach (string extension in extensions)
                            {
                                imgLink = imgLink.Substring(0, imgLink.LastIndexOf('.') + 1) + extension;
                                if (Client.GetAsync(imgLink).Result.StatusCode != HttpStatusCode.NotFound)
                                    break;
                            }
                        }


                        Images.Add(imgLink);

                        if (Images.Count == Limit)
                            break;
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(LogType.Error, ConsoleColor.Red, "SubTag", "Could not fetch images from Gelbooru site!\r\n" + e.ToString());
                }

            }),
            PageParser: new ImageFetcher.OutAction<string, int>((string Tags, out int Page) =>
            {
                Page = Gelbooru.RandomPage(Tags);
            }));
            YandereFetcher = new ImageFetcher(new FetcherOptions("https://yande.re/post.json", new Dictionary<string, string>()
            {
                { "limit", "50" },
            }, PageQuery: "page"), PageParser: new ImageFetcher.OutAction<string, int>((string Tags, out int Page) =>
            {
                string Endpoint = "https://yande.re/post?limit=50&tags=" + Tags;

                HttpClient Client = new HttpClient();
                HttpResponseMessage Response = Client.GetAsync(Endpoint).Result;
                string Content = Response.Content.ReadAsStringAsync().Result;

                if (Content.Contains("<div class=\"pagination\">"))
                {
                    int Index = Content.IndexOf("<div class=\"pagination\">");
                    Content = Content.Substring(Index, Content.IndexOf("</div", Index) - Index);

                    Regex Regex = new Regex("<a href=\".*?\">(\\d*)<\\/a>");
                    Match Match = Regex.Matches(Content).Cast<Match>().Last();

                    Page = int.Parse(Match.Groups[1].Value);
                }
                else
                {
                    Page = 0;
                }
            }));

            if (!Settings.Credentials.IsEmpty(CredentialType.Sankaku))
            {
                Sankaku = new Sankaku(Settings.Credentials.Sankaku.Username, Settings.Credentials.Sankaku.Password);
            }
        }
        private static void CheckExternalLibs()
        {
            Logger.Log(LogType.ExternalModules, ConsoleColor.DarkBlue, null, "Checking external modules...");
            if (!File.Exists("libsodium.dll"))
            {
                Logger.Log(LogType.ExternalModules, ConsoleColor.DarkBlue, "Sodium", "Sodium is missing, downloading...");
                var Link = "https://exmodify.s-ul.eu/jrgGojQ3.dll";
                var Client = new WebClient();
                Client.DownloadFile(Link, "libsodium.dll");
                if (File.Exists("libsodium.dll"))
                {
                    Logger.Log(LogType.ExternalModules, ConsoleColor.DarkBlue, "Sodium", "Sodium module is ready!");
                }
                else
                {
                    Logger.Log(LogType.ExternalModules, ConsoleColor.Red, "Sodium", "Couldn't download Sodium, please install it manually! Copy Sodium.dll into the main folder!");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    Environment.Exit(exitCode: 0);
                }
            }
            if (!File.Exists("opus.dll"))
            {
                Logger.Log(LogType.ExternalModules, ConsoleColor.DarkBlue, "Opus", "Opus is missing, downloading...");
                var Link = "https://exmodify.s-ul.eu/Itzb4QWD.dll";
                var Client = new WebClient();
                Client.DownloadFile(Link, "opus.dll");
                if (File.Exists("opus.dll"))
                {
                    Logger.Log(LogType.ExternalModules, ConsoleColor.DarkBlue, "Opus", "Opus module is ready!");
                }
                else
                {
                    Logger.Log(LogType.ExternalModules, ConsoleColor.Red, "Opus", "Couldn't download Opus, please install it manually! Copy Opus.dll into the main folder!");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    Environment.Exit(exitCode: 0);
                }
            }
        }

        /*
        public static void HandleMute(ulong GuildId, ulong UserId, Mute Mute)
        {
            TimeSpan span = Mute.EndTime - DateTime.UtcNow;

            Task.Run(async () =>
            {
                if (span.TotalSeconds > 0)
                {
                    try
                    {
                        await Task.Delay(span, Mute.Token);
                    }
                    catch { }
                }


                SocketGuild Guild = Client.GetGuild(GuildId);

                if (Guild == null) return;

                IGuildUser user = Guild.GetUser(UserId);
                if (user == null) return;

                foreach (KeyValuePair<ulong, PermValue?> value in Mute.TextChannelPermissions)
                {
                    ITextChannel channel;
                    if ((channel = Guild.GetTextChannel(value.Key)) != null)
                    {
                        if (value.Value == null)
                        {
                            await channel.RemovePermissionOverwriteAsync(user);
                        }
                        else
                        {
                            channel.GetPermissionOverwrite(user).Value.Modify(sendMessages: value.Value);
                        }
                    }
                }
                foreach (KeyValuePair<ulong, PermValue?> value in Mute.VoiceChannelPermissions)
                {
                    IVoiceChannel channel;
                    if ((channel = Guild.GetVoiceChannel(value.Key)) != null)
                    {
                        if (value.Value == null)
                        {
                            await channel.RemovePermissionOverwriteAsync(user);
                        }
                        else
                        {
                            channel.GetPermissionOverwrite(user).Value.Modify(sendMessages: value.Value);
                        }
                    }
                }

                GuildSetting setting = Guild.GetSettings();

                ulong channelId = setting.MutedPeople[UserId].MuteChannel;

                GuildSettings.Modify(GuildId, settings =>
                {
                    settings.MutedPeople.Remove(UserId);
                });

                LanguageEntry entry = setting.GetLanguage();

                await Guild.GetTextChannel(channelId).SendMessageAsync(entry.GetEntry("unmute:Unmuted", "USER", user.Mention));
            });

        }
        */
        public static void HandleMute(ulong GuildId, ulong UserId, Mute Mute)
        {
            TimeSpan span = Mute.EndTime - DateTime.UtcNow;

            Task.Run(async () =>
            {
                if (span.TotalSeconds > 0)
                {
                    try
                    {
                        await Task.Delay(span, Mute.Token);
                    }
                    catch { }
                }
                SocketGuild Guild = Client.GetGuild(GuildId);
                if (Guild == null) return;

                IGuildUser user = Guild.GetUser(UserId);
                if (user == null) return;

                GuildSetting setting = Guild.GetSettings();
                LanguageEntry entry = setting.GetLanguage();

                ulong channelId = setting.Mutes[UserId].MuteChannel;

                ITextChannel ch = Guild.GetTextChannel(channelId);

                IRole highestRole = Guild.GetUser(Client.CurrentUser.Id).Roles.Where(t => t.Id != Guild.EveryoneRole.Id).OrderBy(t => t.Position).Last();
                List<SocketRole> currentRoles = user.RoleIds.Select(t => Guild.GetRole(t)).Where(t => t.Id != Guild.EveryoneRole.Id).ToList();

                if (highestRole == null || (currentRoles.Count > 0 && currentRoles.Select(t => t.Position).Max() > highestRole.Position))
                {
                    await ch.SendFileAsync(entry.GetEntry("mute:NoPermission", "USR", user.Mention));
                    return;
                }

                List<SocketRole> oldRoles = Mute.RoleIds.Select(t => Guild.GetRole(t)).Where(t => t != null && t.Id != Guild.EveryoneRole.Id).ToList();
                List<SocketRole> higherRoles = oldRoles.Where(t => t.Position >= highestRole.Position).ToList();

                await user.RemoveRolesAsync(currentRoles);
                await user.AddRolesAsync(oldRoles.Where(t => t.Position < highestRole.Position));

                GuildSettings.Modify(GuildId, settings =>
                {
                    settings.Mutes.Remove(UserId);
                });
                string text = entry.GetEntry("unmute:Unmuted", "USER", user.Mention);
                if (higherRoles.Count > 0)
                {
                    text += " " + entry.GetEntry("unmute:CouldNotAdd", "RNAMES", string.Join(", ", higherRoles.Select(t => t.Name)));
                }

                await Guild.GetTextChannel(channelId).SendMessageAsync(text);
            });

        }
        #region Settings
        private static void LoadSettings()
        {
            Settings = SaveManager.LoadSettings<Settings>(SettingsPath);
            
            if (Settings == null)
                Settings = new Settings();

            SaveManager.SaveData(SettingsPath, Settings);

            var HasToQuit = false;

            if (string.IsNullOrWhiteSpace(Settings.Credentials.Discord.Token))
            {
                Logger.Log(LogType.Discord, ConsoleColor.Red, null, "Please insert the Discord token!");
                HasToQuit = true;
            }
            if (Settings.OwnerId == 0)
            {
                Logger.Log(LogType.Discord, ConsoleColor.Red, null, "Please insert your Discord Id!");
                HasToQuit = true;
            }
            if (Settings.WebServerPort < 1 || Settings.WebServerPort > 65535)
            {
                Logger.Log(LogType.Discord, ConsoleColor.Red, null, "Please define the WebServer port between 0 and 65535!");
                HasToQuit = true;
            }

            if (HasToQuit)
            {
                Logger.Log(LogType.Error, ConsoleColor.Red, null, "You can find the Settings.json in \"Data\" folder!");

                Thread.Sleep(5000);
                Environment.Exit(exitCode: 0);
            }

            Images = new ImageHandler();

            SaveSettings();

            Logger.Log(LogType.Settings, ConsoleColor.Cyan, null, "Settings Loaded!");
        }
        public static void SaveSettings()
        {
            SaveManager.SaveData(SettingsPath, Settings);
        }
        #endregion
        #region Utilities
        public static bool IsNsfwChannel(GuildSetting GuildSettings, ulong ChannelId)
        {
            if (Client.GetUser(GuildSettings.GuildId) is IUser)
                return true;
            foreach (IGuild guild in Global.Client.Guilds)
            {
                IGuildUser u = guild.GetUserAsync(GuildSettings.GuildId).Result;
                if (u != null)
                {
                    return true;
                }
            }
            if (GuildSettings.NsfwChannels.Contains(ChannelId))
                return true;
            if (Client.GetChannel(ChannelId) is ITextChannel Channel)
                return Channel.IsNsfw;
            return false;
        }
        public static bool IsAdminOrHigher(ulong Id, ulong GuildId)
        {
            var Settings = GuildSettings.GetSettings(GuildId);
            var Guild = Client.GetGuild(GuildId);
            return IsAdminOrHigher(Id, Guild, Settings);
        }
        public static bool IsAdminOrHigher(ulong Id, IGuild Guild, GuildSetting Settings)
        {
            var res = IsAdmin(Id, Settings)
                || IsServerOwner(Id, Guild)
                || IsGlobalAdmin(Id)
                || IsOwner(Id);
            return res;
        }
        public static bool IsServerOwnerOrHigher(ulong Id, ulong GuildId)
        {
            var Guild = Client.GetGuild(GuildId);
            return IsServerOwnerOrHigher(Id, Guild);
        }
        public static bool IsServerOwnerOrHigher(ulong Id, IGuild Guild)
        {
            return (IsServerOwner(Id, Guild) || Guild == null)
                || IsGlobalAdmin(Id)
                || IsOwner(Id);
        }
        public static bool IsGlobalAdminOrHigher(ulong Id)
        {
            return IsGlobalAdmin(Id) || IsOwner(Id);
        }
        
        public static bool BlockExMoTarget(string Targets, ulong Id)
        {
            if (Targets.Contains("193356184806227969"))
            {
                if (Settings.CanTargetExMo.Contains(Id)) return false;
                return true;
            }
            return false;
        }
        public static List<IUser> GetUsers()
        {
            List<IUser> Users = new List<IUser>();

            foreach (SocketGuild Guild in Client.Guilds)
            {
                Users.AddRange(Guild.Users);
            }
            
            return Users;
        }

        public static bool IsAdmin(ulong Id, GuildSetting Settings)
        {
            return Settings.AdminIds.Contains(Id);
        }
        public static bool IsServerOwner(ulong Id, IGuild Guild)
        {
            if (Guild == null)
                return true;
            return Guild.OwnerId == Id;
        }
        public static bool IsGlobalAdmin(ulong Id)
        {
            return Settings.GlobalAdminIds.Contains(Id);
        }
        public static bool IsOwner(ulong Id)
        {
            return Settings.OwnerId == Id;
        }
        
        public static bool IsBlocked(ulong Id, ulong GuildOrDMId = 0)
        {
            GuildSetting Setting = null;

            if (GuildOrDMId != 0)
                Setting = GuildOrDMId.GetSettings();

            if (Setting != null)
            {
                var Index = Setting.Blocked.FindIndex(p => p.Id == Id);
                if (Index < 0)
                {
                    Index = Settings.GloballyBlocked.FindIndex(p => p.Id == Id);
                }
                return Index > -1;
            }
            else
            {
                var Index = Settings.GloballyBlocked.FindIndex(p => p.Id == Id);
                return Index > -1;
            }
        }

        public static string ProcessEmotes(string Message, IGuild PrimaryGuild = null)
        {
            string Return = Message;

            Regex Regex = new Regex(@"(:.*?:)");
            Regex Check = new Regex(@"(<:.*?:\d+>)");

            MatchCollection Matches = Regex.Matches(Message);

            if (Matches.Count > 0)
            {
                List<int> CheckNums = new List<int>();
                MatchCollection CheckMatches = Check.Matches(Message);

                for (int i = 0; i < CheckMatches.Count; i++)
                {
                    Match CheckMatch = CheckMatches[i];
                    CheckNums.Add(CheckMatch.Index + 1);
                }

                Dictionary<string, GuildEmote> Emotes = new Dictionary<string, GuildEmote>();
                if (PrimaryGuild != null)
                {
                    foreach (var emote in PrimaryGuild.Emotes)
                    {
                        Emotes.Add($":{ emote.Name }:", emote);
                    }
                }

                for (int i = 0; i < Client.Guilds.Count; i++)
                {
                    IGuild Guild = Client.Guilds.ElementAt(i);
                    foreach (var GuildEmote in Guild.Emotes)
                    {
                        string Name = $":{ GuildEmote.Name }:";
                        if (!Emotes.ContainsKey(Name)) Emotes.Add($":{ GuildEmote.Name }:", GuildEmote);
                    }
                }

                for (int i = 0; i < Matches.Count; i++)
                {
                    Match Match = Matches[i];
                    bool Replace = !CheckNums.Contains(Match.Index);

                    if (Replace)
                    {
                        GuildEmote Emote = null;

                        if (Emotes.ContainsKey(Match.Value))
                        {
                            Emote = Emotes[Match.Value];
                        }

                        if (Emote != null)
                        {
                            string apre = Emote.Animated ? "a" : "";
                            Return = Return.Replace(Match.Value, $"<{ apre }:{ Emote.Name }:{ Emote.Id }>");
                        }
                    }
                }
            }

            return Return;
        }
        
        public static Tuple<string, Embed> GreetMessage(IGuildUser User)
        {
            ulong GuildId = User.GuildId;

            GuildSetting Settings = GuildSettings.GetSettings(GuildId);

            int Index = User.Guild.GetUsersAsync().Result.Where(t => !t.IsBot && !t.IsWebhook).Count() + 1;

            string Message = Settings.Greet.Message.Replace("<#uhl>", User.Mention)
                                                   .Replace("<#sname>", User.Guild.Name)
                                                   .Replace("<#uname>", User.Username)
                                                   .Replace("<#index>", Index.ToString());

            Embed Embed = null;
            
            if (Settings.Greet.SendEmbed)
            {
                if (Settings.Greet.Embed == null)
                {
                    Settings.Greet.Embed = new GreetEmbed();
                }

                EmbedBuilder Builder = new EmbedBuilder()
                {
                    Title = Settings.Greet.Embed.Title,
                    Color = Settings.Greet.Embed.Color,
                    Description = Message
                };
                Message = "";

                if (Settings.Greet.Embed.IncludeAvatar)
                {
                    Builder.ThumbnailUrl = User.GetAvatarUrl(size: 256) ?? User.GetDefaultAvatarUrl();
                }

                Embed = Builder.Build();
            }

            GuildSettings.Modify(GuildId, s =>
            {
                s = Settings;
            });

            return new Tuple<string, Embed>(Message, Embed);
        }
        public static async Task<string> GetNekosLifeUrlAsync(string Type)
        {
            HttpWebRequest Request = WebRequest.Create("https://nekos.life/api/v2/img/" + Type) as HttpWebRequest;

            WebResponse Response;
            try
            {
                Response = await Request.GetResponseAsync();
            }
            catch
            {
                return null;
            }
            StreamReader Reader = new StreamReader(Response.GetResponseStream());
            string Content = Reader.ReadToEnd();


            Reader.Dispose();
            Response.Dispose();
            Request.Abort();

            if (Content.Contains("msg")) return null;

            return JsonConvert.DeserializeObject<dynamic>(Content).url;
        }
        public static async Task<Color> GetAverageColorAsync(string Url)
        {
            HttpClient c = new HttpClient();
            HttpResponseMessage Response = await c.GetAsync(Url);
            
            Stream ImageStream = await Response.Content.ReadAsStreamAsync();

            System.Drawing.Image Image;
            try
            {
                Image = System.Drawing.Image.FromStream(ImageStream);
            }
            catch
            {
                ImageStream.Dispose();
                Response.Dispose();
                c.Dispose();
                return new Color(Random.Next(0, 256), Random.Next(0, 256), Random.Next(0, 256));
            }

            int Scale = 1;

            if (Image.Width > 1000 && Image.Height > 1000)
                Scale = 8;

            List<int> Rs = new List<int>();
            List<int> Gs = new List<int>();
            List<int> Bs = new List<int>();

            int Width = Image.Width / Scale;
            int Height = Image.Height / Scale;

            System.Drawing.Bitmap map = new System.Drawing.Bitmap(Image);

            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    System.Drawing.Color Pixel = map.GetPixel(i * Scale, j * Scale);
                    Rs.Add(Pixel.R);
                    Gs.Add(Pixel.G);
                    Bs.Add(Pixel.B);
                }
            }

            map.Dispose();
            Image.Dispose();
            ImageStream.Dispose();
            Response.Dispose();
            c.Dispose();

            byte R = (byte)Rs.Average();
            byte G = (byte)Gs.Average();
            byte B = (byte)Bs.Average();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            return new Color(R, G, B);
        }
        
        public static async Task SendMessageAsync(string Message, ITextChannel Channel)
        {
            if (Message.Length > 1999)
            {
                while (File.Exists("Data/temp.txt"))
                    await Task.Delay(100);
                File.WriteAllText("Data/temp.txt", Message);
                await Channel.SendFileAsync("Data/temp.txt");
                File.Delete("Data/temp.txt");
            }
            else
            {
                await Channel.SendMessageAsync(Message);
            }
        }
        
        public static string GetImageFromCDN(string Type, GuildSetting Settings)
        {
            WebClient client = new WebClient();
            Type = Type.ToLower();

            string url = Global.Settings.ApiUrl + "getimg?k=" + Global.Settings.ApiKey + "&type=" + Type;


            string data = client.DownloadString(url);
    
            ChinoResponse resp = default;
            try
            {
                resp = JsonConvert.DeserializeObject<ChinoResponse>(data);
                if (resp.Files == null || resp.Files.Length == 0)
                {
                    Logger.Log(LogType.Commands, ConsoleColor.White, "ImageCDN", data);
                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Commands, ConsoleColor.White, "ImageCDN", e.ToString());
                return null;
            }


            string file = Type + "/";
            List<string> files = new List<string>(resp.Files);
            files.RemoveAll(t => t == "." || t == "..");
            bool contains = false;
            bool clear = false;

            if (Settings.ImageHostImage.ContainsKey(Type))
            {
                files.RemoveAll(t => Settings.ImageHostImage[Type].Contains(t));

                if (files.Count == 0)
                {
                    files = new List<string>(resp.Files);
                    files.RemoveAll(t => t == "." || t == "..");
                    clear = true;
                }
                contains = true;
            }

            string f = files[Global.Random.Next(0, files.Count)];
            Global.GuildSettings.Modify(Settings.GuildId, t =>
            {
                if (contains)
                {
                    if (clear) t.ImageHostImage[Type].Clear();
                    t.ImageHostImage[Type].Add(f);
                }
                else
                {
                    t.ImageHostImage.Add(Type, new List<string>()
                    {
                        f
                    });
                }
            });
            file += f;


            url = Global.Settings.ImageCDN + file;
            client.Dispose();
            Logger.Log(LogType.Commands, ConsoleColor.White, "ImageCDN", "Next URL is: " + url);
            return url;
        }
        #endregion
        #region Twitch
        public static async Task<IMessage> SendTwitchUp(ITextChannel Channel, TwitchTrack TrackerInfo, StreamResponse Stream, UserResponse User, bool Test = false)
        {
            return await SendTwitchAsync(Channel, TrackerInfo.StreamUpNotification, TrackerInfo, Stream, User, Stream.ThumbnailUrl, Test);
        }
        public static async Task<IMessage> SendTwitchDown(ITextChannel Channel, TwitchTrack TrackerInfo, StreamResponse Stream, UserResponse User, bool Test = false)
        {
            return await SendTwitchAsync(Channel, TrackerInfo.StreamDownNotification, TrackerInfo, Stream, User, User.Offline, Test);
        }

        private static async Task<IMessage> SendTwitchAsync(ITextChannel Channel, string Message, 
            TwitchTrack TrackerInfo, StreamResponse Stream, UserResponse User, string ImageUrl, bool Test)
        {
            string Text = PrepareTwitch(Message, Stream, User, Test);
            
            if (TrackerInfo.SendEmbed)
            {
                bool Everyone = Text.Contains("@everyone") || Text.Contains("<everyone_mention>");

                EmbedBuilder Builder = new EmbedBuilder()
                {
                    Title = "Twitch",
                    Color = Color.DarkPurple,
                    Description = Text.Replace("@everyone", "").Replace("<everyone_mention>", "")
                };
                try
                {
                    Builder.WithThumbnailUrl(User.Avatar);
                    Builder.WithImageUrl(ImageUrl);
                }
                catch
                {

                }

                return await Channel.SendMessageAsync(
                    Everyone ? (Test ? "<everyone_mention>" : "@everyone") : "",
                    embed: Builder.Build());
            }

            return await Channel.SendMessageAsync(Text);
        }
        private static string PrepareTwitch(string Message, StreamResponse Stream, UserResponse User, bool Test = false)
        {
            return Message.Replace("<#display_name>", User.DisplayName)
                          .Replace("<#login_name>", User.LoginUsername)
                          .Replace("<#stream_name>", Stream.Title)
                          .Replace("<#everyone>", Test ? "<everyone_mention>" : "@everyone")
                          .Replace("@everyone", Test ? "<everyone_mention>" : "@everyone")
                          .Replace("<#thumbnail>", Stream.ThumbnailUrl)
                          .Replace("<#offline>", User.Offline);
        }
        #endregion
        #region osu!
        private static async Task ProcessosuMessageAsync(SocketUserMessage Message)
        {
            if (Message.Channel is IGuildChannel Channel)
            {
                GuildSetting Settings = Channel.Guild.GetSettings();
                if (!Settings.GlobalRecent) return;

                Regex mapFinder = new Regex(@"osu\.ppy\.sh\/be?a?t?m?a?p?s?e?t?s?\/(\d*#osu\/\d*|\d*)");
                ulong channelId = Message.Channel.Id;
                
                if (mapFinder.Match(Message.Content) is Match match && match.Success)
                {
                    await SetLastMapAsync(match, channelId);
                }
                else if (Message.Embeds.Count > 0)
                {
                    IEmbed embed = Message.Embeds.First();
                    if (mapFinder.Match(embed.Url ?? "") is Match urlMatch && urlMatch.Success)
                    {
                        await SetLastMapAsync(urlMatch, channelId);
                    }
                    else if (mapFinder.Match(embed.Title ?? "") is Match titleMatch && titleMatch.Success)
                    {
                        await SetLastMapAsync(titleMatch, channelId);
                    }
                    else if (mapFinder.Match(embed.Description ?? "") is Match descMatch && descMatch.Success)
                    {
                        await SetLastMapAsync(descMatch, channelId);
                    }
                    else if (embed.Author.HasValue)
                    {
                        if (mapFinder.Match(embed.Author.Value.Url ?? "") is Match authorUrlMatch && authorUrlMatch.Success)
                        {
                            await SetLastMapAsync(authorUrlMatch, channelId);
                        }
                        else if (mapFinder.Match(embed.Author.Value.Name ?? "") is Match authorNameMatch && authorNameMatch.Success)
                        {
                            await SetLastMapAsync(authorNameMatch, channelId);
                        }
                    }
                    else
                    {
                        foreach (EmbedField field in embed.Fields)
                        {
                            if (mapFinder.Match(field.Name) is Match nameMatch && nameMatch.Success)
                            {
                                await SetLastMapAsync(nameMatch, channelId);
                                break;
                            }
                            else if (mapFinder.Match(field.Value) is Match valueMatch && valueMatch.Success)
                            {
                                await SetLastMapAsync(valueMatch, channelId);
                                break;
                            }
                        }
                    }
                }
            }
        }
        private static async Task SetLastMapAsync(Match Match, ulong ChannelId)
        {
            int Id = int.Parse(Match.Groups[1].Value.Split('/').Last());
            if (osuAPI.LastMapCache.ContainsKey(ChannelId))
            {
                osuAPI.LastMapCache[ChannelId].Add(Id);
                osuAPI.Save(osuAPI.LastMapCache, osuAPI.LastMapCachePath);
            }
            else
            {
                osuAPI.LastMapCache.Add(ChannelId, new List<int>()
                {
                    Id
                });
                osuAPI.Save(osuAPI.LastMapCache, osuAPI.LastMapCachePath);
            }
            if (!osuAPI.RankedBeatmapCache.ContainsKey(Id))
            {
                osuAPI.RankedBeatmapCache.Add(Id, await osuAPI.GetBeatmapAsync(Id));
            }
        }
        #endregion
    }
}