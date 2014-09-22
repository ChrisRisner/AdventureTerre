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
    public class RoomGrain : Orleans.Grain<IRoomState>, IRoomGrain
    {
        Task IRoomGrain.Enter(PlayerInfo player)
        {
            State.players.RemoveAll(x => x.Key == player.Key);
            State.players.Add(player);
            return State.WriteStateAsync();
        }

        Task IRoomGrain.Exit(PlayerInfo player)
        {
            State.players.RemoveAll(x => x.Key == player.Key);
            return State.WriteStateAsync();
        }

        Task IRoomGrain.Enter(MonsterInfo monster)
        {
            State.monsters.RemoveAll(x => x.Id == monster.Id);
            State.monsters.Add(monster);
            return State.WriteStateAsync();
        }

        Task IRoomGrain.Exit(MonsterInfo monster)
        {
            State.monsters.RemoveAll(x => x.Id == monster.Id);
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

        Task IRoomGrain.SetInfo(RoomInfo info)
        {
            this.State.roomId = info.Id;
            this.State.description = info.Description;

            foreach (var kv in info.Directions)
            {
                this.State.exitRoomIds[kv.Key] = kv.Value;
            }
            return State.WriteStateAsync();
        }

        Task IRoomGrain.SetExitRoomForKey(string key, IRoomGrain roomGrain)
        {
            this.State.exits[key] = roomGrain;
            State.WriteStateAsync();
            return TaskDone.Done;
        }

        Task<Dictionary<string, long>> IRoomGrain.ExitRoomIds()
        {
            return Task.FromResult(this.State.exitRoomIds);
        }

        Task<Thing> IRoomGrain.FindThing(string name)
        {
            return Task.FromResult(State.things.Where(x => x.Name == name).FirstOrDefault());
        }

        Task<PlayerInfo> IRoomGrain.FindPlayer(string name)
        {
            name = name.ToLower();
            return Task.FromResult(State.players.Where(x => x.Name.ToLower().Contains(name)).FirstOrDefault());
        }

        Task<MonsterInfo> IRoomGrain.FindMonster(string name)
        {
            name = name.ToLower();
            return Task.FromResult(State.monsters.Where(x => x.Name.ToLower().Contains(name)).FirstOrDefault());
        }

        Task<string> IRoomGrain.Description(PlayerInfo whoisAsking)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(
                this.State.description);

            if (State.things.Count > 0)
            {
                sb.AppendLine("The following things are present:");
                foreach (var thing in State.things)
                {
                    sb.Append("  ").AppendLine(thing.Name);
                }
            }

            var others = State.players.Where(pi => pi.Key != whoisAsking.Key).ToArray();

            if (others.Length > 0 || State.monsters.Count > 0)
            {
                sb.AppendLine("Beware! These guys are in the room with you:");
                if (others.Length > 0)
                    foreach (var player in others)
                    {
                        sb.Append(" player:  ").AppendLine(player.Name);
                    }
                if (State.monsters.Count > 0)
                    foreach (var monster in State.monsters)
                    {
                        sb.Append(" monster: ").AppendLine(monster.Name);
                    }
            }

            return Task.FromResult(sb.ToString());
        }

        Task<IRoomGrain> IRoomGrain.ExitTo(string direction)
        {
            return Task.FromResult((State.exits.ContainsKey(direction)) ? State.exits[direction] : null);
        }

        Task<PlayerInfo[]> IRoomGrain.GetPlayersInRoom()
        {
            return Task.FromResult(this.State.players.ToArray());
        }

        async Task IRoomGrain.ClearGrainAndState()
        {
            await this.State.ClearStateAsync();
            this.DeactivateOnIdle();
        }
    }
}
