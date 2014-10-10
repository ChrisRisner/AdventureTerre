using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AdventureTerreInterfaces.GrainInterfaces;

namespace AdventureTerreInterfaces.Models
{
    public class Direction
    {
        public string Cardinal { get; set; }
        public Dictionary<string, bool> Flags { get; set; }
        public long RoomId { get; set; }
        public IRoomGrain Room { get; set; }
    }
}
