using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.API
{
    public enum UserAccess
    {
        Owner = 0,
        GlobalAdmin = 1,
        ServerOwner = 2,
        Admin = 3,
        Common = 4
    }
}
