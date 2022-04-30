using Chino_chan.Models.Settings.Language;
using Chino_chan.Models.SoundCloud;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode.Playlists;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;

namespace Chino_chan.Modules
{
    public class Choice
    {
        public ulong UserId { get; set; }
        public ulong ChannelId { get; set; }

        public event Action<int> ChoiceMade;
        public event Action Canceled;

        public int MaxNumber = 0;

        public void Selected(int Index)
        {
            ChoiceMade?.Invoke(Index);
        }
        public void Cancel() => Canceled?.Invoke();
    }
    public class MusicHandler
    {
        Dictionary<ulong, MusicPlayer> Clients;
        public bool Ready { get; private set; } = false;

        public List<Choice> RequestUserChoice { get; set; } = new List<Choice>();

        bool SaveDelay = false;

        public int ConnectedVoiceClients
        {
            get
            {
                int number = 0;
                foreach (MusicPlayer player in Clients.Values)
                {
                    if (player.State > PlayerState.Disconnected) number++;
                }
                return number;
            }
        }

        public MusicHandler()
        {
            if (File.Exists("Data/Music.json"))
            {
                Logger.Log(LogType.Music, ConsoleColor.Cyan, null, "Loading saved clients...");
                Clients = SaveManager.LoadSettings<Dictionary<ulong, MusicPlayer>>("Music");
            }
            else
            {
                Clients = new Dictionary<ulong, MusicPlayer>();
            }

            Logger.Log(LogType.Music, ConsoleColor.Cyan, null, "Ready!");
            Ready = true;
        }

        public async Task RestoreConnectionsAsync()
        {
            IEnumerator<ulong> Keys = Clients.Keys.GetEnumerator();
            List<ulong> Remove = new List<ulong>();
            while (Keys.MoveNext())
            {
                if (!(Global.Client.GetGuild(Clients[Keys.Current].GuildId) is SocketGuild Guild))
                {
                    Remove.Add(Keys.Current);
                    continue;
                }
                PlayerState OriginalState = Clients[Keys.Current].State;

                Clients[Keys.Current].PropertyChanged += QueueSave;

                if (Clients[Keys.Current].VoiceChannelId == 0 || !(Global.Client.GetChannel(Clients[Keys.Current].VoiceChannelId) is IVoiceChannel)
                    || Clients[Keys.Current].TextChannelId == 0 || !(Global.Client.GetChannel(Clients[Keys.Current].TextChannelId) is ITextChannel))
                {
                    Clients[Keys.Current].State = PlayerState.Disconnected;
                    Clients[Keys.Current].VoiceChannelId = 0;
                }
                else
                {
                    await Clients[Keys.Current].RestoreConnectionAsync();
                }
            }

            for (int i = 0; i < Remove.Count; i++) Clients.Remove(Remove[i]);

            Logger.Log(LogType.Music, ConsoleColor.Cyan, null, $"Restore complete, removed { Remove.Count } clients!");
        }
        
        public MusicPlayer GetClient(ulong GuildId)
        {
            while (!Ready) Thread.Sleep(100);
            if (!Clients.ContainsKey(GuildId))
            {
                MusicPlayer client = new MusicPlayer(GuildId);
                client.PropertyChanged += QueueSave;
                Clients.Add(GuildId, client);
            }
            return Clients[GuildId];
        }


        public async Task<Track> Select(MusicPlayer client, List<Track> Tracks, ICommandContext Context, LanguageEntry Language)
        {
            if (Tracks.Count > 20)
            {
                Tracks.RemoveRange(20, Tracks.Count - 20);
            }
            string Description = string.Join("\n", Tracks.Select((t, index) => "#" + (index + 1) + " - **" + t.Title + "** by " + t.User.Username));
            return await SelectAsync(Tracks, client, Context, Language.GetEntry("MusicHandler:SelectTrackSoundCloud"), Description, Language);
        }
        public async Task<Video> Select(MusicPlayer client, List<Video> Videos, ICommandContext Context, LanguageEntry Language)
        {
            if (Videos.Count > 20)
            {
                Videos.RemoveRange(20, Videos.Count - 20);
            }
            string Description = string.Join("\n", Videos.Select((t, index) => "#" + (index + 1) + " - **" + t.Title + "** by " + t.Author));
            return await SelectAsync(Videos, client, Context, Language.GetEntry("MusicHandler:SelectVideoYouTube"), Description, Language);
        }
        public async Task<PlaylistVideo> Select(MusicPlayer client, List<PlaylistVideo> Videos, ICommandContext Context, LanguageEntry Language)
        {
            if (Videos.Count > 20)
            {
                Videos.RemoveRange(20, Videos.Count - 20);
            }
            string Description = string.Join("\n", Videos.Select((t, index) => "#" + (index + 1) + " - **" + t.Title + "** by " + t.Author));
            return await SelectAsync(Videos, client, Context, Language.GetEntry("MusicHandler:SelectVideoYouTube"), Description, Language);
        }
        public async Task<VideoSearchResult> Select(MusicPlayer client, List<VideoSearchResult> Videos, ICommandContext Context, LanguageEntry Language)
        {
            if (Videos.Count > 20)
            {
                Videos.RemoveRange(20, Videos.Count - 20);
            }
            string Description = string.Join("\n", Videos.Select((t, index) => "#" + (index + 1) + " - **" + t.Title + "** by " + t.Author.ChannelTitle));
            return await SelectAsync(Videos, client, Context, Language.GetEntry("MusicHandler:SelectVideoYouTube"), Description, Language);
        }
        public async Task<Models.SoundCloud.Playlist> Select(MusicPlayer client, List<Models.SoundCloud.Playlist> Playlists, ICommandContext Context, LanguageEntry Language)
        {
            if (Playlists.Count > 20)
            {
                Playlists.RemoveRange(20, Playlists.Count - 20);
            }
            string Description = string.Join("\n", Playlists.Select((t, index) => "#" + (index + 1) + " - **" + t.Title + "** by " + t.User.Username + " (" + t.TrackCount + " tracks)"));
            return await SelectAsync(Playlists, client, Context, Language.GetEntry("MusicHandler:SelectPlaylistSoundCloud"), Description, Language);
        }
        
        private async Task<T> SelectAsync<T>(List<T> Tracks, MusicPlayer client, ICommandContext Context, string Title, string Description, LanguageEntry Language) 
            where T : class
        {
            EmbedBuilder Builder = new EmbedBuilder()
            {
                Title = Title,
                Color = client.Color,
                Description = Description,
                Footer = new EmbedFooterBuilder()
                {
                    Text = Language.GetEntry("MusicHandler:SelectOneMinute")
                }
            };
            IUserMessage Message = await Context.Channel.SendMessageAsync("", embed: Builder.Build());
            Choice choice = new Choice()
            {
                UserId = Context.User.Id,
                ChannelId = Context.Channel.Id,
                MaxNumber = Tracks.Count
            };
            T selected = null;

            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            choice.ChoiceMade += (i) =>
            {
                selected = Tracks[i];
                source.Cancel();
            };
            choice.Canceled += () => source.Cancel();

            RequestUserChoice.Add(choice);

            try { await Task.Delay(60 * 1000, token); } catch { } // cancelled task

            RequestUserChoice.Remove(choice);
            try
            {
                await Message.DeleteAsync();
            }
            catch { } // message got already deleted

            return selected;
        }

        private void QueueSave()
        {
            SaveManager.SaveData("Music", Clients);
        }
    }
}
