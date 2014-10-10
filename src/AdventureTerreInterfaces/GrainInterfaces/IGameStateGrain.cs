using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AdventureTerreInterfaces.Models;

namespace AdventureTerreInterfaces.GrainInterfaces
{
    public interface IGameStateGrain : Orleans.IGrainWithGuidKey
    {
        Task SetGameStateFlags(Dictionary<string, bool> newFlags);
        Task InitGameState(MapInfo mapInfo);
        Task UpdateGameState(string key, bool value);
        Task<bool> GetStateForKey(string key);
        Task<string> OutputGameState();
        Task ClearGrainAndState();
    }

    public interface IGameState : IGrainState
    {
        Dictionary<string, bool> gameState { get; set; }
    }
}
