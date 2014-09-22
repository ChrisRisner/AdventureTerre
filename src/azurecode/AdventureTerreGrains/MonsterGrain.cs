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
    public class MonsterGrain : Orleans.Grain<IMonsterState>, IMonsterGrain
    {
        public override Task ActivateAsync()
        {
            this.State.monsterInfo.Key = this.GetPrimaryKey();
            RegisterTimer((_) => Move(), null, TimeSpan.FromSeconds(120), TimeSpan.FromMinutes(2));
            //RegisterTimer((_) => Move(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
            return base.ActivateAsync();
        }

        Task IMonsterGrain.SetInfo(MonsterInfo info)
        {
            this.State.monsterInfo = info;
            this.State.monsterInfo.Key = this.GetPrimaryKey();
            return State.WriteStateAsync();
        }

        Task<string> IMonsterGrain.Name()
        {
            return Task.FromResult(this.State.monsterInfo.Name);
        }

        Task IMonsterGrain.SetPlayerGuid(Guid playerGuid)
        {
            this.State.playerGuid = playerGuid;
            return TaskDone.Done;
        }

        async Task IMonsterGrain.SetRoomGrain(IRoomGrain room)
        {
            if (this.State.roomGrain != null)
                await this.State.roomGrain.Exit(this.State.monsterInfo);
            this.State.roomGrain = room;
            await this.State.roomGrain.Enter(this.State.monsterInfo);
            await State.WriteStateAsync();
        }

        Task<IRoomGrain> IMonsterGrain.RoomGrain()
        {
            return Task.FromResult(State.roomGrain);
        }

        async Task Move()
        {
            var directions = new string[] { "north", "south", "west", "east" };

            var rand = new Random().Next(0, 4);
            IRoomGrain nextRoom = await this.State.roomGrain.ExitTo(directions[rand]);

            if (null == nextRoom)
                return;

            await this.State.roomGrain.Exit(this.State.monsterInfo);
            await nextRoom.Enter(this.State.monsterInfo);

            //Get Push grain
            var notifier = PushNotifierGrainFactory.GetGrain(0);
            //get the room grain
            //loop through players in the room grain
            var playersInRoom = await this.State.roomGrain.GetPlayersInRoom();
            for (int i = 0; i < playersInRoom.Length; i++)
            {
                Guid playerId = playersInRoom[i].Key;
                await notifier.SendMessage(this.State.monsterInfo.Name + " has left the room", playerId.ToString());
            }

            this.State.roomGrain = nextRoom;
            //Send messages to players in new room
            playersInRoom = await this.State.roomGrain.GetPlayersInRoom();
            for (int i = 0; i < playersInRoom.Length; i++)
            {
                Guid playerId = playersInRoom[i].Key;
                await notifier.SendMessage(this.State.monsterInfo.Name + " has entered room", playerId.ToString());
            }

            await State.WriteStateAsync();
        }


        Task<string> IMonsterGrain.Kill(IRoomGrain room)
        {
            if (this.State.roomGrain != null)
            {
                if (this.State.roomGrain.GetPrimaryKey() != room.GetPrimaryKey())
                {
                    return Task.FromResult(State.monsterInfo.Name + " snuck away. You were too slow!");
                }
                return this.State.roomGrain.Exit(this.State.monsterInfo).ContinueWith(t => State.monsterInfo.Name + " is dead.");
            }
            return Task.FromResult(State.monsterInfo.Name + " is already dead. You were too slow and someone else got to him!");
        }

        async Task IMonsterGrain.ClearGrainAndState()
        {
            await this.State.ClearStateAsync();
            this.DeactivateOnIdle();
        }
    }
}
