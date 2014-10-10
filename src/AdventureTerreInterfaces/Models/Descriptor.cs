using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdventureTerreInterfaces.Models
{
    public class Descriptor
    {
        public string Name { get; set; }
        public Dictionary<string, bool> Flags { get; set; }
        public string Text { get; set; }
        public bool IsDefault { get; set; }
        public Dictionary<string, bool> SetFlags { get; set; }
    }
}
