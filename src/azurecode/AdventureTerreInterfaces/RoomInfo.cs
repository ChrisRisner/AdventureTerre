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
    public class RoomInfo
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, long> Directions { get; set; }
    }
}
