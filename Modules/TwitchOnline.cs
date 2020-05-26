using Chino_chan.Models.Twitch;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Streams;
using TwitchLib.Api.Helix.Models.Users;
using TwitchLib.Client;
using TwitchLib.Client.Models;

namespace Chino_chan.Modules
{
    public enum RegisterStatus
    {
        AlreadyTracking,
        UserNotFound,
        Success
    }
    public enum UnregisterStatus
    {
        NotTracking,
        UserNotFound,
        Success
    }

    public class TwitchTracker
    {
        private Dictionary<long, UserResponse> UserDatabase { get; set; }
        private Dictionary<long, StreamResponse> StreamDatabase { get; set; }

        public event Action<StreamResponse, UserResponse> OnStreamUp;
        public event Action<StreamResponse, UserResponse> OnStreamDown;

        private List<long> Tracking { get; set; }

        private List<long> Online { get; set; }
        private List<long> Offline { get; set; }
        
        private Timer Update { get; set; }
        private Timer UserDatabaseUpdater { get; set; }

        TwitchAPI api;
        
        public TwitchTracker()
        {
            Tracking = new List<long>();

            Online = new List<long>();
            Offline = new List<long>();

            try
            {
                if (File.Exists("Data\\TwitchTracking.json"))
                {
                    TwitchLocal obj = JsonConvert.DeserializeObject<TwitchLocal>(File.ReadAllText("Data\\TwitchTracking.json"));

                    Online.AddRange(obj.Online);
                    Offline.AddRange(obj.Offline);
                }
            }
            catch { /* Probably some errors with the json file, manual impact or sth ~ better be sure ~ */ }
            
            StreamDatabase = new Dictionary<long, StreamResponse>();
            UserDatabase = new Dictionary<long, UserResponse>();

            Update = new Timer(Global.Settings.TwitchStreamUpdate);
            UserDatabaseUpdater = new Timer(Global.Settings.TwitchUserUpdate);

            api = new TwitchAPI();
            api.Settings.ClientId = Global.Settings.Credentials.Twitch.ClientId;
            api.Settings.Secret = Global.Settings.Credentials.Twitch.ClientSecret;
            string token = api.Helix.Streams.GetAccessToken();
            api.Settings.AccessToken = token;

            Restore();

            Update.Elapsed += (s, a) =>
            {
                List<StreamResponse> Streams = new List<StreamResponse>();

                Validate();

                for (int i = 0; i < Tracking.Count; i += 100)
                {
                    GetStreamsResponse resp = api.Helix.Streams.GetStreamsAsync(userIds: Tracking.Skip(i).Take(100).Select(t => t.ToString()).ToList()).Result;
                    Streams.AddRange(resp.Streams.Select(t => new StreamResponse(t)));
                }

                long[] Ids = Streams.Select(t => t.UserId).ToArray();
                for (int i = 0; i < Ids.Length; i++)
                {
                    long UserId = Ids[i];

                    if (!Online.Contains(UserId))
                    {
                        Online.Add(UserId);
                        Offline.Remove(UserId);

                        StreamDatabase.Add(UserId, Streams[i]);

                        OnStreamUp?.Invoke(Streams[i], UserDatabase[UserId]);
                    }
                }
                for (int i = 0; i < Online.Count; i++)
                {
                    long UserId = Online[i];

                    if (!Ids.Contains(UserId))
                    {
                        Offline.Add(UserId);
                        Online.Remove(UserId);
                        if (StreamDatabase.ContainsKey(UserId))
                        {
                            OnStreamDown?.Invoke(StreamDatabase[UserId], UserDatabase[UserId]);
                            StreamDatabase.Remove(UserId);
                        }
                    }
                }

                File.WriteAllText("Data\\TwitchTracking.json", JsonConvert.SerializeObject(new TwitchLocal()
                {
                    Online = Online,
                    Offline = Offline
                }));
            };

            UserDatabaseUpdater.Elapsed += (s, a) =>
            {
                var Keys = UserDatabase.Keys;

                Validate();


                List<UserResponse> Users = new List<UserResponse>();

                for (int i = 0; i < Keys.Count; i += 100)
                {
                    GetUsersResponse resp = api.Helix.Users.GetUsersAsync(Keys.Skip(i).Take(100).Select(t => t.ToString()).ToList()).Result;
                    Users.AddRange(resp.Users.Select(t => new UserResponse(t)));
                }

                UserDatabase.Clear();

                for (int i = 0; i < Users.Count; i++)
                {
                    UserResponse User = Users[i];
                    UserDatabase.Add(User.Id, User);
                }
            };

            UserDatabaseUpdater.Start();
        }
        private void Validate()
        {
            if (!api.Helix.Streams.CheckCredentialsAsync().Result.Result)
            {
                string token = api.Helix.Streams.GetAccessToken();
                api.Settings.AccessToken = token;
            }
        }
        private void Restore()
        {
            List<long> UserIds = new List<long>();

            UserIds.AddRange(Online);
            UserIds.AddRange(Offline);

            if (UserIds.Count == 0)
            {
                foreach (var Settings in Global.GuildSettings.Settings)
                {
                    foreach (long Id in Settings.Value.TwitchTrack.UserIds)
                    {
                        if (!UserIds.Contains(Id))
                            UserIds.Add(Id);
                    }
                }
            }

            Tracking.AddRange(UserIds);

            List<UserResponse> Users = new List<UserResponse>();

            for (int i = 0; i < UserIds.Count; i += 100)
            {
                GetUsersResponse resp = api.Helix.Users.GetUsersAsync(UserIds.Skip(i).Take(100).Select(t => t.ToString()).ToList()).Result;
                Users.AddRange(resp.Users.Select(t => new UserResponse(t)));
            }
            
            for (int i = 0; i < Users.Count; i++)
            {
                UserResponse User = Users[i];
                UserDatabase.Add(User.Id, User);
            }
        }

        public void StartTrack()
        {
            Update.Start();
            Logger.Log(LogType.Twitch, ConsoleColor.DarkMagenta, null, "Twitch tracker started!");
        }
        
        public RegisterStatus Register(string Username)
        {
            GetUsersResponse resp = api.Helix.Users.GetUsersAsync(logins: new List<string>() { Username }).Result;
            List<UserResponse> user = resp.Users.Select(t => new UserResponse(t)).ToList();

            if (user.Count == 0)
            {
                return RegisterStatus.UserNotFound;
            }
            
            if (Tracking.Contains(user[0].Id))
            {
                return RegisterStatus.AlreadyTracking;
            }

            Tracking.Add(user[0].Id);
            Offline.Add(user[0].Id);
            UserDatabase.Add(user[0].Id, user[0]);

            return RegisterStatus.Success;

        }
        public UnregisterStatus Unregister(string Username)
        {
            GetUsersResponse resp = api.Helix.Users.GetUsersAsync(logins: new List<string>() { Username }).Result;
            List<UserResponse> user = resp.Users.Select(t => new UserResponse(t)).ToList();

            if (user.Count == 0)
            {
                return UnregisterStatus.UserNotFound;
            }

            if (!Tracking.Contains(user[0].Id))
            {
                return UnregisterStatus.NotTracking;
            }

            Tracking.Remove(user[0].Id);
            UserDatabase.Remove(user[0].Id);

            Offline.Remove(user[0].Id);
            Online.Remove(user[0].Id);

            return UnregisterStatus.Success;
        }

        public long GetUserId(string Username)
        {
            UserResponse? User = GetUser(Username);

            if (User.HasValue)
                return User.Value.Id;
            else return -0;
        }

        public UserResponse? GetUser(string Username)
        {
            Username = Username.ToLower();

            foreach (KeyValuePair<long, UserResponse> Pair in UserDatabase)
            {
                if (Pair.Value.LoginUsername.ToLower() == Username)
                {
                    return Pair.Value;
                }
            }
            GetUsersResponse resp = api.Helix.Users.GetUsersAsync(logins: new List<string>() { Username }).Result;
            List<UserResponse> user = resp.Users.Select(t => new UserResponse(t)).ToList();

            if (user.Count == 0)
            {
                return null;
            }
            else
            {
                return user[0];
            }
        }
    }
}
