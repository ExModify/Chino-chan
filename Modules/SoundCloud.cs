using Chino_chan.Models.SoundCloud;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Modules
{
    // 4dd97a35cf647de595b918944aa6915d
    public class SoundCloud
    {
        private string Base { get; } = "https://api.soundcloud.com/";

        public string ClientId { get; private set; }

        public SoundCloud(string ClientId)
        {
            this.ClientId = ClientId;

            try
            {
                WebClient client = new WebClient();

                client.DownloadString(Base + "tracks?client_id=" + ClientId + "&limit=1");
            }
            catch (WebException e)
            {
                if (e.Message.ToLower().Contains("(401)"))
                {
                    throw new Exception("InvalidApi");
                }
                else if (!e.Message.ToLower().Contains("(404)"))
                {
                    throw e;
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            Logger.Log(LogType.SoundCloud, ConsoleColor.DarkYellow, "SoundCloud", "Client initialized!");
        }
        
        public List<Track> SearchSongs(string Keywords)
        {
            try
            {
                string endpoint = Base + "tracks?client_id=" + ClientId + "&limit=10&q=" + Keywords.Replace(" ", "%20");
                
                WebClient client = new WebClient();
                return JsonConvert.DeserializeObject<List<Track>>(client.DownloadString(endpoint));
            }
            catch (Exception)
            {
                return null;
            }
        }
        public List<Track> GetTracks(Playlist Playlist)
        {
            try
            {
                string endpoint = Playlist.TracksUrl + "?client_id=" + ClientId;

                WebClient client = new WebClient();
                return JsonConvert.DeserializeObject<List<Track>>(client.DownloadString(endpoint));
            }
            catch (Exception)
            {
                return null;
            }
        }
        public List<Playlist> SearchPlaylist(string Keywords)
        {
            try
            {
                string endpoint = Base + "playlists?client_id=" + ClientId + "&limit=10&q=" + Keywords.Replace(" ", "%20");

                WebClient client = new WebClient();
                return JsonConvert.DeserializeObject<List<Playlist>>(client.DownloadString(endpoint));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
