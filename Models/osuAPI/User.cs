using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chino_chan.Models.osuAPI
{
    public struct User
    {
        [JsonProperty("user_id")]
        public int UserId { get; set; }

        [JsonProperty("join_date")]
        public DateTime JoinedDate { get; set; }

        [JsonProperty("username")]
        public string UserName { get; set; }

        [JsonProperty("count300")]
        public int Count300 { get; set; }

        [JsonProperty("count100")]
        public int Count100 { get; set; }

        [JsonProperty("count50")]
        public int Count50 { get; set; }

        [JsonProperty("playcount")]
        public int PlayCount { get; set; }
        
        [JsonProperty("ranked_score")]
        public long RankedScore { get; set; }

        [JsonProperty("total_score")]
        public long TotalScore { get; set; }

        [JsonProperty("pp_rank")]
        public long Rank { get; set; }
        
        [JsonProperty("level")]
        public float Level { get; set; }

        [JsonProperty("pp_raw")]
        public float PP { get; set; }

        [JsonProperty("accuracy")]
        public float Accuracy { get; set; }

        [JsonProperty("count_rank_ss")]
        public int? CountSS { get; set; }

        [JsonProperty("count_rank_s")]
        public int? CountS { get; set; }

        [JsonProperty("count_rank_ssh")]
        public int? CountHDSS { get; set; }

        [JsonProperty("count_rank_sh")]
        public int? CountHDS { get; set; }

        [JsonProperty("count_rank_a")]
        public int? CountA { get; set; }

        [JsonProperty("country")]
        public string CountryFlag { get; set; }

        [JsonProperty("pp_country_rank")]
        public int CountryRank { get; set; }

        [JsonProperty("total_seconds_played")]
        public int TotalSecondsPlayed { get; set; }
    }
}
