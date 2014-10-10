using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AdventureTerreInterfaces.Models;

namespace AdventureTerreInterfaces.GrainInterfaces
{
    public interface ICharacterGrain : Orleans.IGrainWithStringKey
    {
        Task<string> Name();
        Task<string> Description(IGameStateGrain gameState);
        Task<string> Response(IGameStateGrain gameState);
        Task SetInfo(CharacterInfo info, IPlayerGrain player);
        Task SetPlayerGuid(Guid playerGuid);
        Task SetRoomGrain(IRoomGrain room);
        Task<IRoomGrain> RoomGrain();
        Task<string> GetPrintableInfo();
        Task ClearGrainAndState();
        
    }

    public interface ICharacterState : IGrainState
    {
        IRoomGrain roomGrain { get; set; }
        Guid playerGuid { get; set; }
        IPlayerGrain playerGrain { get; set; }
    }
}
