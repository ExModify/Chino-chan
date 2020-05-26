using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chino_chan.Models.osuAPI
{
    public struct UserEvent
    {
        [JsonProperty("display_html")]
        public string DisplayHTMl { get; set; }

        [JsonProperty("beatmap_id")]
        public int BeatmapID { get; set; }

        [JsonProperty("beatmapset_id")]
        public int BeatmapSetID { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("epicfactor")]
        public int EpicFactor { get; set; }
    }
}
