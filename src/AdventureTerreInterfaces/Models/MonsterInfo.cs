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
    public class MonsterInfo : CharacterInfo
    {                
        public List<long> KilledBy { get; set; }
    }
}
