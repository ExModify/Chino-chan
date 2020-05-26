using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chino_chan.Models.osuAPI
{
    public struct Match
    {
        [JsonProperty("match_id")]
        public int ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("start_time")]
        public DateTime Started { get; set; }

        [JsonProperty("end_time")]
        public DateTime? Ended { get; set; }
    }
}
