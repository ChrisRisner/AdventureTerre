using AdventureTerreInterfaces;
using Orleans;
using Orleans.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AdventureTerreInterfaces.GrainInterfaces;

namespace AdventureTerreGrains.Grains
{
    [StorageProvider(ProviderName = "AzureStore")]
    public class UniverseGrain : Orleans.Grain<IUniverseState>, IUniverseGrain
    {
    }
}
