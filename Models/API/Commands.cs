using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chino_chan.Models.API
{
    public struct Module
    {
        public string name { get; set; }
        public List<Command> commands { get; set; }
    }
    public struct Command
    {
        public string name { get; set; }
        public string help { get; set; }
    }
}
