﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chino_chan.Models.osuAPI
{
    public struct BestScore
    {
        [JsonProperty("beatmap_id")]
        public int BeatmapId { get; set; }
		
        [JsonProperty("score")]
        public long Score { get; set; }

        [JsonProperty("count300")]
        public int Count300 { get; set; }

        [JsonProperty("count100")]
        public int Count100 { get; set; }

        [JsonProperty("count50")]
        public int Count50 { get; set; }

        [JsonProperty("countmiss")]
        public int Misses { get; set; }

        [JsonProperty("maxcombo")]
        public int MaxCombo { get; set; }

        [JsonProperty("countkatu")]
        public int CountKatu { get; set; }

        [JsonProperty("countgeki")]
        public int CountGeki { get; set; }

        [JsonProperty("perfect")]
        public int Perfect { get; set; }
        
        [JsonProperty("enabled_mods")]
        public int Mods { get; set; }
        
        [JsonProperty("user_id")]
        public int UserID { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("rank")]
        public string Rank { get; set; }

        [JsonProperty("score_id")]
        public long ScoreId { get; set; }

        [JsonIgnore]
        public float PP
        {
            get
            {
                if (PP_Value == null)
                    return 0;
                else return float.Parse(PP_Value);
            }
        }

        [JsonProperty("pp")]
        public string PP_Value { get; set; }

        [JsonIgnore]
        public float Accuracy
        {
            get
            {
                return (float)Math.Truncate(((Count300 * 6) + (Count100 * 2) + Count50) /
                                           ((Count300 + Count100 + Count50 + Misses) * 6f) * 100 * 100f) / 100f;
            }
        }
    }
}
