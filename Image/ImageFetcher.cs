using Chino_chan.Modules;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Chino_chan.Image
{
    public class ImageFetcher
    {
        public delegate void OutAction<T1, T2>(T1 a, out T2 b);
        public delegate void OutAction<T1, T2, T3>(T1 a, T2 b, out T3 c);

        FetcherOptions Options;

        string Endpoint
        {
            get
            {
                return Options.Endpoint;
            }
        }
        string UrlPrefix
        {
            get
            {
                return Options.UrlPrefix;
            }
        }
        string Query
        {
            get
            {
                return Options.Query;
            }
        }
        string PageQuery
        {
            get
            {
                return Options.PageQuery;
            }
        }

        OutAction<string, int, List<string>> Parse;
        OutAction<string, int> PageParser;

        public ImageFetcher() { }
        public ImageFetcher(FetcherOptions Options, OutAction<string, int, List<string>> Parse = null,
            OutAction<string, int> PageParser = null)
        {
            this.Options = Options;

            this.Parse = Parse;
            this.PageParser = PageParser;
        }
        
        public virtual async Task<List<string>> GetImagesAsync(IEnumerable<string> Tags, bool IsNsfw, int Count = 1, bool RandomPage = true)
        {
            return await GetImagesAsync(ConvertTags(Tags, IsNsfw), Count, RandomPage);
        }
        public virtual async Task<List<string>> GetImagesAsync(string TagsQuery, int Count = 1, bool RandomPage = true)
        {
            HttpClient Client = new HttpClient();
            
            int Page = 0;
            if (RandomPage)
                Page = GetRandomPage(TagsQuery);
            
            List<string> Images = new List<string>();
            bool Success = false;
            int count = 0;
            
            while (true)
            {
                count++;
                string Endpoint = this.Endpoint + Query + "&tags=" + TagsQuery;
                if (Page != -1 && PageQuery != null)
                    Endpoint += "&" + PageQuery + "=" + Page;

                try
                {
                    HttpResponseMessage Response = await Client.GetAsync(Endpoint);

                    if (Response.IsSuccessStatusCode)
                    {
                        string Content = await Response.Content.ReadAsStringAsync();
                        Response.Dispose();

                        if (Content == "Too deep! Pull it back some. Holy fuck.")
                        {
                            Page /= 2;

                            continue;
                        }
                        if (Content.ToLower().Contains("response success=\"false\""))
                        {
                            break;
                        }

                        Regex rx = new Regex("\\\"" + UrlPrefix + "\\\":\"(.*?)\"");

                        MatchCollection Matches = rx.Matches(Content);

                        foreach (Match Match in Matches)
                        {
                            Images.Add(Match.Groups[1].Value);
                        }

                        Success = true;
                        break;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    Logger.Log(LogType.Error, ConsoleColor.Red, "ImageFetcher", ex.ToString());
                    break;
                }
                if (count > 3) break;
            }
            
            if (Parse != null && !Success)
            {
                if (Page != -1 && PageQuery != null)
                    TagsQuery += "&" + PageQuery + "=" + Page * 42;
                Parse.Invoke(TagsQuery, Count, out Images);
            }

            return Images;
        }

        public int GetRandomPage(string TagsQuery)
        {
            if (PageParser == null) return -1;

            int Page = 0;
            PageParser?.Invoke(TagsQuery, out Page);

            return Global.Random.Next(0, Page);
        }

        public string ConvertTags(IEnumerable<string> Tags, bool IsNsfw)
        {
            string TagsQuery = string.Join("+", Tags).ToLower();

            if (!IsNsfw && TagsQuery.Contains("rating:explicit"))
                TagsQuery.Replace("rating:explicit", "rating:safe");

            if (!IsNsfw && TagsQuery.Contains("rating:questionable"))
                TagsQuery.Replace("rating:questionable", "rating:safe");

            if (!IsNsfw && !TagsQuery.Contains("rating:safe"))
                TagsQuery += (TagsQuery == "" ? "" : "+") + "rating:safe";

            return TagsQuery;
        }
    }
}
