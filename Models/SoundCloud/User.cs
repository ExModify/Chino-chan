using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.SoundCloud
{
    public struct User
    {
        [JsonProperty("avatar_url")]
        public string Avatar { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("permalink_url")]
        public string Url { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("last_modified")]
        public DateTime LastModified { get; set; }
    }
}
