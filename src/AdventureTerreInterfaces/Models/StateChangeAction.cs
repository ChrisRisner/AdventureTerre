using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdventureTerreInterfaces.Models
{
    public class StateChangeAction
    {
        public string Flag { get; set; }
        public bool ToValue { get; set; }
        public string PrintText { get; set; }
    }
}
