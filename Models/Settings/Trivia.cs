using System.Collections.Generic;

namespace Chino_chan.Models.Settings
{
    public class Trivia
    {
        public ulong Server { get; set; }
        public List<string> Responses { get; set; }
        public bool NeedPrefix { get; set; }
    }
}
