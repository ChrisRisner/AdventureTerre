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
    public class MonsterInfo
    {
        public long Id { get; set; }
        public Guid Key { get; set; }
        public string Name { get; set; }
        public List<long> KilledBy { get; set; }
    }
}
