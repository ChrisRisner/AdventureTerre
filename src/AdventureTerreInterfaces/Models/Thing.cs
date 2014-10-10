using Orleans.Concurrency;
using System.Collections.Generic;

namespace AdventureTerreInterfaces.Models
{
    [Immutable]
    public class Thing
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public long FoundIn { get; set; }
        public List<Descriptor> Descriptors { get; set; }
        public List<CommandAction> CommandActions { get; set; }
    }
}
