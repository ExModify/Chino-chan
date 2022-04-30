using Chino_chan.Models;
using Chino_chan.Models.API;
using Chino_chan.Models.Privileges;
using Chino_chan.Models.Settings;
using Chino_chan.Models.Settings.Language;
using Chino_chan.Models.SoundCloud;
using Chino_chan.Modules;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Playlists;
using System.Diagnostics;
using YoutubeExplode.Search;

namespace Chino_chan.Commands
{
#pragma warning disable CS1998 // async surprass
    public class Music : ChinoContext
    {
        public MusicPlayer Client
        {
            get
            {
                return Global.MusicHandler.GetClient(Context.Guild.Id);
            }
        }

        [Command("connect"), Alias("c"), ServerCommand(), Summary("Connects to the voice channel where you are, or to a specific one")]
        public async Task ConnectAsync(params string[] Args)
        {
            string Arg = string.Join(" ", Args).ToLower();

            if (Args.Length > 0)
            {
                List<IVoiceChannel> VoiceChannels = (await Context.Guild.GetVoiceChannelsAsync()).ToList();

                foreach (IVoiceChannel VoiceChannel in VoiceChannels)
                {
                    if (VoiceChannel.Name.ToLower().Contains(Arg.ToLower()))
                    {
                        await Client.ConnectAsync(Context, VoiceChannel);
                        break;
                    }
                }
            }
            else
            {
                await Client.ConnectAsync(Context);

            }
        }

        [Command("disconnect"), Alias("dc"), ServerCommand(), Summary("Disconnects from the voice channel")]
        public async Task DisconnectAsync(params string[] Args)
        {
            await Client.DisconnectAsync(Context);
        }

        [Command("seek"), ServerCommand(), Summary("Seeks to a specific point (available: h:hours; m:minutes, s:seconds, like 2m 10s)")]
        public async Task SeekAsync(params string[] Args)
        {
            if (Client.State < PlayerState.Playing)
            {
                await Context.Channel.SendMessageAsync();
            }
            if (Args.Length > 0)
            {
                int h = -1;
                int m = -1;
                int s = -1;

                for (int i = 0; i < Args.Length; i++)
                {
                    string Arg = Args[i].ToLower();

                    if (Arg.EndsWith("h"))
                    {
                        if (h != -1)
                        {
                            await Context.Channel.SendMessageAsync(GetEntry("HoursOnce"));
                            return;
                        }
                        if (int.TryParse(Arg.Substring(0, Arg.Length - 1), out h))
                        {
                            if (h < 0)
                            {
                                await Context.Channel.SendMessageAsync(GetEntry("OnlyPositive"));
                                return;
                            }
                        }
                    }
                    else if (Arg.EndsWith("m"))
                    {
                        if (m != -1)
                        {
                            await Context.Channel.SendMessageAsync(GetEntry("MinutesOnce"));
                            return;
                        }
                        if (int.TryParse(Arg.Substring(0, Arg.Length - 1), out m))
                        {
                            if (m < 0)
                            {
                                await Context.Channel.SendMessageAsync(GetEntry("OnlyPositive"));
                                return;
                            }
                        }
                    }
                    if (Arg.EndsWith("s"))
                    {
                        if (s != -1)
                        {
                            await Context.Channel.SendMessageAsync(GetEntry("SecondsOnce"));
                            return;
                        }
                        if (int.TryParse(Arg.Substring(0, Arg.Length - 1), out s))
                        {
                            if (s < 0)
                            {
                                await Context.Channel.SendMessageAsync(GetEntry("OnlyPositive"));
                                return;
                            }
                        }
                    }
                }

                TimeSpan ts = TimeSpan.FromSeconds(0);

                if (h != -1)
                {
                    ts = ts.Add(TimeSpan.FromHours(h));
                }
                if (m != -1)
                {
                    ts = ts.Add(TimeSpan.FromMinutes(m));
                }
                if (s != -1)
                {
                    ts = ts.Add(TimeSpan.FromSeconds(s));
                }

                if (Client.Seek(ts))
                {
                    await Context.Channel.SendMessageAsync(GetEntry("SetTimeTo", "TIME", ts.ToString()));
                }
                else
                {
                    await Context.Channel.SendMessageAsync(GetEntry("CannotSet"));
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync(GetEntry("NoParam"));
            }
        }

        [Command("play"), ServerCommand(), Summary("Adds a YouTube video by keyword or link, also resumes the current playing if it's written without parameters~")]
        public async Task PlayAsync(params string[] Args)
        {
            if (Client.State == PlayerState.Paused)
            {
                Client.State = PlayerState.Playing;
                return;
            }
            EmbedBuilder Builder = new EmbedBuilder()
            {
                Color = Client.Color,
                Title = GetEntry("Enqueue")
            };
            if (Args.Length > 0)
            {
                if (Args[0] == "soundcloud" && Args.Length > 1)
                {
                    if (Args[1] == "playlist" && Args.Length > 2)
                    {
                        List<Models.SoundCloud.Playlist> playlists = Client.SearchSoundCloudPlaylist(string.Join(" ", Args.Skip(2)));

                        Models.SoundCloud.Playlist playlist = await Global.MusicHandler.Select(Client, playlists, Context, Language);

                        if (playlist == null)
                        {
                            Builder.Description = GetEntry("SCPlaylistCancelled");
                            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
                        }
                        else
                        {
                            Client.Enqueue(playlist);
                            
                            Builder.Url = playlist.Url;
                            Builder.Description = GetEntry("SCPlaylistEnqueued");
                            Builder.AddField(GetEntry("TitleTrackCount"), playlist.Title + " | " + playlist.TrackCount);
                            Builder.AddField(GetEntry("PlaylistMadeBy"), playlist.User.Username);
                            Builder.ThumbnailUrl = playlist.ThumbnailUrl;

                            await Context.Channel.SendMessageAsync("", embed: Builder.Build());

                            if (Client.State < PlayerState.Playing)
                                await Client.PlayAsync(Context);
                        }
                    }
                    else
                    {
                        List<Models.SoundCloud.Track> tracks = Client.SearchSoundCloudTrack(string.Join(" ", Args.Skip(1)));

                        Models.SoundCloud.Track track = await Global.MusicHandler.Select(Client, tracks, Context, Language);

                        if (track == null)
                        {
                            Builder.Description = GetEntry("SCTrackCancelled");
                            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
                        }
                        else
                        {
                            Client.Enqueue(track);
                            if (Client.State < PlayerState.Playing)
                                await Client.PlayAsync(Context);
                            else
                            {
                                Builder.Url = track.Url;
                                Builder.Description = GetEntry("SCTrackEnqueued");
                                Builder.AddField(GetEntry("TitleUploadedBy"), track.Title + " | " + track.User.Username);
                                Builder.ThumbnailUrl = track.ThumbnailUrl;

                                await Context.Channel.SendMessageAsync("", embed: Builder.Build());
                            }
                        }
                    }
                }
                else if (Args[0].ToLower() == "listen.moe")
                {
                    Client.EnqueueLISTENmoe(Args.Length > 1 && Args[1].ToLower() == "kpop");
                    Builder.Description = "LISTEN.moe";
                    await Context.Channel.SendMessageAsync("", embed: Builder.Build());

                    if (Client.State < PlayerState.Playing)
                        await Client.PlayAsync(Context);
                }
                else
                {
                    string url = string.Join(" ", Args);
                    YoutubeClient client = new YoutubeClient();
                    PlaylistId? id = PlaylistId.TryParse(url);
                    if (id.HasValue)
                    {
                        List<PlaylistVideo> videos = await client.Playlists.GetVideosAsync(id.Value).ToListAsync();
                        if (videos.Count == 0)
                        {
                            Builder.Description = GetEntry("YTPlaylistEmpty");
                            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
                        }
                        else
                        {
                            YoutubeExplode.Playlists.Playlist playlist = await client.Playlists.GetAsync(id.Value);

                            Client.Enqueue(playlist);

                            if (Client.State < PlayerState.Playing)
                                await Client.PlayAsync(Context);
                            else
                            {
                                Builder.Url = playlist.Url;
                                Builder.Description = GetEntry("YTPlaylistEnqueued");
                                Builder.AddField(GetEntry("TitleUploadedBy"), playlist.Title + " | " + playlist.Author);
                                Builder.ThumbnailUrl = videos[0].Thumbnails.GetMaxResUrl();

                                await Context.Channel.SendMessageAsync("", embed: Builder.Build());
                            }
                        }
                    }
                    VideoId? vid = VideoId.TryParse(url);
                    if (vid.HasValue)
                    {
                        Video video = await client.Videos.GetAsync(vid.Value);
                        await EnqueueVideo(video, Builder);
                    }
                    else
                    {
                        List<VideoSearchResult> videos = await Client.SearchYouTubeVideoAsync(url);
                        VideoSearchResult video = await Global.MusicHandler.Select(Client, videos, Context, Language);

                        if (video == null)
                        {
                            Builder.Description = GetEntry("YTVideoCancelled");
                            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
                        }
                        else
                        {
                            await EnqueueVideo(video, Builder);
                        }
                    }
                }
            }
            else
            {
                if (Client.Queue.Count != 0 && Client.State < PlayerState.Playing)
                {
                    await Client.PlayAsync(Context);
                }
                else
                {
                    await Context.Channel.SendMessageAsync(GetEntry("Empty"));
                }
            }
        }

        [Command("pause"), ServerCommand(), Summary("Pauses playing")]
        public async Task PauseAsync(params string[] Args)
        {
            if (Client.State == PlayerState.Playing)
            {
                Client.State = PlayerState.Paused;
                return;
            }
            else if (Client.State == PlayerState.Paused)
            {
                Client.State = PlayerState.Playing;
            }
            else
            {
                await Context.Channel.SendMessageAsync(GetEntry("NoPlayback"));
            }
        }

        [Command("stop"), ServerCommand(), Summary("Stops playing")]
        public async Task StopAsync(params string[] Args)
        {
            await Client.StopAsync(Context);
        }

        [Command("volume"), ServerCommand(), Summary("Changes the volume of the playback")]
        public async Task VolumeAsync(params string[] Args)
        {
            if (Args.Length > 0)
            {
                if (int.TryParse(Args[0], out int Volume))
                {
                    if (Volume < 0 || Volume > 200)
                    {
                        await Context.Channel.SendMessageAsync(GetEntry("OutOfRange"));
                        return;
                    }

                    Client.Volume = Volume;
                }
            }
            EmbedBuilder Builder = new EmbedBuilder()
            {
                Color = Client.Color,
                Title = GetEntry("Volume"),
                Description = GetEntry("CurrentVolume") + ": " + Client.Volume + "\n["
            };
            int Length = 11;
            double Max = 200.0;
            int Index = (int)((Client.Volume / (Max + (Max / Length))) * Length);
            for (int i = 0; i < Length; i++)
            {
                if (i == Index) Builder.Description += "ꞁ";
                else Builder.Description += "-";
            }
            Builder.Description += "]";
            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("next"), Alias("skip"), ServerCommand(), Summary("Goes to the next song")]
        public async Task NextAsync(params string[] Args)
        {
            if (Client.State < PlayerState.Playing)
            {
                await Context.Channel.SendMessageAsync(GetEntry("NoPlayback"));
            }
            else
            {
                Client.Request = PlayerRequest.Next;
            }
        }

        [Command("queue"), Alias("list", "playlist"), ServerCommand(), Summary("Lists all the songs from the queue")]
        public async Task SendQueueAsync(params string[] Args)
        {
            await Client.ListAsync(Context);
        }

        [Command("current"), ServerCommand(), Summary("Sends the current playing")]
        public async Task CurrentAsync(params string[] Args)
        {
            await Client.SendNowPlayingAsync(Context);
        }
        
        [Command("remove"), ServerCommand(), Summary("Removes a song by index")]
        public async Task RemoveAsync(params string[] Args)
        {
            if (Args.Length > 0)
            {
                if (int.TryParse(Args[0], out int Id))
                {
                    if (Client.Dequeue(Id))
                    {
                        await Context.Channel.SendMessageAsync(GetEntry("Successful"));
                    }
                    else
                    {
                        if (Client.Queue.Count == 0)
                        {
                            await Context.Channel.SendMessageAsync(GetEntry("Empty"));
                        }
                        else
                        {
                            await Context.Channel.SendMessageAsync(GetEntry("InvalidIndex"));
                        }
                    }

                    return;
                }
            }
            await Context.Channel.SendMessageAsync("Please provide the number of queue item to remove it~");
        }
        
        [Command("clear"), ServerCommand(), Summary("Clears the queue")]
        public async Task ClearAsync(params string[] Args)
        {
            Client.ClearQueue();
            await Context.Channel.SendMessageAsync(GetEntry("Cleared"));
        }

        [Command("shuffle"), ServerCommand(), Summary("Shuffles the playlist")]
        public async Task ShuffleAsync(params string[] Args)
        {
            if (Client.ShufflePlaylist())
            {
                await ReplyAsync(GetEntry("Shuffled"));
            }
            else
            {
                await ReplyAsync(GetEntry("NotEnoughItems"));
            }
        }

        [Command("repeat"), ServerCommand(), Summary("Changes the repeat mode. Use the command without arguments to display the indices of the modes!")]
        public async Task ChangeRepeatAsync(int Index = -1)
        {
            Index--;
            if (Index < 0 || Index > 4)
            {
                await Context.Channel.SendMessageAsync(GetEntry("NoValidIndex",
                                                                "MODE", GetEntry(((int)Client.Repeat).ToString()),
                                                                "FIRST", GetEntry("0"),
                                                                "SECOND", GetEntry("1"),
                                                                "THIRD", GetEntry("2"),
                                                                "FOURTH", GetEntry("3"),
                                                                "FIFTH", GetEntry("4")));
            }
            else
            {
                Client.Repeat = (RepeatMode)Index;
                await Context.Channel.SendMessageAsync(GetEntry("Changed", "MODE", GetEntry(Index.ToString())));
            }
        }

        private async Task EnqueueVideo(Video video, EmbedBuilder Builder)
        {
            Client.Enqueue(video);

            if (Client.State < PlayerState.Playing)
                await Client.PlayAsync(Context);
            else
            {
                Builder.Url = video.Url;
                Builder.Description = GetEntry("YTVideoEnqueued");
                Builder.AddField(GetEntry("TitleUploadedBy"), video.Title + " | " + video.Author.ChannelTitle);
                Builder.ThumbnailUrl = video.Thumbnails.GetMaxResUrl();

                await Context.Channel.SendMessageAsync("", embed: Builder.Build());
            }
        }
        private async Task EnqueueVideo(VideoSearchResult video, EmbedBuilder Builder)
        {
            Client.Enqueue(video);

            if (Client.State < PlayerState.Playing)
                await Client.PlayAsync(Context);
            else
            {
                Builder.Url = video.Url;
                Builder.Description = GetEntry("YTVideoEnqueued");
                Builder.AddField(GetEntry("TitleUploadedBy"), video.Title + " | " + video.Author.ChannelTitle);
                Builder.ThumbnailUrl = video.Thumbnails.GetMaxResUrl();

                await Context.Channel.SendMessageAsync("", embed: Builder.Build());
            }
        }
    }
}
