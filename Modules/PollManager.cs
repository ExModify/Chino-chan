using Chino_chan.Models.Poll;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Modules
{
    public class PollManager
    {
        private string Filename { get; set; } = "Data\\Polls.json";

        Dictionary<ulong, List<Poll>> Polls { get; set; }
        Dictionary<ulong, List<PollIdentifier>> PollMessageIds { get; set; }

        public PollManager(DiscordSocketClient Client)
        {
            
            Polls = new Dictionary<ulong, List<Poll>>();
            PollMessageIds = new Dictionary<ulong, List<PollIdentifier>>();
            Client.ReactionAdded += async (Cachable, Channel, Reaction) =>
            {
                if (!(Channel is IGuildChannel GuildChannel)) return;
                IUserMessage message = await Cachable.GetOrDownloadAsync();

                if (PollMessageIds.ContainsKey(message.Id))
                {
                    PollIdentifier id = new PollIdentifier()
                    {
                        MessageId = message.Id,
                        ChannelId = GuildChannel.Id,
                        GuildId = GuildChannel.GuildId
                    };

                    id = PollMessageIds[message.Id].Find(t => t.Identical(id));

                    if (id != null)
                    {
                        int Index = Polls[id.PollCreator].FindIndex(t => t.Identical(id));
                        if (Index > -1)
                        {
                            Poll poll = Polls[id.PollCreator][Index];
                            bool save = false;
                            for (int i = 0; i < poll.ReactionEmotes.Length; i++)
                            {
                                if (poll.ReactionEmotes[i] == Reaction.Emote.Name)
                                {
                                    Polls[id.PollCreator][Index].Results[i]++;
                                    save = true;
                                }
                            }
                            if (save)
                            {
                                SavePolls();
                            }
                        }
                    }
                }
            };
            Client.ReactionRemoved += async (Cachable, Channel, Reaction) =>
            {
                if (!(Channel is IGuildChannel GuildChannel)) return;
                IUserMessage message = await Cachable.GetOrDownloadAsync();

                if (PollMessageIds.ContainsKey(message.Id))
                {
                    PollIdentifier id = new PollIdentifier()
                    {
                        MessageId = message.Id,
                        ChannelId = GuildChannel.Id,
                        GuildId = GuildChannel.GuildId
                    };

                    id = PollMessageIds[message.Id].Find(t => t.Identical(id));

                    if (id != null)
                    {
                        int Index = Polls[id.PollCreator].FindIndex(t => t.Identical(id));
                        if (Index > -1)
                        {
                            Poll poll = Polls[id.PollCreator][Index];
                            bool save = false;
                            for (int i = 0; i < poll.ReactionEmotes.Length; i++)
                            {
                                if (poll.ReactionEmotes[i] == Reaction.Emote.Name)
                                {
                                    Polls[id.PollCreator][Index].Results[i]--;
                                    save = true;
                                }
                            }
                            if (save)
                            {
                                SavePolls();
                            }
                        }
                    }
                }
            };

            Client.MessageDeleted += async (Cachable, Channel) =>
            {
                if (!(Channel is IGuildChannel guildChannel))
                    return;

                if (PollMessageIds.ContainsKey(Cachable.Id))
                {
                    List<PollIdentifier> ids = PollMessageIds[Cachable.Id];
                    int index = 0;
                    PollIdentifier id = ids[index];
                    if (ids.Count != 1)
                    {
                        for (int i = 0; i < ids.Count; i++)
                        {
                            if (ids[i].ChannelId == Channel.Id)
                            {
                                index = i;
                                id = ids[index];
                                break;
                            }
                        }
                    }
                    CancelPoll(id.PollCreator, id.PollId);

                    IUser usr = Global.Client.GetUser(id.PollCreator);
                    if (usr == null)
                    {
                        foreach (IGuild guild in Global.Client.Guilds)
                        {
                            IUser u = guild.GetUserAsync(id.PollCreator).Result;
                            if (u != null)
                            {
                                usr = u;
                                break;
                            }
                        }
                    }

                    IDMChannel ch = await usr.GetOrCreateDMChannelAsync();
                    await ch.SendMessageAsync($"The message of the poll you made with id \"{ id.PollId }\" has been removed from #{ Channel.Name } on guild \"{ guildChannel.Guild.Name }\"!");
                }
            };
            Client.LeftGuild += async (Guild) =>
            {
                foreach (KeyValuePair<ulong, List<PollIdentifier>> poll in PollMessageIds)
                {
                    for (int i = 0; i < poll.Value.Count; i++)
                    {
                        PollIdentifier id = poll.Value[i];
                        if (id.GuildId == Guild.Id)
                        {
                            CancelPoll(id.PollCreator, id.PollId);

                            IUser usr = Global.Client.GetUser(id.PollCreator);
                            if (usr == null)
                            {
                                foreach (IGuild guild in Global.Client.Guilds)
                                {
                                    IUser u = guild.GetUserAsync(id.PollCreator).Result;
                                    if (u != null)
                                    {
                                        usr = u;
                                        break;
                                    }
                                }
                            }

                            IDMChannel ch = await usr.GetOrCreateDMChannelAsync();
                            await ch.SendMessageAsync($"Chino-chan has been kicked or banned form { Guild.Name } so your poll with id { id.PollId } has been canceled!");
                        }
                    }
                }
            };
            Client.ChannelDestroyed += async (Channel) =>
            {
                if (!(Channel is IGuildChannel guildChannel))
                    return;

                foreach (KeyValuePair<ulong, List<PollIdentifier>> poll in PollMessageIds)
                {
                    for (int i = 0; i < poll.Value.Count; i++)
                    {
                        PollIdentifier id = poll.Value[i];
                        if (id.GuildId == Channel.Id)
                        {
                            CancelPoll(id.PollCreator, id.PollId);

                            IUser usr = Global.Client.GetUser(id.PollCreator);
                            if (usr == null)
                            {
                                foreach (IGuild guild in Global.Client.Guilds)
                                {
                                    IUser u = guild.GetUserAsync(id.PollCreator).Result;
                                    if (u != null)
                                    {
                                        usr = u;
                                        break;
                                    }
                                }
                            }

                            IDMChannel ch = await usr.GetOrCreateDMChannelAsync();
                            await ch.SendMessageAsync($"{ guildChannel.Name } channel has been removed from { guildChannel.Guild.Name } so the poll with id ");
                        }
                    }
                }
            };
            Logger.Log(LogType.Poll, ConsoleColor.Green, "Poll", "Manager ready!");
        }
        public void Load()
        {
            if (File.Exists(Filename))
            {
                string content = File.ReadAllText(Filename);
                Polls = JsonConvert.DeserializeObject<Dictionary<ulong, List<Poll>>>(content);
                
                int active = ActivePolls();
                if (active != 0)
                {
                    StartPolls();
                }
                
                Logger.Log(LogType.Poll, ConsoleColor.Green, "Poll", $"Loaded { CountPolls() } polls, and { active } of them are active!");
                
            }
            else
            {
                Logger.Log(LogType.Poll, ConsoleColor.Blue, "Poll", $"Loaded 0 poll!");
            }
        }
        private bool IsSaving { get; set; } = false;

        private void SavePolls()
        {
            if (IsSaving)
                return;

            IsSaving = true;

            Task.Run(() =>
            {
                File.WriteAllText(Filename, JsonConvert.SerializeObject(Polls));
                Task.Delay(5000);
                IsSaving = false;
            });
        }
        private void StartPolls()
        {
            ulong[] array = Polls.Keys.ToArray();
            for (int i = 0; i < array.Length; i++)
            {
                List<Poll> userPolls = Polls[array[i]];
                for (int j = 0; j < userPolls.Count; j++)
                {
                    Poll poll = userPolls[j];
                    if (poll.Active)
                    {
                        try
                        {
                            IUserMessage message = Global.Client.GetGuild(poll.GuildId).GetTextChannel(poll.ChannelId).GetMessageAsync(poll.MessageId).Result as IUserMessage;
                            
                            for (int k = 0; k < poll.Results.Length; k++)
                            {
                                poll.Results[k] = 0;
                            }
                            foreach (KeyValuePair<IEmote, ReactionMetadata> reaction in message.Reactions)
                            {
                                for (int k = 0; k < poll.ReactionEmotes.Length; k++)
                                {
                                    if (poll.ReactionEmotes[k] == reaction.Key.Name)
                                    {
                                        poll.Results[k] = reaction.Value.ReactionCount - 1;
                                    }
                                }
                            }

                            RegisterPollAsync(poll, false).ConfigureAwait(false).GetAwaiter().GetResult();
                        }
                        catch
                        {
                            CancelPoll(poll.PollCreator, poll.PollId);

                            IUser usr = Global.Client.GetUser(poll.PollCreator);
                            if (usr == null)
                            {
                                foreach (IGuild guild in Global.Client.Guilds)
                                {
                                    IUser u = guild.GetUserAsync(poll.PollCreator).Result;
                                    if (u != null)
                                    {
                                        usr = u;
                                        break;
                                    }
                                }
                            }

                            IDMChannel ch = usr.GetOrCreateDMChannelAsync().Result;
                            if (ch != null)
                            {
                                ch.SendMessageAsync($"I'm sorry, but the poll with id { poll.PollId } couldn't be continued because the message or channel may have been deleted or I have left the server!").ConfigureAwait(false).GetAwaiter().GetResult();
                            }
                        }
                    }
                }
            }
        }
        public async Task RegisterPollAsync(Poll Poll, bool SendEmbed = true)
        {
            if (SendEmbed)
            {
                SocketGuild guild = Global.Client.GetGuild(Poll.GuildId);
                ITextChannel ch = guild.GetTextChannel(Poll.ChannelId);
                IUserMessage message = await ch.SendMessageAsync("", embed: Poll.CreateEmbed().Build());
                foreach (IEmote emote in Poll.ReactionEmotes.Select(t => new Emoji(t)))
                {
                    await message.AddReactionAsync(emote);
                }

                IUser usr = Global.Client.GetUser(Poll.PollCreator);
                if (usr == null)
                {
                    foreach (IGuild g in Global.Client.Guilds)
                    {
                        IUser u = g.GetUserAsync(Poll.PollCreator).Result;
                        if (u != null)
                        {
                            usr = u;
                            break;
                        }
                    }
                }

                IDMChannel channel = await usr.GetOrCreateDMChannelAsync();
                EmbedBuilder builder = new EmbedBuilder()
                {
                    Title = "New poll registered!",
                    Description = $"Question: { Poll.PollText }\nID: { Poll.PollId }\nMade on guild: { guild.Name } in #{ ch.Name }",
                    Color = Color.Green
                };
                await channel.SendMessageAsync("", embed: builder.Build());

                Poll.MessageId = message.Id;

                if (!Polls.ContainsKey(Poll.PollCreator))
                {
                    Polls.Add(Poll.PollCreator, new List<Poll>());
                }

                Polls[Poll.PollCreator].Add(Poll);
            }

            if (PollMessageIds.ContainsKey(Poll.MessageId))
            {
                PollMessageIds[Poll.MessageId].Add(Poll);
            }
            else
            {
                PollMessageIds.Add(Poll.MessageId, new List<PollIdentifier>()
                {
                    Poll
                });
            }

            Task pollTask = Task.Run(async () =>
            {
                TimeSpan ts = Poll.PollCreatedAt + Poll.Duration - TimeSpan.FromTicks(DateTime.Now.Ticks);
                if (ts > TimeSpan.Zero)
                {
                    try
                    {
                        await Task.Delay(ts, Poll.CToken);
                    }
                    catch (OperationCanceledException) { }
                }
                await UnregisterPollAsync(Poll);
            });
        }

        public bool CancelPoll(ulong UserId, int PollId)
        {
            if (Polls.ContainsKey(UserId))
            {
                int Index = Polls[UserId].FindIndex(t => t.PollId == PollId);
                if (Index > -1)
                {
                    Poll poll = Polls[UserId][Index];
                    if (poll.Active)
                    {
                        Polls[UserId][Index].Duration = TimeSpan.Zero;
                        Polls[UserId][Index].CTokenSource.Cancel();
                        return true;
                    }
                }
            }
            return false;
        }
        private async Task UnregisterPollAsync(Poll Poll)
        {
            RemovePollFromId(Poll);
            
            if (!Poll.ReportedResult)
            {
                IUser usr = Global.Client.GetUser(Poll.PollCreator);
                if (usr == null)
                {
                    foreach (IGuild g in Global.Client.Guilds)
                    {
                        IUser u = g.GetUserAsync(Poll.PollCreator).Result;
                        if (u != null)
                        {
                            usr = u;
                            break;
                        }
                    }
                }

                IDMChannel channel = await usr.GetOrCreateDMChannelAsync();

                if (Poll.Duration == TimeSpan.Zero)
                {
                    await channel.SendMessageAsync($"Poll with id { Poll.PollId } (\"{ Poll.PollText }\") has been canceled!");
                }
                else
                {
                    EmbedBuilder builder = new EmbedBuilder()
                    {
                        Title = "Results for poll: " + Poll.PollText,
                        Color = Color.Green
                    };

                    float total = Poll.Results.Sum();

                    if (total == 0)
                    {
                        builder.Description = "Poll failed, noone has voted!";
                        builder.Color = Color.Red;
                    }
                    else
                    {
                        int maxVote = 0;
                        List<int> indices = new List<int>();

                        for (int i = 0; i < Poll.Options.Length; i++)
                        {
                            builder.Description += $"#{ i + 1 }: { Poll.Options[i] } - { Poll.Results[i] } votes ({ (Poll.Results[i] / total * 100).ToString("N0") }%)\n";
                            if (Poll.Results[i] > maxVote)
                            {
                                maxVote = Poll.Results[i];
                                indices.Clear();
                            }

                            if (Poll.Results[i] == maxVote)
                            {
                                indices.Add(i);
                            }
                        }
                        builder.Description += "-------------\n" +
                            $"Top voted: { Poll.Options[indices[0]] }";

                        for (int i = 1; i < indices.Count; i++)
                        {
                            builder.Description += "\n" + $"{ Poll.Options[indices[i]] }".PadLeft(11);
                        }
                    }
                    
                    await channel.SendMessageAsync("", embed: builder.Build());
                }

                int Index = Polls[Poll.PollCreator].FindIndex(t => t.Identical(Poll));
                Poll.ReportedResult = true;
                Polls[Poll.PollCreator][Index] = Poll;

                SavePolls();
            }
            try
            {
                IUserMessage message = (await Global.Client.GetGuild(Poll.GuildId).GetTextChannel(Poll.ChannelId).GetMessageAsync(Poll.MessageId)) as IUserMessage;
                await message.ModifyAsync((MP) =>
                {
                    MP.Embed = new Optional<Embed>(new EmbedBuilder()
                    {
                        Title = Poll.PollText,
                        Description = "This poll has been ended!"
                    }.Build());
                });
            }
            catch { } // Couldn't modify message - channel / message / guild deleted
        }

        private int CountPolls()
        {
            int c = 0;
            ulong[] array = Polls.Keys.ToArray();
            for (int i = 0; i < array.Length; i++)
            {
                c += Polls[array[i]].Count;
            }
            return c;
        }
        private int ActivePolls()
        {
            int c = 0;
            ulong[] array = Polls.Keys.ToArray();
            for (int i = 0; i < array.Length; i++)
            {
                List<Poll> userPolls = Polls[array[i]];
                for (int j = 0; j < userPolls.Count; j++)
                {
                    if (userPolls[j].Active) c++;
                    else if (!userPolls[j].ReportedResult)
                    {
                        UnregisterPollAsync(userPolls[j]).ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                }
            }
            return c;
        }
        private void RemovePollFromId(PollIdentifier Poll)
        {
            if (PollMessageIds.ContainsKey(Poll.MessageId))
            {
                List<PollIdentifier> polls = new List<PollIdentifier>();
                polls.RemoveAll(t => t.Identical(Poll));

                if (polls.Count == 0)
                {
                    PollMessageIds.Remove(Poll.MessageId);
                }
                else
                {
                    PollMessageIds[Poll.MessageId] = polls;
                }
            }
        }
        
        public int CreatePollId(ulong UserId)
        {
            if (!Polls.ContainsKey(UserId))
                return 1;
            int[] ids = Polls[UserId].Select(t => t.PollId).ToArray();

            for (int id = 1; id < int.MaxValue; id++)
            {
                if (!ids.Contains(id))
                    return id; 
            }

            return -1;
        }
    }
}
