using AdventureTerreInterfaces;
using Orleans;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Script.Serialization;

using AdventureTerreInterfaces.Models;
using AdventureTerreInterfaces.GrainInterfaces;

namespace AdventureTerreWebRole
{
    public class AdventureSetup
    {
        string mapFileName = "~/AdventureMap.json";

        public async Task setup(IPlayerGrain player)
        {
            try
            {
                mapFileName = HttpContext.Current.Server.MapPath(mapFileName);
                if (!File.Exists(mapFileName))
                {
                    Trace.WriteLine("Unable to load map file: " + mapFileName, "Error");
                    throw new Exception("Unable to load map file: " + mapFileName);
                    //return;
                }

                var rand = new Random();

                var bytes = File.ReadAllText(mapFileName);
                JavaScriptSerializer deserializer = new JavaScriptSerializer();
                var data = deserializer.Deserialize<MapInfo>(bytes);                
                //var rooms = new List<IRoomGrain>();
                foreach (var room in data.Rooms)
                {
                    var roomGr = await MakeRoom(room, player.GetPrimaryKey());
                    if (room.Id >= 0)
                        await player.AddRoom(roomGr);
                }
                //await player.SetupRooms();

                foreach (var thing in data.Things)
                {
                    await MakeThing(player, thing);
                }                
                
                foreach (var monster in data.Monsters)
                {
                    await MakeMonster(monster, player);
                }

                foreach (var npc in data.NPCs)
                {
                    await MakeNpc(npc, player);
                }

                var thingIds = data.Things.Select(t => t.Id).ToArray();
                var monsterIds = data.Monsters.Select(m => m.Id).ToArray();
                var npcIds = data.NPCs.Select(n => n.Id).ToArray();
                //await player.InitGameState(thingIds, monsterIds, npcIds);
                await player.InitGameState(data);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error with setup: " + ex.Message);
                Trace.TraceError("Error with setup STACK TRACE: " + ex.StackTrace);
            }
        }

        public string GetFileContents()
        {

            if (!File.Exists(mapFileName))
            {

                mapFileName = HttpContext.Current.Server.MapPath(mapFileName);
                if (!File.Exists(mapFileName))
                {
                    return "*** File not found: " + mapFileName;
                }
            }

            var bytes = File.ReadAllText(mapFileName);
            return bytes;
        }

        private async Task<IRoomGrain> MakeRoom(RoomInfo data, Guid playerGuid)
        {
            //TEST: GrainChange
            //IRoomGrain roomGrain = RoomGrainFactory.GetGrain(Guid.NewGuid());
            //IRoomGrain roomGrain = GrainFactory.GetGrain<IRoomGrain>(playerGuid.ToString() + "Room" + data.Id);
            IRoomGrain roomGrain = GrainFactory.GetGrain<IRoomGrain>(playerGuid.ToString() + "Room" + data.Id);
            await roomGrain.SetInfo(data, playerGuid);
            return roomGrain;
        }

        private async Task MakeThing(IPlayerGrain player, Thing thing)
        {
            //IRoomGrain roomGrain = await player.GetRoomGrainByRoomId(thing.FoundIn);
            IRoomGrain roomGrain = GrainFactory.GetGrain<IRoomGrain>(player.GetPrimaryKey().ToString() + "Room" + thing.FoundIn);
            await roomGrain.Drop(thing);
        }

        private async Task MakeMonster(MonsterInfo data, IPlayerGrain player)
        {
            //var monsterGrain = MonsterGrainFactory.GetGrain(data.Id);
            //var monsterGrain = MonsterGrainFactory.GetGrain(Guid.NewGuid());
            var monsterGrain = GrainFactory.GetGrain<IMonsterGrain>(player.GetPrimaryKey().ToString() + "Monster" + data.Id);
            //var room = await player.GetRandomRoom();
            await monsterGrain.SetInfo(data, player);
            IRoomGrain room = GrainFactory.GetGrain<IRoomGrain>(player.GetPrimaryKey().ToString() + "Room" + data.StartIn);
            await monsterGrain.SetRoomGrain(room);            
            await monsterGrain.SetPlayerGuid(await player.GetPlayerGuid());
            await player.AddMonster(monsterGrain);
        }

        private async Task MakeNpc(NPCInfo data, IPlayerGrain player)
        {
            var npcGrain = GrainFactory.GetGrain<INPCGrain>(player.GetPrimaryKey().ToString() + "Npc" + data.Id);
            await npcGrain.SetInfo(data, player);
            IRoomGrain room = GrainFactory.GetGrain<IRoomGrain>(player.GetPrimaryKey().ToString() + "Room" + data.StartIn);
            await npcGrain.SetRoomGrain(room);
            await npcGrain.SetPlayerGuid(await player.GetPlayerGuid());
            await player.AddNpc(npcGrain);
        }
    }
}