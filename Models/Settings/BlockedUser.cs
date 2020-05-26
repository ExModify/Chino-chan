using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.Settings
{
    public class BlockedUser
    {
        public ulong Who { get; set; }
        public ulong Id { get; set; }

        public string Reason { get; set; }
    }
}
