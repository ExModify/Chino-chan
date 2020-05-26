using Chino_chan.Modules;
using Chino_chan.Models.File;
using System;
using System.Collections.Generic;

namespace Chino_chan.Image
{
    public class ImageHandler
    {
        public Dictionary<string, FileCollection> Images;

        public int Count
        {
            get
            {
                return Images.Count;
            }
        }
        
        public ImageHandler()
        {
            Images = new Dictionary<string, FileCollection>();

            Logger.Log(LogType.Images, ConsoleColor.DarkMagenta, null, "Loading images...");
            
            foreach (var Item in Global.Settings.ImagePaths)
            {
                if (System.IO.Directory.Exists(Item.Path))
                {
                    var Collection = new FileCollection(Item.Name, Item.Path, Item.TitleIncludeName, Item.IsNsfw, Item.SearchSubDirs);
                    Images.Add(Item.Name.ToLower(), Collection);
                    Logger.Log(LogType.Images, ConsoleColor.DarkMagenta, null, Item.Name + " images loaded!");
                }
                else
                {
                    Logger.Log(LogType.Images, ConsoleColor.Red, null, Item.Name + ": " + Item.Path + " is not found!");
                }
            }

            Logger.Log(LogType.Images, ConsoleColor.DarkMagenta, null, "Loaded " + Images.Count + " folder" + (Images.Count > 1 ? "s" : ""));
        }
    }
}
