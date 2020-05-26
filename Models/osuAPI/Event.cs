using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.osuAPI
{
    public struct Event
    {
        [JsonProperty("display_html")]
        public string DisplayHTML { get; set; }

        [JsonProperty("beatmap_id")]
        public int? BeatmapId { get; set; }

        [JsonProperty("beatmapset_id")]
        public int? BeatmapSetId { get; set; }

        [JsonProperty("date")]
        public DateTime? Date { get; set; }

        [JsonProperty("epicfactor")]
        public int? EpicFactor { get; set; }
    }
}
