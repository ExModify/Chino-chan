using Chino_chan.Models.Settings;
using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Modules
{
    public class WelcomeBanner
    {
        [JsonIgnore]
        Bitmap Frame;

        [JsonIgnore]
        Bitmap Background;

        [JsonIgnore]
        FontFamily FontFamily;
        
        public ulong GuildId { get; set; }
        
        public string FrameLocation { get; set; } = "";
        public string BackgroundLocation { get; set; } = "";
        public string FontLocation { get; set; } = "";
        public float FontSize { get; set; } = -1;
        public System.Drawing.Color TextColor { get; set; } = System.Drawing.Color.White;

        public Point AvatarPosition { get; set; } = new Point(-1, -1);
        public Size AvatarSize { get; set; } = new Size(-1, -1);
        public Point TextPosition { get; set; } = new Point(-1, -1);

        public string Text { get; set; } = "Welcome, {NAME}";

        public bool CircularAvatar { get; set; } = true;

        public WelcomeBanner(ulong GuildId)
        {
            this.GuildId = GuildId;
        }
        public WelcomeBanner() { } // json

        public bool Load(Bitmap DefaultFrame, Bitmap DefaultBackground, FontFamily DefaultFontFamily, float DefaultFontSize, Point DefaultAvatarPosition, Size DefaultAvatarSize, Point DefaultTextPosition, bool DefaultCircularAvatar, PrivateFontCollection Collection)
        {
            if (string.IsNullOrWhiteSpace(FrameLocation))
            {
                Frame = DefaultFrame;
            }
            else
            {
                if (File.Exists(FormPath(FrameLocation)))
                {
                    Frame = new Bitmap(System.Drawing.Image.FromFile(FormPath(FrameLocation)));
                }
                else
                {
                    Logger.Log(LogType.WelcomeBanner, ConsoleColor.Red, "Error", $"Frame file not found for server with id { GuildId }!");
                    SetDefault(DefaultFrame, DefaultBackground, DefaultFontFamily, DefaultFontSize, DefaultAvatarPosition, DefaultAvatarSize, DefaultTextPosition, DefaultCircularAvatar);
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(BackgroundLocation))
            {
                Background = DefaultBackground;
            }
            else
            {
                if (File.Exists(FormPath(BackgroundLocation)))
                {
                    Frame = new Bitmap(System.Drawing.Image.FromFile(FormPath(BackgroundLocation)));
                }
                else
                {
                    Logger.Log(LogType.WelcomeBanner, ConsoleColor.Red, "Error", $"Background file not found for server with id { GuildId }!");
                    SetDefault(DefaultFrame, DefaultBackground, DefaultFontFamily, DefaultFontSize, DefaultAvatarPosition, DefaultAvatarSize, DefaultTextPosition, DefaultCircularAvatar);
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(FontLocation))
            {
                FontFamily = DefaultFontFamily;
            }
            else
            {
                if (File.Exists(FormPath(FontLocation)))
                {
                    Collection.AddFontFile(FormPath(FontLocation));
                    FontFamily = Collection.Families[Collection.Families.Length - 1];
                }
                else
                {
                    Logger.Log(LogType.WelcomeBanner, ConsoleColor.Red, "Error", $"Font file not found for server with id { GuildId }!");
                    SetDefault(DefaultFrame, DefaultBackground, DefaultFontFamily, DefaultFontSize, DefaultAvatarPosition, DefaultAvatarSize, DefaultTextPosition, DefaultCircularAvatar);
                    return false;
                }
            }

            if (AvatarPosition.X < 0 || AvatarPosition.Y < 0)
            {
                AvatarPosition = DefaultAvatarPosition;
            }
            if (AvatarSize.Width < 0 || AvatarSize.Height < 0)
            {
                AvatarSize = DefaultAvatarSize;
            }
            if (TextPosition.X < 0 || TextPosition.Y < 0)
            {
                TextPosition = DefaultTextPosition;
            }

            return true;
        }

        public Bitmap Create(IUser User, Greet Greet)
        {
            Bitmap image = new Bitmap(Background.Width, Background.Height);
            using (Graphics g = Graphics.FromImage(image))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                g.DrawImage(Background, new PointF(0, 0));
                g.DrawImage(Frame, new PointF(0, 0));
                
                HttpClient client = new HttpClient();
                Stream s = client.GetStreamAsync(User.GetAvatarUrl(ImageFormat.Png, size: GetSize(AvatarSize.Width))).Result;

                Rectangle avatarRectangle = new Rectangle(AvatarPosition, AvatarSize);
                
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(avatarRectangle);
                    g.SetClip(path);
                    g.DrawImage(System.Drawing.Image.FromStream(s), 0, 0);
                    g.ResetClip();
                }
                g.DrawString(Text.Replace("{NAME}", User.Username), new Font(FontFamily, FontSize), new SolidBrush(TextColor), TextPosition);
            }
            return image;
        }
        private ushort GetSize(int AvatarSizeBiggest)
        {
            ushort[] availableSizes = new ushort[] { 16, 32, 64, 128, 256, 512, 1024, 2048 };
            for (int i = availableSizes.Length - 1; i > 0; i--)
            {
                if (AvatarSizeBiggest < availableSizes[i] && AvatarSizeBiggest > availableSizes[i - 1])
                {
                    return availableSizes[i];
                }
            }
            return 16;
        }
        private void SetDefault(Bitmap DefaultFrame, Bitmap DefaultBackground, FontFamily DefaultFontFamily, float DefaultFontSize, Point DefaultAvatarPosition, Size DefaultAvatarSize, Point DefaultTextPosition, bool DefaultCircularAvatar)
        {
            Frame = DefaultFrame;
            Background = DefaultBackground;
            FontFamily = DefaultFontFamily;
            FontSize = DefaultFontSize;

            AvatarPosition = DefaultAvatarPosition;
            AvatarSize = DefaultAvatarSize;
            TextPosition = DefaultTextPosition;
            CircularAvatar = DefaultCircularAvatar;
        }
        private string FormPath(string Filename)
        {
            return $"Data\\Resources\\ServerBanners\\{ GuildId }\\{ Filename }";
        }
    }
}