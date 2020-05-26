using Newtonsoft.Json;
using System;

namespace Chino_chan.Models.osuAPI
{
    public class Beatmap
    {
        [JsonProperty("approved")]
        public int Approved { get; set; }

        [JsonProperty("approved_date")]
        public DateTime? ApprovedDate { get; set; }

        [JsonProperty("last_update")]
        public DateTime? LastUpdate { get; set; }

        [JsonProperty("artist")]
        public string Artist { get; set; }

        [JsonProperty("beatmap_id")]
        public int BeatmapId { get; set; }

        private int _BeatmapSetID = 0;

        [JsonProperty("beatmapset_id")]
        public int BeatmapSetId
        {
            get
            {
                return _BeatmapSetID;
            }
            set
            {
                _BeatmapSetID = value;
                ThumbnailURL = "https://b.ppy.sh/thumb/" + value + "l.jpg";
            }
        }

        [JsonProperty("bpm")]
        public float BPM { get; set; }

        [JsonProperty("creator")]
        public string Creator { get; set; }

        [JsonProperty("creator_id")]
        public int CreatorId { get; set; }

        [JsonProperty("difficultyrating")]
        public float StarRating { get; set; }

        [JsonProperty("diff_size")]
        public float CS { get; set; }

        [JsonProperty("diff_overall")]
        public float OD { get; set; }

        [JsonProperty("diff_approach")]
        public float AR { get; set; }

        [JsonProperty("diff_drain")]
        public float HP { get; set; }

        [JsonProperty("hit_length")]
        public int HitLength { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("genre_id")]
        public int GenreID { get; set; }

        [JsonProperty("language_id")]
        public int LanguageID { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("total_length")]
        public long TotalLength { get; set; }

        [JsonProperty("version")]
        public string DifficultyName { get; set; }

        [JsonProperty("file_md5")]
        public string MD5 { get; set; }

        [JsonProperty("mode")]
        public long Mode { get; set; }

        [JsonProperty("tags")]
        public string Tags { get; set; }

        [JsonProperty("favourite_count")]
        public int FavouriteCount { get; set; }

        [JsonProperty("playcount")]
        public int PlayCount { get; set; }

        [JsonProperty("passcount")]
        public int PassCount { get; set; }

        [JsonProperty("max_combo")]
        public int MaxCombo { get; set; }

        [JsonProperty("thumbnail_url")]
        public string ThumbnailURL { get; set; }

        [JsonIgnore]
        public bool CanGetPP
        {
            get
            {
                return Approved == 2 || Approved == 1; // Ranked || Approved
            }
        }
    }
}
