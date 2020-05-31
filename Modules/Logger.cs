using Chino_chan.Models.osuAPI;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chino_chan.Modules
{
    public struct LogMessage
    {
        public string Type { get; set; }
        public string Severity { get; set; }
        public string Message { get; set; }
        public ConsoleColor Color { get; set; }
        public DateTime Date { get; set; }

        public LogMessage(LogType Type, ConsoleColor Color, string Severity, string Message)
        {
            this.Type = Type.ToString();
            this.Color = Color;
            this.Severity = Severity;
            this.Message = Message;

            Date = DateTime.Now;
        }

        public override string ToString()
        {
            string str = $"[{ Date:yyyy/MM/dd hh:mm:ss}] [{ Type ?? "" }] ";

            if (Severity != null)
            {
                str += "[" + Severity + "] ";
            }

            return str + Message;
        }
    }
    public enum LogType 
    {
        Debug,
        Discord,
        osuApi,
        Language,
        Settings,
        Updater,
        Info,
        Error,
        Commands,
        GC,
        SysInfo,
        API,
        Images,
        Sankaku,
        Imgur,
        GoogleDrive,
        ExternalModules,
        YouTubeAPI,
        Remote,
        Twitch,
        SoundCloud,
        Music,
        Poll,
        WelcomeBanner,
        SubTag,
        BeatmapManager,
        WebSocket,
        osuTracker
    }
    public class Logger
    {
        public static event Action<LogMessage> NewLog;

        private static string Filename { get; set; } = "";

        private static FileStream fs;
        private static List<LogMessage> messages;
        private static Task sendTask;

        public static void Setup()
        {
            messages = new List<LogMessage>();
            if (!Entrance.IsHandler)
            {
                if (!Directory.Exists("log"))
                {
                    Directory.CreateDirectory("log");
                }
                IEnumerable<string> Filenames = Directory.EnumerateFiles("log", "log.*.log").Select(t => t.ToLower());

                for (int i = 0; i < int.MaxValue; i++)
                {
                    Filename = "log/log." + i + ".log";

                    if (!Filenames.Contains(Filename))
                    {
                        break;
                    }
                }

                fs = new FileStream(Filename, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
            }
        }

        private static void Log(LogMessage Message)
        {
            byte[] data = Encoding.UTF8.GetBytes(Message.ToString() + "\r\n");
            fs.Write(data, 0, data.Length);
            fs.Flush();

            NewLog?.Invoke(Message);
        }

        public static void Log(LogType Type, ConsoleColor Color, string Severity, string Message)
        {
            LogMessage msg = new LogMessage(Type, Color, Severity, Message);
            messages.Add(msg);
            if (Entrance.IsHandler)
            {
                Console.WriteLine(JsonConvert.SerializeObject(msg));
                NewLog?.Invoke(msg);
            }
            else
            {
                Log(msg);
            }
        }

        public static void StartDiscordLogging(DiscordSocketClient Client)
        {
            SocketGuild dev = Client.GetGuild(Global.Settings.DevServer.Id);
            ITextChannel logChannel = dev.GetTextChannel(Global.Settings.DevServer.LogChannelId);
            sendTask = new Task(async () =>
            {
                while (!Entrance.CancellationTokenSource.IsCancellationRequested)
                {
                    for (int i = 0; i < messages.Count; i++)
                    {
                        LogMessage log = messages[i];

                        if (Client.ConnectionState == ConnectionState.Connected && log.Type != LogType.Discord.ToString())
                        {
                            string msg = $"```css\n{ log }```";
                            await Global.SendMessageAsync(msg, logChannel);
                        }

                        messages.RemoveAt(i);
                        i--;
                    }
                    await Task.Delay(10);
                }
            });
            sendTask.Start();
        }
    }
}
