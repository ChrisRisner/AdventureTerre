using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdventureTerreInterfaces.Models
{
    [Immutable]
    public abstract class CharacterInfo
    {
        public long Id { get; set; }
        public Guid Key { get; set; }
        public string Name { get; set; }        
        public long StartIn { get; set; }        
        public List<Descriptor> Descriptors { get; set; }
        public List<Descriptor> Responses { get; set; }
        public bool MovesRandomly { get; set; }
    }
}
