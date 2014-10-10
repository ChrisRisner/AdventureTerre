using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AdventureTerreInterfaces.Models;

namespace AdventureTerreInterfaces.GrainInterfaces
{
    public interface INPCGrain : ICharacterGrain
    {
        Task<NPCInfo> GetInfo();
    }

    public interface INPCState : ICharacterState
    {
        NPCInfo npcInfo { get; set; }
    }
}
