using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Chino_chan.Models.Settings;
using System.IO;

namespace Chino_chan
{
    public class GuildSettings
    {
        private readonly object SaveLock = new object();
        private string GuildSettingsPath
        {
            get
            {
                return "Data/GuildSettings.json";
            }
        }
        
        public Dictionary<ulong, GuildSetting> Settings { get; private set; }

        readonly FileSystemWatcher Watcher;

        public GuildSettings()
        {
            Settings = new Dictionary<ulong, GuildSetting>();
            Watcher = new FileSystemWatcher("Data", "GuildSettings.json");

            Watcher.Changed += (sender, args) =>
            {
                Watcher.EnableRaisingEvents = false;
                Settings = JsonConvert.DeserializeObject<Dictionary<ulong, GuildSetting>>(File.ReadAllText(GuildSettingsPath));
                Watcher.EnableRaisingEvents = true;
            };
            Load();
        }

        public void Save()
        {
            Watcher.EnableRaisingEvents = false;
            lock(SaveLock)
            {
                if (File.Exists(GuildSettingsPath))
                {
                    File.Delete(GuildSettingsPath);
                }
                if (!Directory.Exists("Data"))
                {
                    Directory.CreateDirectory("Data");
                }
                File.WriteAllText(GuildSettingsPath, JsonConvert.SerializeObject(Settings, Formatting.Indented));
            }
            Watcher.EnableRaisingEvents = true;
        }
        private void Load()
        {
            if (File.Exists(GuildSettingsPath))
            {
                Settings = JsonConvert.DeserializeObject<Dictionary<ulong, GuildSetting>>(File.ReadAllText(GuildSettingsPath));
                
                foreach (KeyValuePair<ulong, GuildSetting> setting in Settings)
                {
                    if (setting.Value.ReactionAssignChannel != 0 && !setting.Value.ReactionAssignChannels.Contains(setting.Value.ReactionAssignChannel))
                    {
                        Settings[setting.Key].ReactionAssignChannels.Add(setting.Value.ReactionAssignChannel);
                    }
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
