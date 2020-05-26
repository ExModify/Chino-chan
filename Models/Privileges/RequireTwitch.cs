using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.Privileges
{
    public class RequireTwitch : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (Global.TwitchTracker != null)
            {
                return Task.Run(() => PreconditionResult.FromSuccess());
            }
            return Task.Run(() => PreconditionResult.FromError("Twitch"));
        }
    }
}
