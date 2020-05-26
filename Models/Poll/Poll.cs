using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chino_chan.Models.Poll
{
    public class Poll : PollIdentifier
    {
        public bool Active
        {
            get
            {
                return PollCreatedAt + Duration >= TimeSpan.FromTicks(DateTime.Now.Ticks);
            }
        }
        
        public string PollText { get; set; }

        public string[] Options { get; set; }
        public int[] Results { get; set; }
        public string[] ReactionEmotes { get; set; }

        public TimeSpan PollCreatedAt { get; set; }
        public TimeSpan Duration { get; set; }

        [JsonIgnore]
        public CancellationTokenSource CTokenSource { get; set; }
        [JsonIgnore]
        public CancellationToken CToken { get; set; }
        
        public bool ReportedResult { get; set; } = false;

        public EmbedBuilder CreateEmbed()
        {
            SocketGuildUser user = Global.Client.GetGuild(GuildId).GetUser(PollCreator);
            
            string name = user.Nickname ?? user.Username;

            EmbedBuilder builder = new EmbedBuilder
            {
                Title = $"New poll created by { name }: { PollText }",
                Color = Color.DarkMagenta
            };

            for (int i = 0; i < Options.Length; i++)
            {
                builder.Description += $"{ ReactionEmotes[i] } => { Options[i] }\n";
            }
            
            string endsIn = new DateTime(PollCreatedAt.Ticks).Add(Duration).ToUniversalTime().ToString();
            
            builder.WithCurrentTimestamp();

            builder.Timestamp = builder.Timestamp.Value.Add(Duration);
            
            

            return builder;
        }

        public Poll()
        {
            CTokenSource = new CancellationTokenSource();
            CToken = CTokenSource.Token;
        }
        public Poll(PollIdentifier Identifier) : this()
        {
            GuildId = Identifier.GuildId;
            MessageId = Identifier.MessageId;
            PollCreator = Identifier.PollCreator;
            PollId = Identifier.PollId;
        }

        public void SetOptions(params string[] Options)
        {
            this.Options = Options;
            Results = new int[Options.Length];
            ReactionEmotes = new string[Options.Length];
            //string Characters = "🇦🇧🇨🇩🇪🇫🇬🇭🇮🇯🇰🇱🇳🇴🇵🇶🇷🇸🇹🇺🇻🇼🇽🇾🇿";
            string[] Characters = new string[26] { "🇦", "🇧", "🇨", "🇩", "🇪", "🇫", "🇬", "🇭", "🇮", "🇯", "🇰", "🇱", "🇲", "🇳", "🇴", "🇵", "🇶", "🇷", "🇸", "🇹", "🇺", "🇻", "🇼", "🇽", "🇾", "🇿" };
            for (int i = 0; i < Options.Length; i++)
            {
                Results[i] = 0;
                ReactionEmotes[i] = Characters[i];
            }
        }
    }
}
