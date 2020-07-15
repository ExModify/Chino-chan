using Chino_chan.Models;
using Chino_chan.Models.osu;
using Chino_chan.Models.osuAPI;
using Chino_chan.Models.Privileges;
using Chino_chan.Models.Settings;
using Chino_chan.Modules;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using osuBeatmapUtilities.Rulesets;
using osuBeatmapUtilities.Rulesets.Difficulty;
using osuBeatmapUtilities.Rulesets.Osu;
using osuBeatmapUtilities.Rulesets.Scoring;
using osuBeatmapUtilities.Scoring;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Commands
{
    [Name("osu!")]
    public class osu : ChinoContext
    {
        [Command("osu"), Summary("Get osu! profile")]
        public async Task osuAsync(params string[] Args)
        {
            osuApi api = Global.osuAPI;

            string Username = "";
            if (Args.Length == 0 && !Global.Settings.osuDiscordUserDatabase.ContainsKey(Context.User.Id))
            {
                Username = await Tools.GetNicknameOrUsernameAsync(Context, Context.User);
            }
            else
            {
                Username = string.Join(" ", Args);
            }
            User user = default(User);

            try
            {
                if (Context.Message.MentionedUserIds.Count == 1)
                {
                    IGuildUser gUser = await Context.Guild.GetUserAsync(Context.Message.MentionedUserIds.ToList()[0]);

                    string usrName = gUser.Nickname ?? gUser.Username;
                    if (Global.Settings.osuDiscordUserDatabase.ContainsKey(gUser.Id))
                    {
                        user = await api.GetUser(Global.Settings.osuDiscordUserDatabase[gUser.Id], true);
                    }
                    else
                    {
                        user = await api.GetUser(usrName, true);
                    }
                }
                if (string.IsNullOrWhiteSpace(user.UserName))
                {
                    if (Global.Settings.osuDiscordUserDatabase.ContainsKey(Context.User.Id) && Username.Length == 0)
                    {
                        user = await api.GetUser(Global.Settings.osuDiscordUserDatabase[Context.User.Id], true);
                    }
                    else
                    {
                        user = await api.GetUser(Username, true);
                    }
                }
            }
            catch
            {
                await Context.Channel.SendMessageAsync(GetEntry("NoInfo"));
                return;
            }
            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                await Context.Channel.SendMessageAsync(GetEntry("InvalidUser", "USERNAME", Username));
                return;
            }
            TimeSpan playtime = new TimeSpan(0, 0, user.TotalSecondsPlayed);
            EmbedBuilder builder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"{ user.UserName } | { user.PP }pp | #{ user.Rank.ToString("N0") } | { user.CountryFlag }#{ user.CountryRank }",
                    Url = "https://osu.ppy.sh/u/" + user.UserId,
                    IconUrl = "https://a.ppy.sh/" + user.UserId
                },
                Description = GetEntry("Description",
                                "DATE", user.JoinedDate.ToString(),
                                "TSCORE", user.TotalScore.ToString("N0"),
                                "RSCORE", user.RankedScore.ToString("N0"),
                                "PC", user.PlayCount.ToString("N0"),
                                "PT", string.Format("{0}d {1}h {2}m (**{3}h**)", 
                                        playtime.Days.ToString("N0"), playtime.Hours.ToString("N0"),
                                        playtime.Minutes.ToString("N0"), playtime.TotalHours.ToString("N0")),
                                "HINF", $"{ (user.Count300 + user.Count100 + user.Count50).ToString("N0") } [ { user.Count300.ToString("N0") } / { user.Count100.ToString("N0") } / { user.Count50.ToString("N0") } ]",
                                "LEVEL", Math.Truncate(user.Level).ToString("N0"),
                                "PR", ((user.Level - Math.Truncate(user.Level)) * 100).ToString("N2") + "%",
                                "HDSS", user.CountHDSS.Value.ToString("N0"),
                                "SS", user.CountSS.Value.ToString("N0"),
                                "HDS", user.CountHDS.Value.ToString("N0"),
                                "S", user.CountS.Value.ToString("N0"),
                                "A", user.CountA.Value.ToString("N0")),
                ThumbnailUrl = "https://a.ppy.sh/" + user.UserId,
                Color = await Global.GetAverageColorAsync("https://a.ppy.sh/" + user.UserId)
            };
            await ReplyAsync("", embed: builder.Build());
        }

        [Command("recentbest"), Alias("rb"), Summary("Gets the most recent top score of a user"), osuAPIRequired]
        public async Task RecentBestAsync(params string[] Args)
        {
            osuApi api = Global.osuAPI;

            int recentBest = 0;
            string Username = "";
            if (Args.Length == 0 && !Global.Settings.osuDiscordUserDatabase.ContainsKey(Context.User.Id))
            {
                Username = await Tools.GetNicknameOrUsernameAsync(Context, Context.User);
            }
            else if (Args.Length != 0)
            {
                if (int.TryParse(Args[Args.Length - 1], out recentBest))
                {
                    recentBest = Math.Min(99, Math.Max(0, recentBest));
                    recentBest--;
                    Args = Args.Take(Args.Length - 1).ToArray();
                }
                Username = string.Join(" ", Args);
            }
            User user = default(User);

            if (Context.Message.MentionedUserIds.Count == 1)
            {
                IGuildUser gUser = await Context.Guild.GetUserAsync(Context.Message.MentionedUserIds.ToList()[0]);

                string usrName = gUser.Nickname ?? gUser.Username;
                if (Global.Settings.osuDiscordUserDatabase.ContainsKey(gUser.Id))
                {
                    user = await api.GetUser(Global.Settings.osuDiscordUserDatabase[gUser.Id], true);
                }
                else
                {
                    user = await api.GetUser(usrName, true);
                }
            }
            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                if (Global.Settings.osuDiscordUserDatabase.ContainsKey(Context.User.Id) && Username.Length == 0)
                {
                    user = await api.GetUser(Global.Settings.osuDiscordUserDatabase[Context.User.Id], true);
                }
                else
                {
                    user = await api.GetUser(Username, true);
                }
            }
            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                await Context.Channel.SendMessageAsync(GetEntry("InvalidUser", "USERNAME", Username));
                return;
            }

            BestScore[] scores = await api.GetUserBestScores(user.UserId, Limit: 100);
            recentBest = Math.Min(scores.Length, Math.Max(0, recentBest));

            if (scores.Length == 0)
            {
                await Context.Channel.SendMessageAsync(GetEntry("DidNotPlay", "USERNAME", user.UserName));
                return;
            }

            BestScore score = scores.OrderByDescending(t => t.Date).ElementAt(recentBest);

            PPCalcSTDReport result = PPCalculator.CountStd(score.BeatmapId, score.MaxCombo, score.Count100, score.Count50, score.Misses, 0, score.Mods);

            Beatmap apiBeatmap = await api.GetBeatmapAsync(score.BeatmapId, ChannelId: Context.Channel.Id);


            EmbedBuilder Builder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"{ user.UserName }: ({ user.PP.ToString("N2") }pp #{ user.Rank.ToString("N0") }" +
                        $" - { user.CountryFlag }: #{ user.CountryRank.ToString("N0") }) ",
                    IconUrl = "https://a.ppy.sh/" + user.UserId,
                    Url = "https://osu.ppy.sh/u/" + user.UserId
                },
                Title = $"{ apiBeatmap.Artist } - { apiBeatmap.Title } [{ apiBeatmap.DifficultyName }]",
                Url = "https://osu.ppy.sh/b/" + apiBeatmap.BeatmapId,
                ThumbnailUrl = apiBeatmap.ThumbnailURL,
                Color = await Global.GetAverageColorAsync("https://a.ppy.sh/" + user.UserId)
            };

            double PP = result.PP;

            Builder.Description = GetEntry("PersonalBest", "NUMBER", (scores.ToList().FindIndex(t => t.ScoreId == score.ScoreId) + 1).ToString());
            PP = score.PP;

            string dash = "";
            UserScore[] Scores = await api.GetBeatmapTopScores(score.BeatmapId, Limit: 100);

            string peepee = PP.ToString("N2");
            string maxpeepee = result.MaxPP.ToString("N2");

            string SameAccFCpp = "";
            if (score.MaxCombo != result.MaxCombo)
                SameAccFCpp = $" ({ GetEntry("SameACCFC", "ACCURACY", result.SameAccFCPP.ToString("N2")) })";

            EmbedFieldBuilder field1 = new EmbedFieldBuilder()
            {
                Name = GetEntry("PlayInfo"),
                Value = GetEntry("ScoreLine", "SCORE", score.Score.ToString("N0"),
                                              "ACCURACY", score.Accuracy.ToString("N2"),
                                              "RANK", score.Rank) + "\n" +

                        GetEntry("ComboLine", "CURRENT", score.MaxCombo.ToString("N0"),
                                              "MAX", result.MaxCombo.ToString("N0"),
                                              "300", score.Count300.ToString("N0"),
                                              "100", score.Count100.ToString("N0"),
                                              "50", score.Count50.ToString("N0"),
                                              "MISS", score.Misses.ToString("N0")) + "\n" +

                        GetEntry("ModsLine", "MODS", GetShortMods(score.Mods)) + "\n" +

                        $"**__pp: { dash }{ PP.ToString("N2") }pp / { result.MaxPP.ToString("N2") }pp{ dash }__**{ SameAccFCpp }"
            };

            int mrank = Scores.ToList().FindIndex(t => t.ScoreId == score.ScoreId) + 1;
            if (mrank != 0)
            {
                field1.Value += GetEntry("BeatmapRank", "RANK", mrank.ToString());
            }

            Builder.AddField(field1);

            float AR = apiBeatmap.AR;
            float CS = apiBeatmap.CS;
            float HP = apiBeatmap.HP;
            float OD = apiBeatmap.OD;

            float bpm_multiplier = 1;
            double length_multiplier = 1;

            Mods mods = ((Mods)score.Mods);

            if (mods.HasFlag(Mods.HardRock))
            {
                AR = CalcHRParameter(AR);
                CS = (float)(CS * 1.3);
                HP = CalcHRParameter(HP);
                OD = CalcHRParameter(OD);
            }
            else if (mods.HasFlag(Mods.Easy))
            {
                AR /= 2;
                CS /= 2;
                HP /= 2;
                OD /= 2;
            }
            if (mods.HasFlag(Mods.DoubleTime))
            {
                AR = CalcDTParameter(AR);
                HP = CalcDTParameter(HP);
                OD = CalcDTParameter(OD);
                bpm_multiplier = 1.5f;
                length_multiplier = 2.0 / 3.0;
            }
            else if (mods.HasFlag(Mods.HalfTime))
            {
                AR = CalcHTParameter(AR);
                HP = CalcHTParameter(HP);
                OD = CalcHTParameter(OD);
                bpm_multiplier = .75f;
                length_multiplier = 4.0 / 3.0;
            }

            string Bdiff = $"CS: { CS.ToString("N2") } | AR: { AR.ToString("N2") } | HP: { HP.ToString("N2") } | OD: { OD.ToString("N2") }";

            Builder.AddField(GetEntry("BeatmapInfoHeader"),
                            GetEntry("BeatmapInfoContent", "LENGTH", TimeSpan.FromSeconds(apiBeatmap.TotalLength * length_multiplier).ToString("mm\\:ss"),
                                                           "DRAIN", TimeSpan.FromSeconds(apiBeatmap.HitLength * length_multiplier).ToString("mm\\:ss"),
                                                           "BPM", (apiBeatmap.BPM * bpm_multiplier).ToString(),
                                                           "BDIFF", Bdiff,
                                                           "STARS", result.Stars.ToString("N2"),
                                                           "BSID", apiBeatmap.BeatmapSetId.ToString()));

            Builder.Footer = new EmbedFooterBuilder()
            {
                Text = GetEntry("BeatmapInfoFooter", "MAPPER", apiBeatmap.Creator),
                IconUrl = "https://a.ppy.sh/" + apiBeatmap.CreatorId
            };
            Builder.Timestamp = score.Date.Add(DateTime.Now - DateTime.UtcNow);

            await Context.Channel.SendMessageAsync(embed: Builder.Build());
        }

        [Command("best"), Alias("ts", "topscore"), Summary("Gets the top play of a user"), osuAPIRequired]
        public async Task TopAsync(params string[] Args)
        {
            osuApi api = Global.osuAPI;

            int topCount = 0;
            string Username = "";
            if (Args.Length == 0 && !Global.Settings.osuDiscordUserDatabase.ContainsKey(Context.User.Id))
            {
                Username = await Tools.GetNicknameOrUsernameAsync(Context, Context.User);
            }
            else if (Args.Length != 0)
            {
                if (int.TryParse(Args[Args.Length - 1], out topCount))
                {
                    topCount = Math.Min(99, Math.Max(0, topCount));
                    topCount--;
                    Args = Args.Take(Args.Length - 1).ToArray();
                }
                Username = string.Join(" ", Args);
            }
            User user = default(User);

            if (Context.Message.MentionedUserIds.Count == 1)
            {
                IGuildUser gUser = await Context.Guild.GetUserAsync(Context.Message.MentionedUserIds.ToList()[0]);

                string usrName = gUser.Nickname ?? gUser.Username;
                if (Global.Settings.osuDiscordUserDatabase.ContainsKey(gUser.Id))
                {
                    user = await api.GetUser(Global.Settings.osuDiscordUserDatabase[gUser.Id], true);
                }
                else
                {
                    user = await api.GetUser(usrName, true);
                }
            }
            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                if (Global.Settings.osuDiscordUserDatabase.ContainsKey(Context.User.Id) && Username.Length == 0)
                {
                    user = await api.GetUser(Global.Settings.osuDiscordUserDatabase[Context.User.Id], true);
                }
                else
                {
                    user = await api.GetUser(Username, true);
                }
            }
            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                await Context.Channel.SendMessageAsync(GetEntry("InvalidUser", "USERNAME", Username));
                return;
            }

            BestScore[] scores = await api.GetUserBestScores(user.UserId, Limit: 100);
            topCount = Math.Min(scores.Length, Math.Max(topCount, 0));

            if (scores.Length == 0)
            {
                await Context.Channel.SendMessageAsync(GetEntry("DidNotPlay", "USERNAME", user.UserName));
                return;
            }

            BestScore score = scores[topCount];

            PPCalcSTDReport result = PPCalculator.CountStd(score.BeatmapId, score.MaxCombo, score.Count100, score.Count50, score.Misses, 0, score.Mods);

            Beatmap apiBeatmap = await api.GetBeatmapAsync(score.BeatmapId, ChannelId: Context.Channel.Id);


            EmbedBuilder Builder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"{ user.UserName }: ({ user.PP.ToString("N2") }pp #{ user.Rank.ToString("N0") }" +
                        $" - { user.CountryFlag }: #{ user.CountryRank.ToString("N0") }) ",
                    IconUrl = "https://a.ppy.sh/" + user.UserId,
                    Url = "https://osu.ppy.sh/u/" + user.UserId
                },
                Title = $"{ apiBeatmap.Artist } - { apiBeatmap.Title } [{ apiBeatmap.DifficultyName }]",
                Url = "https://osu.ppy.sh/b/" + apiBeatmap.BeatmapId,
                ThumbnailUrl = apiBeatmap.ThumbnailURL,
                Color = await Global.GetAverageColorAsync("https://a.ppy.sh/" + user.UserId)
            };

            double PP = result.PP;

            Builder.Description = GetEntry("PersonalBest", "NUMBER", (topCount + 1).ToString());
            PP = score.PP;

            string dash = "";
            UserScore[] Scores = await api.GetBeatmapTopScores(score.BeatmapId, Limit: 100);

            string peepee = PP.ToString("N2");
            string maxpeepee = result.MaxPP.ToString("N2");

            string SameAccFCpp = "";
            if (score.MaxCombo != result.MaxCombo)
                SameAccFCpp = $" ({ GetEntry("SameACCFC", "ACCURACY", result.SameAccFCPP.ToString("N2")) })";

            EmbedFieldBuilder field1 = new EmbedFieldBuilder()
            {
                Name = GetEntry("PlayInfo"),
                Value = GetEntry("ScoreLine", "SCORE", score.Score.ToString("N0"),
                                              "ACCURACY", score.Accuracy.ToString("N2"),
                                              "RANK", score.Rank) + "\n" +

                        GetEntry("ComboLine", "CURRENT", score.MaxCombo.ToString("N0"),
                                              "MAX", result.MaxCombo.ToString("N0"),
                                              "300", score.Count300.ToString("N0"),
                                              "100", score.Count100.ToString("N0"),
                                              "50", score.Count50.ToString("N0"),
                                              "MISS", score.Misses.ToString("N0")) + "\n" +

                        GetEntry("ModsLine", "MODS", GetShortMods(score.Mods)) + "\n" +

                        $"**__pp: { dash }{ PP.ToString("N2") }pp / { result.MaxPP.ToString("N2") }pp{ dash }__**{ SameAccFCpp }"
            };

            int mrank = Scores.ToList().FindIndex(t => t.ScoreId == score.ScoreId) + 1;
            if (mrank != 0)
            {
                field1.Value += GetEntry("BeatmapRank", "RANK", mrank.ToString());
            }

            Builder.AddField(field1);

            float AR = apiBeatmap.AR;
            float CS = apiBeatmap.CS;
            float HP = apiBeatmap.HP;
            float OD = apiBeatmap.OD;

            float bpm_multiplier = 1;
            double length_multiplier = 1;

            Mods mods = ((Mods)score.Mods);

            if (mods.HasFlag(Mods.HardRock))
            {
                AR = CalcHRParameter(AR);
                CS = (float)(CS * 1.3);
                HP = CalcHRParameter(HP);
                OD = CalcHRParameter(OD);
            }
            if (mods.HasFlag(Mods.DoubleTime))
            {
                AR = CalcDTParameter(AR);
                HP = CalcDTParameter(HP);
                OD = CalcDTParameter(OD);
                bpm_multiplier = 1.5f;
                length_multiplier = 2.0 / 3.0;
            }
            else if (mods.HasFlag(Mods.HalfTime))
            {
                AR = CalcHTParameter(AR);
                HP = CalcHTParameter(HP);
                OD = CalcHTParameter(OD);
                bpm_multiplier = .75f;
                length_multiplier = 4.0 / 3.0;
            }

            string Bdiff = $"CS: { CS.ToString("N2") } | AR: { AR.ToString("N2") } | HP: { HP.ToString("N2") } | OD: { OD.ToString("N2") }";

            Builder.AddField(GetEntry("BeatmapInfoHeader"),
                            GetEntry("BeatmapInfoContent", "LENGTH", TimeSpan.FromSeconds(apiBeatmap.TotalLength * length_multiplier).ToString("mm\\:ss"),
                                                           "DRAIN", TimeSpan.FromSeconds(apiBeatmap.HitLength * length_multiplier).ToString("mm\\:ss"),
                                                           "BPM", (apiBeatmap.BPM * bpm_multiplier).ToString(),
                                                           "BDIFF", Bdiff,
                                                           "STARS", result.Stars.ToString("N2"),
                                                           "BSID", apiBeatmap.BeatmapSetId.ToString()));

            Builder.Footer = new EmbedFooterBuilder()
            {
                Text = GetEntry("BeatmapInfoFooter", "MAPPER", apiBeatmap.Creator),
                IconUrl = "https://a.ppy.sh/" + apiBeatmap.CreatorId
            };
            Builder.Timestamp = score.Date.Add(DateTime.Now - DateTime.UtcNow);

            await Context.Channel.SendMessageAsync(embed: Builder.Build());
        }

        [Command("recent"), Alias("r", "rs"), Summary("Gets the most recent score of an osu! player from the official server"), osuAPIRequired]
        public async Task RecentAsync(params string[] Args)
        {
            osuApi api = Global.osuAPI;

            string Username = "";
            if (Args.Length == 0 && !Global.Settings.osuDiscordUserDatabase.ContainsKey(Context.User.Id))
            {
                Username = await Tools.GetNicknameOrUsernameAsync(Context, Context.User);
            }
            else
            {
                Username = string.Join(" ", Args);
            }
            User user = default(User);

            if (Context.Message.MentionedUserIds.Count == 1)
            {
                IGuildUser gUser = await Context.Guild.GetUserAsync(Context.Message.MentionedUserIds.ToList()[0]);

                string usrName = gUser.Nickname ?? gUser.Username;
                if (Global.Settings.osuDiscordUserDatabase.ContainsKey(gUser.Id))
                {
                    user = await api.GetUser(Global.Settings.osuDiscordUserDatabase[gUser.Id], true);
                }
                else
                {
                    user = await api.GetUser(usrName, true);
                }
            }
            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                if (Global.Settings.osuDiscordUserDatabase.ContainsKey(Context.User.Id) && Username.Length == 0)
                {
                    user = await api.GetUser(Global.Settings.osuDiscordUserDatabase[Context.User.Id], true);
                }
                else
                {
                    user = await api.GetUser(Username, true);
                }
            }
            
            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                await Context.Channel.SendMessageAsync(GetEntry("InvalidUser", "USERNAME", Username));
                return;
            }
            
            RecentScore[] recent_scores = await api.GetUserRecentScores(user.UserName);

            if (recent_scores.Length == 0)
            {
                await Context.Channel.SendMessageAsync(GetEntry("DidNotPlay", "USERNAME", user.UserName));
                return;
            }
            RecentScore recent = recent_scores[0];

            PPCalcSTDReport result = PPCalculator.CountStd(recent.BeatmapId, recent.MaxCombo, recent.Count100, recent.Count50, recent.Misses, 0, recent.Mods);
            
            Beatmap apiBeatmap = await api.GetBeatmapAsync(recent.BeatmapId, ChannelId: Context.Channel.Id);

            BestScore[] scores = await api.GetUserBestScores(recent.UserId, Limit: 100);

            EmbedBuilder Builder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"{ user.UserName }: ({ user.PP.ToString("N2") }pp #{ user.Rank.ToString("N0") }" +
                        $" - { user.CountryFlag }: #{ user.CountryRank.ToString("N0") }) ",
                    IconUrl = "https://a.ppy.sh/" + user.UserId,
                    Url = "https://osu.ppy.sh/u/" + user.UserId
                },
                Title = $"{ apiBeatmap.Artist } - { apiBeatmap.Title } [{ apiBeatmap.DifficultyName }]",
                Url = "https://osu.ppy.sh/b/" + apiBeatmap.BeatmapId,
                ThumbnailUrl = apiBeatmap.ThumbnailURL,
                Color = await Global.GetAverageColorAsync("https://a.ppy.sh/" + user.UserId)
            };

            double PP = result.PP;


            for (int i = 0; i < scores.Length; i++)
            {
                if (scores[i].Score == recent.Score
                    && scores[i].BeatmapId == recent.BeatmapId
                    && scores[i].Mods == recent.Mods)
                {
                    Builder.Description = GetEntry("PersonalBest", "NUMBER", (i + 1).ToString());
                    PP = scores[i].PP;
                    break;
                }
            }

            string dash = "";
            UserScore[] Scores = await api.GetBeatmapTopScores(recent.BeatmapId, Limit: 100);
            if (recent.Rank == "F" || !apiBeatmap.CanGetPP)
            {
                dash = "~~";
            }
            else
            {
                UserScore[] ScoresOnMap = await api.GetBeatmapTopScores(recent.BeatmapId, recent.UserId);

                bool NewPlaySuperior = false;
                for (int i = 0; i < ScoresOnMap.Length; i++)
                {
                    if (ScoresOnMap[i].Score == recent.Score && ScoresOnMap[i].Mods == recent.Mods)
                    {
                        NewPlaySuperior = true;
                        break;
                    }
                }
                if (!NewPlaySuperior)
                {
                    dash = "~~";
                }
            }
            
            string peepee = PP.ToString("N2");
            string maxpeepee = result.MaxPP.ToString("N2");

            string SameAccFCpp = "";
            if (recent.MaxCombo != result.MaxCombo)
                SameAccFCpp = $" ({ GetEntry("SameACCFC", "ACCURACY", result.SameAccFCPP.ToString("N2")) })";

            double Progress = Math.Truncate(recent.HitCount / result.HitCount * 10000) / 100;

            EmbedFieldBuilder field1 = new EmbedFieldBuilder()
            {
                Name = GetEntry("PlayInfo") + (recent.Rank == "F" ? $" ({ Progress.ToString("N2") }%)" : ""),
                Value = GetEntry("ScoreLine", "SCORE", recent.Score.ToString("N0"),
                                              "ACCURACY", recent.Accuracy.ToString("N2"),
                                              "RANK", recent.Rank) + "\n" +

                        GetEntry("ComboLine", "CURRENT", recent.MaxCombo.ToString("N0"),
                                              "MAX", result.MaxCombo.ToString("N0"),
                                              "300", recent.Count300.ToString("N0"),
                                              "100", recent.Count100.ToString("N0"),
                                              "50", recent.Count50.ToString("N0"),
                                              "MISS", recent.Misses.ToString("N0")) + "\n" +

                        GetEntry("ModsLine", "MODS", GetShortMods(recent.Mods)) + "\n" +

                        $"**__pp: { dash }{ PP.ToString("N2") }pp / { result.MaxPP.ToString("N2") }pp{ dash }__**{ SameAccFCpp }"
            };

            for (int i = 0; i < Scores.Length; i++)
            {
                if (Scores[i].Score == recent.Score
                    && Scores[i].UserId == recent.UserId
                    && Scores[i].Mods == recent.Mods)
                {
                    field1.Value += GetEntry("BeatmapRank", "RANK", (i + 1).ToString());
                    break;
                }
            }

            Builder.AddField(field1);

            float AR = apiBeatmap.AR;
            float CS = apiBeatmap.CS;
            float HP = apiBeatmap.HP;
            float OD = apiBeatmap.OD;

            float bpm_multiplier = 1;
            double length_multiplier = 1;

            Mods mods = ((Mods)recent.Mods);

            if (mods.HasFlag(Mods.HardRock))
            {
                AR = CalcHRParameter(AR);
                CS = (float)(CS * 1.3);
                HP = CalcHRParameter(HP);
                OD = CalcHRParameter(OD);
            }
            if (mods.HasFlag(Mods.DoubleTime))
            {
                AR = CalcDTParameter(AR);
                HP = CalcDTParameter(HP);
                OD = CalcDTParameter(OD);
                bpm_multiplier = 1.5f;
                length_multiplier = 2.0 / 3.0;
            }
            else if (mods.HasFlag(Mods.HalfTime))
            {
                AR = CalcHTParameter(AR);
                HP = CalcHTParameter(HP);
                OD = CalcHTParameter(OD);
                bpm_multiplier = .75f;
                length_multiplier = 4.0 / 3.0;
            }
            
            string Bdiff = $"CS: { CS.ToString("N2") } | AR: { AR.ToString("N2") } | HP: { HP.ToString("N2") } | OD: { OD.ToString("N2") }";
            
            Builder.AddField(GetEntry("BeatmapInfoHeader"),
                            GetEntry("BeatmapInfoContent", "LENGTH", TimeSpan.FromSeconds(apiBeatmap.TotalLength * length_multiplier).ToString("mm\\:ss"),
                                                           "DRAIN", TimeSpan.FromSeconds(apiBeatmap.HitLength * length_multiplier).ToString("mm\\:ss"),
                                                           "BPM", (apiBeatmap.BPM * bpm_multiplier).ToString(),
                                                           "BDIFF", Bdiff,
                                                           "STARS", result.Stars.ToString("N2"),
                                                           "BSID", apiBeatmap.BeatmapSetId.ToString()));

            Builder.Footer = new EmbedFooterBuilder()
            {
                Text = GetEntry("BeatmapInfoFooter", "MAPPER", apiBeatmap.Creator),
                IconUrl = "https://a.ppy.sh/" + apiBeatmap.CreatorId
            };
            Builder.Timestamp = recent.Date.AddHours(1);

            int try_count = 0;

            for (int i = 0; i < recent_scores.Length; i++)
            {
                if (recent_scores[i].BeatmapId == recent.BeatmapId && recent_scores[i].Mods == recent.Mods)
                {
                    try_count++;
                }
                else break;
            }
            
            await Context.Channel.SendMessageAsync(GetEntry("Try", "TRY", try_count.ToString()), embed: Builder.Build());
        }

        [Command("compare"), Summary("Returns the user's play on the beatmap what was the most recent in this channel"), osuAPIRequired]
        public async Task CompareAsync(params string[] Args)
        {
            osuApi api = Global.osuAPI;

            if (!api.LastMapCache.ContainsKey(Context.Channel.Id))
            {
                await Context.Channel.SendMessageAsync(GetEntry("NoMap"));
                return;
            }
            
            string Username = "";
            
            if (Args.Length == 0 && !Global.Settings.osuDiscordUserDatabase.ContainsKey(Context.User.Id))
            {
                Username = await Tools.GetNicknameOrUsernameAsync(Context, Context.User);
            }
            else
            {
                Username = string.Join(" ", Args);
            }
            User user = default(User);
            if (Context.Message.MentionedUserIds.Count == 1)
            {
                IGuildUser gUser = await Context.Guild.GetUserAsync(Context.Message.MentionedUserIds.ToList()[0]);

                string usrName = gUser.Nickname ?? gUser.Username;
                if (Global.Settings.osuDiscordUserDatabase.ContainsKey(gUser.Id))
                {
                    user = await api.GetUser(Global.Settings.osuDiscordUserDatabase[gUser.Id], true);
                }
                else
                {
                    user = await api.GetUser(usrName, true);
                }
            }
            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                if (Global.Settings.osuDiscordUserDatabase.ContainsKey(Context.User.Id) && Username.Length == 0)
                {
                    user = await api.GetUser(Global.Settings.osuDiscordUserDatabase[Context.User.Id], true);
                }
                else
                {
                    user = await api.GetUser(Username, true);
                }

            }

            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                await Context.Channel.SendMessageAsync(GetEntry("InvalidUser", "USERNAME", Username));
                return;
            }

            int BeatmapId = api.LastMapCache[Context.Channel.Id].FindLast(p => api.RankedBeatmapCache.ContainsKey(p));


            UserScore[] Scores = await api.GetBeatmapTopScores(BeatmapId, user.UserId);
            if (Scores.Length == 0)
            {
                await Context.Channel.SendMessageAsync(GetEntry("NoScores", "USERNAME", user.UserName));
                return;
            }
            Models.osuAPI.Beatmap apiBeatmap = await api.GetBeatmapAsync(BeatmapId);
            
            EmbedBuilder Builder = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"{ user.UserName } ({ user.PP.ToString("N2") }pp #{ user.Rank.ToString("N0") }" +
                        $" - { user.CountryFlag }: #{ user.CountryRank.ToString("N0") }) ",
                    IconUrl = "https://a.ppy.sh/" + user.UserId,
                    Url = "https://osu.ppy.sh/u/" + user.UserId
                },
                Title = $"{ apiBeatmap.Artist } - { apiBeatmap.Title } [{ apiBeatmap.DifficultyName }]",
                Url = "https://osu.ppy.sh/b/" + apiBeatmap.BeatmapId,
                ThumbnailUrl = apiBeatmap.ThumbnailURL,
                Color = await Global.GetAverageColorAsync("https://a.ppy.sh/" + user.UserId)
            };

            int MaxCombo = apiBeatmap.MaxCombo;

            foreach (UserScore score in Scores)
            {
                Mods mods = (Mods)score.Mods;

                PPCalcSTDReport result = PPCalculator.CountStd(BeatmapId, score.MaxCombo, score.Count100, score.Count50, score.Misses, 0, score.Mods);

                float AR = apiBeatmap.AR;
                float CS = apiBeatmap.CS;
                float HP = apiBeatmap.HP;
                float OD = apiBeatmap.OD;

                if (mods.HasFlag(Mods.HardRock))
                {
                    AR = CalcHRParameter(AR);
                    CS = (float)(CS * 1.3);
                    HP = CalcHRParameter(HP);
                    OD = CalcHRParameter(OD);
                }

                if (mods.HasFlag(Mods.DoubleTime))
                {
                    AR = CalcDTParameter(AR);
                    HP = CalcDTParameter(HP);
                    OD = CalcDTParameter(OD);
                }
                else if (mods.HasFlag(Mods.HalfTime))
                {
                    AR = CalcHTParameter(AR);
                    HP = CalcHTParameter(HP);
                    OD = CalcHTParameter(OD);
                }

                string SameAccFCpp = "";
                if (score.MaxCombo != apiBeatmap.MaxCombo)
                    SameAccFCpp = $" ({ GetEntry("SameACCFC", "ACCURACYPP", result.SameAccFCPP.ToString("N2")) })";

                string PPLine = $"__**pp: { result.PP.ToString("N2") }pp / { result.MaxPP.ToString("N2") }pp**__{ SameAccFCpp }";
                string HitInfo = $"{ score.Count300.ToString("N0") } / { score.Count100.ToString("N0") } / { score.Count50.ToString("N0") } / { score.Misses.ToString("N0") }";
                
                Builder.AddField("+" + GetShortMods((int)score.Mods) + $" [*{ result.Stars.ToString("N2") }★*] - { score.Date.ToString() }",
                    GetEntry("FieldContent", "SCORE", score.Score.ToString("N0"),
                                             "ACC", score.Accuracy.ToString("N2"),
                                             "RANK", score.Rank,
                                             "CURRENT", score.MaxCombo.ToString("N0"),
                                             "MAX", MaxCombo.ToString("N0"),
                                             "HITINFO", HitInfo,
                                             "PPLINE", PPLine));
            }
            
            Builder.AddField(GetEntry("BeatmapInfoHeader"),
                GetEntry("BeatmapInfoContent",
                "BSID", apiBeatmap.BeatmapSetId.ToString()));

            await Context.Channel.SendMessageAsync("", embed: Builder.Build());
        }

        [Command("pair"), Summary("Pair your Discord account with an osu! account. Use `pair ?` to display the instructions!"), osuAPIRequired]
        public async Task PairAsync([Remainder]string Arg = "")
        {
            if (string.IsNullOrWhiteSpace(Arg))
            {
                Global.Settings.osuDiscordUserDatabase.Remove(Context.User.Id);
                Global.SaveSettings();
                await Context.Channel.SendMessageAsync(GetEntry("BindingReleased"));
            }
            else
            {
                if (Global.osuAPI != null)
                {
                    User user = default(User);
                    if (int.TryParse(Arg, out int userId))
                    {
                        if (userId.ToString() == Arg)
                        {
                            user = await Global.osuAPI.GetUser(userId);
                        }
                    }

                    if (string.IsNullOrWhiteSpace(user.UserName))
                    {
                        user = await Global.osuAPI.GetUser(Arg);
                    }

                    if (string.IsNullOrWhiteSpace(user.UserName))
                    {
                        await Context.Channel.SendMessageAsync(GetGlobalEntry("UserNotFound"));
                    }
                    else
                    {
                        Global.Settings.osuDiscordUserDatabase.Remove(Context.User.Id);
                        Global.Settings.osuDiscordUserDatabase.Add(Context.User.Id, user.UserId);
                        Global.SaveSettings();
                        await Context.Channel.SendMessageAsync(GetEntry("Paired", "MENTION", Context.User.Mention, "USERNAME", user.UserName));
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync(GetEntry("APINotAvailable"));
                }
            }
        }

        [Command("pp"), osuAPIRequired]
        public async Task PPAsync(params string[] Args)
        {
            osuApi api = Global.osuAPI;
            osuBeatmapUtilities.BeatmapManager manager = Global.Beatmaps;

            int BeatmapId = -1;

            bool gaveNumber = false;
            if (Args.Length > 0)
            {
                if (gaveNumber = int.TryParse(Args[0], out BeatmapId))
                {
                    if (BeatmapId > 0)
                    {
                        Args = Args.Skip(1).ToArray();
                    }
                    else
                    {
                        gaveNumber = false;

                    }
                }
            }
            if (BeatmapId <= 0)
            {
                if (gaveNumber)
                {
                    await ReplyAsync(GetEntry("GiveValidId"));
                    return;
                }
                else
                {
                    List<int> ids = new List<int>();
                    if (api.LastMapCache.TryGetValue(Context.Channel.Id, out ids))
                    {
                        if (ids.Count > 0)
                            BeatmapId = ids.Last();
                    }

                    if (BeatmapId < 0)
                    {
                        await ReplyAsync(GetEntry("NoLastMap"));
                        return;
                    }
                }
            }
            
            ScoreInfo info = new ScoreInfo();
            for (int i = 0; i < 7; i++)
            {
                info.Statistics[(HitResult)i] = 0;
            }
            for (int i = 0; i < Args.Length; i++)
            {
                int s = -1;
                switch (Args[i].ToLower())
                {
                    case "-300s":
                        if (Args.Length <= i + 1 || !int.TryParse(Args[i + 1], out s) || s < 0)
                        {
                            await ReplyAsync(GetEntry("CheckUsage"));
                            return;
                        }
                        info.Statistics[HitResult.Perfect] = s;
                        break;
                    case "-300":
                        if (Args.Length <= i + 1 || !int.TryParse(Args[i + 1], out s) || s < 0)
                        {
                            await ReplyAsync(GetEntry("CheckUsage"));
                            return;
                        }
                        info.Statistics[HitResult.Great] = s;
                        break;
                    case "-100":
                        if (Args.Length <= i + 1 || !int.TryParse(Args[i + 1], out s) || s < 0)
                        {
                            await ReplyAsync(GetEntry("CheckUsage"));
                            return;
                        }
                        info.Statistics[HitResult.Good] = s;
                        break;
                    case "-50":
                        if (Args.Length <= i + 1 || !int.TryParse(Args[i + 1], out s) || s < 0)
                        {
                            await ReplyAsync(GetEntry("CheckUsage"));
                            return;
                        }
                        info.Statistics[HitResult.Meh] = s;
                        break;
                    case "-miss":
                        if (Args.Length <= i + 1 || !int.TryParse(Args[i + 1], out s) || s < 0)
                        {
                            await ReplyAsync(GetEntry("CheckUsage"));
                            return;
                        }
                        info.Statistics[HitResult.Miss] = s;
                        break;
                    case "-score":
                        if (Args.Length <= i + 1 || !int.TryParse(Args[i + 1], out s) || s < 0)
                        {
                            await ReplyAsync(GetEntry("CheckUsage"));
                            return;
                        }
                        info.TotalScore = s;
                        break;
                    case "-acc":
                        if (Args.Length <= i + 1 || !double.TryParse(Args[i + 1], out double acc) || acc < 0)
                        {
                            await ReplyAsync(GetEntry("CheckUsage"));
                            return;
                        }
                        info.Accuracy = acc;
                        break;
                    case "-combo":
                        if (Args.Length <= i + 1 || !int.TryParse(Args[i + 1], out s) || s < 0)
                        {
                            await ReplyAsync(GetEntry("CheckUsage"));
                            return;
                        }
                        info.MaxCombo = s;
                        break;
                }
            }
            
            osuBeatmapUtilities.Beatmap beatmap = manager.LoadBeatmap(BeatmapId);
            if (beatmap == null)
            {
                await ReplyAsync(GetEntry("CoudNotDownloadMap", "ID", BeatmapId.ToString()));
                return;
            }

            List<osuBeatmapUtilities.Mod> mods = new List<osuBeatmapUtilities.Mod>();
            Ruleset ruleset = null;

            string hitres = "";

            switch (beatmap.Mode)
            {
                case osuBeatmapUtilities.Mode.Osu:
                    ruleset = new OsuRuleset();
                    break;
                default:
                    await ReplyAsync(GetEntry("ModeNotSupported"));
                    return;
            }
            if (Args.Length > 0)
            {
                string joined = string.Join("", Args).ToLower();
                IEnumerable<osuBeatmapUtilities.Mod> allMods = ruleset.GetAllMods();

                foreach (osuBeatmapUtilities.Mod mod in allMods)
                {
                    if (joined.Contains(mod.Acronym.ToLower()))
                        mods.Add(mod);
                }
            }
            info.Mods = mods.ToArray();
            PerformanceCalculator calc = ruleset.CreatePerformanceCalculator(beatmap.WorkingBeatmap, info);
            Dictionary<string, double> inf = new Dictionary<string, double>();
            double pp = calc.Calculate(inf);
            info = calc.FixedScore;

            switch (beatmap.Mode)
            {
                case osuBeatmapUtilities.Mode.Osu:
                    hitres = $"[{ info.Statistics[HitResult.Great] }/{ info.Statistics[HitResult.Good] }/{ info.Statistics[HitResult.Meh] }/{ info.Statistics[HitResult.Miss] }]";
                    break;
            }
            
            var metadata = beatmap.WorkingBeatmap.BeatmapInfo.Metadata;

            string thurl = "https://b.ppy.sh/thumb/" + beatmap.WorkingBeatmap.BeatmapInfo.BeatmapSet.OnlineBeatmapSetID + "l.jpg";
            EmbedBuilder builder = new EmbedBuilder()
            {
                Title = $"{ metadata.Artist } - { metadata.Title } [{ beatmap.WorkingBeatmap.BeatmapInfo.Version }]",
                Url = "https://osu.ppy.sh/b/" + BeatmapId,
                ThumbnailUrl = thurl,
                Footer = new EmbedFooterBuilder()
                {
                    Text = GetEntry("MadeBy", "NAME", metadata.AuthorString)
                },
                Fields = new List<EmbedFieldBuilder>()
                {
                    new EmbedFieldBuilder()
                    {
                        IsInline = true,
                        Name = GetEntry("Hits"),
                        Value = hitres + " - " + info.Accuracy.ToString("N2") + "%"
                    },
                    new EmbedFieldBuilder()
                    {
                        IsInline = true,
                        Name = GetEntry("Mods"),
                        Value = mods.Count > 0 ? string.Join(", ", mods.Select(t => t.Name)) : "NoMod"
                    },
                    new EmbedFieldBuilder()
                    {
                        IsInline = true,
                        Name = GetEntry("Combo"),
                        Value = info.MaxCombo + "x/" + inf["Max Combo"] + "x"
                    },
                    new EmbedFieldBuilder()
                    {
                        IsInline = true,
                        Name = GetEntry("ppsr"),
                        Value = "**" + pp.ToString("N2") + $"pp** // **{ inf["Star Rating"].ToString("N2") }\\***"
                    }
                },
                Color = await Global.GetAverageColorAsync(thurl)
            };
            await ReplyAsync("", embed: builder.Build());
        }

        [Command("track"), osuAPIRequired, Admin, Summary("osu! score tracking, write `{PREFIX}track` for more info")]
        public async Task TrackAsync(params string[] Args)
        {
            if (Args.Length == 0)
            {
                await ReplyAsync(GetEntry("AdvancedHelp"));
            }
            else
            {
                Track track = new Track()
                {
                    UserId = 0,
                    ChannelId = Context.Channel.Id,
                    ServerId = Context.Guild.Id,
                    Mode = Mode.Standard,
                    Rank = 0,
                    MinPP = 0,
                    Top = 100,
                    Both = false
                };
                string username = "";
                bool delete = false;
                foreach (string t in Args)
                {
                    string[] split = t.Split('=');
                    if (split.Length == 1)
                    {
                        username += " " + split[0];
                    }
                    else
                    {
                        switch (split[0].ToLower())
                        {
                            case "top":
                                if (int.TryParse(split[1], out int top))
                                {
                                    if (top < 0)
                                    {
                                        top = 0;
                                        await ReplyAsync(GetEntry("TopNegative"));
                                    }
                                    else if (top > 100)
                                    {
                                        top = 100;
                                        await ReplyAsync(GetEntry("TopOver100"));
                                    }
                                    track.Top = top;
                                }
                                else
                                {
                                    await ReplyAsync(GetEntry("TopCannotParse"));
                                }
                                break;
                            case "rank":
                                if (int.TryParse(split[1], out int rank))
                                {
                                    if (rank < 0)
                                    {
                                        rank = 0;
                                        await ReplyAsync(GetEntry("RankNegative"));
                                    }
                                    else if (rank > 100)
                                    {
                                        rank = 100;
                                        await ReplyAsync(GetEntry("RankAbove100"));
                                    }
                                    track.Rank = rank;
                                }
                                else
                                {
                                    await ReplyAsync(GetEntry("RankCannotParse"));
                                }
                                break;
                            case "minpp":
                                if (double.TryParse(split[1], out double pp))
                                {
                                    if (pp < 0)
                                    {
                                        pp = 0;
                                        await ReplyAsync(GetEntry("MinPPNegative"));
                                    }
                                    track.MinPP = pp;
                                }
                                else
                                {
                                    await ReplyAsync(GetEntry("MinPPCannotParse"));
                                }
                                break;
                            case "mode":
                                await ReplyAsync(GetEntry("ModeNotSupported"));
                                break;
                            case "sort":
                                switch (split[1].ToLower())
                                {
                                    case "both":
                                        track.Both = true;
                                        break;
                                    case "either": break;
                                    default:
                                        await ReplyAsync(GetEntry("IncorrectSort"));
                                        break;
                                }
                                break;
                            case "delete":
                                if (split[1].ToLower() != "true")
                                {
                                    await ReplyAsync(GetEntry("IncorrectDelete"));
                                }
                                else
                                {
                                    delete = true;
                                }
                                break;
                        }
                    }
                }
                username = username.Trim();
                if (string.IsNullOrWhiteSpace(username))
                {
                    await ReplyAsync(GetEntry("AdvancedHelp"));
                    return;
                }
                osuTrackUser usr = Global.Tracker.Add(username, Mode.Standard);
                if (usr == null)
                {
                    await ReplyAsync(GetEntry("osuAPIError"));
                    return;
                }

                track.UserId = usr.UserId;
                if (delete)
                {
                    if (Settings.Tracks.ContainsKey(usr.UserId))
                    {
                        int index = Settings.Tracks[usr.UserId].FindIndex(t => track.Identical(t));
                        if (index > -1)
                        {
                            Global.GuildSettings.Modify(Settings.GuildId, t =>
                            {
                                if (t.Tracks[usr.UserId].Count == 1)
                                {
                                    t.Tracks.Remove(usr.UserId);
                                }
                                else
                                {
                                    t.Tracks[usr.UserId].RemoveAt(index);
                                }
                            });
                            Global.Tracker.Check(usr.UserId, usr.Mode);
                            await ReplyAsync(GetEntry("RemovedTracking", "USR", usr.UsernameAtReg));
                            return;
                        }
                    }
                    await ReplyAsync(GetEntry("NotTracked"));
                    return;
                }
                bool updated = false;
                Global.GuildSettings.Modify(Settings.GuildId, t =>
                {
                    if (t.Tracks.ContainsKey(usr.UserId))
                    {
                        for (int i = 0; i < t.Tracks[usr.UserId].Count; i++)
                        {
                            if (track.Identical(t.Tracks[usr.UserId][i]))
                            {
                                t.Tracks[usr.UserId][i].Rank = track.Rank;
                                t.Tracks[usr.UserId][i].Top = track.Top;
                                t.Tracks[usr.UserId][i].MinPP = track.MinPP;
                                updated = true;
                            }
                        }
                    }
                    else
                    {
                        t.Tracks.Add(usr.UserId, new List<Track>()
                        {
                            track
                        });
                    }
                });
                if (updated)
                {
                    await ReplyAsync(GetEntry("TrackModified", "USR", usr.UsernameAtReg, "MODE", track.Mode.ToString())
                                   + GetEntry("TrackInfo", "TOP", track.Top.ToString(), "RANK", track.Rank.ToString(), "PP", track.MinPP.ToString("N2")) +
                       (track.Both ? GetEntry("Both")
                                   : GetEntry("Either")));
                }
                else
                {
                    await ReplyAsync(GetEntry("TrackAdded", "USR", usr.UsernameAtReg, "MODE", track.Mode.ToString())
                                   + GetEntry("TrackInfo", "TOP", track.Top.ToString(), "RANK", track.Rank.ToString(), "PP", track.MinPP.ToString("N2")) +
                       (track.Both ? GetEntry("Both")
                                   : GetEntry("Either")));
                }
            }
        }

        [Command("tracklist"), osuAPIRequired, Admin, Summary("Lists the current tracking on the server")]
        public async Task TrackListAsync(int page = 1)
        {
            if (page < 1) page = 1;

            IEnumerable<Track> tracks = Settings.Tracks.Values.Select(t => t[0]).Where(t => t.ChannelId == Context.Channel.Id);
            int pages = (tracks.Count() / 5) + 1;
            if (page > pages) page = pages;
            tracks = tracks.Skip((page - 1) * 5).Take(Math.Min(tracks.Count() - ((page - 1) * 5), 5));

            EmbedBuilder embed = new EmbedBuilder()
            {
                Title = GetEntry("TrackList"),
                Description = Settings.Tracks.Count == 0 ? GetEntry("Empty") : "",
                Color = new Color(255, 209, 220),
                Footer = new EmbedFooterBuilder()
                {
                    Text = GetEntry("Page") + $" { page } / { pages }"
                }
            };

            List<Track> tracksToRemove = new List<Track>();
            
            foreach (Track t in tracks)
            {
                osuTrackUser usr = Global.Tracker.Get(t.UserId, t.Mode);
                if (usr == null)
                {
                    tracksToRemove.Add(t);
                }
                
                embed.AddField(usr.UsernameAtReg, $"[{ GetEntry("Profile") }](https://osu.ppy.sh/users/{ t.UserId }) - top=`{ t.Top }` | rank=`{ t.Rank }` | minpp=`{ t.MinPP.ToString("N2") }pp`");
            }

            foreach (Track t in tracksToRemove)
            {
                Settings.Tracks.Remove(t.UserId);
            }

            try
            {
                await ReplyAsync("", embed: embed.Build());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [Command("cleartrack"), osuAPIRequired, Admin, Summary("Clears all osu tracking on the server.")]
        public async Task ClearTrackAsync(params string[] args)
        {
            Global.Tracker.RemoveAll(Context.Guild.Id);
            await ReplyAsync("Tracklist cleared!");
        }

        public static string GetShortMods(int EnabledMods, string Separator = "")
        {
            if (EnabledMods == 0) return "NoMod";

            string[] Mods = { "NF", "EZ", "TD", "HD", "HR", "SD", "DT", "RX", "HT", "NC", "FL", "Auto", "SO", "AP", "PF",
                "4 KEY", "5 KEY", "6 KEY", "7 KEY", "8 KEY","FI", "RND", "CN", "TG", "9 KEY", "COOP KEY", "1 KEY", "2 KEY", "3 KEY", "V2", "LM" };

            List<string> Final = new List<string>();

            for (int i = 0; i < Mods.Length; i++)
            {
                int shifted = EnabledMods & (1 << i);
                if ((EnabledMods & (1 << i)) > 0)
                {
                    if (Mods[i] == "NC")
                        Final.Remove("DT");
                    Final.Add(Mods[i]);
                }
            }

            return string.Join(Separator, Final);
        }
        
        public static float CalcDTParameter(float AR)
        {
            float multiplier = (AR - 5) * 10;
            int ms = (int)(1200 - (15 * multiplier));
            int newms = ms - (ms / 3);
            return ((1200 - newms) / (float)150.0) + (float)5.0;
        }
        public static float CalcHTParameter(float AR)
        {
            float multiplier = (AR - 5) * 10;
            int ms = (int)(1200 - (15 * multiplier));
            int newms = ms + (ms / 3);
            return ((1200 - newms) / (float)150.0) + (float)5.0;
        }
        public  static float CalcHRParameter(float AR)
        {
            return (float)Math.Min(10.0, AR * 1.4);
        }
    }

}
