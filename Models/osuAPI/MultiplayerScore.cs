using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chino_chan.Models.osuAPI
{
    public struct MultiplayerScore
    {
        [JsonProperty("slot")]
        public int Slot { get; set; }

        [JsonProperty("team")]
        public int Team { get; set; }

        [JsonProperty("user_id")]
        public int UserID { get; set; }

        [JsonProperty("score")]
        public long Score { get; set; }

        [JsonProperty("maxcombo")]
        public int MaxCombo { get; set; }

        [JsonProperty("rank")]
        public int Rank { get; set; }

        [JsonProperty("count50")]
        public int Count50 { get; set; }

        [JsonProperty("count100")]
        public int Count100 { get; set; }

        [JsonProperty("count300")]
        public int Count300 { get; set; }

        [JsonProperty("countmiss")]
        public int Misses { get; set; }

        [JsonProperty("countgeki")]
        public int CountGeki { get; set; }

        [JsonProperty("countkatu")]
        public int CountKatu { get; set; }

        [JsonProperty("perfect")]
        public int Perfect { get; set; }

        [JsonProperty("pass")]
        public int Pass { get; set; }
    }
}
