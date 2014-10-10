using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdventureTerreInterfaces.GrainInterfaces
{
    [StatelessWorker]
    public interface IPushNotifierGrain : Orleans.IGrain
    {
        Task SendMessage(string message);
        Task SendMessage(string message, string recipient);
        Task ClearGrainAndState();
    }
}
