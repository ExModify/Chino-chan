using Chino_chan.Models.SoundCloud;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace Chino_chan.Models.Music
{
    public class MusicItem
    {
        public MusicItem() { }
        public MusicItem(Video Video)
        {
            Title = Video.Title;
            Thumbnail = Video.Thumbnails.MaxResUrl;
            Author = Video.Author;
            UrlOrId = Video.Id;
            Duration = Video.Duration;
            PublicUrl = Video.Url;
            IsYouTube = true;
        }
        public MusicItem(PlaylistVideo Video)
        {
            Title = Video.Title;
            Thumbnail = Video.Thumbnails.MaxResUrl;
            Author = Video.Author;
            UrlOrId = Video.Id;
            Duration = Video.Duration;
            PublicUrl = Video.Url;
            IsYouTube = true;
        }
        public MusicItem(Track SoundcloudData)
        {
            Author = SoundcloudData.User.Username;
            Duration = SoundcloudData.Duration;
            Thumbnail = SoundcloudData.ThumbnailUrl;
            Title = SoundcloudData.Title;
            UrlOrId = SoundcloudData.StreamUrl;
            PublicUrl = SoundcloudData.Url;
            IsYouTube = false;
        }

        public bool IsListenMoe { get; set; } = false;
        public bool IsYouTube { get; set; }
        public string Title { get; set; }
        
        public string Thumbnail { get; set; }
        
        public string Author { get; set; }
        
        public string UrlOrId { get; set; }
        public string PublicUrl { get; set; }
        
        public TimeSpan Duration { get; set; }
    }
}
