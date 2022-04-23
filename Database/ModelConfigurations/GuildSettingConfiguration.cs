using Chino_chan.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chino_chan.Database.ModelConfigurations
{
    public class GuildSettingConfiguration : IEntityTypeConfiguration<GuildSetting>
    {
        public void Configure(EntityTypeBuilder<GuildSetting> builder)
        {
            builder.HasKey(prop => prop.GuildId);

        }
    }
}
