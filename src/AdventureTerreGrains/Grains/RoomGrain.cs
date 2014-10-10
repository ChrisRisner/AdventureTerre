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
    public class RoomGrain : Orleans.Grain<IRoomState>, IRoomGrain
    {
        Task IRoomGrain.Enter(IPlayerGrain player)
        {
            State.players.Add(player);
            return State.WriteStateAsync();
        }
        Task IRoomGrain.Exit(IPlayerGrain player)
        {
            State.players.Remove(player);
            return State.WriteStateAsync();
        }
        Task IRoomGrain.Enter(IMonsterGrain monster)
        {            
            State.monsters.Add(monster);
            return State.WriteStateAsync();
        }
        Task IRoomGrain.Exit(IMonsterGrain monster)
        {
            State.monsters.Remove(monster);
            return State.WriteStateAsync();
        }
        Task IRoomGrain.Enter(INPCGrain npc)
        {
            State.npcs.Add(npc);
            return State.WriteStateAsync();
        }
        Task IRoomGrain.Exit(INPCGrain npc)
        {
            State.npcs.Remove(npc);
            return State.WriteStateAsync();
        }
        Task IRoomGrain.Drop(Thing thing)
        {
            State.things.RemoveAll(x => x.Id == thing.Id);
            State.things.Add(thing);
            return State.WriteStateAsync();
        }
        Task IRoomGrain.Take(Thing thing)
        {
            State.things.RemoveAll(x => x.Name == thing.Name);
            return State.WriteStateAsync();
        }
        Task<long> IRoomGrain.RoomId()
        {
            return Task.FromResult(this.State.roomId);            
        }
        Task IRoomGrain.SetInfo(RoomInfo info, Guid rootGuid)
        {
            this.State.roomId = info.Id;
            this.State.name = info.Name;
            this.State.descriptors = info.Descriptors;
            this.State.directions = info.Directions;
            foreach (var direction in this.State.directions)
            {
                direction.Room = GrainFactory.GetGrain<IRoomGrain>(rootGuid.ToString() + "Room" + direction.RoomId);
            }
            return State.WriteStateAsync();
        }
        Task<Thing> IRoomGrain.FindThing(string name)
        {
            name = name.ToLower();
            return Task.FromResult(State.things.Where(x => x.Name.ToLower().Contains(name)).FirstOrDefault());
        }
        async Task<PlayerInfo> IRoomGrain.FindPlayer(string name)
        {
            name = name.ToLower();
            foreach (var player in State.players)
            {
                string playerName = await player.Name();
                if (playerName.ToLower().Contains(name))
                    return await player.GetPlayerInfo();
            }
            return null;
        }
        async Task<MonsterInfo> IRoomGrain.FindMonster(string name)
        {
            name = name.ToLower();
            foreach (var monster in State.monsters)
            {
                string monsterName = await monster.Name();
                if (monsterName.ToLower().Contains(name))
                    return await monster.GetInfo();
            }
            return null;
        }
        async Task<NPCInfo> IRoomGrain.FindNpc(string name)
        {
            name = name.ToLower();
            foreach (var npc in State.npcs)
            {
                string npcName = await npc.Name();
                if (npcName.ToLower().Contains(name))
                    return await npc.GetInfo();
            }
            return null;
        }
        Task<string> IRoomGrain.Name()
        {
            return Task.FromResult(this.State.name);
        }
        async Task<string> IRoomGrain.Description(PlayerInfo whoisAsking, IGameStateGrain gameState)
        {
            StringBuilder sb = new StringBuilder();            
            var playerGrain = GrainFactory.GetGrain<IPlayerGrain>(whoisAsking.Key);
            sb.AppendLine(await GrainHelper.GetDescriptorForState(gameState, State.descriptors, playerGrain));
            if (State.things.Count > 0)
            {
                sb.AppendLine("The following things are present:");
                foreach (var thing in State.things)
                {
                    sb.Append("  ").AppendLine(thing.Name);
                }
            }
            if (State.players.Count > 1 || 
                State.monsters.Count > 0 ||
                State.npcs.Count > 0)
            {
                sb.AppendLine("Beware! These guys are in the room with you:");
                foreach (var player in State.players)
                {
                    if (player.GetPrimaryKey() != whoisAsking.Key)
                        sb.Append(" player:  ").AppendLine(await player.Name());
                }
                if (State.monsters.Count > 0)
                    foreach (var monster in State.monsters)
                    {
                        sb.Append(" monster: ").AppendLine(await monster.Name());
                    }
                if (State.npcs.Count > 0)
                    foreach (var npc in State.npcs)
                    {
                        sb.Append(" npc: ").AppendLine(await npc.Name());
                    }
            }
            return sb.ToString();
        }
        //This exit method is called by players so they will check against flags
        async Task<IRoomGrain> IRoomGrain.ExitTo(string direction, IGameStateGrain gameState)
        {
            var directionObject = State.directions.Where(p => p.Cardinal == direction).FirstOrDefault();
            if (directionObject != null)
            {
                if (directionObject.Flags.Count == 0)
                    return directionObject.Room;
                else
                {
                    bool conditionsFailed = false;
                    foreach (var flag in directionObject.Flags)
                    {
                        if ((await gameState.GetStateForKey(flag.Key)) != flag.Value)
                            conditionsFailed = true;
                    }
                    if (conditionsFailed == false)
                    {
                        return directionObject.Room;
                    }                    
                }
            }
            return null;
        }
        //This exit to method should only be called by characters (NPCs and Monsters)
        Task<IRoomGrain> IRoomGrain.ExitTo(string direction, ICharacterGrain character)
        {
            var directionObject = State.directions.Where(p => p.Cardinal == direction).FirstOrDefault();
            return Task.FromResult(directionObject != null ? directionObject.Room : null);           
        }
        Task<IPlayerGrain[]> IRoomGrain.GetPlayersInRoom()
        {
            return Task.FromResult(this.State.players.ToArray());
        }        
        async Task IRoomGrain.ClearGrainAndState()
        {
            await this.State.ClearStateAsync();
            this.DeactivateOnIdle();
        }
        async Task IRoomGrain.TriggerStateSave()
        {
            await this.State.WriteStateAsync();
        }
        async Task IRoomGrain.UpdateThing(Thing updatedThing)
        {
            var oldThing = this.State.things.Where(t => t.Id == updatedThing.Id).FirstOrDefault();
            if (oldThing != null)
            {
                oldThing = updatedThing;
                await this.State.WriteStateAsync();
            }
        }

        Task<string> IRoomGrain.GetPrintableInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Key:");
            sb.Append(GrainHelper.GetPrimaryKeyStringFromGrain(this));
            sb.Append(":ID:");
            sb.Append(this.State.roomId);
            sb.Append(":Name:");
            sb.Append(this.State.name);
            return Task.FromResult(sb.ToString());
        }
    }
}
