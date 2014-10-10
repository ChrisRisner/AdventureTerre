using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AdventureTerreInterfaces.Models;

namespace AdventureTerreInterfaces.GrainInterfaces
{
    public interface IPlayerGrain : Orleans.IGrainWithGuidKey
    {
        Task<Guid> GetPlayerGuid();
        Task ProcessMessage(string message);
        Task AddRoom(IRoomGrain room);
        Task AddMonster(IMonsterGrain monster);
        Task AddNpc(INPCGrain npc);
        Task<IRoomGrain> GetRoomGrainByRoomId(long id);
        Task<IRoomGrain> GetRandomRoom();
        Task<PlayerInfo> GetPlayerInfo();
        Task<string> Name();
        Task SetName(string name);
        Task SetInfoGuid(Guid guid);
        Task<IRoomGrain> RoomGrain();
        Task Die();
        Task<string> Play(string command);
        Task ClearGrainAndState();
        Task<string[]> GetPlayerHistory();
        Task<IGameStateGrain> GetGameState();
        Task InitGameState(MapInfo mapInfo);
        Task SetVersion(int newVersion);
        Task<int> Version();
    }

    public interface IPlayerState : IGrainState
    {
        IRoomGrain roomGrain { get; set; }
        List<Thing> things { get; set; }
        List<IRoomGrain> roomGrains { get; set; }
        List<IMonsterGrain> monsterGrains { get; set; }
        List<INPCGrain> npcGrains { get; set; }
        Boolean killed { get; set; }
        PlayerInfo myInfo { get; set; }
        List<string> playerHistory { get; set; }
        IGameStateGrain gameState { get; set; }
        int version { get; set; }
        List<StateChangeAction> stateChangeActions { get; set; }
    }
}
