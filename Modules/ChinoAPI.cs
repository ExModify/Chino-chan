using Chino_chan.Models.API;
using Chino_chan.Models.Settings.Language;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Chino_chan.Modules
{
    public class ChinoAPI
    {
        public bool Available
        {
            get
            {
                return Listener?.IsListening ?? false;
            }
        }

        HttpListener Listener;
        public ChinoAPI()
        {
            if (Global.Settings.APIPort < 1 && Global.Settings.APIPort > 65535)
            {
                Logger.Log(LogType.API, ConsoleColor.Red, "Error", "Wrong API settings were given, so this part of the bot is disabled!");
                return;
            }

            int Port = Global.Settings.APIPort;

            Port = FindAvailablePort(Port);
            if (Port == -1)
            {
                Logger.Log(LogType.API, ConsoleColor.Red, "Error", "No available port was found!");
                return;
            }

            Listener = new HttpListener();
            Listener.Prefixes.Add($"http://*:{ Port }/");
            try
            {
                Listener.Start();
                Logger.Log(LogType.API, ConsoleColor.Magenta, "Status", "Listening on port: " + Port);
                
                new Thread(() =>
                {
                    while (Listener.IsListening && !Entrance.CancellationToken.IsCancellationRequested)
                    {
                        HttpListenerContext Context = Listener.GetContext();
                        if (Context.Request.RawUrl != "/favicon.ico")
                        {
                            lock (Context)
                            {
                                HandleRequest(Context);
                            }
                        }
                    }
                }).Start();
                
            }
            catch
            {
                Logger.Log(LogType.API, ConsoleColor.Red, "Error", "Please run me as Administrator so I can run the API owo");
            }
        }

        int FindAvailablePort(int StartPort)
        {
            IPGlobalProperties Props = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] Infos = Props.GetActiveTcpConnections();
            List<int> Ips = Infos.Select(t => t.LocalEndPoint.Port).ToList();

            int Port = StartPort;

            while (Ips.Contains(Port))
            {
                Port++;
                if (Port > 65535)
                {
                    Port = 1;
                }
                if (StartPort == Port)
                {
                    Port = -1;
                    break;
                }
            }

            return Port;
        }

        void HandleRequest(HttpListenerContext Context)
        {
            Context.Response.AddHeader("Access-Control-Allow-Origin", "*");
            Context.Response.AddHeader("Server", "Chino-chan/1.0");
            Context.Response.AddHeader("Content-Type", "text/plain");
            /*
            if (InvalidReferrer(Context.Request.UrlReferrer))
            {
                string referrer = "Didn't provide referrer!";
                if (Context.Request.UrlReferrer != null)
                    referrer = Context.Request.UrlReferrer.ToString();
                
                byte[] data = Encoding.UTF8.GetBytes("You are unauthorized to perform this action!");
                Context.Response.OutputStream.Write(data, 0, data.Length);
                Context.Response.StatusCode = 401;
                Context.Response.Close();
                return;
            }

            */
            byte[] Data = null;
            NameValueCollection Query = HttpUtility.ParseQueryString(Context.Request.Url.Query);

            string AbsolutePath = "";
            if (Context.Request.Url.AbsolutePath.Length > 0)
            {
                AbsolutePath = Context.Request.Url.AbsolutePath.Substring(1).ToLower();
            }

            switch (AbsolutePath)
            {
                case "ping":
                    Data = Encoding.UTF8.GetBytes("pong");
                    break;
                case "commands":
                    Data = Encoding.UTF8.GetBytes(GetModules());
                    break;
                case "getavatar":
                    Data = Encoding.UTF8.GetBytes(GetAvatar(Query));
                    break;
                case "getico":
                    Data = Encoding.UTF8.GetBytes(GetFavicon());
                    break;
                case "getuser":
                    Data = Encoding.UTF8.GetBytes(GetUser(Query));
                    break;
                case "getinfo":
                    Data = Encoding.UTF8.GetBytes(GetInfo());
                    break;
                case "getguilds":
                    Data = Encoding.UTF8.GetBytes(GetAvailableGuilds(Query));
                    break;
                default:
                    Data = Encoding.UTF8.GetBytes("");
                    break;
            }

            if (Data?.Length == 0)
            {
                Data = Encoding.UTF8.GetBytes("{ \"error\": \"??\" }");
            }

            try
            {
                Context.Response.OutputStream.Write(Data, 0, Data.Length);
                Context.Response.StatusCode = 200;
                Context.Response.Close();
            }
            catch
            {

            }
        }

        bool InvalidReferrer(Uri uri)
        {
            if (uri == null) return true;


            string domain = uri.ToString();
            string[] split = domain.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            int index = 0;
            switch (split[0])
            {
                case "https:":
                case "http:":
                case "www.":
                    index = 1;
                    break;
            }
            if (index == 1)
            {
                if (split[1] == "www.")
                {
                    index = 2;
                }
            }

            if (split.Length <= index) return true;

            return !Global.Settings.ApiHttpReferrers.Contains(split[index].ToLower());
        }

        string GetModules()
        {
            List<string> ModuleNames = new List<string>();
            List<Module> Modules = new List<Module>();
            LanguageEntry lang = Global.Languages.GetLanguage("en_US");

            foreach (var Module in Global.CommandService.Modules)
            {
                Module mod = new Module()
                {
                    name = Module.Name,
                    commands = new List<Command>()
                };

                int Index = -1;
                if ((Index = ModuleNames.IndexOf(Module.Name)) > -1)
                {
                    mod = Modules[Index];
                }
                mod.commands.AddRange(Module.Commands.Select(t =>
                {
                    string Name = t.Name;

                    if (!string.IsNullOrWhiteSpace(Module.Group))
                        Name = Module.Group + (Name.Length > 0 ? " " : "") + Name;

                    string Help = t.Summary;
                    if (lang.Commands.ContainsKey(t.Name))
                    {
                        Help = lang.Prepare(lang.Commands[t.Name].Help, "PREFIX", ";");
                    }


                    return new Command()
                    {
                        name = Name,
                        help = Help
                    };
                }));
                if (Index > -1)
                    Modules[Index] = mod;
                else
                    Modules.Add(mod);

                if (!ModuleNames.Contains(mod.name))
                    ModuleNames.Add(mod.name);
            }
            
            return JsonConvert.SerializeObject(Modules);
        }
        string GetAvatar(NameValueCollection Query)
        {
            IUser User = Global.Client.CurrentUser;
            if (Query.AllKeys.Contains("user"))
            {
                IUser Usr = null;
                string QueryUser = Query.Get("user");
                if (ulong.TryParse(QueryUser, out ulong Id))
                {
                    Usr = Global.Client.GetUser(Id);
                    if (Usr == null)
                    {
                        foreach (IGuild guild in Global.Client.Guilds)
                        {
                            IUser u = guild.GetUserAsync(Id).Result;
                            if (u != null)
                            {
                                Usr = u;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    Regex Regex = new Regex("(.*)#(\\d\\d\\d\\d)");
                    if (Regex.Match(QueryUser) is Match Match)
                    {
                        string Username = Match.Groups[1].Value.ToLower();
                        string Discriminator = Match.Groups[2].Value.ToLower();

                        Usr = Global.GetUsers().Find(t => t.Username.ToLower() == Username && t.Discriminator == Discriminator);
                    }
                }

                if (Usr != null)
                    User = Usr;
            }
            ushort size = 128;
            if (Query.Get("size") is string SizeString)
            {
                if (ushort.TryParse(SizeString, out ushort Size))
                {
                    if (Size > 0 && Size < 2049)
                    {
                        size = Size;
                    }
                }
            }
            return User.GetAvatarUrl(size: size);
        }
        string GetFavicon()
        {
            string AvatarUrl = Global.Client.CurrentUser.GetAvatarUrl(size: 16);
            Uri AvatarUri = new Uri(AvatarUrl);
            string Filename = Path.GetFileNameWithoutExtension(AvatarUri.LocalPath) + ".ico";

            if (!File.Exists("favicon/" + Filename))
            {
                if (!Directory.Exists("favicon")) Directory.CreateDirectory("favicon");

                System.Drawing.Image Image = System.Drawing.Image.FromStream(new HttpClient().GetStreamAsync(AvatarUrl).Result);
                Image.Save("favicon/" + Filename, System.Drawing.Imaging.ImageFormat.Icon);
                Image.Dispose();
            }

            MemoryStream ms = new MemoryStream();
            FileStream fs = new FileStream("favicon/" + Filename, FileMode.Open);

            fs.CopyTo(ms);
            fs.Close();
            fs.Dispose();

            string Data = Convert.ToBase64String(ms.ToArray());
            ms.Close();
            ms.Dispose();

            return Data;
        }
        string GetUser(NameValueCollection Query)
        {
            string Message;
            if (!ulong.TryParse(Query.Get("uid"), out ulong userId))
            {
                Message = "{ \"error\": \"Please specify the user id with the uid query\" }";
            }
            else
            {
                IUser User = Global.Client.GetUser(userId);
                if (User == null)
                {
                    foreach (IGuild guild in Global.Client.Guilds)
                    {
                        IGuildUser u = guild.GetUserAsync(userId).Result;
                        if (u != null)
                        {
                            User = u;
                            break;
                        }
                    }
                }
                if (User == null)
                {
                    Message = "{ \"error\": \"User was not found!\" }";
                }
                else
                {
                    try
                    {
                        Message = JsonConvert.SerializeObject(new User(User), Formatting.Indented, new JsonSerializerSettings()
                        {
                            ContractResolver = new AvoidProperties()
                        });
                    }
                    catch (Exception ex)
                    {
                        Message = JsonConvert.SerializeObject(ex, Formatting.Indented);
                    }
                }
            }

            return Message;
        }
        string GetInfo()
        {
            return JsonConvert.SerializeObject(new Info(), Formatting.Indented);
        }
        string GetAvailableGuilds(NameValueCollection Query)
        {
            string message;
            if (Query.AllKeys.Contains("uid") && ulong.TryParse(Query["uid"], out ulong uid))
            {
                List<Guild> guilds = new List<Guild>();
                try
                {
                    if (Global.IsOwner(uid))
                    {
                        guilds = Global.Client.Guilds.Select(t => new Guild(t, uid)).ToList();
                    }
                    else
                    {
                        guilds = Global.Client.Guilds.Where(t => t.GetUser(uid) != null).Select(t => new Guild(t, uid)).ToList();
                    }
                    message = JsonConvert.SerializeObject(guilds, Formatting.Indented);
                }
                catch (Exception e)
                {
                    message = e.ToString();
                }// better be safe
                
            }
            else 
            {
                message = "{ \"error\": \"Please specify the user id with the uid query\" }";
            }
            return message;
        }
    }
    public class AvoidProperties : DefaultContractResolver
    {
        private List<string> Avoid = new List<string>(new string[] { "MutualGuilds", "Recipient", "DMChannel", "Guild" });

        protected override JsonProperty CreateProperty(System.Reflection.MemberInfo Member, MemberSerialization MemberSerialization)
        {
            JsonProperty Property = base.CreateProperty(Member, MemberSerialization);
            if (Avoid.Contains(Property.PropertyName))
            {
                Property.ShouldSerialize = i => false;
            }

            return Property;
        }
    }
}
