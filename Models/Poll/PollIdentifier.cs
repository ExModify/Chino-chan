using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.Poll
{
    public class PollIdentifier
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public ulong PollCreator { get; set; }
        public int PollId { get; set; }

        public bool Identical(PollIdentifier PollId)
        {
            return GuildId == PollId.GuildId && ChannelId == PollId.ChannelId && MessageId == PollId.MessageId;
        }
    }
}
