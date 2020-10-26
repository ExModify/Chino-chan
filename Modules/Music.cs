using System.Reflection;
using System.Runtime.InteropServices;
using Chino_chan.Models.Music;
using Chino_chan.Models.Settings.Language;
using Chino_chan.Models.SoundCloud;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using System.Diagnostics;

namespace Chino_chan.Modules
{
    public enum PlayerState
    {
        Disconnected    = 0,
        Connected       = 1,
        Playing         = 2,
        Paused          = 3
    }
    public enum RepeatMode
    {
        NoRepeat            = 0,
        RepeatPlaylist_Next = 1,
        RepeatPlaylist_Prev = 2,
        RepeatOne           = 3,
        StopAfterSongFinish = 4
    }
    public enum PlayerRequest
    {
        Idle = 0,
        Stop = 1,
        Next = 2
    }

    public class MusicPlayer
    {
        #region Variables
        public event Action PropertyChanged;
        [JsonIgnore]
        public Color Color { get; } = Color.Magenta;
        [JsonIgnore]
        public IAudioClient Client { get; private set; }
        [JsonIgnore]
        public AudioOutStream PCMStream { get; private set; }

        
        /*
        [JsonIgnore]
        public WaveStream Reader { get; private set; }
        */
        
        [JsonIgnore]
        public FFmpegReader Reader { get; private set; }



        private List<MusicItem> _Queue = new List<MusicItem>();
        public List<MusicItem> Queue
        {
            get
            {
                return _Queue;
            }
            set
            {
                _Queue = Queue;
                PropertyChanged?.Invoke();
            }
        }
        private int _Volume = 100;
        public int Volume
        {
            get
            {
                return _Volume;
            }
            set
            {
                _Volume = value;
                PropertyChanged?.Invoke();
            }
        }

        public ulong GuildId { get; set; }

        [JsonIgnore]
        public TimeSpan CurrentTime
        {
            get
            {
                if (Reader == null) return TimeSpan.Zero;
                else return Reader.CurrentTime;
            }
        }

        public TimeSpan BackupTime { get; set; }

        [JsonIgnore]
        public DateTime ListenMoeStartTime { get; set; }

        [JsonIgnore]
        public TimeSpan ListenMoeCurrentTime
        {
            get
            {
                return DateTime.UtcNow - ListenMoeStartTime;
            }
        }

        [JsonIgnore]
        public TimeSpan TotalTime
        {
            get
            {
                if (Reader == null) return TimeSpan.Zero;
                else return Reader.TotalTime;
            }
        }
        
        private PlayerState _State = PlayerState.Disconnected;
        public PlayerState State
        {
            get
            {
                return _State;
            }
            set
            {
                _State = value;
                PropertyChanged?.Invoke();
            }
        }

        private RepeatMode _Repeat = RepeatMode.NoRepeat;
        public RepeatMode Repeat
        {
            get
            {
                return _Repeat;
            }
            set
            {
                _Repeat = value;
                PropertyChanged?.Invoke();
            }
        }

        private MusicItem _Current;
        public MusicItem Current
        {
            get
            {
                return _Current;
            }
            private set
            {
                _Current = value;
                PropertyChanged?.Invoke();
            }
        }

        private PlayerRequest _Request;
        public PlayerRequest Request
        {
            get
            {
                return _Request;
            }
            set
            {
                _Request = value;
                PropertyChanged?.Invoke();
            }
        }

        private ulong _TextChannelId = 0;
        private ulong _VoiceChannelId = 0;
        public ulong TextChannelId
        {
            get
            {
                return _TextChannelId;
            }
            set
            {
                _TextChannelId = value;
                PropertyChanged?.Invoke();
            }
        }
        public ulong VoiceChannelId
        {
            get
            {
                return _VoiceChannelId;
            }
            set
            {
                _VoiceChannelId = value;
                PropertyChanged?.Invoke();
            }
        }

        private int _QueueIndex = 0;
        public int QueueIndex
        {
            get
            {
                return _QueueIndex;
            }
            set
            {
                _QueueIndex = value;
                PropertyChanged?.Invoke();
            }
        }
        #endregion

        private bool DisconnectInvoked { get; set; } = false;
        
        public MusicPlayer(ulong GuildId)
        {
            this.GuildId = GuildId;
        }
        public MusicPlayer() { } // Json
        
        public async Task<bool> ConnectAsync(ICommandContext Context = null, IVoiceChannel Channel = null)
        {
            if (Context == null)
            {
                if (VoiceChannelId == 0)
                {
                    return false;
                }
                else
                {
                    if (Global.Client.GetChannel(VoiceChannelId) is IVoiceChannel channel)
                    {
                        try
                        {
                            Client = await channel.ConnectAsync();
                            State = PlayerState.Connected;
                            return true;
                        }
                        catch { }
                    }
                    return false;
                }
            }
            await DisconnectAsync();

            LanguageEntry Language = Context.GetSettings().GetLanguage();

            if (Channel != null)
            {
                try
                {
                    Client = await Channel.ConnectAsync();
                    VoiceChannelId = Channel.Id;
                    await Context.Channel.SendMessageAsync(Language.GetEntry("MusicHandler:ConnectedTo", "NAME", Channel.Name));
                }
                catch (Exception e)
                {
                    string msg = $"```${ e }```";
                    if (Global.Client.GetChannel(Global.Settings.DevServer.ErrorReportChannelId) is ITextChannel errChannel)
                    {
                        await Global.SendMessageAsync(msg, errChannel);
                    }
                    await Context.Channel.SendMessageAsync(Language.GetEntry("MusicHandler:CannotConnectToChannel"));
                    return false;
                }
            }
            else if ((Context.User as IGuildUser).VoiceChannel is IVoiceChannel UserChannel)
            {
                try
                {
                    Client = await UserChannel.ConnectAsync();
                    VoiceChannelId = UserChannel.Id;
                    await Context.Channel.SendMessageAsync(Language.GetEntry("MusicHandler:ConnectedTo", "NAME", UserChannel.Name));
                }
                catch (Exception e)
                {
                    string msg = $"```${ e }```";
                    if (Global.Client.GetChannel(Global.Settings.DevServer.ErrorReportChannelId) is ITextChannel errChannel)
                    {
                        await Global.SendMessageAsync(msg, errChannel);
                    }
                    await Context.Channel.SendMessageAsync(Language.GetEntry("MusicHandler:CannotConnectToUser"));
                    return false;
                }
            }
            else
            {
                IReadOnlyCollection<IVoiceChannel> Channels = await Context.Guild.GetVoiceChannelsAsync();
                IEnumerator<IVoiceChannel> enumerator = Channels.GetEnumerator();
                bool Connected = false;

                for (int i = 0; i < Channels.Count; i++)
                {
                    enumerator.MoveNext();

                    try
                    {
                        Client = await enumerator.Current.ConnectAsync();
                        await Context.Channel.SendMessageAsync(Language.GetEntry("MusicHandler:ConnectedTo", "NAME", enumerator.Current.Name));
                        VoiceChannelId = enumerator.Current.Id;
                        Connected = true;
                        break;
                    }
                    catch { } // has no permission to connect
                }
                if (!Connected)
                {
                    await Context.Channel.SendMessageAsync(Language.GetEntry("MusicHandler:CannotConnectToAny"));
                }
            }

            State = PlayerState.Connected;

            // Assumption: only triggered when exception
            Client.Disconnected += (e) =>
            {
                if (!DisconnectInvoked)
                {
                    SetDisconnected();
                }
                return Task.CompletedTask;
            };
            return true;
        }
        
        public async Task DisconnectAsync(ICommandContext Context = null)
        {
            DisconnectInvoked = true;
            if (State > PlayerState.Disconnected)
            {
                await StopAsync();
                try
                {
                    await Client.StopAsync();
                }
                catch (Exception e)
                {
                    Logger.Log(LogType.Music, ConsoleColor.Red, "Exception", e.ToString());
                }

                SetDisconnected();

                if (Context != null)
                {
                    await Context.Channel.SendMessageAsync(Context.GetSettings().GetLanguage().GetEntry("MusicHandler:Disconnected"));
                }
            }
            else if (Context != null)
            {
                await Context.Channel.SendMessageAsync(Context.GetSettings().GetLanguage().GetEntry("MusicHandler:NotConnected"));
            }
            DisconnectInvoked = false;
        }

        public async Task RestoreConnectionAsync(ICommandContext context = null)
        {
            PlayerState originalState = State;
            if (State > PlayerState.Disconnected && (Client == null || Client.ConnectionState < ConnectionState.Connecting))
            {
                if (Global.Client.GetChannel(VoiceChannelId) is IVoiceChannel channel)
                {
                    try
                    {
                        #region Reconnecting
                        try
                        {
                            Client = await channel.ConnectAsync();
                            State = PlayerState.Connected;
                        }
                        catch
                        {
                            VoiceChannelId = 0;
                            TextChannelId = 0;
                            State = PlayerState.Disconnected;

                            return;
                        }

                        try
                        {
                            DisconnectInvoked = true;
                            await Client.StopAsync();
                            await Task.Delay(1000);
                        }
                        catch { }
                        
                        Client = await channel.ConnectAsync();
                        DisconnectInvoked = false;
                        #endregion

                        if (originalState > PlayerState.Connected)
                        {
                            if (Global.Client.GetChannel(TextChannelId) is ITextChannel tc)
                            {
                                new Task(async () =>
                                {
                                    await PlayAsync(context, tc, true, BackupTime);
                                }).Start();

                                if (originalState == PlayerState.Paused)
                                {
                                    while (State != PlayerState.Playing)
                                        await Task.Delay(100);

                                    State = PlayerState.Paused;
                                }
                            }
                            else
                            {
                                VoiceChannelId = 0;
                                TextChannelId = 0;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Log(LogType.Music, ConsoleColor.Red, "Error", $"Couldn't reconnect client to { GuildId }!\n- { e.Message }\n- { e.StackTrace }\n- { e.Source }");
                    }
                }
            }
        }
        
        public async Task StopAsync(ICommandContext Context = null)
        {
            if (Request == PlayerRequest.Stop) return;

            if (State > PlayerState.Connected)
            {
                Request = PlayerRequest.Stop;

                while (Request == PlayerRequest.Stop)
                {
                    await Task.Delay(100);
                }
                
                if (Context != null)
                {
                    await Context.Channel.SendMessageAsync(Context.GetSettings().GetLanguage().GetEntry("MusicHandler:Stopped"));
                }
            }
            else if (Context != null)
            {
                await Context.Channel.SendMessageAsync(Context.GetSettings().GetLanguage().GetEntry("MusicHandler:NotPlaying"));
            }
        }
        
        public async Task<List<Video>> SearchYouTubeVideoAsync(string Keywords)
        {
            YoutubeClient client = new YoutubeClient();
            List<Video> Result = new List<Video>(await client.Search.GetVideosAsync(Keywords).BufferAsync(10));
            return Result;
        }
        public List<Track> SearchSoundCloudTrack(string Keywords)
        {
            if (Global.SoundCloud == null)
                return null;

            return Global.SoundCloud.SearchSongs(Keywords);
        }
        public List<Models.SoundCloud.Playlist> SearchSoundCloudPlaylist(string Keywords)
        {
            if (Global.SoundCloud == null)
                return null;

            return Global.SoundCloud.SearchPlaylist(Keywords);
        }
        
        public bool Seek(TimeSpan span)
        {
            if (TotalTime > span && span >= TimeSpan.Zero)
            {
                if (Reader != null)
                {
                    Reader.SetTime(span);
                    return true;
                }
            }

            return false;
        }
        
        public void Enqueue(Track Track)
        {
            Queue.Add(new MusicItem(Track));
            PropertyChanged?.Invoke();
        }
        public void Enqueue(Video Video)
        {
            Queue.Add(new MusicItem(Video));
            PropertyChanged?.Invoke();
        }
        public void Enqueue(Models.SoundCloud.Playlist Playlist)
        {
            Queue.AddRange(Global.SoundCloud.GetTracks(Playlist).Select(t => new MusicItem(t)));
            PropertyChanged?.Invoke();
        }
        public void Enqueue(YoutubeExplode.Playlists.Playlist Playlist)
        {
            YoutubeClient c = new YoutubeClient();
            Queue.AddRange(c.Playlists.GetVideosAsync(Playlist.Id).ToListAsync().Result.Select(t => new MusicItem(t)));
            PropertyChanged?.Invoke();
        }
        public void EnqueueLISTENmoe(bool IsKpop)
        {
            MusicItem item = new MusicItem()
            {
                PublicUrl = IsKpop ? "https://listen.moe/kpop/fallback" : "https://listen.moe/fallback",
                IsListenMoe = true
            };
            Queue.Add(item);
        }

        public bool ShufflePlaylist()
        {
            if (Queue.Count < 3)
            {
                return false;
            }
            var queue = Queue.Skip(1).OrderBy(t => Global.Random.Next(Queue.Count)).ToList();
            Queue.RemoveRange(1, Queue.Count - 1);
            Queue.AddRange(queue);
            PropertyChanged?.Invoke();
            return true;
        }
        public bool Dequeue(int Index)
        {
            Index--;
            if (Index < 0 || Index >= Queue.Count) return false;

            Queue.RemoveAt(Index);
            PropertyChanged?.Invoke();
            return true;
        }

        public void ClearQueue()
        {
            Queue.Clear();
            PropertyChanged?.Invoke();
        }
        
        public async Task PlayAsync(ICommandContext Context, ITextChannel Channel = null, bool KeepCurrent = false, TimeSpan? SeekTo = null, bool AvoidState = false)
        {
            if (Channel == null)
                Channel = Context.Channel as ITextChannel;

            if (Context.User.Id != 193356184806227969)
            {
                await Channel.SendMessageAsync("Sorry, but the music player is broken, and needs to be fixed! I'm working on it. - ExModify");
                return;
            }

            string lId = Channel.GetSettings().Language;

            LanguageEntry Language = Global.Languages.GetLanguage(lId);

            if (!AvoidState)
            {
                if (State == PlayerState.Playing || State == PlayerState.Paused)
                {
                    await Channel.SendMessageAsync(Language.GetEntry("MusicHandler:AlreadyPlaying"));
                    return;
                }
            }
            if (State == PlayerState.Disconnected)
            {
                bool Success = await ConnectAsync(Context);
                if (!Success) return;
            }
            if (!KeepCurrent || Current == null)
                Current = Queue[QueueIndex];

            string Url = Current.UrlOrId;

            WebSocket ws = null;

            if (Current.IsYouTube)
            {
                YoutubeClient Client = new YoutubeClient();
                StreamManifest sm = await Client.Videos.Streams.GetManifestAsync(Current.UrlOrId);
                IEnumerable<IAudioStreamInfo> streamInfos = sm.GetAudio();
                
                if (streamInfos.Count() > 0)
                {
                    IStreamInfo info = streamInfos.WithHighestBitrate();
                    Url = info.Url;
                }

                if (Url == Current.UrlOrId)
                {
                    Url = null;
                }
            }
            else
            {
                if (Current.IsListenMoe)
                {
                    Url = Current.PublicUrl;
                    ws = new WebSocket(Current.PublicUrl.Contains("kpop") ? "wss://listen.moe/kpop/gateway_v2" : "wss://listen.moe/gateway_v2");

                    Timer t = new Timer((state) =>
                    {
                        try
                        {
                            ws.Send("{ \"op\": 9 }");
                        }
                        catch
                        {
                            Logger.Log(LogType.WebSocket, ConsoleColor.Red, "Error", "Couldn't send heartbeat to LISTEN.moe!");
                        }
                    }, null, -1, -1);

                    ws.OnOpen += (s, e) =>
                    {
                        ws.Send("{ \"op\": 0, \"d\": { \"auth\": \"\" } }"); // { op: 0, d: { auth: "" } }
                    };
                    ws.OnMessage += (s, e) =>
                    {
                        ListenMoe parsed = JsonConvert.DeserializeObject<ListenMoe>(e.Data);
                        if (parsed.OpCode == 0)
                        {
                            t.Change(0, parsed.Data.HeartBeat);
                        }
                        else if (parsed.OpCode == 1 && parsed.Type == "TRACK_UPDATE")
                        {
                            ListenMoeStartTime = parsed.Data.StartTime;
                            Current.Author = string.Join(" && ", parsed.Data.Song.Artists.Select(i => i.NameRomaji ?? i.Name));
                            Current.Duration = TimeSpan.FromSeconds(parsed.Data.Song.Duration);
                            Current.Title = parsed.Data.Song.Title;
                            Current.Thumbnail = null;
                            foreach (ListenMoeAlbum album in parsed.Data.Song.Albums)
                            {
                                if (album.Image != null)
                                {
                                    Current.Thumbnail = album.Image;
                                    break;
                                }
                            }
                            if (State > PlayerState.Connected && State != PlayerState.Paused)
                                SendNowPlayingAsync(Context, Channel).GetAwaiter().GetResult();
                        }
                    };
                    ws.OnClose += (s, e) =>
                    {
                        t.Change(Timeout.Infinite, Timeout.Infinite);
                        t.Dispose();
                    };

                    ws.Connect();
                }
                else if (Global.SoundCloud == null)
                {
                    if (!IterateIndex(true))
                    {
                        Current = null;
                        await Channel.SendMessageAsync(Language.GetEntry("MusicHandler:SoundCloudNotAvailable"));
                    }
                    else
                    {
                        await Channel.SendMessageAsync(Language.GetEntry("MusicHandler:OnlySoundCloudTracks"));
                        new Task(async () => await PlayAsync(Context, Channel, AvoidState: true));
                    }
                    return;
                }
                else
                {
                    Url += "?client_id=" + Global.SoundCloud.ClientId;
                }
            }

            if (Url == null)
            {
                if (!IterateIndex())
                {
                    Current = null;
                    await Channel.SendMessageAsync(Language.GetEntry("MusicHandler:FinishedPlaying"));
                    State = PlayerState.Connected;
                }
                else
                {
                    new Task(async () => await PlayAsync(Context, Channel, AvoidState: true)).Start();
                    return;
                }
            }


            try
            {
                Reader = new FFmpegReader(Url);
                /*
                switch (encoding.Name)
                {
                    case "webm":
                    case "3gpp":
                        Stream s = await new HttpClient().GetStreamAsync(Url);
                        Reader = new NAudio.Vorbis.VorbisWaveReader(s);
                        break;
                        
                    default:
                        Reader = new MediaFoundationReader(Url);
                        break;
                }
                */
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e);
                await Channel.SendMessageAsync(Language.GetEntry("MusicHandler:SongInaccessible", "SONGNAME", Current.Title));
                if (!IterateIndex(Global.SoundCloud == null))
                {
                    Current = null;
                    await Channel.SendMessageAsync(Language.GetEntry("MusicHandler:FinishedPlaying"));
                    State = PlayerState.Connected;
                }
                else
                {
                    new Task(async () => await PlayAsync(Context, Channel, AvoidState: true)).Start();
                }
                return;
            }

            System.Console.WriteLine("got reader");

            if (PCMStream == null || !PCMStream.CanWrite)
                PCMStream = Client.CreatePCMStream(AudioApplication.Music, 128 * 1024, 200, 0);
            System.Console.WriteLine("made pcm stream");

            //WaveFormat OutFormat = new WaveFormat(48000, 16, 2);

            
            /*
            MediaFoundationResampler Resampler = new MediaFoundationResampler(Reader, OutFormat)
            {
                ResamplerQuality = 60
            };
            */

            //if (SeekTo.HasValue && !Current.IsListenMoe) Reader.CurrentTime = SeekTo.Value;
            if (SeekTo.HasValue && SeekTo != TimeSpan.Zero && !Current.IsListenMoe) Reader.ReadUntil(SeekTo.Value);
            BackupTime = TimeSpan.Zero;

            //int Size = OutFormat.AverageBytesPerSecond / 50;
            int Size = Reader.BufferSize(5);
            byte[] Buffer = new byte[Size];
            int Count = 0;
            
            State = PlayerState.Playing;
            TextChannelId = Channel.Id;
            if (!Current.IsListenMoe) await Context.Channel.SendMessageAsync("unga bunga");
                //await SendNowPlayingAsync(Context, Channel);
            /*while (Reader.CanRead && (Count = Resampler.Read(Buffer, 0, Size)) > 0 
                && Request == PlayerRequest.Idle && State > PlayerState.Connected)*/
            while (Reader.CanRead && (Count = Reader.Read(Buffer, 0, Size)) > 0 
                && Request == PlayerRequest.Idle && State > PlayerState.Connected)
            {
                if (State == PlayerState.Paused)
                {
                    await Channel.SendMessageAsync("", embed: new EmbedBuilder()
                    {
                        Title = Language.GetEntry("MusicHandler:Paused"),
                        Color = Color
                    }.Build());


                    while (State == PlayerState.Paused && Request == PlayerRequest.Idle)
                    {
                        Thread.Sleep(100);
                    }

                    string cId = Channel.GetSettings().Language;
                    if (cId != lId)
                        Language = Global.Languages.GetLanguage(cId);
                    lId = cId;
                    
                    if (State == PlayerState.Playing)
                    {
                        await Channel.SendMessageAsync("", embed: new EmbedBuilder()
                        {
                            Title = Language.GetEntry("MusicHandler:Resumed"),
                            Color = Color
                        }.Build());
                        if (Current.IsListenMoe)
                        {
                            await SendNowPlayingAsync(Context, Channel);
                        }
                    }
                }
                if (Request > 0)
                {
                    break;
                }
                
                if (CurrentTime.TotalSeconds % 10 == 0 && BackupTime.TotalSeconds != CurrentTime.TotalSeconds)
                {
                    PropertyChanged?.Invoke();
                    BackupTime = CurrentTime;
                }

                try
                {
                    if (State < PlayerState.Playing)
                        break;
                    ChangeVolume(Buffer, Volume / 100f);
                    await PCMStream.WriteAsync(Buffer, 0, Count);
                }
                catch (Exception e)
                {
                    if (State == PlayerState.Disconnected || Client.ConnectionState == ConnectionState.Disconnected || Client.ConnectionState == ConnectionState.Disconnecting) break;

                    string cId = Channel.GetSettings().Language;
                    if (cId != lId)
                        Language = Global.Languages.GetLanguage(cId);
                    lId = cId;

                    await Channel.SendMessageAsync(Language.GetEntry("MusicHandler:PlaybackErrorOccured"));
                    Request = PlayerRequest.Stop;
                    Logger.Log(LogType.Music, ConsoleColor.Red, "Error", e.ToString());
                    if (Global.Settings.DevServer.ErrorReportChannelId != 0)
                    {
                        ITextChannel Report = Global.Client.GetChannel(Global.Settings.DevServer.ErrorReportChannelId) as ITextChannel;
                        await Report.SendMessageAsync($"An error occured while playing on a server!\n```\n{ e }\n```\n" +
                            $"Client:\n { JsonConvert.SerializeObject(this) }");
                    }
                    break;
                }
            }
            if (ws != null && ws.IsAlive)
                ws.Close();
            
            if (Request == PlayerRequest.Idle && State > PlayerState.Connected)
                Request = PlayerRequest.Next;

            //Resampler.Dispose();
            Reader.Dispose();
            Reader = null;
            //Resampler = null;
            try { await PCMStream.FlushAsync(); } catch { } // It may be disposed
            
            if (Request == PlayerRequest.Next)
            {
                Request = PlayerRequest.Idle;
                if (!IterateIndex(Global.SoundCloud == null))
                {
                    if (State != PlayerState.Connected && State != PlayerState.Disconnected)
                        State = PlayerState.Connected;
                    Current = null;

                    string cId = Channel.GetSettings().Language;
                    if (cId != lId)
                        Language = Global.Languages.GetLanguage(cId);
                    lId = cId;

                    await Channel.SendMessageAsync(Language.GetEntry("MusicHandler:FinishedPlaying"));
                }
            }
            else if (Request == PlayerRequest.Stop)
            {
                Request = PlayerRequest.Idle;
                State = PlayerState.Connected;
                Current = null;
            }

            if (State > PlayerState.Connected)
            {
                new Task(async () => await PlayAsync(Context, Channel, AvoidState: true)).Start();
            }
        }
        
        public async Task ListAsync(ICommandContext Context = null, ITextChannel Channel = null)
        {
            if (Channel == null)
                Channel = Context.Channel as ITextChannel;

            LanguageEntry Language = Channel.GetSettings().GetLanguage();

            var Builder = new EmbedBuilder()
            {
                Color = Color,
                Title = Language.GetEntry("MusicHandler:Queue"),
            };
            if (Queue.Count == 0)
            {
                Builder.Description = Language.GetEntry("MusicHandler:EmptyQueue");
            }
            else
            {
                int Count = Math.Min(5, Queue.Count);
                for (int i = 0; i < Count; i++)
                {
                    var CurrentItem = Queue[i];
                    string fval = $"[URL]({ CurrentItem.PublicUrl })\n" +
                                    $"{ Language.GetEntry("MusicHandler:UploadedBy") }: { CurrentItem.Author }\n" +
                                    $"{ Language.GetEntry("MusicHandler:Length") }: " + CurrentItem.Duration.ToString(@"hh\:mm\:ss");

                    Builder.AddField(new EmbedFieldBuilder()
                    {
                        IsInline = false,
                        Name = "#" + (i + 1) + ": " + (CurrentItem.IsListenMoe ? "LISTEN.moe" : (CurrentItem.Title + " (" + (CurrentItem.IsYouTube ? "YouTube" : "SoundCloud") + ")")),
                        Value = CurrentItem.IsListenMoe ? "[LISTEN.moe](https://listen.moe/)" : fval
                    });
                    if (i != Count - 1)
                    {
                        Builder.Fields[Builder.Fields.Count - 1].Value += "\n-----------";
                    }
                }

                if (Queue.Count > 5)
                {
                    Builder.WithFooter(Language.GetEntry("MusicHandler:MoreItems", "X", "" + (Queue.Count - 5)));
                }
            }
            await Channel.SendMessageAsync("", embed: Builder.Build());
        }
        public async Task SendNowPlayingAsync(ICommandContext Context = null, ITextChannel Channel = null)
        {
            if (Channel == null)
                Channel = Context.Channel as ITextChannel;
            
            LanguageEntry Language = Channel.GetSettings().GetLanguage();

            Embed Final;

            if (Current == null)
            {
                Final = CreateMusicEmbed(Language.GetEntry("MusicHandler:NotPlaying"), Language);
            }
            else
            {
                Final = CreateMusicEmbed(Language.GetEntry("MusicHandler:NowPlaying"), Language, Current.Title,
                        Current.Duration, Current.IsListenMoe ? Current.PublicUrl.Replace("fallback", "") : Current.PublicUrl, Current.Author, Current.IsListenMoe ? ListenMoeCurrentTime : CurrentTime, Current.Thumbnail);

            }

            await Channel.SendMessageAsync("", embed: Final);
        }

        // ---------------
        // Private functions
        // ---------------
        
        private bool IterateIndex(bool OnlyYouTube = false)
        {
            int Index = QueueIndex;

            RepeatMode mode = Repeat;

            if (OnlyYouTube && mode != RepeatMode.RepeatPlaylist_Prev && mode != RepeatMode.RepeatPlaylist_Next)
                mode = RepeatMode.RepeatPlaylist_Next;
            
            switch (mode)
            {
                case RepeatMode.StopAfterSongFinish: return false;
                case RepeatMode.RepeatOne: return true;
                case RepeatMode.RepeatPlaylist_Next:
                    bool Broke = false;
                    for (int i = 0; i < Queue.Count; i++)
                    {
                        if (Index == Queue.Count - 1) Index = 0;
                        else Index++;

                        if (OnlyYouTube && !Queue[Index].IsYouTube) continue;
                        else
                        {
                            Broke = true;
                            break;
                        }
                    }
                    if (Broke)
                    {
                        QueueIndex = Index;
                        return true;
                    }
                    else return false;
                case RepeatMode.RepeatPlaylist_Prev:
                    bool PrevBroke = false;
                    for (int i = 0; i < Queue.Count; i++)
                    {
                        if (Index == 0) Index = Queue.Count - 1;
                        else Index--;

                        if (OnlyYouTube && !Queue[Index].IsYouTube) continue;
                        else
                        {
                            PrevBroke = true;
                            break;
                        }
                    }
                    if (PrevBroke)
                    {
                        QueueIndex = Index;
                        return true;
                    }
                    else return false;
                default:
                    if (OnlyYouTube) goto case RepeatMode.RepeatPlaylist_Next;

                    if (QueueIndex > -1 && QueueIndex < Queue.Count)
                        Queue.RemoveAt(QueueIndex);
                    if (QueueIndex >= Queue.Count) QueueIndex = 0;
                    PropertyChanged?.Invoke();

                    if (Queue.Count == 0) return false;

                    return true;
            }
        }
        private void ChangeVolume(byte[] AudioSamples, float Volume)
        {
            if (Math.Abs(Volume - 1f) < 0.0001f) return;

            int VolumeFixed = (int)Math.Round(Volume * 65536d);

            for (var i = 0; i < AudioSamples.Length; i += 2)
            {
                int Sample = (short)((AudioSamples[i + 1] << 8) | AudioSamples[i]);
                int Processed = (Sample * VolumeFixed) >> 16;

                AudioSamples[i] = (byte)Processed;
                AudioSamples[i + 1] = (byte)(Processed >> 8);
            }
        }
        private Embed CreateMusicEmbed(string EmbedTitle, LanguageEntry Language, string SongTitle = null, TimeSpan? Length = null,
            string Url = null, string UploadedBy = null, TimeSpan? Current = null, string ThumbnailUrl = null)
        {
            EmbedBuilder Builder = new EmbedBuilder()
            {
                Title = EmbedTitle,
                Color = Color
            };

            string Description = "";

            if (SongTitle != null)
            {
                if (!string.IsNullOrWhiteSpace(Url))
                {
                    Description += $"[{ SongTitle }]({ Url })";
                }
                else
                {
                    Description += SongTitle;
                }
            }
            if (Length.HasValue && !Current.HasValue)
            {
                Description += $"\n{ Language.GetEntry("MusicHandler:Length") }: " + Length.Value.ToString(@"hh\:mm\:ss");
            }
            if (!string.IsNullOrWhiteSpace(UploadedBy))
            {
                Description += $"\n{ Language.GetEntry("MusicHandler:UploadedBy") }: { UploadedBy }";
            }

            if (!string.IsNullOrWhiteSpace(Description))
                Builder.Description = Description;

            if (Current.HasValue)
            {
                TimeSpan checkTotal = TotalTime.Ticks == 0 ? this.Current.Duration : TotalTime;

                string Text = $"{ Current.Value:hh\\:mm\\:ss} - { checkTotal:hh\\:mm\\:ss} \n[";
                double CurrentTicks = Current.Value.TotalMilliseconds;
                double TotalTicks = checkTotal.TotalMilliseconds;

                int IndicatorLength = 31;

                int Index = (int)((CurrentTicks / (TotalTicks + (TotalTicks / IndicatorLength))) * IndicatorLength);

                for (int i = 0; i < IndicatorLength; i++)
                {
                    if (i == Index) Text += "ꞁ";
                    else Text += "-";
                }

                Text += "]";

                Builder.AddField(Language.GetEntry("MusicHandler:Current"), Text, false);
            }

            if (ThumbnailUrl != null)
            {
                Builder.WithThumbnailUrl(ThumbnailUrl);
            }

            return Builder.Build();
        }
        private void SetDisconnected()
        {
            State = PlayerState.Disconnected;
            try { PCMStream.Dispose(); } catch { } // it may already be disposed
            PCMStream = null;
            VoiceChannelId = 0;
            TextChannelId = 0;
            BackupTime = TimeSpan.Zero;
        }
        
    }
}