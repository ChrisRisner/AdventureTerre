using AdventureTerreInterfaces;
using Orleans;
using Orleans.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdventureTerreGrains
{
    [StorageProvider(ProviderName = "AzureStore")]
    public class UniverseGrain : Orleans.Grain<IUniverseState>, IUniverseGrain
    {
    }
}
