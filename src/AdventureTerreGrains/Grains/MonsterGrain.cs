using AdventureTerreInterfaces;
using Orleans;
using Orleans.Concurrency;
using Orleans.Providers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AdventureTerreInterfaces.Models;
using AdventureTerreInterfaces.GrainInterfaces;

namespace AdventureTerreGrains.Grains
{
    [Reentrant]
    [StorageProvider(ProviderName = "AzureStore")]
    public class MonsterGrain : Orleans.Grain<IMonsterState>, IMonsterGrain
    {
        private IDisposable mMoveTimer;

        public override Task ActivateAsync()
        {            
            return base.ActivateAsync();
        }

        private async Task Move()
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
                await notifier.SendMessage(this.State.monsterInfo.Name + " has left the room", playerId.ToString());
            }
            this.State.roomGrain = nextRoom;
            //Send messages to players in new room
            playersInRoom = await this.State.roomGrain.GetPlayersInRoom();
            for (int i = 0; i < playersInRoom.Length; i++)
            {
                Guid playerId = playersInRoom[i].GetPrimaryKey();
                await notifier.SendMessage(this.State.monsterInfo.Name + " has entered room", playerId.ToString());
            }
            await State.WriteStateAsync();
        }

        #region IMonsterGrain Methods

        Task<MonsterInfo> IMonsterGrain.GetInfo()
        {
            return Task.FromResult(this.State.monsterInfo);
        }

        Task<string> IMonsterGrain.Kill(IRoomGrain room)
        {
            if (this.State.roomGrain != null)
            {
                string roomGrainPrimaryKey = GrainHelper.GetPrimaryKeyStringFromGrain(State.roomGrain);
                string roomPrimaryKey = GrainHelper.GetPrimaryKeyStringFromGrain(room);
                if (roomGrainPrimaryKey != roomPrimaryKey)
                {
                    return Task.FromResult(this.State.monsterInfo.Name + " snuck away. You were too slow!");
                }
                if (mMoveTimer != null)
                    mMoveTimer.Dispose();
                return this.State.roomGrain.Exit(this).ContinueWith(t => this.State.monsterInfo.Name + " is dead.");
            }
            return Task.FromResult(this.State.monsterInfo.Name + " is already dead. You were too slow and someone else got to him!");
        }

        #endregion

        #region ICharacterGrain Methods

        Task ICharacterGrain.SetInfo(CharacterInfo info, IPlayerGrain player)
        {
            this.State.monsterInfo = (MonsterInfo) info;
            this.State.playerGrain = player;
            if (info.MovesRandomly)
            {
                mMoveTimer = RegisterTimer((_) => Move(), null, TimeSpan.FromSeconds(120), TimeSpan.FromMinutes(2));
            }

            return State.WriteStateAsync();
        }
        Task<string> ICharacterGrain.Name()
        {
            return Task.FromResult(this.State.monsterInfo.Name);
        }
        Task<string> ICharacterGrain.Description(IGameStateGrain gameState)
        {
            return GrainHelper.GetDescriptorForState(gameState, State.monsterInfo.Descriptors, State.playerGrain);
        }
        async Task<string> ICharacterGrain.Response(IGameStateGrain gameState)
        {
            var obj = await GrainHelper.GetDescriptorForState(gameState, State.monsterInfo.Responses, State.playerGrain);
            return (this.State.monsterInfo.Name + " says\n\t" + obj.ToString());
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
            //sb.Append(this.GetPrimaryKey().ToString());
            sb.Append(":ID:");
            sb.Append(this.State.monsterInfo.Id);
            sb.Append(":Name:");
            sb.Append(this.State.monsterInfo.Name);
            sb.Append(":Moves:");
            sb.Append(this.State.monsterInfo.MovesRandomly);
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
