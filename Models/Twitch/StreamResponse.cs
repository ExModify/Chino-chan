using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api.Helix.Models.Streams;

namespace Chino_chan.Models.Twitch
{
    public struct StreamResponse
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("user_id")]
        public long UserId { get; set; }

        [JsonProperty("game_id")]
        public long GameId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonIgnore]
        private string _ThumbnailUrl { get; set; }

        [JsonProperty("thumbnail_url")]
        public string ThumbnailUrl
        {
            get
            {
                return _ThumbnailUrl;
            }
            set
            {
                _ThumbnailUrl = value.Replace("{width}", "1280").Replace("{height}", "720");
            }
        }

        public StreamResponse(Stream stream)
        {
            Id = long.Parse(stream.Id);
            UserId = long.Parse(stream.UserId);
            GameId = long.Parse(stream.GameId);
            Title = stream.Title;
            _ThumbnailUrl = stream.ThumbnailUrl;
        }
    }
}
