using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.Settings.Language
{
    public class CommandEntry
    {
        public string Name { get; set; } = "";

        public string Help { get; set; } = "";
        public Dictionary<string, string> Responses { get; set; } = new Dictionary<string, string>();
    }
}
