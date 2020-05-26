using Chino_chan.Models.Settings.Language;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Modules
{
    public class LanguageHandler
    {
        public Dictionary<string, LanguageEntry> Languages;
        public LanguageHandler()
        {
            Languages = new Dictionary<string, LanguageEntry>();
            Load();
        }

        public void Load()
        {
            Dictionary<string, LanguageEntry> Languages = new Dictionary<string, LanguageEntry>();
            if (Directory.Exists("lang"))
            {
                IEnumerable<string> Files = Directory.EnumerateFiles("lang", "*.json");
                IEnumerator<string> enumerator = Files.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    LanguageEntry le = JsonConvert.DeserializeObject<LanguageEntry>(File.ReadAllText(enumerator.Current, Encoding.UTF8));
                    le.Path = enumerator.Current;

                    if (string.IsNullOrWhiteSpace(le.Id))
                    {
                        continue;
                    }
                    else
                    {
                        if (Languages.ContainsKey(le.Id))
                        {
                            LanguageEntry sle = Languages[le.Id];
                            Logger.Log(LogType.Language, ConsoleColor.Red, "Error", $"Duplication found with id \"{ le.Id }\" " +
                                $"between files: \"{ sle.Path }\" and \"{ le.Path }\"");
                        }
                        else
                        {
                            Languages.Add(le.Id, le);
                            Logger.Log(LogType.Language, ConsoleColor.Cyan, null, $"{ le.Name } ({ le.Id }) language loaded!");
                        }
                    }
                }
                Logger.Log(LogType.Language, ConsoleColor.Cyan, null, $"Loaded { Languages.Count } language files! Be aware that everything is case sensitive!");
            }
            else Directory.CreateDirectory("lang");
            this.Languages = Languages;

            DownloadMustHaveLanguages(false);
        }


        public void UpdateLanguages()
        {
            DownloadMustHaveLanguages(true);
        }

        private void DownloadMustHaveLanguages(bool Redownload = false)
        {
            Dictionary<string, string> MustHaveLanguages = new Dictionary<string, string>()
                {
                    { "Enlgish;en_US", "https://drive.google.com/uc?authuser=0&id=1WgyLE88t137d9Aau-Id_8pgsD0r2qRKz&export=download" },
                    { "Hungarian;hu_HU",    "https://drive.google.com/uc?authuser=0&id=1AHeE_UwwrPOBWGUnx9x7T90tFTIa1_LX&export=download" }
                };
            IEnumerator<string> keys = MustHaveLanguages.Keys.GetEnumerator();
            while (keys.MoveNext())
            {
                string[] split = keys.Current.Split(';');
                if (!Languages.ContainsKey(split[1]) || Redownload)
                {
                    Logger.Log(LogType.Language, ConsoleColor.Cyan, null, $"{ split[0] } ({ split[1] }) file not found! Downloading...");
                    WebClient wc = new WebClient
                    {
                        Encoding = Encoding.UTF8
                    };
                    string Content = wc.DownloadString(MustHaveLanguages[keys.Current]);
                    LanguageEntry le = JsonConvert.DeserializeObject<LanguageEntry>(Content);

                    Logger.Log(LogType.Language, ConsoleColor.Cyan, null, "Saving...");
                    
                    File.WriteAllText($"lang\\{ le.Id }.json", Content);
                    if (Languages.ContainsKey(le.Id))
                         Languages[le.Id] = le;
                    else Languages.Add(le.Id, le);

                    Logger.Log(LogType.Language, ConsoleColor.Cyan, null, $"{ le.Name } ({ le.Id }) language loaded!");
                }
            }
        }
        public LanguageEntry GetLanguageNullDefault(string id)
        {
            if (Languages.ContainsKey(id)) return Languages[id];
            else return null;
        }
        public LanguageEntry GetLanguage(string id)
        {
            if (Languages.ContainsKey(id)) return Languages[id];
            else return Languages["en_US"];
        }
    }
}
