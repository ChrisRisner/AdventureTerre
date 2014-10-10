using AdventureTerreInterfaces;
using Orleans;
using Orleans.Concurrency;
using Orleans.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AdventureTerreInterfaces.Models;
using AdventureTerreInterfaces.GrainInterfaces;

namespace AdventureTerreGrains.Grains
{
    [Reentrant]
    [StorageProvider(ProviderName = "AzureStore")]
    public class GameStateGrain : Orleans.Grain<IGameState>, IGameStateGrain
    {
        async Task IGameStateGrain.SetGameStateFlags(Dictionary<string, bool> newFlags)
        {
            if (newFlags != null && newFlags.Count > 0)
            {
                foreach (var item in newFlags)
                {
                    this.State.gameState[item.Key] = item.Value;
                }
                await this.State.WriteStateAsync();
            }
        }

        async Task IGameStateGrain.InitGameState(MapInfo mapInfo)
        {
            var monsterIds = mapInfo.Monsters.Select(m => m.Id).ToArray();
            var npcIds = mapInfo.NPCs.Select(n => n.Id).ToArray();
            foreach (var thing in mapInfo.Things)
            {
                if (Constants.kTakeableCategories.Contains(thing.Category))
                    State.gameState["HasItem" + thing.Id] = false;
                else if (Constants.kMovableItemCategory.Equals(thing.Category))
                    State.gameState["HasMovedItem" + thing.Id] = false;
            }
            foreach (var monster in monsterIds)
            {
                State.gameState["HasKilled" + monster] = false;
            }
            await State.WriteStateAsync();
        }

        async Task IGameStateGrain.UpdateGameState(string key, bool value)
        {
            State.gameState[key] = value;
            await State.WriteStateAsync();
            //return TaskDone.Done;
        }

        Task<bool> IGameStateGrain.GetStateForKey(string key)
        {
            if (State == null || State.gameState == null ||
                State.gameState.Count == 0)
                return Task.FromResult(false);
            return Task.FromResult(State.gameState[key]);
        }
        Task<string> IGameStateGrain.OutputGameState()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("GameState");
            foreach (var item in State.gameState)
            {
                sb.Append(item.Key);
                sb.Append("---");
                sb.AppendLine(item.Value.ToString());
            }
            return Task.FromResult(sb.ToString());
        }
        async Task IGameStateGrain.ClearGrainAndState()
        {
            await this.State.ClearStateAsync();
            this.DeactivateOnIdle();
        }
    }
}
