using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AdventureTerreInterfaces.Models;

namespace AdventureTerreInterfaces.GrainInterfaces
{
    public interface IRoomGrain : Orleans.IGrainWithStringKey
    {
        Task<string> Description(PlayerInfo whoisAsking, IGameStateGrain gameState);
        Task SetInfo(RoomInfo info, Guid rootGuid);
        Task<string> Name();
        Task<IRoomGrain> ExitTo(string direction, IGameStateGrain gameState);
        Task<IRoomGrain> ExitTo(string direction, ICharacterGrain character);
        Task<long> RoomId();
        Task Enter(IPlayerGrain player);
        Task Exit(IPlayerGrain player);
        Task Enter(IMonsterGrain monster);
        Task Exit(IMonsterGrain monster);
        Task Enter(INPCGrain npc);
        Task Exit(INPCGrain npc);
        Task Drop(Thing thing);
        Task Take(Thing thing);
        Task<Thing> FindThing(string name);
        Task<PlayerInfo> FindPlayer(string name);
        Task<MonsterInfo> FindMonster(string name);
        Task<NPCInfo> FindNpc(string name);
        Task<IPlayerGrain[]> GetPlayersInRoom();
        Task ClearGrainAndState();
        Task TriggerStateSave();
        Task<string> GetPrintableInfo();
        Task UpdateThing(Thing updatedThing);
    }

    public interface IRoomState : IGrainState
    {
        //string description { get; set; }
        long roomId { get; set; }
        string name { get; set; }
        List<IPlayerGrain> players { get; set; }
        List<IMonsterGrain> monsters { get; set; }
        List<Thing> things { get; set; }
        List<INPCGrain> npcs { get; set; }
        List<Descriptor> descriptors { get; set; }
        List<Direction> directions { get; set; }
    }
}
