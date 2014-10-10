using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdventureTerreInterfaces.Models
{
    public class CommandAction
    {
        public string CommandName { get; set; }
        public string Flag { get; set; }
        public bool NewValue { get; set; }
        public bool ShouldFlip { get; set; }
    }
}
