using Chino_chan.Modules;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api.Helix.Models.Streams;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;

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
            try
            {
                Id = long.Parse(stream.Id);
                UserId = long.Parse(stream.UserId);
                GameId = stream.GameId == "" ? 0 : long.Parse(stream.GameId);
                Title = stream.Title;
                _ThumbnailUrl = stream.ThumbnailUrl;
            }
            catch (Exception e)
            {
                Id = 0;
                UserId = 0;
                GameId = 0;
                Title = "";
                _ThumbnailUrl = "";

                Logger.Log(LogType.Twitch, ConsoleColor.Red, "Error", "Failed to parse stream! Stream object dump: "
                    + JsonConvert.SerializeObject(stream, Formatting.Indented));
                Logger.Log(LogType.Twitch, ConsoleColor.Red, "Error", "Exception: " + e.Message);
            }
        }
    }
}
