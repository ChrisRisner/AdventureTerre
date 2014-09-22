using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdventureTerreInterfaces
{
    public interface IRoomGrain : Orleans.IGrain
    {
        Task<string> Description(PlayerInfo whoisAsking);
        Task SetInfo(RoomInfo info);
        Task<IRoomGrain> ExitTo(string direction);
        Task<long> RoomId();
        Task<Dictionary<string, long>> ExitRoomIds();
        Task SetExitRoomForKey(string key, IRoomGrain roomGrain);
        Task Enter(PlayerInfo player);
        Task Exit(PlayerInfo player);
        Task Enter(MonsterInfo monster);
        Task Exit(MonsterInfo monster);
        Task Drop(Thing thing);
        Task Take(Thing thing);
        Task<Thing> FindThing(string name);
        Task<PlayerInfo> FindPlayer(string name);
        Task<MonsterInfo> FindMonster(string name);
        Task<PlayerInfo[]> GetPlayersInRoom();
        Task ClearGrainAndState();
    }

    public interface IRoomState : IGrainState
    {
        string description { get; set; }
        long roomId { get; set; }

        List<PlayerInfo> players { get; set; }
        List<MonsterInfo> monsters { get; set; }
        List<Thing> things { get; set; }

        Dictionary<string, IRoomGrain> exits { get; set; }
        Dictionary<string, long> exitRoomIds { get; set; }

    }
}
