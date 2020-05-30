using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Leveling
{
    public class UserHandler : Dictionary<ulong, User>
    {
        string Filename { get; set; }

        public UserHandler(LevelSystem Level, string Filename = "Data/Users.json")
        {
            this.Filename = Filename;

            uint MaxXP = Load();

            Level.GenerateLevelUntilXP(MaxXP);
            Level.GenerateLevel(1);
        }
        
        uint Load()
        {
            if (File.Exists(Filename))
            {
                try
                {
                    Dictionary<ulong, User> Users = JsonConvert.DeserializeObject<Dictionary<ulong, User>>(File.ReadAllText(Filename));

                    uint XP = 0;
                    
                    foreach (KeyValuePair<ulong, User> User in Users)
                    {
                        foreach (KeyValuePair<ulong, uint> GuildXP in User.Value.GuildXps)
                            if (GuildXP.Value > XP)
                                XP = GuildXP.Value;

                        Add(User.Key, User.Value);
                    }

                    return XP;
                }
                catch { } // Probably RIP Json
            }

            Save();

            return 0;
        }

        void Save()
        {
            File.Delete(Filename);
            File.WriteAllText(Filename, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public void RecalculateLevels(LevelSystem Level)
        {
            bool save = false;
            uint maxLevel = 0;
            foreach (ulong UserKey in Keys)
            {
                foreach (ulong GuildKey in this[UserKey].GuildXps.Keys)
                {
                    uint level = Level.GetLevelFromXP(this[UserKey].GuildXps[GuildKey]);
                    if (maxLevel < level)
                        maxLevel = level;

                    if (level != this[UserKey].GuildLevels[GuildKey])
                    {
                        this[UserKey].GuildLevels[GuildKey] = level;
                        save = true;
                    }
                }
            }
            if (save) Save();
        }

        public User GetUser(ulong Id)
        {
            if (ContainsKey(Id))
                return this[Id];
            else
            {
                User user = new User()
                {
                    UserId = Id
                };
                AddUser(user);
                return user;
            }
        }
        
        public void AddUser(User User)
        {
            Add(User.UserId, User);
            Save();
        }
        public void UpdateUser(User User)
        {
            if (ContainsKey(User.UserId))
                this[User.UserId] = User;
            else
                Add(User.UserId, User);
            
            Save();
        }
    }
}
