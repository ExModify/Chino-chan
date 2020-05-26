using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Chino_chan.Models.Privileges
{
    public class Owner : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (Global.IsOwner(context.User.Id))
            {
                return Task.Run(() => PreconditionResult.FromSuccess());
            }
            return Task.Run(() => PreconditionResult.FromError("Owner"));
        }
    }
}
