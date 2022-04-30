using Newtonsoft.Json;

namespace Chino_chan.Models.Image
{
    public struct ChinoResponse
    {
        [JsonProperty("files")]
        public string[] Files { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }
    }
}