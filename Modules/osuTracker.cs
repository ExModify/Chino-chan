using Chino_chan.Models.osu;
using Chino_chan.Models.osuAPI;
using Chino_chan.Models.Settings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Modules
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    public class osuTracker
    {
        public bool Enabled { get => API != null; }

        readonly osuApi API;
        readonly List<osuTrackUser> Track;
        readonly string Filename = "osuTrack";

        public event Action<osuTrackUser, byte, byte, byte, RecentScore, User, float> OnNewScore;

        public osuTracker(osuApi API)
        {
            if (API == null)
            {
                Logger.Log(LogType.osuTracker, ConsoleColor.Red, "Error", "osu!Tracker is disabled due to missing API!");
                return;
            }
            this.API = API;
            
            if (File.Exists(Filename))
            {
                Track = SaveManager.LoadSettings<List<osuTrackUser>>(Filename);
            }
            else
            {
                Track = new List<osuTrackUser>();
            }

            Task.Run(Check);
        }

        public osuTrackUser Add(string Username, Mode Mode)
        {
            int id;
            User? user = null;
            if (API.IsUserCached(Username))
            {
                id = API.UserNameIdCache[Username.ToLower()];
            }
            else
            {
                user = API.GetUser(Username, true, Mode.Standard).Result;
                id = user.Value.UserId;
                API.UserNameIdCache[Username.ToLower()] = id;
                API.Save(API.UserNameIdCache, API.UserNameIdCachePath);
            }
            int index = Track.FindIndex(t => t.UserId == id && t.Mode == Mode);

            if (index == -1)
            {
                osuTrackUser usr = null;
                try
                {
                    usr = new osuTrackUser(id, Mode, API, user);
                }
                catch
                {
                    return null;
                }
                index = Track.Count;
                Track.Add(usr);
                Save();
            }
            return Track[index];
        }
        public osuTrackUser Get(int Id, Mode Mode)
        {
            return Track.Find(t => t.UserId == Id && t.Mode == Mode); ;
        }

        public void Check(int UserId, Mode Mode)
        {
            bool remove = true;
            for (int i = 0; i < Global.GuildSettings.Settings.Count; i++)
            {
                var setting = Global.GuildSettings.Settings.Values.ElementAt(i);
                if (setting.Tracks.TryGetValue(UserId, out List<Track> tracks))
                {
                    if (tracks.FindIndex(t => t.UserId == UserId && t.Mode == Mode) > -1)
                    {
                        remove = false;
                        break;
                    }
                }
            }
            if (remove)
            {
                Track.RemoveAll(t => t.UserId == UserId && t.Mode == Mode);
                Save();
            }
        }

        public void RemoveAll(ulong GuildId)
        {
            List<int> ids = new List<int>();
            Global.GuildSettings.Modify(GuildId, t =>
            {
                ids.AddRange(t.Tracks.Keys);
                t.Tracks.Clear();
            });

            foreach(int id in ids)
            {
                Check(id, Mode.Standard);
            }
            
            Save();
        }

        async Task Check()
        {
            while (!Entrance.CancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    for (int i = 0; i < Track.Count; i++)
                    {
                        bool save = false;

                        List<RecentScore> recentScores = new List<RecentScore>(await API.GetUserRecentScores(Track[i].UserId, Track[i].Mode, 50));

                        if (recentScores.Count == 0)
                        {
                            continue;
                        }

                        User? usr = null;
                        foreach (RecentScore score in recentScores.Where(t => t.Date > Track[i].LastScoreTime && t.Rank != "F").OrderBy(t => t.Date))
                        {
                            List<BestScore> bestScores = new List<BestScore>(await API.GetUserBestScores(Track[i].UserId, Track[i].Mode, 100));
                            List<UserScore> userScores = new List<UserScore>(await API.GetBeatmapTopScores(score.BeatmapId, null, null, Track[i].Mode, 100));

                            int userBestIndex = bestScores.FindIndex(t => t.Score == score.Score && t.BeatmapId == score.BeatmapId && t.Mods == score.Mods) + 1;
                            int bTopIndex = userScores.FindIndex(t => t.UserId == score.UserId && t.Score == score.Score && t.Mods == score.Mods) + 1;

                            if (userBestIndex == 0)
                            {
                                userBestIndex = 101;
                            }
                            if (bTopIndex == 0)
                            {
                                bTopIndex = 101;
                            }

                            float lastPP = Track[i].PP;
                            if (Track[i].LastScoreTime < score.Date)
                            {
                                Track[i].LastScoreTime = score.Date;
                                save = true;
                            }

                            if (!usr.HasValue)
                            {
                                usr = await API.GetUser(Track[i].UserId, true, Track[i].Mode);
                                Track[i].PP = usr.Value.PP;
                                save = true;
                            }

                            byte counter = 0;
                            foreach (var rs in recentScores)
                            {
                                if (rs.BeatmapId == score.BeatmapId && rs.Score == score.Score && rs.Mods == score.Mods)
                                {
                                    counter++;
                                }
                                if (counter > 0)
                                {
                                    if (rs.BeatmapId == score.BeatmapId)
                                    {
                                        counter++;
                                    }
                                }
                            }
                            OnNewScore?.Invoke(Track[i], (byte)userBestIndex, (byte)bTopIndex, counter, score, usr.Value, lastPP);
                        }

                        if (save)
                        {
                            Save();
                        }

                        await Task.Delay(3000);
                    }
                }
                catch
                {
                    await Task.Delay(5000);
                }
                if (Track.Count == 0)
                    await Task.Delay(1000);
            }
        }

        void Save()
        {
            SaveManager.SaveData(Filename, Track);
        }
    }
}