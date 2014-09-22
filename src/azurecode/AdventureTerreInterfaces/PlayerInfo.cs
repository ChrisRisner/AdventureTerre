using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Orleans;
using Orleans.Concurrency;

namespace AdventureTerreInterfaces
{
    [Immutable]
    public class PlayerInfo
    {
        public Guid Key { get; set; }
        public string Name { get; set; }
    }
}
