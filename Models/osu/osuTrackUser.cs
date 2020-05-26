using Chino_chan.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.osu
{
    public class osuTrackUser
    {
        public string UsernameAtReg { get; set; }
        public int UserId { get; set; }
        public DateTime LastScoreTime { get; set; }
        public Mode Mode { get; set; }
        public float PP { get; set; }

        public osuTrackUser(int UserId, Mode Mode, osuApi API, osuAPI.User? User)
        {
            this.UserId = UserId;
            this.Mode = Mode;
            LastScoreTime = DateTime.UtcNow;
            var user = User ?? API.GetUser(UserId, true, Mode).Result;
            PP = user.PP;
            UsernameAtReg = user.UserName;
        }

        public osuTrackUser() { }
    }
}
