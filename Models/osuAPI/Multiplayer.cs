using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chino_chan.Models.osuAPI
{
    public struct Multiplayer
    {
        [JsonProperty("match")]
        public Match Match { get; set; }

        [JsonProperty("games")]
        public Game[] Games { get; set; }
    }
}
