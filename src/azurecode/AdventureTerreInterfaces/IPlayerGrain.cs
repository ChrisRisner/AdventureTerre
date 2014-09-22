using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdventureTerreInterfaces
{
    public interface IPlayerGrain : Orleans.IGrain
    {
        Task<Guid> GetPlayerGuid();
        Task ProcessMessage(string message);
        Task AddRoom(IRoomGrain room);
        Task AddMonster(IMonsterGrain monster);
        Task<IRoomGrain> GetRoomGrainByRoomId(long id);
        Task<IRoomGrain> GetRandomRoom();
        Task SetupRooms();
        Task<PlayerInfo> GetPlayerInfo();

        Task<string> Name();
        Task SetName(string name);
        Task SetInfoGuid(Guid guid);
        //Task SetRoomGrain(IRoomGrain room);
        Task<IRoomGrain> RoomGrain();

        Task Die();
        Task<string> Play(string command);

        Task ClearGrainAndState();
    }

    public interface IPlayerState : IGrainState
    {
        IRoomGrain roomGrain { get; set; }
        List<Thing> things { get; set; }
        List<IRoomGrain> roomGrains { get; set; }
        List<IMonsterGrain> monsterGrains { get; set; }
        Boolean killed { get; set; }
        PlayerInfo myInfo { get; set; }
    }
}
