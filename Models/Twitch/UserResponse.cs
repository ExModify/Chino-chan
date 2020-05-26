using Newtonsoft.Json;
using TwitchLib.Api.Helix.Models.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.Twitch
{
    public struct UserResponse
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("login")]
        public string LoginUsername { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("profile_image_url")]
        public string Avatar { get; set; }

        [JsonProperty("offline_image_url")]
        public string Offline { get; set; }

        public UserResponse(User user)
        {
            Id = long.Parse(user.Id);
            LoginUsername = user.Login;
            DisplayName = user.DisplayName;
            Avatar = user.ProfileImageUrl;
            Offline = user.OfflineImageUrl;
        }
    }
}
