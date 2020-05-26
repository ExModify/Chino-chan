using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Leveling
{
    public class User
    {
        public ulong UserId { get; set; }
        
        public ulong XpSum
        {
            get
            {
                ulong Xp = 0;
                
                IEnumerator<KeyValuePair<ulong, uint>> enumer = GuildXps.GetEnumerator();

                while (enumer.MoveNext())
                {
                    Xp += enumer.Current.Value;
                }

                return Xp;
            }
        }

        public Dictionary<ulong, uint> GuildXps { get; set; } = new Dictionary<ulong, uint>();

        public Dictionary<ulong, uint> GuildLevels { get; set; } = new Dictionary<ulong, uint>();

        public List<object> Items { get; set; } = new List<object>(); // soonTM
    }
}
