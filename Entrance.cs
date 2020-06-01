using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Chino_chan.Models.Settings;
using Discord;
using Chino_chan.Modules;
using System.IO.Pipes;
using System.IO;
using Newtonsoft.Json;
using Chino_chan.Models.Settings.Language;

namespace Chino_chan
{
    public class Entrance
    {
        [DllImport("kernel32.dll")]
        static extern ErrorModes SetErrorMode(ErrorModes uMode);

        [Flags]
        public enum ErrorModes : uint
        {
            SYSTEM_DEFAULT = 0x0,
            SEM_FAILCRITICALERRORS = 0x0001,
            SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,
            SEM_NOGPFAULTERRORBOX = 0x0002,
            SEM_NOOPENFILEERRORBOX = 0x8000
        }


        static Thread CommandThread;
        static ITextChannel ReloadChannel;
        static ITextChannel UpdateChannel;

        public static bool IsHandler { get; set; }

        static string[] Args;

        static bool FirstRun = true;

        static AnonymousPipeClientStream PipeInput;

        public static CancellationTokenSource CancellationTokenSource;
        public static CancellationToken CancellationToken;
        static TextReader InputStream;
        static Task DataSendTask;
        
        public static void Main(string[] args)
        {
            Console.Title = "Chino-chan";
            InputStream = Console.In;
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken = CancellationTokenSource.Token;

            if (FirstRun)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) SetErrorMode(ErrorModes.SEM_NOGPFAULTERRORBOX);
                Args = args;
                
                if (Args.Length > 1)
                {
                    if (Args[0] == "1")
                    {
                        IsHandler = true;

                        try
                        {
                            PipeInput = new AnonymousPipeClientStream(PipeDirection.In, Args[1]);
                            InputStream = new StreamReader(PipeInput);
                        }
                        catch (Exception e)
                        {
                            File.WriteAllText("AnonymousPipeClient_Exception.json", JsonConvert.SerializeObject(e, Formatting.Indented));
                        }

                        DataSendTask = new Task(() =>
                        {
                            if (IsHandler)
                            {
                                while (!CancellationToken.IsCancellationRequested)
                                {
                                    int ServerCount = 0;
                                    int ConnectedVoiceClients = 0;
                                    if (Global.MusicHandler != null && Global.MusicHandler.Ready)
                                    {
                                        ConnectedVoiceClients = Global.MusicHandler.ConnectedVoiceClients;
                                    }
                                    if (Global.ReadyFired)
                                    {
                                        ServerCount = Global.Client.Guilds.Count;
                                    }

                                    Console.WriteLine("Information:{0}|{1}|{2}|{3}", ServerCount, Global.CommandsUnderExecution, ConnectedVoiceClients, Global.Uptime.TotalMilliseconds);
                                    Thread.Sleep(100);
                                }
                            }
                        }, CancellationToken);

                        DataSendTask.Start();
                    }
                }

                if (!IsHandler)
                {
                    Logger.NewLog += Message =>
                    {
                        if (Message.Type == "Discord") return;

                        lock (Console.Out)
                        {
                            Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.Write("[" + Message.Date.ToString("yyyy/MM/dd hh:mm:ss") + "] ");

                            Console.ForegroundColor = Message.Color;
                            Console.Write("[" + Message.Type.ToString() + "] ");


                            if (Message.Severity != null)
                            {
                                Console.Write("[" + Message.Severity + "] ");
                            }
                            Console.ResetColor();

                            Console.WriteLine(Message.Message);
                            //Console.Write("\n" + new string(' ', Console.WindowWidth) + "\n");
                        }
                    };
                }

                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((s, a) =>
                {
                    Exception e = a.ExceptionObject as Exception;
                    Logger.Log(LogType.Error, ConsoleColor.Red, "Exception", "Message: " + e.Message + "\r\nStack Trace: " + e.StackTrace + "\r\nSource: " + e.Source);
                });

                FirstRun = false;
            }


            if (CommandThread == null || !CommandThread.IsAlive)
            {
                CommandThread = new Thread(ManageConsoleCommands);
                CommandThread.Start();
            }
            Global.Setup();

            Global.Client.Ready += async () =>
            {
                await Global.Client.SetStatusAsync(UserStatus.Online);
                await Global.Client.SetGameAsync(Global.Settings.Game);
                
                await Global.Client.DownloadUsersAsync(Global.Client.Guilds);

                if (ReloadChannel != null)
                {
                    await ReloadChannel.SendMessageAsync(ReloadChannel?.GetSettings().GetLanguage().GetEntry("Global:Reloaded"));
                    ReloadChannel = null;
                }
            };

            Global.StartAsync().Wait();
        }

        private static void ManageConsoleCommands()
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                var input = InputStream.ReadLine();
                if (!string.IsNullOrWhiteSpace(input))
                    HandleCommand(input);
                else Thread.Sleep(10);
            }
        }
        public static void HandleCommand(string Command, ITextChannel Channel = null)
        {
            var Lower = Command.ToLower();
            var Trim = Lower.Trim();
            var Parameter = "";
            var Index = Command.IndexOf(" ");
            
            if (Index >= 0)
            {
                Lower = Lower.Substring(0, Index);
                Trim = Lower.Trim();
                Parameter = Command.Substring(Index).TrimEnd().TrimStart();
            }
            if (Trim == "gc")
            {
                Clean();
                if (Channel != null)
                {
                    Channel.SendMessageAsync(Channel?.GetSettings().GetLanguage().GetEntry("Global:GC_Complete"));
                }
            }
            else if (Trim == "quit" || Trim == "exit" || Trim == "shutdown")
            {
                if (Channel != null)
                {
                    Channel.SendMessageAsync(Channel?.GetSettings().GetLanguage().GetEntry("Global:Shutdown")).Wait();
                }
                Global.Stop();
                Environment.Exit(3);
            }
            else if (Trim == "reload")
            {
                if (Channel != null)
                {
                    Channel.SendMessageAsync(Channel?.GetSettings().GetLanguage().GetEntry("Global:Reload")).Wait();
                }
                Reload(Channel);
            }
            else if (Trim == "restart")
            {
                Global.Stop();
                Process.GetCurrentProcess().Kill();
            }
            else if (Trim == "update")
            {
                Update();
            }
            else if (Trim == "noupdatefound")
            {
                if (UpdateChannel != null)
                {
                    UpdateChannel.SendMessageAsync(UpdateChannel?.GetSettings().GetLanguage().GetEntry("Global:No_Update")).Wait();
                }
            }
            else if (Trim == "updatefound")
            {
                if (UpdateChannel != null)
                {
                    UpdateChannel.SendMessageAsync(UpdateChannel?.GetSettings().GetLanguage().GetEntry("Global:Update_Found")).Wait();
                }
                Global.Stop();
                Process.GetCurrentProcess().Kill();
            }
        }

        private static void Clean()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Logger.Log(LogType.GC, ConsoleColor.Green, null, "Garbage collected!");
        }

        public static void Reload(ITextChannel Channel = null)
        {
            Task.Run(() =>
            {
                Global.Stop();
                Logger.Log(LogType.Info, ConsoleColor.Blue, null, "\r\n\\\\\r\n" +
                    "\\\\ Reloading....\r\n" +
                    "\\\\\r\n");
                ReloadChannel = Channel;
                Main(Args);
            });
        }
        public static void Update(ITextChannel Channel = null)
        {
            UpdateChannel = Channel;
            Console.WriteLine("update");
        }
    }

    public static class SettingsExtension
    {
        public static GuildSetting GetSettings(this Discord.Commands.ICommandContext Context)
        {
            return Global.GuildSettings.GetSettings(Context.Guild != null ? Context.Guild.Id : Context.Message.Author.Id);
        }
        public static GuildSetting GetSettings(this ITextChannel Channel)
        {
            return Global.GuildSettings.GetSettings(Channel.Guild != null ? Channel.Guild.Id : (Channel as IDMChannel).Recipient.Id);
        }
        public static GuildSetting GetSettings(this IGuild Guild)
        {
            return Guild.Id.GetSettings();
        }
        public static GuildSetting GetSettings(this ulong GuildId)
        {
            return Global.GuildSettings.GetSettings(GuildId);
        }
        public static LanguageEntry GetLanguage(this GuildSetting Settings)
        {
            return Global.Languages.GetLanguage(Settings.Language);
        }
    }
}
