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

namespace AdventureTerreGrains
{
    [Reentrant]
    [StorageProvider(ProviderName = "AzureStore")]
    public class NPCGrain : Orleans.Grain<INPCState>, INPCGrain
    {
        private IDisposable mMoveTimer;

        async Task Move()
        {
            var directions = new string[] { "north", "south", "west", "east" };
            var rand = new Random().Next(0, 4);
            IRoomGrain nextRoom = await this.State.roomGrain.ExitTo(directions[rand], this);
            if (null == nextRoom)
                return;
            await this.State.roomGrain.Exit(this);
            await nextRoom.Enter(this);
            //Get Push grain
            var notifier = PushNotifierGrainFactory.GetGrain(0);
            //loop through players in the room grain
            var playersInRoom = await this.State.roomGrain.GetPlayersInRoom();
            for (int i = 0; i < playersInRoom.Length; i++)
            {
                Guid playerId = playersInRoom[i].GetPrimaryKey();
                await notifier.SendMessage(this.State.npcInfo.Name + " has left the room", playerId.ToString());
            }

            this.State.roomGrain = nextRoom;
            //Send messages to players in new room
            playersInRoom = await this.State.roomGrain.GetPlayersInRoom();
            for (int i = 0; i < playersInRoom.Length; i++)
            {
                Guid playerId = playersInRoom[i].GetPrimaryKey();
                await notifier.SendMessage(this.State.npcInfo.Name + " has entered room", playerId.ToString());
            }

            await State.WriteStateAsync();
        }

        #region INPCGrain Methods

        Task<NPCInfo> INPCGrain.GetInfo()
        {
            return Task.FromResult(this.State.npcInfo);
        }

        #endregion

        #region ICharacterGrain Methods
        Task ICharacterGrain.SetInfo(CharacterInfo info, IPlayerGrain player)
        {
            this.State.npcInfo = (NPCInfo)info;
            this.State.playerGrain = player;
            if (info.MovesRandomly)
            {
                mMoveTimer = RegisterTimer((_) => Move(), null, TimeSpan.FromSeconds(120), TimeSpan.FromMinutes(2));
            }
            return State.WriteStateAsync();
        }

        Task<string> ICharacterGrain.Name()
        {
            return Task.FromResult(this.State.npcInfo.Name);
        }
        Task<string> ICharacterGrain.Description(IGameStateGrain gameState)
        {
            return GrainHelper.GetDescriptorForState(gameState, State.npcInfo.Descriptors, State.playerGrain);
        }        
        async Task<string> ICharacterGrain.Response(IGameStateGrain gameState)
        {
            var obj = await GrainHelper.GetDescriptorForState(gameState, State.npcInfo.Responses, State.playerGrain);
            return (this.State.npcInfo.Name + " says\n\t" + obj.ToString());
        }
        Task ICharacterGrain.SetPlayerGuid(Guid playerGuid)
        {
            this.State.playerGuid = playerGuid;
            return TaskDone.Done;
        }
        async Task ICharacterGrain.SetRoomGrain(IRoomGrain room)
        {
            if (this.State.roomGrain != null)
                await this.State.roomGrain.Exit(this);
            this.State.roomGrain = room;
            await this.State.roomGrain.Enter(this);
            await State.WriteStateAsync();
        }
        Task<IRoomGrain> ICharacterGrain.RoomGrain()
        {
            return Task.FromResult(State.roomGrain);
        }        
        Task<string> ICharacterGrain.GetPrintableInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Key:");
            sb.Append(GrainHelper.GetPrimaryKeyStringFromGrain(this));
            sb.Append(":ID:");
            sb.Append(this.State.npcInfo.Id);
            sb.Append(":Name:");
            sb.Append(this.State.npcInfo.Name);
            sb.Append(":Moves:");
            sb.Append(this.State.npcInfo.MovesRandomly);
            return Task.FromResult(sb.ToString());
        }
        async Task ICharacterGrain.ClearGrainAndState()
        {
            if (mMoveTimer != null)
                mMoveTimer.Dispose();
            await this.State.ClearStateAsync();
            this.DeactivateOnIdle();
        }

        #endregion
    }
}
