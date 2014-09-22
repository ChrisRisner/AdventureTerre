using AdventureTerreInterfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Script.Serialization;

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
                    var roomGr = await MakeRoom(room);
                    if (room.Id >= 0)
                        await player.AddRoom(roomGr);
                }
                await player.SetupRooms();

                foreach (var thing in data.Things)
                {
                    await MakeThing(player, thing);
                }
                
                foreach (var monster in data.Monsters)
                {
                    await MakeMonster(monster, player);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error with setup: " + ex.Message);
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

        private async Task<IRoomGrain> MakeRoom(RoomInfo data)
        {
            IRoomGrain roomGrain = RoomGrainFactory.GetGrain(Guid.NewGuid());
            await roomGrain.SetInfo(data);
            return roomGrain;
        }

        private async Task MakeThing(IPlayerGrain player, Thing thing)
        {
            IRoomGrain roomGrain = await player.GetRoomGrainByRoomId(thing.FoundIn);
            await roomGrain.Drop(thing);
        }

        private async Task MakeMonster(MonsterInfo data, IPlayerGrain player)
        {
            //var monsterGrain = MonsterGrainFactory.GetGrain(data.Id);
            var monsterGrain = MonsterGrainFactory.GetGrain(Guid.NewGuid());
            var room = await player.GetRandomRoom();
            await monsterGrain.SetInfo(data);
            await monsterGrain.SetRoomGrain(room);            
            await monsterGrain.SetPlayerGuid(await player.GetPlayerGuid());                       
        }
    }
}