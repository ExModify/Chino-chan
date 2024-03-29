﻿using Chino_chan.Image;
using Chino_chan.Models;
using Chino_chan.Models.Settings;
using Chino_chan.Modules;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Chino_chan.Commands
{
    public class NSFW : ChinoContext
    {
        private IGuild DevGuild
        {
            get
            {
                return Global.Client.GetGuild(Global.Settings.DevServer.Id);
            }
        }
        public Color EmbedColor = Color.Magenta;

        [Command("nhentai"), Summary("Search doujinshis [18+, nsfw channels only]\nAvailable prefixes: character, tag, group, artist, parody, random\nExamples:\n- artist: nhentai artist shiratama\n- search: nhentai chino-chan to ecchi")]
        public async Task NHentaiAsync(params string[] Args)
        {
            if (!Global.IsNsfwChannel(Settings, Context.Channel.Id))
            {
                await Context.Channel.SendMessageAsync(GetGlobalEntry("OnlyNSFWChannels"));
                return;
            }

            string Base = "https://nhentai.net/";
            string Endpoint = Base;

            bool Random = false;

            if (Args.Length > 0)
            {
                string Type = Args[0].ToLower();
                bool Search = false;

                string[] nhentaiTypes = new string[]
                {
                    "character", "tag", "parody", "group", "artist", "random"
                };

                if (nhentaiTypes.Contains(Type))
                {
                    Endpoint += Type + "/";

                    Random = Type == "random";
                }
                else
                {
                    Search = true;
                    Endpoint += "search/?q=";
                }

                if (Endpoint != Base && !Random && !Search)
                {
                    Args = Args.Skip(1).ToArray();
                }
            }

            if (!Random)
                Endpoint += string.Join("+", Args);

            HttpWebRequest Request = WebRequest.Create(Endpoint) as HttpWebRequest;

            if (Random)
            {
                Request.AllowAutoRedirect = true;
            }

            HttpWebResponse Response = null;
            try
            {
                Response = await Request.GetResponseAsync() as HttpWebResponse;
            }
            catch
            {
                await Context.Channel.SendMessageAsync(GetEntry("Error"));
                return;
            }

            StreamReader Reader = new StreamReader(Response.GetResponseStream());
            string Content = Reader.ReadToEnd();

            if (!Random)
            {
                Reader.Close();
                Response.Close();
                Request.Abort();

                Regex PageRegex = new Regex(@"<a href=\""[^;]*;page=(.*?)\"" class=\""last\"">");

                if (PageRegex.IsMatch(Content))
                {
                    int Page = int.Parse(PageRegex.Match(Content).Groups[1].Value);

                    Page = Global.Random.Next(1, Page + 1);

                    if (Page != 1)
                    {
                        Request = WebRequest.Create(Endpoint + "?page=" + Page) as HttpWebRequest;
                        Response = await Request.GetResponseAsync() as HttpWebResponse;

                        Reader = new StreamReader(Response.GetResponseStream());
                        Content = Reader.ReadToEnd();
                    }
                }

                if (!Content.Contains("div class=\"gallery\""))
                {
                    await Context.Channel.SendMessageAsync(GetEntry("NoDoujinshi"));

                    Reader.Dispose();
                    Response.Close();
                    Request.Abort();

                    return;
                }

                MatchCollection Collection = new Regex(@"<a href=""\/(.*)"" class=""cover""").Matches(Content);

                Match RandomMatch = Collection[Global.Random.Next(0, Collection.Count)];

                Reader.Close();
                Response.Close();
                Request.Abort();

                Request = WebRequest.Create(Base + RandomMatch.Groups[1]) as HttpWebRequest;
                Response = await Request.GetResponseAsync() as HttpWebResponse;

                Reader = new StreamReader(Response.GetResponseStream());
                Content = Reader.ReadToEnd();
            }

            string Url = Response.ResponseUri.ToString();

            string spans = Content.Split("<h1 class=\"title\"><span class=\"before\">")[1].Split("</span></h1>")[0];
            string[] splitSpans = spans.Split("</span><span class=\"pretty\">");
            string Title = splitSpans[0];
            splitSpans = splitSpans[1].Split("</span><span class=\"after\">");
            Title += splitSpans[0] + splitSpans[1];

            string Thumbnail = Regex.Match(Content, @"https:\/\/t\.nhentai\.net\/galleries\/\d*\/cover\.[^""]*").Value;

            Regex TagsRegex = new Regex(@"<a href=""[^""]*"" class=""tag\s[^""]*""><span class=""name"">([^""]*)<\/span>");
            MatchCollection TagCollection = TagsRegex.Matches(Content);
            List<string> Tags = new List<string>(TagCollection.Cast<Match>().Select(t => t.Groups[1].Value));

            EmbedBuilder Builder = new EmbedBuilder()
            {
                Color = await Global.GetAverageColorAsync(Thumbnail),
                Title = WebUtility.HtmlDecode(Title),
                Url = Url,
                ImageUrl = Thumbnail,
            };
            Builder.AddField("- " + GetEntry("Tags") + " -", string.Join(", ", Tags));

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("lewdneko"), Summary("Random lewd image from https://nekos.life owo")]
        public async Task LewdNekoAsync(params string[] Args)
        {
            if (!Global.IsNsfwChannel(Settings, Context.Channel.Id))
            {
                await Context.Channel.SendMessageAsync(GetGlobalEntry("OnlyNSFWChannels"));
                return;
            }

            string[] Types = new string[]
            {
                "lewdkemo", "erokemo", "hololewd", "nsfw_neko_gif"
            };

            string Url = await Global.GetNekosLifeUrlAsync(Types[Global.Random.Next(0, Types.Length)]);

            EmbedBuilder Builder = new EmbedBuilder()
            {
                Title = GetGlobalEntry("ImgUrlNoLoad", "N", "1"),
                Url = Url,
                ImageUrl = Url,
                Color = await Global.GetAverageColorAsync(Url)
            };

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("keta"), Summary("Random keta art from https://nekos.life owo")]
        public async Task KetaAsync(params string[] Args)
        {
            if (!Global.IsNsfwChannel(Settings, Context.Channel.Id))
            {
                await Context.Channel.SendMessageAsync(GetGlobalEntry("OnlyNSFWChannels"));
                return;
            }

            string Url = await Global.GetNekosLifeUrlAsync("keta");


            EmbedBuilder Builder = new EmbedBuilder()
            {
                Title = GetGlobalEntry("ImgUrlNoLoad", "N", "1"),
                Url = Url,
                ImageUrl = Url,
                Color = await Global.GetAverageColorAsync(Url)
            };

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("fuck"), Alias("fucc"), Summary("Fucks someones by mentioning them - l-lewd >.>")]
        public async Task FuckAsync(params string[] Args)
        {
            if (!Global.IsNsfwChannel(Settings, Context.Channel.Id))
            {
                await Context.Channel.SendMessageAsync(GetEntry("OnlyNSFW"));
                return;
            }

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
                url = Global.GetImageFromCDN(Settings.AllowLoliContent ? "fuck_loli" : "fuck", Settings);
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
        [Command("spank"), Summary("Spanks someone by mentioning them *visible pain*")]
        public async Task SpankAsync(params string[] Args)
        {
            if (!Global.IsNsfwChannel(Settings, Context.Channel.Id))
            {
                await Context.Channel.SendMessageAsync(GetEntry("OnlyNSFW"));
                return;
            }

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
                url = Global.GetImageFromCDN("spank", Settings);
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


        [Command("suck"), Alias("succ"), Summary("Sucks people by mentioning them - >///>")]
        public async Task SuckAsync(params string[] Args)
        {
            if (!Global.IsNsfwChannel(Settings, Context.Channel.Id))
            {
                await Context.Channel.SendMessageAsync(GetEntry("OnlyNSFW"));
                return;
            }

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
                url = Global.GetImageFromCDN("suck", Settings);
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
                    builder.Title = "";
                    builder.ImageUrl = "";
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


    }
}
