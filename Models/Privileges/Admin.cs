using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.Privileges
{
    public class Admin : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (Global.IsAdminOrHigher(context.User.Id, context.Guild == null ? context.Channel.Id : context.Guild.Id))
            {
                return Task.Run(() => PreconditionResult.FromSuccess());
            }
            if (context.Guild != null)
            {
                var user = context.Guild.GetUserAsync(context.User.Id).Result;
                foreach (ulong id in user.RoleIds)
                {
                    if (context.Guild.GetRole(id).Permissions.Administrator)
                    {
                        return Task.Run(() => PreconditionResult.FromSuccess());
                    }
                }

            }
            return Task.Run(() => PreconditionResult.FromError("Admin"));
        }
    }
}
