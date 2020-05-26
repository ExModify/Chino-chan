using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.API
{
    public struct User
    {
        public IUser MainUser { get; set; }

        public string AvatarUrl { get; set; }
        public string DefaultAvatarUrl { get; set; }
        
        public bool GuildUser { get; set; }
        

        public User(IUser User)
        {
            MainUser = User;

            AvatarUrl = User.GetAvatarUrl();
            DefaultAvatarUrl = User.GetDefaultAvatarUrl();
            
            GuildUser = User is SocketGuildUser;
        }
    }
}
