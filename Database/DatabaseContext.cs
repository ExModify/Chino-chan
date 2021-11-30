using Chino_chan.Models.Music;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chino_chan.Database
{
    public partial class DatabaseContext : DbContext
    {
        public virtual DbSet<MusicItem> MusicDatabase { get; set; }


        private string Host { get; set; }
        private string DatabaseName { get; set; }
        private string Username { get; set; }
        private string Password { get; set; }


        public DatabaseContext(string host, string database, string username, string password): base()
        {
            Host = host;
            DatabaseName = database;
            Username = username;
            Password = password;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseNpgsql($"Host={ Host };Database={ DatabaseName };Username={ Username };Password={ Password }")
                        .UseSnakeCaseNamingConvention();

    }
}
