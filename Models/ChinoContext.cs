using Chino_chan.Models.Settings;
using Chino_chan.Models.Settings.Language;
using Chino_chan.Modules;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models
{
    public abstract class ChinoContext : ModuleBase
    {
        public GuildSetting Settings
        {
            get
            {
                return Context.GetSettings();
            }
        }
        public LanguageEntry Language
        {
            get
            {
                return Settings.GetLanguage();
            }
        }

        [DontInject]
        public string CommandName { get; set; }

        public string GetHelp(string CommandName, params string[] Swap)
        {
            Swap = new List<string>(Swap)
            {
                "PREFIX", Settings.Prefix,
            }.ToArray();
            return Language.GetEntry(CommandName + ":Help", Swap);
        }

        public string GetEntry(string Entry, params string[] Swap)
        {
            if (CommandName == null)
            {
                int Position = 0;
                if (!Context.Message.HasMentionPrefix(Context.Client.CurrentUser, ref Position))
                    Context.Message.HasStringPrefix(Settings.Prefix, ref Position);

                CommandName = Context.Message.Content.Substring(Position).Split(' ')[0].ToLower();
                CommandInfo info = Global.CommandService.Commands.First(t => t.Name.ToLower() == CommandName || (t.Aliases.Count > 0 && t.Aliases.Contains(CommandName)));
                CommandName = (info.Module.Group ?? "").ToLower() + info.Name.ToLower();
            }
            Swap = new List<string>(Swap)
            {
                "PREFIX", Settings.Prefix,
            }.ToArray();
            return Language.GetEntry(CommandName + ":" + Entry, Swap);
        }
        public string GetGlobalEntry(string Entry, params string[] Swap)
        {
            Swap = new List<string>(Swap)
            {
                "PREFIX", Settings.Prefix,
            }.ToArray();
            return Language.GetEntry("Global:" + Entry, Swap);
        }
    }
}
