using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.Music
{
    public class ListenMoe
    {
        [JsonProperty("op")]
        public int OpCode { get; set; }

        [JsonProperty("d")]
        public ListenMoeData Data { get; set; }

        [JsonProperty("t")]
        public string Type { get; set; }
    }

    public class ListenMoeData
    {
        [JsonProperty("heartbeat")]
        public int HeartBeat { get; set; }

        [JsonProperty("song")]
        public ListenMoeSong Song { get; set; }

        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }
    }

    public class ListenMoeSong
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("artists")]
        public List<ListenMoeArtist> Artists { get; set; }

        [JsonProperty("albums")]
        public List<ListenMoeAlbum> Albums { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }
    }

    public class ListenMoeArtist
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("nameRomaji")]
        public string NameRomaji { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }
    }
    public class ListenMoeAlbum
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("nameRomaji")]
        public string NameRomaji { get; set; }

        private string _Image;

        [JsonProperty("image")]
        public string Image
        {
            get
            {
                return _Image == null ? null : "https://cdn.listen.moe/covers/" + _Image;
            }
            set
            {
                _Image = value;
            }
        }
    }
}
