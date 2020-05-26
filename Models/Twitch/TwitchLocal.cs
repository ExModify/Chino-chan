using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.Twitch
{
    public class TwitchLocal
    {
        public List<long> Online { get; set; }
        public List<long> Offline { get; set; }
    }
}
