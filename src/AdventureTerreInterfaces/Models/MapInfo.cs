using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Orleans;
using Orleans.Concurrency;

namespace AdventureTerreInterfaces.Models
{
    [Immutable]
    public class MapInfo
    {
        public string Name { get; set; }
        public List<RoomInfo> Rooms { get; set; }
        public List<CategoryInfo> Categories { get; set; }
        public List<Thing> Things { get; set; }
        public List<MonsterInfo> Monsters { get; set; }
        public List<NPCInfo> NPCs { get; set; }
        public List<StateChangeAction> stateChangeActions { get; set; }
    }
}
