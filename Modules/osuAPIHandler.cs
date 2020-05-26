using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Chino_chan.Models.osuAPI;
using Newtonsoft.Json;

namespace Chino_chan.Modules
{
    public class osuApi
    {
        #region API Endpoints
        private string Base
        {
            get
            {
                return "https://osu.ppy.sh/api/";
            }
        }
        private string User
        {
            get
            {
                return Base + "get_user?k=" + Global.Settings.Credentials.osu.Token;
            }
        }
        private string Beatmap
        {
            get
            {
                return Base + "get_beatmaps?k=" + Global.Settings.Credentials.osu.Token;
            }
        }
        private string Scores
        {
            get
            {
                return Base + "get_scores?k=" + Global.Settings.Credentials.osu.Token;
            }
        }
        private string Match
        {
            get
            {
                return Base + "get_match?k=" + Global.Settings.Credentials.osu.Token;
            }
        }
        private string UserBest
        {
            get
            {
                return Base + "get_user_best?k=" + Global.Settings.Credentials.osu.Token;
            }
        }
        private string UserRecent
        {
            get
            {
                return Base + "get_user_recent?k=" + Global.Settings.Credentials.osu.Token;
            }
        }
        #endregion

        #region CacheFiles
        public const string Folder = "Data\\osu\\";
        public string RankedBeatmapCachePath = Folder + "RankedBeatmapCache.json";
        public string UserCachePath          = Folder + "UserCache.json";
        public string UserNameIdCachePath    = Folder + "UserNameIdCache.json";
        public string LastMapCachePath       = Folder + "LastMapCache.json";
        #endregion

        public event Action<int> OnResetBegin;

        public Dictionary<int, Beatmap> RankedBeatmapCache;
        public Dictionary<int, User> UserCache;
        public Dictionary<string, int> UserNameIdCache;
        public Dictionary<ulong, List<int>> LastMapCache;


        public int CurrentCalls { get; private set; } = 0;
        private int CallLimit
        {
            get
            {
                return Global.Settings.OSUAPICallLimit;
            }
        }
        private int ValidState = -1;
        private bool IsValid
        {
            get
            {
                if (ValidState == -1)
                {
                    string Content = "";
                    try
                    {
                        Content = new HttpClient().GetStringAsync(User + "&u=2&type=id").Result;
                    }
                    catch
                    {
                        ValidState = 0;
                    }
                    ValidState = Content.Contains("Please provide a valid API key.") ? 0 : 1;
                }
                return ValidState == 1;
            }
        }

        private Timer ResetTimer;

        public osuApi()
        {
            if (!IsValid)
            {
                throw new Exception("Invalid API");
            }

            RankedBeatmapCache = new Dictionary<int, Beatmap>();
            UserCache = new Dictionary<int, User>();
            UserNameIdCache = new Dictionary<string, int>();
            LastMapCache = new Dictionary<ulong, List<int>>();

            if (File.Exists(RankedBeatmapCachePath))
            {
                RankedBeatmapCache = JsonConvert.DeserializeObject<Dictionary<int, Beatmap>>(File.ReadAllText(RankedBeatmapCachePath));
            }
            if (File.Exists(UserCachePath))
            {
                UserCache = JsonConvert.DeserializeObject<Dictionary<int, User>>(File.ReadAllText(UserCachePath));
            }
            if (File.Exists(UserNameIdCachePath))
            {
                UserNameIdCache = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText(UserNameIdCachePath));
            }
            if (File.Exists(LastMapCachePath))
            {
                LastMapCache = JsonConvert.DeserializeObject<Dictionary<ulong, List<int>>>(File.ReadAllText(LastMapCachePath));
            }

            ResetTimer = new Timer(60000)
            {
                Repeat = true
            };
            ResetTimer.Elapsed += () =>
            {
                OnResetBegin?.Invoke(CurrentCalls);
                CurrentCalls = 0;
            };
            ResetTimer.Start();
        }

        #region Get Multiplayer
        public async Task<Multiplayer> GetMultiplayer(int ID)
        {
            var Endpoint = Match + "&mp=" + ID;
            return await Call<Multiplayer>(Endpoint);
        }
        #endregion

        #region Get User
        public async Task<User> GetUser(int UserID, bool Fresh = false, Mode Mode = Mode.Standard, int? EventDays = null)
        {
            if (!Fresh)
            {
                if (UserCache.ContainsKey(UserID))
                {
                    if (!UserNameIdCache.ContainsValue(UserID))
                    {
                        UserNameIdCache.Add(UserCache[UserID].UserName, UserID);
                    }
                    return UserCache[UserID];
                }
            }
            var Endpoint = User + "&u=" + UserID + "&type=id&m=" + (int)Mode;
            if (EventDays != null)
                Endpoint += "&event_days=" + EventDays;

            User[] users = await Call<User[]>(Endpoint);
            if (users.Length == 0) return default(User);
            User usr = users[0];

            UserCache.Remove(UserID);
            UserNameIdCache.Remove(usr.UserName.ToLower());
            
            UserCache.Add(UserID, usr);
            UserNameIdCache.Add(usr.UserName.ToLower(), UserID);

            Save(UserCache, UserCachePath);
            Save(UserNameIdCache, UserNameIdCachePath);
            return usr;
        }
        public async Task<User> GetUser(string UserName, bool Fresh = false, Mode Mode = Mode.Standard)
        {
            if (!Fresh)
            {
                if (UserNameIdCache.ContainsKey(UserName.ToLower()))
                {
                    return UserCache[UserNameIdCache[UserName.ToLower()]];
                }
            }
            var Endpoint = User + "&u=" + UserName + "&type=string&m=" + (int)Mode;

            User[] users = await Call<User[]>(Endpoint);
            if (users.Length == 0) return default(User);
            User usr = users[0];

            UserCache.Remove(usr.UserId);
            UserNameIdCache.Remove(usr.UserName.ToLower());

            UserCache.Add(usr.UserId, usr);
            UserNameIdCache.Add(usr.UserName.ToLower(), usr.UserId);

            Save(UserCache, UserCachePath);
            Save(UserNameIdCache, UserNameIdCachePath);
            return usr;
        }

        public bool IsUserCached(string Username)
        {
            return UserNameIdCache.ContainsKey(Username.ToLower());
        }
        public bool IsUserCached(int UserId)
        {
            return UserCache.ContainsKey(UserId);
        }
        #endregion

        #region Get Scores
        public async Task<UserScore> GetUserScoreOn(int UserID, int BeatmapID, Mode Mode = Mode.Standard)
        {
            return (await GetUserScoresOn(UserID, BeatmapID, Mode, 1))[0];
        }
        public async Task<UserScore> GetUserScoreOn(string UserName, int BeatmapID, Mode Mode = Mode.Standard)
        {
            return (await GetUserScoresOn(UserName, BeatmapID, Mode, 1))[0];
        }

        public async Task<UserScore[]> GetUserScoresOn(int UserID, int BeatmapID, Mode Mode = Mode.Standard, int Limit = 50)
        {
            var Endpoint = Scores + "&u=" + UserID + "&type=id&m=" + (int)Mode + "&b=" + BeatmapID + "&limit=" + Limit;
            return await Call<UserScore[]>(Endpoint);
        }
        public async Task<UserScore[]> GetUserScoresOn(string UserName, int BeatmapID, Mode Mode = Mode.Standard, int Limit = 50)
        {
            var Endpoint = Scores + "&u=" + UserName + "&type=string&m=" + (int)Mode + "&b=" + BeatmapID + "&limit=" + Limit;
            return await Call<UserScore[]>(Endpoint);
        }

        public async Task<UserScore[]> GetBeatmapTopScores(int BeatmapID, int? UserId = null, string Username = null, Mode Mode = Mode.Standard, int Limit = 50)
        {
            var Endpoint = Scores + "&m=" + (int)Mode + "&b=" + BeatmapID + "&limit=" + Limit;
            if (UserId != null)
            {
                Endpoint += $"&u={ UserId.Value }&type=id";
            }
            else if (Username != null)
            {
                Endpoint += $"&u={ Username }&type=string";
            }
            return await Call<UserScore[]>(Endpoint);
        }
        #endregion
        #region Get Best Scores
        public async Task<BestScore[]> GetUserBestScores(string UserName, Mode Mode = Mode.Standard, int Limit = 50, bool Fresh = true)
        {
            var Endpoint = UserBest + "&u=" + UserName + "&type=string&m=" + (int)Mode + "&limit=" + Limit;
            return await Call<BestScore[]>(Endpoint);
        }
        public async Task<BestScore[]> GetUserBestScores(int UserID, Mode Mode = Mode.Standard, int Limit = 50, bool Fresh = true)
        {
            var Endpoint = UserBest + "&u=" + UserID + "&type=id&m=" + (int)Mode + "&limit=" + Limit;
            return await Call<BestScore[]>(Endpoint);
        }

        public async Task<BestScore> GetUserBestScore(string UserName, Mode Mode = Mode.Standard)
        {
            try
            {
                return (await GetUserBestScores(UserName, Mode, 1))[0];
            }
            catch { return default(BestScore); }
        }
        public async Task<BestScore> GetUserBestScore(int UserID, Mode Mode = Mode.Standard)
        {
            try
            {
                return (await GetUserBestScores(UserID, Mode, 1))[0];
            }
            catch { return default(BestScore); }
        }
        #endregion
        #region Get Recent
        public async Task<RecentScore[]> GetUserRecentScores(string UserName, Mode Mode = Mode.Standard, int Limit = 10)
        {
            var Endpoint = UserRecent + "&u=" + UserName + "&type=string&m=" + (int)Mode + "&limit=" + Limit;
            return await Call<RecentScore[]>(Endpoint);
        }
        public async Task<RecentScore[]> GetUserRecentScores(int UserID, Mode Mode = Mode.Standard, int Limit = 10)
        {
            var Endpoint = UserRecent + "&u=" + UserID + "&type=id&m=" + (int)Mode + "&limit=" + Limit;
            return await Call<RecentScore[]>(Endpoint);
        }

        public async Task<RecentScore> GetUserRecentScore(string UserName, Mode Mode = Mode.Standard)
        {
            try
            {
                return (await GetUserRecentScores(UserName, Mode, 1))[0];
            }
            catch { return default(RecentScore); }
        }
        public async Task<RecentScore> GetUserRecentScore(int UserID, Mode Mode = Mode.Standard)
        {
            try
            {
                return (await GetUserRecentScores(UserID, Mode, 1))[0];
            }
            catch { return default(RecentScore); }
        }
        #endregion
        #region Get Beatmaps
        public async Task<Beatmap> GetBeatmapAsync(int BeatmapID, Mode Mode = Mode.Standard, ulong? ChannelId = null)
        {
            if (RankedBeatmapCache.ContainsKey(BeatmapID))
            {
                if (ChannelId != null)
                {
                    if (LastMapCache.ContainsKey(ChannelId.Value))
                    {
                        LastMapCache[ChannelId.Value].Add(BeatmapID);
                    }
                    else
                    {
                        LastMapCache.Add(ChannelId.Value, new List<int> { BeatmapID });
                    }
                    Save(LastMapCache, LastMapCachePath);
                }

                return RankedBeatmapCache[BeatmapID];
            }
            var Endpoint = Beatmap + "&b=" + BeatmapID + "&a=1&m=" + (int)Mode;
            Beatmap[] beatmaps = await Call<Beatmap[]>(Endpoint);
            if (beatmaps.Length == 0)
                return null;
            Beatmap bmap = beatmaps[0];

            if (ChannelId != null)
            {
                if (LastMapCache.ContainsKey(ChannelId.Value))
                {
                    LastMapCache[ChannelId.Value].Add(BeatmapID);
                }
                else
                {
                    LastMapCache.Add(ChannelId.Value, new List<int> { BeatmapID });
                }
                Save(LastMapCache, LastMapCachePath);
            }
            if (bmap.CanGetPP)
            {
                RankedBeatmapCache.Remove(BeatmapID);
                RankedBeatmapCache.Add(BeatmapID, bmap);

                Save(RankedBeatmapCache, RankedBeatmapCachePath);
            }

            return bmap;
        }
        public async Task<Beatmap[]> GetBeatmapsAsync(int BeatmapSetID, Mode Mode = Mode.Standard)
        {
            var Endpoint = Beatmap + "&s=" + BeatmapSetID + "&m=" + (int)Mode;
            return await Call<Beatmap[]>(Endpoint);
        }
        #endregion

        public int ResolveUserId(string Username)
        {
            string url = "https://osu.ppy.sh/users/" + Username;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "HEAD";
            request.AllowAutoRedirect = true;
            HttpWebResponse resp = (HttpWebResponse)request.GetResponse();
            try
            {
                return int.Parse(resp.ResponseUri.ToString().Split('/').Last());
            }
            catch
            {
                return -1;
            }
        }
        public int ResolveBeatmapSetId(int BeatmapId)
        {
            string url = "https://osu.ppy.sh/b/" + BeatmapId;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "HEAD";
            request.AllowAutoRedirect = true;
            HttpWebResponse resp = (HttpWebResponse)request.GetResponse();
            try
            {
                return int.Parse(resp.ResponseUri.ToString().Split('/').First(t => t.Contains('#')).Split('#')[0]);
            }
            catch
            {
                return -1;
            }
        }
        private async Task<T> Call<T>(string Endpoint)
        {
            if (CurrentCalls >= CallLimit)
            {
                await Task.Delay(ResetTimer.LastStartTime.AddSeconds(60) - DateTime.Now);
            }

            CurrentCalls++;
            string Content = "";

            try
            {
                Content = await new HttpClient().GetStringAsync(Endpoint);

                if (Content == "")
                {
                    return default;
                }
                else
                {
                    return JsonConvert.DeserializeObject<T>(Content);
                }
            }
            catch
            {
                return default;
            }
        }

        public void Save(object Object, string Path)
        {
            if (!Directory.Exists("Data\\osu"))
            {
                if (!Directory.Exists("Data"))
                    Directory.CreateDirectory("Data");

                Directory.CreateDirectory("Data\\osu");
            }
            File.WriteAllText(Path, JsonConvert.SerializeObject(Object, Formatting.Indented));
        }
    }
    public enum Mode
    {
        Standard = 0,
        Taiko = 1,
        CatchTheBeat = 2,
        Mania = 3
    }
}
