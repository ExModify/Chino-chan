using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.Settings.Credentials
{
    public struct TwitchCredentials
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string BotToken { get; set; }
    }
}
