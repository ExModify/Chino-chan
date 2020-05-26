using System.Collections.Generic;

namespace Chino_chan.Models.Settings
{
    public class SayPreferences
    {
        public ulong UserId { get; set; }
        public Dictionary<ulong, ulong> Listening { get; set; } = new Dictionary<ulong, ulong>();
        public bool AutoDel { get; set; } = true;
    }
}
