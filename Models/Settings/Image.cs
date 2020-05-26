using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.Settings
{
    public struct Image
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool TitleIncludeName { get; set; }
        public bool IsNsfw { get; set; }
        public bool SearchSubDirs { get; set; }
    }
}
