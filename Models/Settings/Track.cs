using Chino_chan.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.Settings
{
    public class Track
    {
        public int UserId { get; set; }
        public ulong ServerId { get; set; }
        public ulong ChannelId { get; set; }

        public Mode Mode { get; set; }

        public int Rank { get; set; }
        public int Top { get; set; }
        public double MinPP { get; set; }
        public bool Both { get; set; }

        public bool Identical(Track t)
        {
            return t.UserId == UserId
                && t.ServerId == ServerId
                && t.ChannelId == ChannelId
                && t.Mode == Mode;
        }
    }
}
