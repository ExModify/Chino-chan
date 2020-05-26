using Chino_chan.Modules;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.API
{
    public class Info
    {
        public CPUInfo CPU { get; set; }
        public OsInfo OS { get; set; }
        public MemInfo RAM { get; set; }
        public List<VideoCardInfo.CardInfo> VideoCard { get; set; }
        
        public TimeSpan Uptime { get; set; }

        public int UserCount { get; set; }

        public long CurrentMemoryUsage { get; set; }
        public string DiscordVersion { get; set; }

        public int CommandsUnderExecution { get => Global.CommandsUnderExecution; }
        public int ActiveVoiceConnections { get => Global.ReadyFired ? Global.MusicHandler.ConnectedVoiceClients : 0; }

        public int GuildCount { get; set; }
        
        public Info()
        {
            CPU = Global.SysInfo.CPU;
            OS = Global.SysInfo.OS;
            RAM = Global.SysInfo.MemInfo;
            VideoCard = Global.SysInfo.VideoCardInfo.VideoCards;

            List<ulong> userIds = new List<ulong>();
            foreach (SocketGuild guild in Global.Client.Guilds)
            {
                foreach (SocketGuildUser user in guild.Users)
                {
                    if (!userIds.Contains(user.Id))
                        userIds.Add(user.Id);
                }
            }
            UserCount = userIds.Count;

            Process CurrentProcess = Process.GetCurrentProcess();
            CurrentMemoryUsage = CurrentProcess.NonpagedSystemMemorySize64 + CurrentProcess.PagedMemorySize64;
            DiscordVersion = DiscordConfig.Version;
            Uptime = Global.Uptime;

            GuildCount = Global.Client.Guilds.Count;
        }
    }
}
