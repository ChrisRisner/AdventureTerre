using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;

using AdventureTerreInterfaces.Models;

namespace AdventureTerreInterfaces.GrainInterfaces
{
    /// <summary>
    /// Orleans grain communication interface IMonsterGrain
    /// </summary>
    public interface IMonsterGrain : ICharacterGrain
    {
        Task<MonsterInfo> GetInfo();
        Task<string> Kill(IRoomGrain room);
    }

    public interface IMonsterState : ICharacterState
    {
        MonsterInfo monsterInfo { get; set; }
    }
}
