using AdventureTerreInterfaces;
using Orleans;
using Orleans.Providers;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace AdventureTerreGrains
{
    [Reentrant]
    [StorageProvider(ProviderName = "AzureStore")]
    public class PlayerGrain : Orleans.Grain<IPlayerState>, IPlayerGrain
    {
        private string previousCommand;
        public override Task ActivateAsync()
        {
            return base.ActivateAsync();
        }

        public async Task ProcessMessage(string message)
        {
            Trace.TraceInformation("ProcessMessage: " + message);
            var notifier = PushNotifierGrainFactory.GetGrain(0);
            //var speed = GetSpeed(this.LastMessage, message);

            // record the last message
            //this.LastMessage = message;

            // forward the message to the notifier grain
            //var velocityMessage = new VelocityMessage(message, speed);
            await notifier.SendMessage(message);
            //this.State.myInfo.Key = this.GetPrimaryKey();
        }
        Task IPlayerGrain.SetInfoGuid(Guid guid)
        {
            this.State.myInfo.Key = guid;
            return TaskDone.Done;
        }
        Task<PlayerInfo> IPlayerGrain.GetPlayerInfo()
        {
            return Task.FromResult(this.State.myInfo);
        }

        Task<Guid> IPlayerGrain.GetPlayerGuid()
        {
            return Task.FromResult(this.GetPrimaryKey());
        }
        Task<string> IPlayerGrain.Name()
        {
            return Task.FromResult(State.myInfo.Name);
        }

        Task<IRoomGrain> IPlayerGrain.RoomGrain()
        {
            return Task.FromResult(State.roomGrain);
        }

        async Task<IRoomGrain> IPlayerGrain.GetRoomGrainByRoomId(long id)
        {
            return await pGetRoomGrainByRoomId(id);
        }

        Task<IRoomGrain> IPlayerGrain.GetRandomRoom()
        {
            var rand = new Random();
            //return this.State.roomGrains[rand.Next(0, this.State.roomGrains.Count)];            
            return Task.FromResult(this.State.roomGrains[rand.Next(0, this.State.roomGrains.Count)]);            
        }

        private async Task<IRoomGrain> pGetRoomGrainByRoomId(long id)
        {
            for (int i = 0; i < this.State.roomGrains.Count; i++)
            {
                IRoomGrain room = this.State.roomGrains[i];
                long roomId = await room.RoomId();
                if (roomId == id)
                    return room;
            }
            return null;
        }

        async Task IPlayerGrain.Die()
        {
            // Drop everything
            var tasks = new List<Task<string>>();
            foreach (var thing in new List<Thing>(State.things))
            {
                tasks.Add(this.Drop(thing));
            }
            await Task.WhenAll(tasks);

            // Exit the game
            if (this.State.roomGrain != null)
            {
                await this.State.roomGrain.Exit(State.myInfo);
                this.State.roomGrain = null;
                State.killed = true;
            }
            await State.WriteStateAsync();
        }

        async Task<string> Drop(Thing thing)
        {
            if (State.killed)
                return await CheckAlive();

            if (thing != null)
            {
                this.State.things.Remove(thing);
                await this.State.roomGrain.Drop(thing);
                await State.WriteStateAsync();
                return "Okay.";
            }
            else
                return "I don't understand.";
        }

        async Task<string> Take(Thing thing)
        {
            if (State.killed)
                return await CheckAlive();

            if (thing != null)
            {
                this.State.things.Add(thing);
                await this.State.roomGrain.Take(thing);
                await State.WriteStateAsync();
                return "Okay.";
            }
            else
                return "I don't understand.";
        }

        Task IPlayerGrain.SetName(string name)
        {
            this.State.myInfo.Name = name;
            return TaskDone.Done;
        }
        
        async Task SetRoomGrain(IRoomGrain room)
        {
            this.State.roomGrain = room;
            await room.Enter(State.myInfo);
            await State.WriteStateAsync();
        }

        async Task<string> Go(string direction)
        {
            IRoomGrain destination = await this.State.roomGrain.ExitTo(direction);

            StringBuilder description = new StringBuilder();

            if (destination != null)
            {
                await this.State.roomGrain.Exit(State.myInfo);
                await destination.Enter(State.myInfo);

                this.State.roomGrain = destination;
                var desc = await destination.Description(State.myInfo);

                if (desc != null)
                    description.Append(desc);
            }
            else
            {
                description.Append("You cannot go in that direction.");
            }

            await State.WriteStateAsync();
            return description.ToString();
        }

        async Task<string> CheckAlive()
        {
            if (!State.killed)
                return null;

            // Go to room '-2', which is the place of no return.
            var room = await pGetRoomGrainByRoomId(-2);
            return await room.Description(State.myInfo);
        }

        async Task<string> Kill(string target)
        {
            if (State.things.Count == 0)
                return "With what? Your bare hands?";

            var player = await this.State.roomGrain.FindPlayer(target);
            if (player != null)
            {
                var weapon = State.things.Where(t => t.Category == "weapon").FirstOrDefault();
                if (weapon != null)
                {
                    await PlayerGrainFactory.GetGrain(player.Key).Die();
                    return target + " is now dead.";
                }
                return "With what? Your bare hands?";
            }

            var monster = await this.State.roomGrain.FindMonster(target);
            if (monster != null)
            {
                var weapons = monster.KilledBy.Join(State.things, id => id, t => t.Id, (id, t) => t);
                if (weapons.Count() > 0)
                {
                    //TODO: Test killing, may need to change to monster.Key from monster.Id
                    await MonsterGrainFactory.GetGrain(monster.Id).Kill(this.State.roomGrain);
                    return target + " is now dead.";
                }
                return "You don't have anything that can kill " + target + "!";
            }
            return "I can't see " + target + " here. Are you sure?";
        }

        private string RemoveStopWords(string s)
        {
            string[] stopwords = new string[] { " on ", " the ", " a " };

            StringBuilder sb = new StringBuilder(s);
            foreach (string word in stopwords)
            {
                sb.Replace(word, " ");
            }

            return sb.ToString();
        }

        private Thing FindMyThing(string name)
        {
            return State.things.Where(x => x.Name == name).FirstOrDefault();
        }

        private string Rest(string[] words)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 1; i < words.Length; i++)
                sb.Append(words[i] + " ");

            return sb.ToString().Trim().ToLower();
        }

        async Task IPlayerGrain.AddRoom(IRoomGrain room)
        {
            this.State.roomGrains.Add(room);
            if (State.roomGrain == null)
            {
                //State.roomGrain = room;
                //await Enter
                await SetRoomGrain(room);
                //await SetRoomGrain(room);
            }
             await State.WriteStateAsync();
        }

        async Task IPlayerGrain.AddMonster(IMonsterGrain monster)
        {
            this.State.monsterGrains.Add(monster);
            await State.WriteStateAsync();
        }

        async Task IPlayerGrain.SetupRooms()
        {
            var tasks = new List<Task>();
            foreach (var room in this.State.roomGrains)
            {
                var exitRoomIds = await room.ExitRoomIds();                
                foreach (var exitId in exitRoomIds)
                {
                    IRoomGrain grain = await pGetRoomGrainByRoomId(exitId.Value);
                    tasks.Add(room.SetExitRoomForKey(exitId.Key, grain));
                }
            }
            await Task.WhenAll(tasks);
            await State.WriteStateAsync();
        }

        async Task IPlayerGrain.ClearGrainAndState()
        {
            foreach (var grain in this.State.monsterGrains)
                await grain.ClearGrainAndState();
            foreach (var grain in this.State.roomGrains)
                await grain.ClearGrainAndState();

            await this.State.ClearStateAsync();
            this.DeactivateOnIdle();
        }

        async Task<string> IPlayerGrain.Play(string command)
        {
            Thing thing;
            command = RemoveStopWords(command);

            string[] words = command.Split(' ');

            string verb = words[0].ToLower();

            if (State.killed && verb != "end")
                return await CheckAlive();

            try
            {

                switch (verb)
                {
                    case "start":
                        return await this.State.roomGrain.Description(State.myInfo);
                    case "look":
                        return await this.State.roomGrain.Description(State.myInfo);
                    case "go":
                        if (words.Length == 1)
                            return "Go where?";
                        return await Go(words[1]);
                    case "north":
                    case "south":
                    case "east":
                    case "west":
                        return await Go(verb);
                    case "kill":
                        if (words.Length == 1)
                            return "Kill what?";
                        var target = command.Substring(verb.Length + 1);
                        return await Kill(target);
                    case "drop":
                        thing = FindMyThing(Rest(words));
                        return await Drop(thing);
                    case "take":
                        thing = await State.roomGrain.FindThing(Rest(words));
                        return await Take(thing);
                    case "help":
                        if (words.Length == 1)
                            return "This is a text based game.\nYou might be able to go in different directions, talk to different things,\nand explore the world.  Oh and try to survive.\n\nFor more assistance, try 'help commands'.";
                        else if (words[1] == "commands")
                            return "There are a few different commands you can try using in this game:\n\ngo <direction>\ntake <item name>\nkill <who>\ndrop <item name>\ninventory\nrestart";
                        else return "I don't understand what you need help with";
                    case "inv":
                    case "inventory":
                        return "You are carrying: " + string.Join(" ", State.things.Select(x => x.Name));
                    case "end":
                        return "What do you want to end?";
                    case "broadcasttest":
                        await ProcessMessage("This was a test");
                        return "sigtested";
                    case "restart":
                        return "Are you sure you want to restart?  All progress will be lost.";
                    case "yes":
                        switch (previousCommand)
                        {
                            case "restart":
                                return "COMMAND::RESTART";
                        }
                        return "What are you saying Yes to?";
                    case "no":
                        switch (previousCommand)
                        {
                            case "restart":
                                return "Ok, we won't restart";
                        }
                        return "I'm not sure what we're not doing but we won't!";
                }
                return "I don't understand.";
            }
            finally
            {
                this.previousCommand = verb;
            }
        }
    }
}
