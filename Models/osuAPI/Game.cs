using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chino_chan.Models.osuAPI
{
    public struct Game
    {
        [JsonProperty("game_id")]
        public int ID { get; set; }

        [JsonProperty("start_time")]
        public DateTime Started { get; set; }

        [JsonProperty("end_time")]
        public DateTime? Ended { get; set; }
        
        [JsonProperty("beatmap_id")]
        public int BeatmapID { get; set; }

        [JsonProperty("play_mode")]
        public int Playmode { get; set; }

        [JsonProperty("match_type")]
        public int MatchType { get; set; }
        
        [JsonProperty("scoring_type")]
        public int ScoringType { get; set; }

        [JsonProperty("team_type")]
        public int TeamType { get; set; }

        [JsonProperty("mods")]
        public double Mods { get; set; }

        [JsonProperty("scores")]
        public MultiplayerScore[] Scores { get; set; }
    }
}
