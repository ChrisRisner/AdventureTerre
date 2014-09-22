using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdventureTerreInterfaces
{
    public interface IUniverseGrain : Orleans.IGrain
    {
    }

    public interface IUniverseState : IGrainState
    {

    }
}
