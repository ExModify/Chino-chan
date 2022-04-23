namespace Chino_chan.Models.PerformanceCalculator
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class PpcOutput
    {
        [JsonProperty("score")]
        public Score Score { get; set; }

        [JsonProperty("pp")]
        public double PP { get; set; }

        [JsonProperty("performance_attributes")]
        public PerformanceAttributes PerformanceAttributes { get; set; }

        [JsonProperty("difficulty_attributes")]
        public DifficultyAttributes DifficultyAttributes { get; set; }
    }

    public partial class DifficultyAttributes
    {
        [JsonProperty("star_rating")]
        public double StarRating { get; set; }

        [JsonProperty("max_combo")]
        public long MaxCombo { get; set; }

        [JsonProperty("aim_strain")]
        public double AimStrain { get; set; }

        [JsonProperty("speed_strain")]
        public double SpeedStrain { get; set; }

        [JsonProperty("flashlight_rating")]
        public double FlashlightRating { get; set; }

        [JsonProperty("slider_factor")]
        public double SliderFactor { get; set; }

        [JsonProperty("approach_rate")]
        public double ApproachRate { get; set; }

        [JsonProperty("overall_difficulty")]
        public double OverallDifficulty { get; set; }
    }

    public partial class PerformanceAttributes
    {
        [JsonProperty("aim")]
        public double Aim { get; set; }

        [JsonProperty("speed")]
        public double Speed { get; set; }

        [JsonProperty("accuracy")]
        public double Accuracy { get; set; }

        [JsonProperty("flashlight")]
        public double Flashlight { get; set; }

        [JsonProperty("od")]
        public double OverallDifficulty { get; set; }

        [JsonProperty("ar")]
        public double ApproachRate { get; set; }

        [JsonProperty("max_combo")]
        public long MaxCombo { get; set; }
    }

    public partial class Score
    {
        [JsonProperty("ruleset_id")]
        public long RulesetId { get; set; }

        [JsonProperty("beatmap_id")]
        public long BeatmapId { get; set; }

        [JsonProperty("beatmap")]
        public string Beatmap { get; set; }

        [JsonProperty("mods")]
        public List<Mod> Mods { get; set; }

        [JsonProperty("total_score")]
        public long TotalScore { get; set; }

        [JsonProperty("accuracy")]
        public double Accuracy { get; set; }

        [JsonProperty("combo")]
        public long Combo { get; set; }

        [JsonProperty("statistics")]
        public Statistics Statistics { get; set; }
    }

    public partial class Mod
    {
        [JsonProperty("acronym")]
        public string Acronym { get; set; }
    }

    public partial class Statistics
    {
        [JsonProperty("Great")]
        public long Great { get; set; }

        [JsonProperty("Ok")]
        public long Ok { get; set; }

        [JsonProperty("Meh")]
        public long Meh { get; set; }

        [JsonProperty("Miss")]
        public long Miss { get; set; }
    }
}
