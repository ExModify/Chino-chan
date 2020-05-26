using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.SoundCloud
{
    public class Playlist
    {
        [JsonProperty("id")]
        public long Id { get; set; }
        
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("last_modified")]
        public DateTime LastModified { get; set; }

        [JsonProperty("track_count")]
        public int TrackCount { get; set; }

        [JsonProperty("genre")]
        public string Genre { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("permalink_url")]
        public string Url { get; set; }

        [JsonProperty("user")]
        public User User { get; set; }

        [JsonProperty("tracks_uri")]
        public string TracksUrl { get; set; }

        [JsonProperty("artwork_url")]
        public string ThumbnailUrl { get; set; }
    }
}
