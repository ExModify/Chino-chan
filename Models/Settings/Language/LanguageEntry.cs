using Chino_chan.Modules;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chino_chan.Models.Settings.Language
{
    public class LanguageEntry
    {
        public string Name { get; set; } = "";
        public string NameTranslated { get; set; } = "";
        public string Id { get; set; } = "";

        public ulong By { get; set; }

        [JsonIgnore]
        public string Path { get; set; } = "";

        public Dictionary<string, string> Global { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Preconditions { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, Dictionary<string, string>> Modules { get; set; } = new Dictionary<string, Dictionary<string, string>>();
        public Dictionary<string, CommandEntry> Commands { get; set; } = new Dictionary<string, CommandEntry>();

        public string GetEntry(string Entry, params string[] Swap)
        {
            string[] Data = Entry.Split(':');
            if (Data.Length > 1)
            {
                if (Data[0] == "Global")
                {
                    if (Global.ContainsKey(Data[1]))
                        return Prepare(Global[Data[1]], Swap);
                }
                else if (Data[0] == "Preconditions")
                {
                    if (Preconditions.ContainsKey(Data[1]))
                        return Prepare(Preconditions[Data[1]], Swap);
                }
                else if(Commands.ContainsKey(Data[0]))
                {
                    CommandEntry ce = Commands[Data[0]];

                    if (Data[1] == "Help")
                    {
                        return Prepare(ce.Help, Swap);
                    }
                    else if (Data[1] == "Name")
                    {
                        return Prepare(ce.Name, Swap);
                    }
                    else if (ce.Responses.ContainsKey(Data[1]))
                    {
                        return Prepare(ce.Responses[Data[1]], Swap);
                    }
                }
                else if (Modules.ContainsKey(Data[0]))
                {
                    if (Modules[Data[0]].ContainsKey(Data[1]))
                        return Prepare(Modules[Data[0]][Data[1]], Swap);
                }
            }

            Logger.Log(LogType.Language, ConsoleColor.Red, "ERROR", $"Entry not found: { Entry }!");

            return Entry;
        }
        public string Prepare(string Original, params string[] Swap)
        {
            if (Swap.Length == 0 || Swap.Length % 2 != 0) return Original;
            string Final = Original;
            for (int i = 0; i < Swap.Length; i += 2)
            {
                Final = Final.Replace("{" + Swap[i].ToUpper() + "}", Swap[i + 1]);
            }
            return Final;
        }
    }
}
