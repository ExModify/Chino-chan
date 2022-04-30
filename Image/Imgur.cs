using Chino_chan.Modules;
using Imgur.API;
using Imgur.API.Authentication;
using Imgur.API.Endpoints;
using Imgur.API.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Chino_chan.Image
{
    public class Imgur
    {
        private readonly ApiClient _imgurClient;
        private readonly ImageEndpoint _endpoint;
        private readonly HttpClient _httpClient;
        
        public Imgur()
        {
            Logger.Log(LogType.Imgur, ConsoleColor.Green, "Login", "Logging in...");

            try
            {
                _httpClient = new HttpClient();
                _imgurClient = new ApiClient(Global.Settings.Credentials.Imgur.ClientId, Global.Settings.Credentials.Imgur.ClientSecret);
                _endpoint = new ImageEndpoint(_imgurClient, _httpClient);

                Logger.Log(LogType.Imgur, ConsoleColor.Green, "Login", "Logged in!");
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Imgur, ConsoleColor.Red, "Login", "Couldn't login due: " + e.Message);
            }
        }

        public async Task<string> UploadImage(string path)
        {
            if (!File.Exists(path))
            {
                Logger.Log(LogType.Imgur, ConsoleColor.Red, "Upload", "File doesn't exists: " + path);
                return "";
            }
            using (var Stream = new FileStream(path, FileMode.Open))
            {
                return await UploadImage(Stream);
            }
        }
        public async Task<string> UploadImage(Stream stream)
        {
            try
            {
                IImage image = await _endpoint.UploadImageAsync(stream);
                return image.Link;
            }
            catch (ImgurException Exception)
            {
                Logger.Log(LogType.Imgur, ConsoleColor.Red, "Upload", "An error occured while uploading an image:"
                    + Environment.NewLine
                    + Exception.Message);
            }
            return "";
        }
    }
}
