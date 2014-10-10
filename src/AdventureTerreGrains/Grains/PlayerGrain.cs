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

using AdventureTerreInterfaces.Models;
using AdventureTerreInterfaces.GrainInterfaces;

namespace AdventureTerreGrains.Grains
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
            await notifier.SendMessage(message);
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
            return await GetRoomGrain(id);
        }
        Task<IRoomGrain> IPlayerGrain.GetRandomRoom()
        {
            var rand = new Random();
            return Task.FromResult(this.State.roomGrains[rand.Next(0, this.State.roomGrains.Count)]);            
        }
        private async Task<IRoomGrain> GetRoomGrain(long id)
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
                await this.State.roomGrain.Exit(this);
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
                string stateChangeResult = await setGameStateToValue("HasItem" + thing.Id, false);
                await State.WriteStateAsync();
                return "Okay." + stateChangeResult + "\n";
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
                if (!Constants.kTakeableCategories.Contains(thing.Category))
                {
                    return "You can't take the " + thing.Name;
                }
                this.State.things.Add(thing);
                await this.State.roomGrain.Take(thing);
                string stateChangeResult = await setGameStateToValue("HasItem" + thing.Id, true);
                await State.WriteStateAsync();
                return "Okay." + stateChangeResult + "\n";
            }
            else
                return "How could you take that?";
        }
        private async Task<string> setGameStateToValue(string flag, bool value)
        {
            await State.gameState.UpdateGameState(flag, value);
            var stateActionChange = State.stateChangeActions
                .Where(sca => sca.Flag == flag 
                    && sca.ToValue == value).FirstOrDefault();
            if (stateActionChange != null)
            {
                //Perform any actions required and return print text
                return stateActionChange.PrintText;
            }
            return null;
        }
        async Task<string> Move(Thing thing)
        {
            if (State.killed)
                return await CheckAlive();
            if (thing != null)
            {
                if (!Constants.kMovableItemCategory.Contains(thing.Category))
                {
                    return "You can't move the " + thing.Name + "\n";
                }
                return await GetCommandActionResponse(thing, "You've moved the ");
            }
            else
                return "How could you move that?\n";
        }

        private async Task<string> GetCommandActionResponse(Thing thing, string actionText)
        {
            var commandAction = thing.CommandActions.Where(ca => ca.CommandName == "move").FirstOrDefault();
            if (commandAction == null)
                return "Programmer error";
            if ((await this.State.gameState.GetStateForKey(commandAction.Flag)) != commandAction.NewValue)
            {
                string stateChangeResult = await setGameStateToValue(commandAction.Flag, commandAction.NewValue);
                if (commandAction.ShouldFlip)
                {
                    commandAction.NewValue = !commandAction.NewValue;
                    //Since we've altered the thing, save it's state as well!
                    await this.State.roomGrain.UpdateThing(thing);
                }
                return actionText + thing.Name + "." + stateChangeResult + "\n";
            }
            else
            {
                return "It looks like you've already done that!\n";
            }
        }
        Task IPlayerGrain.SetName(string name)
        {
            this.State.myInfo.Name = name;
            return TaskDone.Done;
        }
        
        async Task SetRoomGrain(IRoomGrain room)
        {
            this.State.roomGrain = room;
            await room.Enter(this);
            await State.WriteStateAsync();
        }

        async Task<string> Go(string direction)
        {
            IRoomGrain destination = await this.State.roomGrain.ExitTo(direction, this.State.gameState);
            StringBuilder description = new StringBuilder();
            if (destination != null)
            {
                await this.State.roomGrain.Exit(this);
                await destination.Enter(this);
                this.State.roomGrain = destination;
                var desc = await destination.Description(State.myInfo, this.State.gameState);
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
            var room = await GetRoomGrain(-2);
            return await room.Description(State.myInfo, State.gameState);
        }
        async Task<string> TalkTo(string target, string verb)
        {
            var monster = await this.State.roomGrain.FindMonster(target);
            if (monster != null)
            {
                var monsterGrain = GrainFactory.GetGrain<IMonsterGrain>(this.GetPrimaryKey().ToString() + "Monster" + monster.Id);
                return (await monsterGrain.Response(State.gameState) + "\n");
            }
            var npc = await this.State.roomGrain.FindNpc(target);
            if (npc != null)
            {
                var npcGrain = GrainFactory.GetGrain<INPCGrain>(this.GetPrimaryKey().ToString() + "Npc" + npc.Id);
                return (await npcGrain.Response(State.gameState) + "\n");
            }
            return "I can't see " + target + " here. You can't " + verb + " to them!";
        }
        async Task<string> Inspect(string target, string verb)
        {
            var player = await this.State.roomGrain.FindPlayer(target);
            if (player != null)
            {
                return "You see " + player.Name + "\n";
            }            
            var monster = await this.State.roomGrain.FindMonster(target);
            if (monster != null)
            {
                var monsterGrain = GrainFactory.GetGrain<IMonsterGrain>(this.GetPrimaryKey().ToString() + "Monster" + monster.Id);
                return await monsterGrain.Description(State.gameState) + "\n";
            }
            var npc = await this.State.roomGrain.FindNpc(target);
            if (npc != null)
            {
                var npcGrain = GrainFactory.GetGrain<INPCGrain>(this.GetPrimaryKey().ToString() + "Npc" + npc.Id);
                return await npcGrain.Description(State.gameState) + "\n";
            }
            var thing = await this.State.roomGrain.FindThing(target);
            if (thing != null)
            {
                return await GrainHelper.GetDescriptorForState(State.gameState, thing.Descriptors, this) + "\n";
            }

            var onPersonThing = this.State.things.Where(t => t.Name.Contains(target)).FirstOrDefault();
            if (onPersonThing != null)
            {
                return await GrainHelper.GetDescriptorForState(State.gameState, onPersonThing.Descriptors, this) + "\n";
            }
            return "I can't see " + target + " here. You can't " + verb + " something that isn't here!\n";
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
                    await GrainFactory.GetGrain<IPlayerGrain>(player.Key).Die();
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
                    await GrainFactory.GetGrain<IMonsterGrain>(this.GetPrimaryKey().ToString()+"Monster" + monster.Id).Kill(this.State.roomGrain);
                    string stateChangeResult = await setGameStateToValue("HasKilled" + monster.Id, true);
                    await State.WriteStateAsync();
                    return target + " is now dead." + stateChangeResult;
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
            return State.things.Where(x => x.Name.ToLower().Contains(name)).FirstOrDefault();
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
                await SetRoomGrain(room);
            }
            await State.WriteStateAsync();
        }

        async Task IPlayerGrain.AddMonster(IMonsterGrain monster)
        {
            this.State.monsterGrains.Add(monster);
            await State.WriteStateAsync();
        }

        async Task IPlayerGrain.AddNpc(INPCGrain npc)
        {
            this.State.npcGrains.Add(npc);
            await State.WriteStateAsync();
        }
        async Task IPlayerGrain.ClearGrainAndState()
        {
            foreach (var grain in this.State.monsterGrains)
                await grain.ClearGrainAndState();
            foreach (var grain in this.State.roomGrains)
                await grain.ClearGrainAndState();
            await this.State.gameState.ClearGrainAndState();
            await this.State.ClearStateAsync();
            this.DeactivateOnIdle();
        }

        async Task<string> IPlayerGrain.Play(string command)
        {
            if (command.ToLower() != "history")
                this.State.playerHistory.Add(command);
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
                        return await this.State.roomGrain.Description(State.myInfo, State.gameState);
                    case "version":
                        return "Player is version " + this.State.version;
                    case "look":
                        if (words.Length > 2)
                        {
                            string[] contextArray = { "in", "around", "at" };
                            if (contextArray.Contains(words[1]))
                            {
                                var target = command.Substring(verb.Length + words[1].Length + 2);
                                return await Inspect(target, verb);
                            }                            
                        }
                        return await this.State.roomGrain.Description(State.myInfo, State.gameState);
                    case "go":
                        if (words.Length == 1)
                            return "Go where?";
                        return await Go(words[1]);
                    case "north":
                    case "south":
                    case "east":
                    case "west":
                        return await Go(verb);
                    case "examine":
                    case "inspect":
                        {
                            if (words.Length == 1)
                                return verb + " what?";
                            var target = command.Substring(verb.Length + 1);
                            return await Inspect(target, verb);
                        }
                    case "speak":
                    case "talk":
                        {
                            if (words.Length == 1)
                                return "Who are you trying to talk to?";
                            if (words.Length == 2)
                                return "Huh?";
                            var target = command.Substring(verb.Length + words[1].Length + 2);
                            return await TalkTo(target, verb);
                        }
                    case "kill":
                        {
                            if (words.Length == 1)
                                return "Kill what?";
                            var target = command.Substring(verb.Length + 1);
                            try
                            {
                                return await Kill(target);
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceInformation("Kill error: " + ex.Message);
                                Trace.TraceInformation("Kill errorST: " + ex.StackTrace);
                                return "KillError: + " + ex.Message + "\nst\n" + ex.StackTrace;
                            }
                        }
                    case "drop":
                        thing = FindMyThing(Rest(words));
                        return await Drop(thing);
                    case "take":
                        thing = await State.roomGrain.FindThing(Rest(words));
                        return await Take(thing);
                    case "move":
                        {
                            thing = await State.roomGrain.FindThing(Rest(words));
                            return await Move(thing);
                        }
                    case "statechangeactions":
                        {
                            StringBuilder sb = new StringBuilder("StateChangeActions:\n");
                            foreach (var stateChangeAction in this.State.stateChangeActions)
                            {
                                sb.Append(stateChangeAction.Flag);
                                sb.Append(":value:");
                                sb.Append(stateChangeAction.ToValue);
                                sb.Append(":text:");
                                sb.AppendLine(stateChangeAction.PrintText);
                            }
                            return sb.ToString();
                        }
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
                    case "history":
                        string history = "HISTORY START:\n";
                        history += string.Join("\n", State.playerHistory);
                        history += "\nHISTORY END";
                        return history;
                    case "gamestatekey":
                        {
                            return State.gameState.GetPrimaryKey().ToString();
                        }
                    case "gamestate":
                        {
                            return await State.gameState.OutputGameState();
                        }
                    case "monstermash":
                        {
                            StringBuilder monsterSb = new StringBuilder();
                            monsterSb.AppendLine("Monsters");
                            foreach (var monster in this.State.monsterGrains)
                            {
                                monsterSb.AppendLine(await monster.GetPrintableInfo());
                            }
                            return monsterSb.ToString();
                        }
                    case "npcmash":
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine("NPCs");
                            foreach (var npc in this.State.npcGrains)
                            {                                
                                sb.AppendLine(await npc.GetPrintableInfo());
                            }
                            return sb.ToString();
                        }
                    case "roommash":
                        {
                            StringBuilder roomSb = new StringBuilder();
                            roomSb.AppendLine("Rooms");
                            foreach (var room in this.State.roomGrains)
                            {
                                roomSb.AppendLine(await room.GetPrintableInfo());
                            }
                            return roomSb.ToString();
                        }                        
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
            catch (Exception ex)
            {
                Trace.TraceError("Error: " + ex.Message + "\nStackTrace:\n" + ex.StackTrace);
                return "Error: " + ex.Message + "\nStackTrace:\n" + ex.StackTrace;
            }
            finally
            {
                this.previousCommand = verb;
            }
        }
        Task<string[]> IPlayerGrain.GetPlayerHistory()
        {
            return Task.FromResult(State.playerHistory.ToArray());
        }
        Task<IGameStateGrain> IPlayerGrain.GetGameState()
        {
            return Task.FromResult(State.gameState);
        }
        async Task IPlayerGrain.InitGameState(MapInfo mapInfo)
        {
            this.State.version = 1;          
            State.gameState = GrainFactory.GetGrain<IGameStateGrain>(this.GetPrimaryKey());
            await State.gameState.InitGameState(mapInfo);
            this.State.stateChangeActions = mapInfo.stateChangeActions;
            await State.WriteStateAsync();
        }

        Task IPlayerGrain.SetVersion(int newVersion)
        {
            this.State.version = newVersion;
            return TaskDone.Done;
        }
        Task<int> IPlayerGrain.Version()
        {
            return Task.FromResult(this.State.version);
        }
    }
}
