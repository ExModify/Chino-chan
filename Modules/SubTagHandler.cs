using Chino_chan.Image;
using Chino_chan.Models.Settings.Language;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Chino_chan.Modules
{
    public class SubTag
    {
        public List<string> Tags { get; set; }

        public List<string> PostIds { get; set; } = new List<string>();

        public bool Identical(SubTag tag) => ContainsIgnoreOrder(tag.Tags, Tags);

        private bool ContainsIgnoreOrder(List<string> arr1, List<string> arr2)
        {
            for (int i = 0; i < arr1.Count; i++)
            {
                if (!arr2.Contains(arr1[0], StringComparer.CurrentCultureIgnoreCase)) return false;
            }
            return true;
        }
    }
    public enum AddResult
    {
        Success = 1,
        AlreadyContains = 2,
        NoImages = 3
    }
    public class SubTagHandler
    {
        public Dictionary<ulong, List<SubTag>> Tags;
        public System.Timers.Timer Checker;
        

        public SubTagHandler()
        {
            if (File.Exists("Data\\SubTags.json"))
            {
                Tags = JsonConvert.DeserializeObject<Dictionary<ulong, List<SubTag>>>(File.ReadAllText("Data\\SubTags.json"));
            }
            else
            {
                Tags = new Dictionary<ulong, List<SubTag>>();
                Save();
            }

            Checker = new System.Timers.Timer()
            {
                Interval = Global.Settings.SubTagSleepTime,
                AutoReset = true
            };
            Checker.Elapsed += ProcessImages;
        }

        private void ProcessImages(object sender, EventArgs e)
        {
            bool SaveTags = false;
            
            foreach (ulong key in Tags.Keys)
            {
                if (!Checker.Enabled || Entrance.CancellationToken.IsCancellationRequested) break;

                if (Global.Client.GetChannel(key) is ITextChannel channel)
                {
                    LanguageEntry language = channel.GetSettings().GetLanguage();
                    List<SubTag> SubTags = Tags[key];

                    for (int i = 0; i < SubTags.Count; i++)
                    {
                        if (!Checker.Enabled || Entrance.CancellationToken.IsCancellationRequested) break;

                        List<Post> CurrentImages = new List<Post>();
                        try
                        {
                            CurrentImages.AddRange(Gelbooru.FetchImages(SubTags[i].Tags));
                        }
                        catch (Exception ex)
                        {
                            SendToErrorChannel(ex, $"Couldn't fetch images with tags: `{ string.Join(", ", SubTags[i].Tags) }`!\nWaiting 30 seconds...");
                            if (!Checker.Enabled || Entrance.CancellationToken.IsCancellationRequested) break;
                            Thread.Sleep(30 * 1000);
                        }
                        if (!Checker.Enabled || Entrance.CancellationToken.IsCancellationRequested) break;
                        Thread.Sleep(2000);
                        if (CurrentImages.Count == 0) continue;

                        bool PerformSave = false;
                        for (int j = 0; j < CurrentImages.Count; j++)
                        {
                            Post post = CurrentImages[j];

                            if (!SubTags[i].PostIds.Contains(post.PostId))
                            {
                                PerformSave = true;
                                string joined_tags = SubTags[i].Tags.Count == 0 ? language.GetEntry("SubTagHandler:NoTag") : string.Join(" ", SubTags[i].Tags);
                                channel.SendMessageAsync("", embed: new EmbedBuilder()
                                {
                                    Color = Color.Magenta,
                                    Title = language.GetEntry("SubTagHandler:NewImage", "TAGS", joined_tags) + (post.IsAnimated ? "[ANIMATED]" : "") + " - " + post.Filename,
                                    ImageUrl = (!post.IsAnimated || post.Filename.EndsWith(".gif") ? post.Link : post.ThumbnailUrl),
                                    Url = post.PostLink
                                }.Build());
                                if (!Checker.Enabled || Entrance.CancellationToken.IsCancellationRequested) break;
                                Thread.Sleep(2000);
                            }
                        }

                        if (PerformSave)
                        {
                            Tags[key][i].PostIds = CurrentImages.Select(t => t.PostId).ToList();
                            Save();
                        }
                    }
                }
                else
                {
                    Tags.Remove(key);
                    SaveTags = true;
                }
            }
            if (SaveTags) Save();
        }

        private void SendToErrorChannel(Exception ex, string Message)
        {
            if (Global.Client.GetChannel(Global.Settings.DevServer.ErrorReportChannelId) is SocketTextChannel channel)
            {
                MemoryStream ms = new MemoryStream();
                byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ex, Formatting.Indented));
                ms.Write(data, 0, data.Length);
                ms.Position = 0;

                channel.SendFileAsync(ms, "exception.txt", Message).ConfigureAwait(false).GetAwaiter().GetResult();

                ms.Close();
                ms.Dispose();
                ms = null;
            }
        }
        
        public void Start()
        {
            Checker.Start();
        }

        public void Stop()
        {
            Checker.Stop();
        }

        public void Save()
        {
            File.WriteAllText("Data\\SubTags.json", JsonConvert.SerializeObject(Tags, Formatting.Indented));
        }
        
        public AddResult Add(ulong ChannelId, List<string> Tags)
        {
            SubTag n = new SubTag()
            {
                Tags = Tags
            };
            if (this.Tags.ContainsKey(ChannelId))
            {
                for (int i = 0; i < this.Tags[ChannelId].Count; i++)
                {
                    SubTag current = this.Tags[ChannelId][i];

                    if (current.Identical(n))
                    {
                        return AddResult.AlreadyContains;
                    }
                }

                List<Post> posts = Gelbooru.FetchImages(Tags);
                if (posts.Count == 0) return AddResult.NoImages;

                n.PostIds = posts.Select(t => t.PostId).ToList();
                this.Tags[ChannelId].Add(n);
            }
            else
            {
                List<Post> posts = Gelbooru.FetchImages(Tags);
                if (posts.Count == 0) return AddResult.NoImages;

                n.PostIds = posts.Select(t => t.PostId).ToList();
                this.Tags.Add(ChannelId, new List<SubTag>() { n });
            }
            Save();
            
            return AddResult.Success;
        }
        public SubTag Remove(ulong ChannelId, int Index)
        {
            if (Tags.ContainsKey(ChannelId))
            {
                if (Tags[ChannelId].Count <= Index || Index < 0) return null;
                SubTag tag = Tags[ChannelId][Index];

                Tags[ChannelId].RemoveAt(Index);
                Save();

                return tag;
            }
            return null;
        }
        
        public List<SubTag> ListSubscription(ulong ChannelId)
        {
            if (Tags.ContainsKey(ChannelId)) return Tags[ChannelId];
            else return new List<SubTag>();
        }
    }
}
