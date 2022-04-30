using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Net;
using Chino_chan.Modules;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Chino_chan.Image
{
    public class Attributes
    {
        [JsonProperty("limit")]
        public int Limit { get; set; }
        [JsonProperty("offset")]
        public int Offset { get; set; }
        [JsonProperty("count")]
        public int Count { get; set; }
    }
    public class DAPI
    {
        [JsonProperty("@attributes")]
        public Attributes Attributes { get; set; }

        [JsonProperty("post")]
        public List<Post> Posts { get; set; }
    }
    public class Post
    {
        [JsonProperty("id")]
        public string PostId { get; set; }

        [JsonProperty("file_url")]
        public string Link { get; private set; }
        [JsonProperty("image")]
        public string Filename { get; private set; }
        [JsonProperty("md5")]
        public string MD5 { get; private set; }
        [JsonProperty("tags")]
        public string Tags { get; private set; }

        [JsonIgnore]
        public string ThumbnailUrl { get => $"https://gelbooru.com/thumbnails/{ MD5.Substring(0, 2) }/{ MD5.Substring(2, 2) }/thumbnail_{ MD5 }.jpg"; }
        [JsonIgnore]
        public string PostLink { get => $"https://gelbooru.com/index.php?page=post&s=view&id={ PostId }"; }
        [JsonIgnore]
        public bool IsAnimated { get => Link.EndsWith(".gif") || Link.EndsWith(".webm") || Link.EndsWith(".mp4"); }
    }

    public class Gelbooru
    {
        public static List<Post> FetchImages(IEnumerable<string> Tags, bool RandomPage = false)
        {
            return FetchImages(string.Join("+", Tags), RandomPage);
        }
        public static List<Post> FetchImages(string Tags, bool RandomPage = false)
        {
            string baseLink = $"https://gelbooru.com/index.php?page=dapi&s=post&q=index&limit=42&json=1&tags={ Tags }";
            if (RandomPage)
            {
                baseLink += "&pid=" + Gelbooru.RandomPage(Tags);
            }

            if (!Global.Settings.Credentials.IsEmpty(Models.Settings.Credentials.CredentialType.Gelbooru))
            {
                baseLink += $"&api_key={ Global.Settings.Credentials.Gelbooru.APIKey }&user_id={ Global.Settings.Credentials.Gelbooru.UserId }";
            }

            HttpClientHandler handler = new HttpClientHandler();
            CookieContainer container = new CookieContainer();
            container.Add(new Cookie("fringeBenefits", "yup", "/", "gelbooru.com"));
            handler.CookieContainer = container;
            HttpClient client = new HttpClient(handler);
            HttpResponseMessage message = client.GetAsync(baseLink).Result;

            string Content = message.Content.ReadAsStringAsync().Result;

            List<Post> posts;
            try
            {
                posts = JsonConvert.DeserializeObject<DAPI>(Content).Posts;
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, ConsoleColor.Red, "SubTag", "Base Link: " + baseLink);
                Logger.Log(LogType.Error, ConsoleColor.Red, "SubTag", "Content: " + Content);
                throw e;
            }

            return posts;
        }

        public static int RandomPage(string Tags)
        {
            string Endpoint = "https://gelbooru.com/index.php?page=post&s=list&tags=" + Tags;

            try
            {
                HttpClient Client = new HttpClient();
                HttpResponseMessage Response = Client.GetAsync(Endpoint).Result;
                string Content = Response.Content.ReadAsStringAsync().Result;

                Regex Regex = new Regex("<a href=\"\\?page=post&amp;s=list&amp;tags=.*?;pid=(\\d*)\" alt=\"last page\">&raquo;<\\/a><\\/div><\\/div>");
                if (Regex.IsMatch(Content))
                {
                    Match match = Regex.Match(Content);
                    int parse = Convert.ToInt32(match.Groups[1].Value) / 42;

                    return Global.Random.Next(-1, parse);
                }
            }
            catch
            {
                Logger.Log(LogType.Error, ConsoleColor.Red, "Gelbooru", "Couldn't get random page!");
            }
            return 0;
        }
    }
}
