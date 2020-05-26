using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Chino_chan.Models.Privileges
{
    class ServerCommand : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.Guild != null)
            {
                return Task.Run(() => PreconditionResult.FromSuccess());
            }
            return Task.Run(() => PreconditionResult.FromError("ServerSide"));
        }
    }
}
