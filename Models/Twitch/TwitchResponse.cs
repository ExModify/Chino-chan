using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.Twitch
{
    public class TwitchResponse<T>
    {
        [JsonProperty("data")]
        public List<T> Data { get; set; }
    }
}
