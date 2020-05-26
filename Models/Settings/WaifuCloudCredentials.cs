using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.Settings
{
    public struct WaifuCloudCredentials
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public string Server { get; set; }
        public string DefaultPath { get; set; }
    }
}
