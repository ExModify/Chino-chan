using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Chino_chan.Models.Settings;
using System.IO;
using Chino_chan.Modules;

namespace Chino_chan
{
    public class GuildSettings
    {
        private string GuildSettingsPath = "GuildSettings";
        
        
        public Dictionary<ulong, GuildSetting> Settings { get; private set; }

        readonly FileSystemWatcher Watcher;

        public GuildSettings()
        {
            Settings = new Dictionary<ulong, GuildSetting>();
            Watcher = new FileSystemWatcher("Data", "GuildSettings.json");

            Watcher.Changed += (sender, args) =>
            {
                Watcher.EnableRaisingEvents = false;
                Settings = SaveManager.LoadSettings<Dictionary<ulong, GuildSetting>>(GuildSettingsPath);
                Watcher.EnableRaisingEvents = true;
            };
            Load();
        }

        public void Save()
        {
            Watcher.EnableRaisingEvents = false;
            SaveManager.SaveData(GuildSettingsPath, Settings);
            Watcher.EnableRaisingEvents = true;
        }
        private void Load()
        {
            Settings = SaveManager.LoadSettings<Dictionary<ulong, GuildSetting>>(GuildSettingsPath);

            if (Settings.Count != 0)
            {
                Logger.Log(LogType.Settings, ConsoleColor.Green, "Settings Loader", "Guild settings loaded from normal configuration.");
            }
            else
            {
                Logger.Log(LogType.Settings, ConsoleColor.Yellow, "Settings Loader", "Empty guild settings configuration file!");
            }

            foreach (KeyValuePair<ulong, GuildSetting> setting in Settings)
            {
                if (setting.Value.ReactionAssignChannel != 0 && !setting.Value.ReactionAssignChannels.Contains(setting.Value.ReactionAssignChannel))
                {
                    Settings[setting.Key].ReactionAssignChannels.Add(setting.Value.ReactionAssignChannel);
                }
            }
            Save();
        }

        public GuildSetting GetSettings(ulong GuildId)
        {
            if (!Settings.TryGetValue(GuildId, out GuildSetting Setting))
            {
                Setting = new GuildSetting()
                {
                    GuildId = GuildId
                };
                Settings.Add(GuildId, Setting);
                Save();
            }
            return Setting;
        }
        public void Modify(ulong GuildId, Action<GuildSetting> Modification)
        {
            var Setting = GetSettings(GuildId);
            Modification?.Invoke(Setting);
            Settings[GuildId] = Setting;
            Save();
        }
        public void Update(GuildSetting Setting)
        {
            Settings[Setting.GuildId] = Setting;
            Save();
        }
    }
}
