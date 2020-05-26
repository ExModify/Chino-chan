using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.SoundCloud
{
    public class Track
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("last_modified")]
        public DateTime LastModified { get; set; }

        [JsonProperty("permalink_url")]
        public string Url { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("stream_url")]
        public string StreamUrl { get; set; }

        [JsonProperty("duration")]
        public int ms_Duration { get; set; }

        [JsonIgnore]
        public TimeSpan Duration
        {
            get
            {
                return TimeSpan.FromMilliseconds(ms_Duration);
            }
        }

        [JsonProperty("artwork_url")]
        public string ThumbnailUrl { get; set; }

        [JsonProperty("streamable")]
        public bool Streamable { get; set; }

        [JsonProperty("genre")]
        public string Genre { get; set; }

        [JsonProperty("user")]
        public User User { get; set; }
    }
}
