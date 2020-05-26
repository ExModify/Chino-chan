using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Chino_chan.Image
{
    /// <summary>
    /// Most of this class is from
    /// https://github.com/CryShana/Sankaku-Channel-Downloader/blob/master/SankakuChannelAPI/SankakuChannelAPI/SankakuHttpHandler.cs
    /// </summary>
    public class Sankaku : ImageFetcher
    {
        string Username { get; set; }
        string Password { get; set; }

        string ActualUsername { get; set; }
        string PasswordHash { get; set; }

        string Cfduid { get; set; }
        string SankakuSessionId { get; set; }

        string Endpoint
        {
            get
            {
                return "https://chan.sankakucomplex.com/";
            }
        }
        string AuthUrl
        {
            get
            {
                return Endpoint + "user/authenticate";
            }
        }
        
        public Sankaku(string Username, string Password) : base()
        {
            this.Username = Username;
            this.Password = Password;
        }

        public bool Login(out bool TooManyRequests)
        {
            PasswordHash = null; Cfduid = null; SankakuSessionId = null; TooManyRequests = false; ActualUsername = null;
            try
            {
                var CookieRequest = (HttpWebRequest)WebRequest.Create("https://chan.sankakucomplex.com/");

                CookieRequest.Method = "GET";
                CookieRequest.Headers.Add("Upgrade-Insecure-Requests: 1");
                CookieRequest.Headers.Add("Accept-Encoding: gzip, deflate, sdch, br");
                CookieRequest.Headers.Add("Accept-Language: en-US,en;q=0.8,sl;q=0.6");
                CookieRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.143 Safari/537.36";
                CookieRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                CookieRequest.Timeout = 18 * 1000;

                HttpWebResponse CookieResponse = (HttpWebResponse)CookieRequest.GetResponse(); ;
                
                var CookieValueValue = CookieResponse.GetResponseHeader("Set-Cookie");
                CookieResponse.Close();
                Cfduid = new Regex(@"__cfduid=(.*?);").Match(CookieValueValue).Groups[1].Value;
                
                var Request = (HttpWebRequest)WebRequest.Create("https://chan.sankakucomplex.com/user/authenticate");
                Request.Method = "POST";
                Request.AllowAutoRedirect = false;
                Request.Headers.Add("Origin", "https://chan.sankakucomplex.com");
                Request.Headers.Add("Cache-Control", "max-age=0");
                Request.Headers.Add("Upgrade-Insecure-Requests", "1");
                Request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
                Request.Headers.Add("Accept-Language", "en-US,en;q=0.8,sl;q=0.6");
                Request.Host = "chan.sankakucomplex.com";
                Request.Referer = "https://chan.sankakucomplex.com/user/login";
                Request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.143 Safari/537.36";
                Request.ContentType = "application/x-www-form-urlencoded";
                Request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                Request.Headers.Add("Cookie", $"__cfduid={Cfduid}; auto_page=1; blacklisted_tags=; locale=en");
                var Content = $"url=&user%5Bname%5D={Username}&user%5Bpassword%5D={Password}&commit=Login";

                var ContentBytes = Encoding.ASCII.GetBytes(Content);
                Request.ContentLength = ContentBytes.Length;
                var RequestStream = Request.GetRequestStream();
                RequestStream.Write(ContentBytes, 0, ContentBytes.Length);
                RequestStream.Close();
                Request.Timeout = 18 * 1000;
                
                var Response = (HttpWebResponse)Request.GetResponse();
                var Value = Response.GetResponseHeader("Set-Cookie");
                Response.Close();

                PasswordHash = new Regex(@"pass_hash=(.*?);").Match(Value).Groups[1].Value;
                ActualUsername = new Regex(@"login=(.*?);").Match(Value).Groups[1].Value;
                SankakuSessionId = new Regex(@"_sankakucomplex_session=(.*?);").Match(Value).Groups[1].Value;

                if (PasswordHash.Length < 2) return false;

                return true;
            }
            catch (WebException Exception)
            {
                if (Exception.Message.ToLower().Contains("too many requests"))
                {
                    TooManyRequests = true;
                    return false;
                }
                else throw Exception;
            }
            catch { return false; }
        }
        
        public override async Task<List<string>> GetImagesAsync(IEnumerable<string> Tags, bool IsNsfw, int Count = 1, bool RandomPage = true)
        {
            List<string> _tags = new List<string>(Tags);

            if (!_tags.Contains("order:random"))
                _tags.Add("order:random");
            
            return await GetImagesAsync(ConvertTags(_tags, IsNsfw), Count, RandomPage);
        }
        public override async Task<List<string>> GetImagesAsync(string Tags, int Count = 1, bool RandomPage = true)
        {
            List<string> Images = new List<string>();

            if (RandomPage)
                if (!Tags.Contains("order:random"))
                    Tags += "+order:random";

            SendQuery(Tags, out List<string> RawImages, Count);

            foreach (string Image in RawImages)
            {
                string Img = Image;

                string Link = GetImageLink(Image);

                string[] NameQuery = Link.Substring(Link.LastIndexOf('/')).Split('.', '?', '&');

                Stream Stream;
                do
                {
                    Stream = DownloadImage(Link, Image, out bool Redirected, true);

                    if (Stream == null)
                    {
                        SendQuery(Tags, out List<string> Imgs, 1);

                        if (Imgs[0] == Img)
                            break;

                        Img = Imgs[0];
                        Link = GetImageLink(Img);
                    }
                }
                while (Stream == null);

                if (Stream != null)
                {
                    if (Global.Imgur == null)
                    {
                        var File = await Global.JunkChannel.SendFileAsync(Stream, "image." + NameQuery[0] + "." + NameQuery[1], "");
                        Images.Add(File.Attachments.First().Url);
                    }
                    else
                    {
                        Images.Add(await Global.Imgur.UploadImage(Stream));
                    }
                }
            }
            
            return Images;
        }
        
        private string GetImageLink(string postReference)
        {
            var Request = (HttpWebRequest)WebRequest.Create(postReference);
            Request.Method = "GET";

            Request.Headers.Add("Cache-Control", "max-age=0");
            Request.Headers.Add("Upgrade-Insecure-Requests", "1");
            Request.Host = "chan.sankakucomplex.com";
            Request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36";
            Request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            Request.Headers.Add("Accept-Encoding", "gzip, deflate, sdch, br");
            Request.Headers.Add("Accept-Language", "en-US,en;q=0.8,sl;q=0.6");
            Request.Headers.Add("Cookie", $"__cfduid={Cfduid}; login={Username}; pass_hash={PasswordHash}; " +
               $"__atuvc=24%7C43; __atuvs=580cc97684a60c23003; mode=view; auto_page=1; " +
               $"blacklisted_tags=full-package_futanari&futanari; locale=en; _sankakucomplex_session={SankakuSessionId}");
            Request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            var Response = (HttpWebResponse)Request.GetResponse();
            var Content = Encoding.UTF8.GetString(ReadStream(Response.GetResponseStream()));
            Response.Close();

            Regex Checker = new Regex(@"<li>Original: <a href=""(.*?)"" id=highres.*?<\/a><\/li>", RegexOptions.Singleline);
            return "http:" + Checker.Match(Content).Groups[1].Value;
        }

        private byte[] DownloadImage(string ImageLink, out bool WasRedirected, bool ContainsVideo, string PostLink)
        {
            var ReadStream = DownloadImage(ImageLink, PostLink, out WasRedirected, ContainsVideo);
            List<byte> ReadBytes = new List<byte>();
            while (true)
            {
                int ReadByte = ReadStream.ReadByte();
                if (ReadByte == -1) break;
                else ReadBytes.Add((byte)ReadByte);
            }

            return ReadBytes.ToArray();
        }
        private Stream DownloadImage(string ImageLink, string PostLink, out bool WasRedirected, bool ContainsVideo)
        {
            WasRedirected = false;

            var Request = (HttpWebRequest)WebRequest.Create(ImageLink.Replace("&amp;", "&"));
            Request.Method = "GET";
            Request.Host = "cs.sankakucomplex.com";
            Request.Referer = PostLink;
            Request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36";
            Request.Accept = "image/webp,image/apng,image/*,*/*;q=0.8";
            Request.Headers.Add("Accept-Encoding", "gzip, deflate, sdch");
            Request.Headers.Add("Accept-Language", "en-US,en;q=0.8,sl;q=0.6");

            Request.Timeout = 1000 * 20;
            var Response = (HttpWebResponse)Request.GetResponse();

            if (Response.ResponseUri.OriginalString.ToLower().Contains("redirect.png"))
            {
                WasRedirected = true;
            }

            if (ContainsVideo == false && (Response.ContentType.ToLower().Contains("gif") ||
                Response.ContentType.ToLower().Contains("webm") ||
                Response.ContentType.ToLower().Contains("mp4") ||
                Response.ContentType.ToLower().Contains("mpeg")))
            {
                return null;
            }
            if (Response.ContentLength / 1024.0 / 1024.0 >= 8 && Global.Imgur == null)
            {
                return null;
            }
            
            return Response.GetResponseStream();
        }

        private byte[] ReadStream(Stream Stream)
        {
            using (var MemoryStream = new MemoryStream())
            {
                Stream.CopyTo(MemoryStream);
                Stream.Flush();
                return MemoryStream.ToArray();
            }
        }
        private bool SendQuery(string Tag, out List<string> Results, int Limit = 20)
        {
            Results = new List<string>();
            try
            {
                string ConvertedQuery = Tag.Replace(" ", "+");

                var Request = (HttpWebRequest)WebRequest.Create($"https://chan.sankakucomplex.com/post/index.content?&tags={ConvertedQuery}");
                Request.Method = "GET";
                Request.Referer = "https://chan.sankakucomplex.com/?tags={convertedQuery}&commit=Search";
                Request.Host = "chan.sankakucomplex.com";
                Request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.143 Safari/537.36";
                Request.Accept = "text/html, */*";
                Request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
                Request.Headers.Add("Accept-Language", "en-GB,en;q=0.8,sl;q=0.6");
                Request.Headers.Add("Cookie", $"__cfduid={Cfduid}; login={Username}; pass_hash={PasswordHash}; " +
                    $"__atuvc=24%7C43; __atuvs=580cc97684a60c23003; mode=view; auto_page=1; " +
                    $"blacklisted_tags=full-package_futanari&futanari; locale=en; _sankakucomplex_session={SankakuSessionId}");
                Request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                var Response = (HttpWebResponse)Request.GetResponse();
                var Content = Encoding.UTF8.GetString(ReadStream(Response.GetResponseStream()));
                Response.Close();

                Regex Regex = new Regex(@"<span class="".*?"" id=.*?><a href=""\/post\/show\/(.*?)"" onclick="".*?"">" +
                    @"<img class=.*? src=""(.*?)"" title=""(.*?)"".*?><\/a><\/span>", RegexOptions.Singleline);

                foreach (Match Match in Regex.Matches(Content))
                {
                    try
                    {
                        var Tags = Match.Groups[3].Value.ToLower();

                        Results.Add("https://chan.sankakucomplex.com/post/show/" + Match.Groups[1].Value);

                        if (Results.Count >= Limit)
                        {
                            break;
                        }
                    }
                    catch { }
                }

                return true;
            }
            catch (Exception Exception)
            {
                throw Exception;
            }
        }
    }
}
