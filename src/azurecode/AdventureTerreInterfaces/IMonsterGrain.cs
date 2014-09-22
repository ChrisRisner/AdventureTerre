using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;

namespace AdventureTerreInterfaces
{
    /// <summary>
    /// Orleans grain communication interface IMonsterGrain
    /// </summary>
    public interface IMonsterGrain : Orleans.IGrain
    {
        Task<string> Name();
        Task SetInfo(MonsterInfo info);
        Task SetPlayerGuid(Guid playerGuid);

        Task SetRoomGrain(IRoomGrain room);
        Task<IRoomGrain> RoomGrain();
        Task<string> Kill(IRoomGrain room);
        Task ClearGrainAndState();
    }

    public interface IMonsterState : IGrainState
    {
        MonsterInfo monsterInfo { get; set; }
        IRoomGrain roomGrain { get; set; }

        Guid playerGuid { get; set; }
    }
}
