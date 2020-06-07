using System.Reflection.PortableExecutable;
using System.Diagnostics;
using Chino_chan.Models.Settings;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Chino_chan.Commands
{
#pragma warning disable CS1998 // no async warning
    public class Owner : ModuleBase
    {
        public GuildSetting Settings
        {
            get
            {
                return Context.GetSettings();
            }
        }

        [Command("game"), Models.Privileges.Owner(), Summary("Changes my game owo")]
        public async Task GameAsync(params string[] Args)
        {
            if (Args.Length == 0)
            {
                if (Context.Client.CurrentUser.Activity.Name != "")
                {
                    await Context.Channel.SendMessageAsync("My game is `" + Context.Client.CurrentUser.Activity.Name + "` owo");
                }
                else
                {
                    await Context.Channel.SendMessageAsync("I'm not playing with anything~");
                }
            }
            else
            {
                await Global.Client.SetGameAsync(string.Join(" ", Args));
                await Context.Channel.SendMessageAsync("My game has been changed to `" + Context.Client.CurrentUser.Activity.Name + "` owo");
            }
        }

        [Command("reload"), Models.Privileges.Owner(), Summary("Reloads me owo")]
        public async Task ReloadAsync(params string[] Args)
        {
            await Context.Channel.SendMessageAsync("Reloading...");
            Entrance.Reload(Context.Channel as ITextChannel);
        }

        [Command("restart"), Models.Privileges.Owner(), Summary("Restarts me owo")]
        public async Task RestartAsync(params string[] Args)
        {
            await Context.Channel.SendMessageAsync("Restarting...");
            Global.Stop();
            Environment.Exit(0);
        }

        [Command("shutdown"), Models.Privileges.Owner(), Summary("Shuts me down :c")]
        public async Task ShutdownAsync(params string[] Args)
        {
            await Context.Channel.SendMessageAsync("Shutting down :c");
            Global.Stop();
            Environment.Exit(exitCode: 3);
        }

        [Command("update"), Models.Privileges.Owner(), Summary("Checks for updates~")]
        public async Task UpdateAsync(params string[] Args)
        {
            Entrance.Update(Context.Channel as ITextChannel);
        }

        [Command("set"), Models.Privileges.Owner(), Summary("Sets an internal variable")]
        public async Task SetAsync(params string[] Args)
        {
            if (Args.Length >= 2)
            {
                object Object = null;
                List<object> Objects = new List<object>();
                List<PropertyInfo> PropertyInfos = new List<PropertyInfo>();

                var Type = typeof(Global);
                Objects.Add(Type);
                var Properties = Type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

                string[] SplitVarName = Args[0].ToLower().Split('.');

                PropertyInfo Final = null;

                for (int i = 0; i < SplitVarName.Length; i++)
                {
                    string Current = SplitVarName[i];

                    PropertyInfo InnerInfo = null;

                    foreach (var Property in Properties)
                    {
                        if (Property.Name.ToLower() == Current)
                        {
                            InnerInfo = Property;
                            break;
                        }
                    }

                    if (InnerInfo == null)
                    {
                        break;
                    }
                    PropertyInfos.Add(InnerInfo);

                    if (i == SplitVarName.Length - 1)
                    {
                        Final = InnerInfo;
                    }
                    else
                    {
                        Properties = InnerInfo.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        Object = InnerInfo.GetValue(Object);
                        Objects.Add(Object);
                    }

                    InnerInfo = null;
                }

                if (Final != null)
                {
                    object Value = string.Join(" ", Args.Skip(1));
                    object obj = null;

                    for (int i = PropertyInfos.Count - 1; i > -1; i--)
                    {
                        obj = Objects[i];
                        try
                        {
                            PropertyInfos[i].SetValue(obj, Convert.ChangeType(Value, PropertyInfos[i].PropertyType));
                            await Context.Channel.SendMessageAsync($"{ PropertyInfos[i].Name } is set to { Value.ToString() }");
                        }
                        catch
                        {
                            await Context.Channel.SendMessageAsync($"Can't convert { Value.GetType().Name } to { PropertyInfos[i].PropertyType.Name } ({ Value.ToString() }) when setting { PropertyInfos[i].Name }");
                            return;
                        }
                        Value = obj;
                    }
                    await Context.Channel.SendMessageAsync($"{ Final.Name }: `{ Final.GetValue(Object) }`");
                    Global.SaveSettings();
                }
                else
                {
                    await Context.Channel.SendMessageAsync("Property wasn't found!");
                }
            }
        }

        [Command("get"), Models.Privileges.Owner(), Summary("Gets an internal variable")]
        public async Task GetAsync(params string[] Args)
        {
            if (Args.Length == 1)
            {
                object Object = null;

                var Type = typeof(Global);
                var Properties = Type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

                string[] SplitVarName = Args[0].ToLower().Split('.');

                PropertyInfo Final = null;

                for (int i = 0; i < SplitVarName.Length; i++)
                {
                    string Current = SplitVarName[i];

                    PropertyInfo InnerInfo = null;

                    foreach (var Property in Properties)
                    {
                        if (Property.Name.ToLower() == Current)
                        {
                            InnerInfo = Property;
                            break;
                        }
                    }

                    if (InnerInfo == null)
                    {
                        break;
                    }

                    if (i == SplitVarName.Length - 1)
                    {
                        Final = InnerInfo;
                    }
                    else
                    {
                        Properties = InnerInfo.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        Object = InnerInfo.GetValue(Object);
                    }

                    InnerInfo = null;
                }

                if (Final != null)
                {
                    var Content = JsonConvert.SerializeObject(Final.GetValue(Object), Formatting.Indented);

                    await Context.Channel.SendMessageAsync($"{ Final.Name }: `{ Content }`");
                }
                else
                {
                    await Context.Channel.SendMessageAsync("Property wasn't found!");
                }
            }
        }

        [Command("lurkserver"), Models.Privileges.Owner(), Summary("Gets info from a guild")]
        public async Task LurkServerAsync(params string[] Args)
        {
            SocketGuild Guild = null;

            string Servername = string.Join(" ", Args).ToLower();
            if (Servername.Length == 0)
            {
                Guild = Global.Client.GetGuild(Context.Guild.Id);
            }
            else
            {
                if (ulong.TryParse(Servername, out ulong ServerId))
                {
                    Guild = Global.Client.GetGuild(ServerId);
                }

                if (Guild == null)
                {
                    IEnumerator<SocketGuild> enumerator = Global.Client.Guilds.GetEnumerator();
                    enumerator.MoveNext();
                    
                    do
                    {
                        if (enumerator.Current.Name.ToLower() == Servername)
                        {
                            Guild = enumerator.Current;
                            break;
                        }
                        enumerator.MoveNext();
                    }
                    while (enumerator.Current != null);
                }
            }
            
            EmbedBuilder Builder = new EmbedBuilder()
            {
                Color = Global.Pink,
                Title = "Server info"
            };
            
            if (Guild == null)
            {
                Builder.Color = Color.Red;
                Builder.Description = "I couldn't find a server named with: " + Servername;
            }
            else
            {
                await Guild.DownloadUsersAsync();

                if (Guild.IconUrl != null)
                    Builder.ThumbnailUrl = Guild.IconUrl + "?size=2048";

                Builder.AddField("Server name + id", Guild.Name + " (" + Guild.Id + ")", true);
                Builder.AddField("User count", Guild.Users.Count, true);
                Builder.AddField("Server was created at", Guild.CreatedAt, true);
                Builder.AddField("Default channel", (Guild.DefaultChannel == null ? "-" : $"{ Guild.DefaultChannel.Name } [{ Guild.DefaultChannel.Id }]"), true);
                Builder.AddField("Guild id", Guild.Id, true);
                
                Builder.AddField("Owner", Tools.GetDisplayName(Guild.Owner) + (Global.IsOwner(Guild.Owner.Id) ? "" : "#" + Guild.Owner.DiscriminatorValue), true);
                if (Guild.TextChannels != null && Guild.TextChannels.Count > 0)
                {
                    Builder.AddField($"Text channels ({ Guild.TextChannels.Count })", string.Join(";", Guild.TextChannels.Select(t => t.Name)).Limit(), true);
                }
                if (Guild.VoiceChannels != null && Guild.VoiceChannels.Count > 0)
                {
                    Builder.AddField($"Voice channels ({ Guild.VoiceChannels.Count })", string.Join(";", Guild.VoiceChannels.Select(t => t.Name)).Limit(), true);
                }
                if (Guild.Roles != null && Guild.Roles.Count > 1)
                {
                    Builder.AddField($"Roles ({ Guild.Roles.Count - 1})", string.Join(";", Guild.Roles.Where(t => t.Name != "@everyone").Select(t => t.Name)).Limit(), true);
                }
                
                IEnumerator<GuildEmote> enumerator = Guild.Emotes.GetEnumerator();

                int step = 10;
                int count = 0;

                for (int i = 0; i < Guild.Emotes.Count; i += step)
                {
                    count++;
                    int length = i + Math.Min(step, Guild.Emotes.Count - i);

                    EmbedFieldBuilder Field = new EmbedFieldBuilder()
                    {
                        Name = "Emotes #" + count,
                        IsInline = true
                    };
                    
                    for (int j = i; j < length; j++)
                    {
                        enumerator.MoveNext();
                        
                        Field.Value += enumerator.Current.ToString();
                        if (j != length - 1)
                        {
                            Field.Value += " ";
                        }
                    }
                    Builder.AddField(Field);
                }
            }
            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("recalc"), Models.Privileges.Owner(), Summary("Recalculated the levels associated to XP")]
        public async Task RecalculateLevelsAsync(params string[] Args)
        {
            await Context.Channel.SendMessageAsync("Recalculating levels...");
            Global.Level.Users.RecalculateLevels(Global.Level);
            await Context.Channel.SendMessageAsync("Levels recalculated!");
        }

        [Command("seterrch"), Models.Privileges.Owner(), Summary("Sets the channel where the errors are going to be sent")]
        public async Task SetErrorChannel(params string[] args)
        {
            Global.Settings.DevServer.ErrorReportChannelId = Context.Channel.Id;
            Global.SaveSettings();
            await ReplyAsync("This channel has been set to the error report channel!");
        }

        [Command("apidomain"), Models.Privileges.Owner(), Summary("Sets the allowed domains for the API")]
        public async Task ApiDomain(params string[] Args)
        {
            if (Args.Length != 0)
            {
                bool Save = false;
                if (Args[0].ToLower() == "remove")
                {
                    for (int i = 1; i < Args.Length; i++)
                    {
                        string lower = Args[i].ToLower();
                        if (Global.Settings.ApiHttpReferrers.Contains(lower))
                        {
                            Save = true;
                            Global.Settings.ApiHttpReferrers.Remove(lower);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < Args.Length; i++)
                    {
                        string lower = Args[i].ToLower();
                        if (!Global.Settings.ApiHttpReferrers.Contains(lower))
                        {
                            Save = true;
                            Global.Settings.ApiHttpReferrers.Add(lower);
                        }
                    }
                }

                if (Save)
                {
                    Global.SaveSettings();
                }
            }

            string Referrers = "There's no allowed referrer!";
            if (Global.Settings.ApiHttpReferrers.Count > 0)
            {
                Referrers = "- " + string.Join("\n- ", Global.Settings.ApiHttpReferrers);
            }
            EmbedBuilder Builder = new EmbedBuilder()
            {
                Title = "Allowed Http Referrers",
                Description = Referrers,
                Color = new Color(255, 192, 203),
                Timestamp = DateTimeOffset.Now
            };

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("redl-langs"), Models.Privileges.Owner(), Summary("Redownloads the must-have language files owo")]
        public async Task RedownloadLangs(params string[] args)
        {
            Global.Languages.UpdateLanguages();
            await Context.Channel.SendMessageAsync("All the messages have been redownloaded and updated owo");
        }
        [Command("addfuck"), Models.Privileges.Owner(), Summary("Adds people to list who can fuck exmo; addfuck id | removefuck id | listfuck")]
        public async Task AddFuck(ulong Id)
        {
            if (!Global.Settings.CanTargetExMo.Contains(Id))
            {
                Global.Settings.CanTargetExMo.Add(Id);
                Global.SaveSettings();

                SocketUser user = Global.Client.GetUser(Id);
                string uname = "Unknwon user";
                if (user != null) uname = user.Username;
                await ReplyAsync($"Added id: { Id } | { uname }");
            }
            else
            {
                await ReplyAsync("Id already in");
            }
        }
        [Command("listfuck"), Models.Privileges.Owner(), Summary("Lists who can fuck exmo; listfuck | removerfuck id | addfuck id")]
        public async Task ListFuck()
        {
            List<string> lines = new List<string>();

            foreach (ulong id in Global.Settings.CanTargetExMo)
            {
                SocketUser user = Global.Client.GetUser(id);
                string line = id + " - ";
                if (user == null)
                {
                    line += "Unknown user";
                }
                else
                {
                    line += user.Username;
                }
                lines.Add(line);
            }

            await ReplyAsync("", embed: new EmbedBuilder()
                .WithTitle("People who can target exmo")
                .WithColor(Color.Magenta)
                .WithDescription(string.Join("\n", lines))
                .Build());
        }
        [Command("removefuck"), Models.Privileges.Owner(), Summary("Remove people from the list who can fuck exmo; removefuck id | addfuck id | listfuck")]
        public async Task RemoveFuck(ulong Id)
        {
            if (Global.Settings.CanTargetExMo.Contains(Id))
            {
                Global.Settings.CanTargetExMo.Remove(Id);
                Global.SaveSettings();

                SocketUser user = Global.Client.GetUser(Id);
                string uname = "Unknwon user";
                if (user != null) uname = user.Username;
                await ReplyAsync($"Removed id: { Id } | { uname }");
            }
            else
            {
                await ReplyAsync("Id not in the list");
            }
        }

        [Command("exec"), Models.Privileges.Owner(), Summary("Executes a PowerShell command")]
        public async Task ExecAsync([Remainder]string Command)
        {
            ProcessStartInfo info = new ProcessStartInfo()
            {
                FileName = "powershell",
                Arguments = Command.Replace("\"", "\\\""),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            Process p = new Process() { StartInfo = info };
            Stopwatch w = Stopwatch.StartNew();
            p.Start();
            p.WaitForExit();
            w.Stop();
            string output = p.StandardOutput.ReadToEnd();
            string msg = $"Command executed in { w.ElapsedMilliseconds } ms!\n```\n{ output }\n```";
            if (msg.Length > 2000)
            {
                MemoryStream ms = new MemoryStream();
                StreamWriter sw = new StreamWriter(ms);
                sw.Write(msg);
                sw.Flush();
                sw.Close();
                await Context.Channel.SendFileAsync(ms, "result.txt");
                ms.Close();
            }
            else
            {
                await Context.Channel.SendMessageAsync(msg);
            }

        }

        [Command("addimg"), Models.Privileges.Owner(), Summary("Add image to a specific type if the website runs on the same server owo")]
        public async Task AddImgAsync(params string[] args)
        {
            string imgFolder = Global.Settings.WebsitePath + "images/" + args[0];

            if (!Directory.Exists(imgFolder))
                Directory.CreateDirectory(imgFolder);

            string[] files = Directory.EnumerateFiles(imgFolder).Select(t => Path.GetFileName(t)).ToArray();
            string newFilename = args[0] + "";
            foreach (string file in files)
            {
                if (file.Contains("_"))
                {
                    newFilename += "_";
                }
            }
            newFilename += (files.Length + 1).ToString();

            HttpClient c = new HttpClient();
            Stream s = await c.GetStreamAsync(args[1]);
            System.Drawing.Image img = System.Drawing.Image.FromStream(s);

            var jpg = System.Drawing.Imaging.ImageFormat.Jpeg;
            var gif = System.Drawing.Imaging.ImageFormat.Gif;
            var png = System.Drawing.Imaging.ImageFormat.Png;

            if (img.RawFormat == jpg)
            {
                newFilename += ".jpg";
            }
            else if (img.RawFormat == gif)
            {
                newFilename += ".gif";
            }
            else if (img.RawFormat == png)
            {
                newFilename += ".png";
            }
            
            img.Save(newFilename);

            await Context.Channel.SendMessageAsync("New file added!");

        }
        /*
        [Command("test"), Models.Privileges.Owner(), Summary("owo")]
        public async Task TestCommandAsync(params string[] args)
        {

        }
        */
    }
}