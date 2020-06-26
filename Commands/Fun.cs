using Chino_chan.Image;
using Chino_chan.Models;
using Chino_chan.Models.osuAPI;
using Chino_chan.Models.Privileges;
using Chino_chan.Models.Settings;
using Chino_chan.Modules;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode;

namespace Chino_chan.Commands
{
    public class Fun : ChinoContext
    {
        private IGuild DevGuild
        {
            get
            {
                return Global.Client.GetGuild(Global.Settings.DevServer.Id);
            }
        }
        private struct ChinoResponse
        {
            public string[] Files { get; set; }
            public string Error { get; set; }
        }

        public Color EmbedColor = Color.Magenta;

        [Command("avatar"), Summary("Gets the avatar of a user by username, nickname or user id owo")]
        public async Task GetAvatarAsync([Remainder]string Username = "")
        {
            IUser User = null;

            if (Username.Length == 0)
                User = Context.User;

            if (User == null)
                User = Tools.ParseUser(Username, Global.IsGlobalAdminOrHigher(Context.User.Id), Context);

            if (User == null)
            {
                await Context.Channel.SendMessageAsync(GetGlobalEntry("UserNotFound"));
            }
            else
            {
                string AvatarUrl = User.GetAvatarUrl(size: 2048);

                EmbedBuilder Builder = new EmbedBuilder()
                {
                    Color = new Color(255, 48, 222),
                    Url = AvatarUrl,
                    ImageUrl = AvatarUrl
                };

                string Name = User.Username;

                if (User is IGuildUser GuildUser)
                {
                    if (GuildUser.Nickname != null)
                        Name = $"{ GuildUser.Nickname } ({ User.Username })";

                    if (GuildUser.GuildId == Context.Guild.Id)
                    {
                        Name += "#" + User.Discriminator;
                        List<IRole> Roles = new List<IRole>();
                        foreach (ulong RoleId in GuildUser.RoleIds)
                        {
                            IRole Role = Context.Guild.GetRole(RoleId);
                            Roles.Add(Role);
                        }
                        Roles = Roles.OrderByDescending(t => t.Position).ToList();

                        foreach (IRole Role in Roles)
                        {
                            if (Role.Color.RawValue != Color.Default.RawValue)
                            {
                                Builder.Color = Role.Color;
                                break;
                            }
                        }
                    }
                }

                Builder.Author = new EmbedAuthorBuilder()
                {
                    IconUrl = User.GetAvatarUrl(),
                    Name = Name,
                    Url = AvatarUrl
                };

                await Context.Channel.SendMessageAsync("", embed: Builder.Build());
            }
        }

        [Command("gicon"), ServerCommand(), Summary("Gets the icon of the current server")]
        public async Task GetGuildAvatar(params string[] Args)
        {
            SocketGuild guild = null;
            if (Args.Length == 0)
            {
                guild = Global.Client.GetGuild(Context.Guild.Id);
            }
            else
            {
                string sname = string.Join(" ", Args).ToLower();

                if (ulong.TryParse(sname, out ulong sId))
                {
                    guild = Global.Client.GetGuild(sId);
                    if (!(guild.GetUser(Context.User.Id) is IUser) && Context.User.Id != Global.Settings.OwnerId)
                    {
                        guild = null;
                    }
                }

                if (guild == null)
                {
                    IEnumerator<SocketGuild> enumerator = Global.Client.Guilds.GetEnumerator();
                    enumerator.MoveNext();

                    do
                    {
                        if (enumerator.Current.Name.ToLower() == sname)
                        {
                            if (enumerator.Current.GetUser(Context.User.Id) is IUser || Context.User.Id == Global.Settings.OwnerId)
                            {
                                guild = enumerator.Current;
                                break;
                            }
                        }
                        enumerator.MoveNext();
                    }
                    while (enumerator.Current != null);
                }
            }

            if (guild == null)
                return;

            if (guild.IconUrl == null)
                await Context.Channel.SendMessageAsync(GetEntry("HasNoIcon"));

            var Url = guild.IconUrl;
            WebClient c = new WebClient();

            string gifUrl = Path.ChangeExtension(Url, "gif");
            try
            {
                c.DownloadData(gifUrl);
                Url = gifUrl;
            }
            catch (WebException)
            {
                // not supported
            }

            Url += "?size=512";
            EmbedBuilder Builder = new EmbedBuilder()
            {
                Color = await Global.GetAverageColorAsync(Url),
                Url = Url,
                ImageUrl = Url
            };

            Builder.WithAuthor(Context.User);

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("waifu"), Summary("no.")]
        public async Task WaifuAsync(params string[] Args)
        {
            if (Global.IsOwner(Context.User.Id) && Args.Length > 0)
            {
                bool save = false;

                switch (Args[0].ToLower())
                {
                    case "add":
                        if (Context.Message.MentionedUserIds.Count > 0)
                        {
                            foreach (ulong id in Context.Message.MentionedUserIds)
                            {
                                if (!Global.Settings.WaifuYes.Contains(id))
                                {
                                    Global.Settings.WaifuYes.Add(id);
                                    save = true;
                                }
                            }

                            if (save)
                                Global.SaveSettings();
                        }
                        goto default;
                    case "remove":
                        if (Context.Message.MentionedUserIds.Count > 0)
                        {
                            foreach (ulong id in Context.Message.MentionedUserIds)
                            {
                                if (Global.Settings.WaifuYes.Contains(id))
                                {
                                    Global.Settings.WaifuYes.RemoveAll(t => t == id);
                                    save = true;
                                }
                            }

                            if (save)
                                Global.SaveSettings();
                        }
                        goto default;
                    case "list":
                        if (Global.Settings.WaifuYes.Count > 0)
                        {
                            string msg = "```\n";
                            foreach (ulong id in Global.Settings.WaifuYes)
                            {
                                IUser user = Global.Client.GetUser(id);
                                if (user == null)
                                {
                                    foreach (IGuild guild in Global.Client.Guilds)
                                    {
                                        IUser u = guild.GetUserAsync(id).Result;
                                        if (u != null)
                                        {
                                            user = u;
                                            break;
                                        }
                                    }
                                }

                                if (user != null)
                                {
                                    msg += user.Id + " - " + user.Username + "\n";
                                }
                                else
                                {
                                    Global.Settings.WaifuYes.RemoveAll(t => t == id);
                                    save = true;
                                }
                            }
                            msg += "```";

                            if (save)
                                Global.SaveSettings();

                            await Context.Channel.SendMessageAsync(msg);
                            return;
                        }
                        goto default;
                    case "clear":
                        if (Global.Settings.WaifuYes.Count != 0)
                        {
                            Global.Settings.WaifuYes.Clear();
                            Global.SaveSettings();
                        }
                        goto default;
                    case "rd":
                        for (int i = 0; i < Global.Settings.WaifuYes.Count;)
                        {
                            ulong c = Global.Settings.WaifuYes[i];
                            i++;

                            for (int j = i; j < Global.Settings.WaifuYes.Count; j++)
                            {
                                if (Global.Settings.WaifuYes[j] == c)
                                {
                                    Global.Settings.WaifuYes.RemoveAt(j);
                                    j--;
                                    save = true;
                                }
                            }
                        }
                        if (save)
                            Global.SaveSettings();
                        goto default;
                    default:
                        await Context.Channel.SendMessageAsync("Done!").ContinueWith(async t =>
                            await Task.Delay(3000).ContinueWith(async s => await t.Result.DeleteAsync()));
                        return;
                }
            }

            if (Global.IsOwner(Context.User.Id) || Global.Settings.WaifuYes.Contains(Context.User.Id))
            {
                await Context.Channel.SendMessageAsync(GetEntry("Yes") + Global.ProcessEmotes(" :ChinoHide:", DevGuild));
            }
            else
            {
                await Context.Channel.SendMessageAsync(GetEntry("No") + Global.ProcessEmotes("no :ChinoSleep:", DevGuild));
            }
        }

        [Command("sankaku"), Summary("Fetches images from the Sankaku servers")]
        public async Task SankakuAsync(params string[] Args)
        {
            if (Global.Sankaku != null)
            {
                await SendImageAsync(Global.Sankaku, Args);
            }
            else
            {
                await Context.Channel.SendMessageAsync(GetEntry("NotAvailable"));
            }
        }

        [Command("danbooru"), Summary("Fetches images from the danbooru servers")]
        public async Task DanbooruAsync(params string[] Args)
        {
            await SendImageAsync(Global.DanbooruFetcher, Args);
        }

        [Command("yandere"), Summary("Fetches images from the yandere servers")]
        public async Task YandereAsync(params string[] Args)
        {
            await SendImageAsync(Global.YandereFetcher, Args);
        }

        [Command("gelbooru"), Summary("Fetches images from the gelbooru servers")]
        public async Task GelbooruAsync(params string[] Args)
        {
            await SendImageAsync(Global.GelbooruFetcher, Args);
        }

        [Command("images"), Summary("Lists the available \"offline\" image folders")]
        public async Task ImagesAsync(params string[] Args)
        {
            if (Global.Images.Count == 0)
            {
                await Context.Channel.SendMessageAsync(GetEntry("NoImages"));
            }
            else
            {
                if (Args.Length == 1)
                {
                    var ImageType = Args[0].ToLower();
                    if (Global.Images.Images.ContainsKey(ImageType))
                    {
                        var Count = Global.Images.Images[ImageType].Count;
                        await Context.Channel.SendMessageAsync(GetEntry("FolderInfo", "FN", ImageType, "C", Count.ToString()));
                        return;
                    }
                }
                await Context.Channel.SendMessageAsync(GetEntry("AvailableFolders", "FNS", string.Join(", ", Global.Images.Images.Select(t => t.Key))));
            }
        }

        [Command("subtag"), Summary("When a new image is posted on gelbooru to the given specific tag(s), it'll send it to the channel where the command was invoked"), ServerCommand()]
        public async Task SubTagAsync(params string[] Args)
        {
            List<string> Tags = new List<string>(Args);

            bool nsfw = Global.IsNsfwChannel(Settings, Context.Channel.Id);

            for (int i = 0; i < Tags.Count;)
            {
                if (!nsfw)
                {
                    if (Tags[i].StartsWith("rating:", StringComparison.CurrentCultureIgnoreCase) && !Tags[i].EndsWith("safe", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Tags.RemoveAt(i);
                    }
                    else i++;
                }
                else i++;
            }

            if (!nsfw && !Tags.Contains("rating:safe"))
            {
                await Context.Channel.SendMessageAsync(GetEntry("SFWAdded"));
                if (!Tags.Contains("rating:safe", StringComparer.CurrentCultureIgnoreCase))
                {
                    Tags.Add("rating:safe");
                }
            }

            AddResult Success = Global.SubTagHandler.Add(Context.Channel.Id, Tags);

            switch (Success)
            {
                case AddResult.Success:
                    await Context.Channel.SendMessageAsync(GetEntry("Added", "Tags", string.Join(" ", Tags)));
                    break;
                case AddResult.AlreadyContains:
                    await Context.Channel.SendMessageAsync(GetEntry("AlreadyPresent"));
                    break;
                case AddResult.NoImages:
                    await Context.Channel.SendMessageAsync(GetEntry("HasNoImages"));
                    break;
            }
        }

        [Command("listsubtag"), Summary("Lists all the tag subscriptions that are reported in the specific channel"), ServerCommand]
        public async Task ListSubTags(params string[] _)
        {
            List<SubTag> tags = Global.SubTagHandler.ListSubscription(Context.Channel.Id);

            EmbedBuilder Builder = new EmbedBuilder()
            {
                Title = GetEntry("TagsOf", "CNAME", Context.Channel.Name),
                Color = Color.DarkBlue,
                Footer = new EmbedFooterBuilder()
                {
                    Text = GetEntry("ToRemove")
                }
            };

            if (tags.Count == 0)
            {
                Builder.Description = GetEntry("Nothing");
            }
            else
            {
                for (int i = 0; i < tags.Count; i++)
                {
                    Builder.Description += $"#{ i + 1 }: `{ string.Join(" ", tags[i].Tags) }`\n";
                }
            }

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("removesubtag"), Summary("Removes a subscription in the current channel by index"), ServerCommand]
        public async Task RemoveSubTagAsync(int Index)
        {
            SubTag tag = Global.SubTagHandler.Remove(Context.Channel.Id, Index - 1);

            if (tag == null)
            {
                await Context.Channel.SendMessageAsync(GetEntry("OutOfRange"));
            }
            else
            {
                await Context.Channel.SendMessageAsync(GetEntry("Removed", "TAGS", string.Join(" ", tag.Tags)));
            }
        }

        [Command("nom"), Summary("Nom someones by mentioning them *noms*")]
        public async Task NomAsync(params string[] _)
        {
            EmbedBuilder builder = new EmbedBuilder()
            {
                Color = EmbedColor
            };

            try
            {
                string url = GetImage("bite");
                builder.ImageUrl = url ?? throw new Exception();
            }
            catch
            {
                builder.Title = GetEntry("CouldNotGetImage");
            }

            string targets = await GetTargetsAsync();
            if (targets == "")
            {
                builder.Description = GetEntry("NoTargetDescription");
            }
            else
            {
                builder.Description = GetEntry("TargetDescription", "WHO", Context.Message.Author.Mention, "TARGETS", targets);
            }
            builder.Description = Global.ProcessEmotes(builder.Description, DevGuild).Limit();

            await Context.Channel.SendMessageAsync("", embed: builder.Build());
        }

        [Command("cuddle"), Summary("Cuddle someones by mentioning them uwu")]
        public async Task CuddleAsync(params string[] _)
        {
            EmbedBuilder builder = new EmbedBuilder()
            {
                Color = EmbedColor
            };

            try
            {
                string url = GetImage("cuddle");
                builder.ImageUrl = url ?? throw new Exception();
            }
            catch
            {
                builder.Title = GetEntry("CouldNotGetImage");
            }

            string targets = await GetTargetsAsync();
            if (targets == "")
            {
                builder.Description = GetEntry("NoTargetDescription");
            }
            else
            {
                builder.Description = GetEntry("TargetDescription", "WHO", Context.Message.Author.Mention, "TARGETS", targets);
            }
            builder.Description = Global.ProcessEmotes(builder.Description, DevGuild).Limit();

            await Context.Channel.SendMessageAsync("", embed: builder.Build());
        }

        [Command("hug"), Summary("Hug someones by mentioning them owo")]
        public async Task HugAsync(params string[] _)
        {
            EmbedBuilder builder = new EmbedBuilder()
            {
                Color = EmbedColor
            };

            try
            {
                string url = GetImage("hug");
                builder.ImageUrl = url ?? throw new Exception();
            }
            catch
            {
                builder.Title = GetEntry("CouldNotGetImage");
            }

            string targets = await GetTargetsAsync();
            if (targets == "")
            {
                builder.Description = GetEntry("NoTargetDescription");
            }
            else
            {
                builder.Description = GetEntry("TargetDescription", "WHO", Context.Message.Author.Mention, "TARGETS", targets);
            }

            builder.Description = Global.ProcessEmotes(builder.Description, DevGuild).Limit();

            await Context.Channel.SendMessageAsync("", embed: builder.Build());
        }

        [Command("pat"), Summary("Pat someones by mentioning them uwu")]
        public async Task PatAsync(params string[] _)
        {
            EmbedBuilder builder = new EmbedBuilder()
            {
                Color = EmbedColor
            };

            try
            {
                string url = GetImage("pat");
                builder.ImageUrl = url ?? throw new Exception();
            }
            catch
            {
                builder.Title = GetEntry("CouldNotGetImage");
            }

            string targets = await GetTargetsAsync();
            if (targets == "")
            {
                builder.Description = GetEntry("NoTargetDescription");
            }
            else
            {
                builder.Description = GetEntry("TargetDescription", "WHO", Context.Message.Author.Mention, "TARGETS", targets);
            }
            builder.Description = Global.ProcessEmotes(builder.Description, DevGuild).Limit();

            await Context.Channel.SendMessageAsync("", embed: builder.Build());
        }

        [Command("kiss"), Summary("Kisses someones by mentioning them uwu")]
        public async Task KissAsync(params string[] _)
        {
            EmbedBuilder builder = new EmbedBuilder()
            {
                Color = EmbedColor
            };

            bool owner = Global.IsOwner(Context.Message.Author.Id) || Context.User.Id == 191650823682392064;
            string targets = await GetTargetsAsync(!owner);
            if (Global.BlockExMoTarget(targets, Context.Message.Author.Id))
            {
                await Context.Message.DeleteAsync();
                return;
            }
            try
            {
                string url = GetImage("kiss");
                builder.ImageUrl = url ?? throw new Exception();
            }
            catch
            {
                builder.Title = GetEntry("CouldNotGetImage");
            }

            if (targets == "")
            {
                if (owner)
                {
                    builder.Description = GetEntry("OwnerDescription");
                }
                else
                {
                    builder.Description = GetEntry("NoTargetDescription");
                    builder.Title = "";
                    builder.ImageUrl = "";
                }
            }
            else
            {
                builder.Description = GetEntry("TargetDescription", "WHO", Context.Message.Author.Mention, "TARGETS", targets);
            }
            builder.Description = Global.ProcessEmotes(builder.Description, DevGuild).Limit();

            await Context.Channel.SendMessageAsync("", embed: builder.Build());
        }

        [Command("bongo"), Alias("bongocat"), Summary("Sends a random bongocat~")]
        public async Task SendBongoCatAsync(params string[] _)
        {
            EmbedBuilder builder = new EmbedBuilder()
            {
                Color = EmbedColor
            };

            try
            {
                string url = GetImage("bongocat");
                builder.ImageUrl = url ?? throw new Exception();
            }
            catch
            {
                builder.Title = GetEntry("CouldNotGetImage");
            }

            await Context.Channel.SendMessageAsync("", embed: builder.Build());
        }

        [Command("pout"), Summary("Pouts at someones by mentioning them *hmpf*")]
        public async Task PoutAsync(params string[] _)
        {
            EmbedBuilder builder = new EmbedBuilder()
            {
                Color = EmbedColor
            };

            try
            {
                string url = GetImage("pout");
                builder.ImageUrl = url ?? throw new Exception();
            }
            catch
            {
                builder.Title = GetEntry("CouldNotGetImage");
            }

            string targets = await GetTargetsAsync();
            if (targets == "" || targets.Contains("<@" + Context.Client.CurrentUser.Id))
            {
                builder.Description = GetEntry("NoTargetDescription");
            }
            else
            {
                builder.Description = GetEntry("TargetDescription", "WHO", Context.Message.Author.Mention, "TARGETS", targets);
            }
            builder.Description = Global.ProcessEmotes(builder.Description, DevGuild).Limit();

            await Context.Channel.SendMessageAsync("", embed: builder.Build());
        }

        [Command("poke"), Summary("Pokes someones by mentioning them *pokes*")]
        public async Task PokeAsync(params string[] _)
        {
            EmbedBuilder builder = new EmbedBuilder()
            {
                Color = EmbedColor
            };

            try
            {
                string url = GetImage("poke");
                builder.ImageUrl = url ?? throw new Exception();
            }
            catch
            {
                builder.Title = GetEntry("CouldNotGetImage");
            }

            string targets = await GetTargetsAsync();
            if (targets == "" || targets.Contains("<@" + Context.Client.CurrentUser.Id))
            {
                builder.Description = GetEntry("NoTargetDescription");
            }
            else
            {
                builder.Description = GetEntry("TargetDescription", "WHO", Context.Message.Author.Mention, "TARGETS", targets);
            }
            builder.Description = Global.ProcessEmotes(builder.Description, DevGuild).Limit();

            await Context.Channel.SendMessageAsync("", embed: builder.Build());
        }

        [Command("tickle"), Summary("Tickles someones by mentioning them uwu")]
        public async Task TickleAsync(params string[] _)
        {
            EmbedBuilder builder = new EmbedBuilder()
            {
                Color = EmbedColor
            };

            try
            {
                string url = GetImage("tickle");
                builder.ImageUrl = url ?? throw new Exception();
            }
            catch
            {
                builder.Title = GetEntry("CouldNotGetImage");
            }

            string targets = await GetTargetsAsync();
            if (targets == "" || targets.Contains("<@" + Context.Client.CurrentUser.Id))
            {
                builder.Description = GetEntry("NoTargetDescription");
            }
            else
            {
                builder.Description = GetEntry("TargetDescription", "WHO", Context.Message.Author.Mention, "TARGETS", targets);
            }
            builder.Description = Global.ProcessEmotes(builder.Description, DevGuild).Limit();

            await Context.Channel.SendMessageAsync("", embed: builder.Build());
        }

        [Command("slap"), Summary("Slaps someones by mentioning them")]
        public async Task SlapAsync(params string[] _)
        {
            EmbedBuilder builder = new EmbedBuilder()
            {
                Color = EmbedColor
            };

            string targets = await GetTargetsAsync(true, Global.Settings.WaifuYes.ToArray());
            if (targets == "")
            {
                IUser user = Global.Client.GetUser(Global.Settings.OwnerId);
                if (user == null)
                {
                    foreach (IGuild guild in Global.Client.Guilds)
                    {
                        IUser u = guild.GetUserAsync(Global.Settings.OwnerId).Result;
                        if (u != null)
                        {
                            user = u;
                            break;
                        }
                    }
                }
                if (user.Status == UserStatus.Offline)
                {
                    builder.Description = GetEntry("OnlineExMoProtection");
                }
                else
                {
                    builder.Description = GetEntry("OfflineExMoProtection");
                }
            }
            else
            {
                try
                {
                    string url = GetImage("slap");
                    builder.ImageUrl = url ?? throw new Exception();
                }
                catch
                {
                    builder.Title = GetEntry("CouldNotGetImage");
                }

                builder.Description = GetEntry("TargetDescription", "WHO", Context.Message.Author.Mention, "TARGETS", targets);
            }
            builder.Description = Global.ProcessEmotes(builder.Description, DevGuild).Limit();

            await Context.Channel.SendMessageAsync("", embed: builder.Build());
        }

        [Command("lick"), Summary("Lick someone by mentioning them uwu")]
        public async Task LickAsync(params string[] _)
        {
            EmbedBuilder builder = new EmbedBuilder()
            {
                Color = EmbedColor
            };

            bool owner = Global.IsOwner(Context.Message.Author.Id) || Context.User.Id == 191650823682392064;
            string targets = await GetTargetsAsync(!owner);
            if (Global.BlockExMoTarget(targets, Context.Message.Author.Id))
            {
                await Context.Message.DeleteAsync();
                return;
            }
            string url = null;
            try
            {
                url = GetImage("lick");
                builder.ImageUrl = url ?? throw new Exception();
            }
            catch
            {
                Logger.Log(LogType.Commands, ConsoleColor.Green, "ImageCDN", $"The url is \"{ url ?? "empty" }\"");
                builder.Title = GetEntry("CouldNotGetImage");
            }
            if (targets == "")
            {
                if (owner)
                {
                    builder.Description = GetEntry("OwnerDescription");
                }
                else
                {
                    builder.Description = GetEntry("NoTargetDescription");
                    builder.Title = "";
                    builder.ImageUrl = "";
                }
            }
            else
            {
                builder.Description = GetEntry("TargetDescription", "WHO", Context.Message.Author.Mention, "TARGETS", targets);
            }
            builder.Description = Global.ProcessEmotes(builder.Description, DevGuild).Limit();

            await Context.Channel.SendMessageAsync("", embed: builder.Build());
        }
        
        private async Task<string> GetTargetsAsync(bool RemoveChinoRelated = false, params ulong[] AdditionalRemove)
        {
            string targets = "";
            
            if (Context.Message.MentionedUserIds.Count != 0)
            {
                targets = string.Join(", ", Context.Message.MentionedUserIds.Select(t => "<@" + t + ">"));

                if (RemoveChinoRelated && targets.Contains($"<@{ Context.Client.CurrentUser.Id }>"))
                {
                    targets = targets.Replace($"<@{ Context.Client.CurrentUser.Id }>", "");
                }
                foreach (ulong id in AdditionalRemove)
                {
                    targets = targets.Replace($"<@{ id }>", "");
                }
                targets = targets.Replace($", , ", ", ");
                targets = targets.Replace($"<@{ Context.Message.Author.Id }>", "");
                while ((targets = targets.Replace($", , ", ", ")) != targets)
                    targets = targets.Replace($", , ", ", ");
            }
            if (Context.Message.MentionedRoleIds.Count != 0)
            {
                targets += string.Join(", ", Context.Message.MentionedRoleIds.Select(t => "<@&" + t + ">"));

                List<string> Exceptions = new List<string>();

                List<IGuildUser> users = new List<IGuildUser>();

                if (RemoveChinoRelated)
                    users.Add(await Context.Guild.GetUserAsync(Context.Client.CurrentUser.Id));
                foreach (ulong id in AdditionalRemove)
                {
                    users.Add(await Context.Guild.GetUserAsync(id));
                }
                
                foreach (ulong id in Context.Message.MentionedRoleIds)
                {
                    foreach (IGuildUser user in users)
                    {
                        if (user.RoleIds.Contains(id))
                        {
                            string name = Tools.GetDisplayName(user);
                            if (!Exceptions.Contains(name))
                            {
                                Exceptions.Add(name);
                            }
                        }
                    }
                }

                if (Exceptions.Count > 0)
                {
                    targets += GetGlobalEntry("ImageMentionException", "EXCEPTIONS", string.Join(", ", Exceptions));
                }
            }

            return targets;
        }

        private async Task SendImageAsync(ImageFetcher Fetcher, params string[] Args)
        {
            int Count = 1;
            if (Args.Length > 0)
            {
                if (int.TryParse(Args[Args.Length - 1], out Count))
                {
                    Args = Args.Take(Args.Length - 1).ToArray();
                    
                    if (Count < 1)
                    {
                        await Context.Channel.SendMessageAsync(GetGlobalEntry("NegativeCount"));
                        return;
                    }
                    else if (Count > 10)
                    {
                        Count = 10;
                    }
                }
                else
                {
                    Count = 1;
                }
            }
            
            List<string> ImageUrls = await Fetcher.GetImagesAsync(Args, Global.IsNsfwChannel(Settings, Context.Channel.Id), Count);
            
            if (ImageUrls.Count > 0)
            {
                int[] Sent = new int[Count];

                for (int i = 0; i < Count; i++)
                {
                    if (ImageUrls.Count == i)
                    {
                        await Context.Channel.SendMessageAsync(GetGlobalEntry("NoMoreImagesOfTag"));
                        break;
                    }

                    int Random;
                    if (Count >= ImageUrls.Count)
                    {
                        Random = i;
                    }
                    else
                    {
                        do
                        {
                            Random = Global.Random.Next(0, ImageUrls.Count);
                        }
                        while (Sent.Contains(Random));
                    }
                    
                    string Url = ImageUrls[Random].Replace("\\/", "/");

                    EmbedBuilder Builder = new EmbedBuilder
                    {
                        Color = await Global.GetAverageColorAsync(Url),
                        ImageUrl = Url,
                        Title = GetGlobalEntry("ImgUrlNoLoad", "N", (i + 1).ToString()),
                        Url = Url
                    };
                    

                    await Context.Channel.SendMessageAsync("", embed: Builder.Build());
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync(GetGlobalEntry("NoImagesOfTag", "TAGS", string.Join(" ", Args)));
            }
        }
        private string GetImage(string Type)
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
                    Logger.Log(LogType.Error, ConsoleColor.Red, "NSFW:Fuck", data);
                    return null;
                }
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, ConsoleColor.Red, "NSFW:Fuck", e.ToString());
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
            return url;
        }

    }

    public static class StringExtension
    {
        public static string Limit(this string limit)
        {
            if (limit.Length > 1999)
            {
                return limit.Substring(0, 1996) + "...";
            }

            return limit;
        }
    }
}
