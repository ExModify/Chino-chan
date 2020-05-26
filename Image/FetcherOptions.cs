using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Chino_chan.Image
{
    public struct AuthOptions
    {
        public bool Authenticate { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class FetcherOptions
    {
        public string Query { get; private set; }
        
        public string Endpoint { get; private set; }
        public string UrlPrefix { get; private set; }

        public AuthOptions Auth { get; private set; }

        public string PageQuery { get; private set; }
        
        public FetcherOptions(string Endpoint, Dictionary<string, string> Query, 
            string UrlPrefix = "file_url", string PageQuery = null)
            : this(Endpoint, Query, default(AuthOptions), UrlPrefix, PageQuery) { }

        public FetcherOptions(string Endpoint, Dictionary<string, string> Query, AuthOptions Auth,
            string UrlPrefix = "file_url", string PageQuery = null)
        {
            this.Endpoint = Endpoint;
            this.UrlPrefix = UrlPrefix;
            this.Query = "";
            this.Auth = Auth;
            this.PageQuery = PageQuery;

            if (Query.Count > 0)
            {
                KeyValuePair<string, string> Current = Query.ElementAt(0);
                this.Query = "?" + Current.Key + "=" + Current.Value;

                for (int i = 1; i < Query.Count; i++)
                {
                    Current = Query.ElementAt(i);
                    this.Query += "&" + Current.Key + "=" + HttpUtility.UrlEncode(Current.Value);
                }
            }
        }


    }
}
