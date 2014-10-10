using AdventureTerreInterfaces;
using Microsoft.AspNet.SignalR;
using Orleans;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

using AdventureTerreInterfaces.Models;
using AdventureTerreInterfaces.GrainInterfaces;

namespace AdventureTerreWebRole.Controllers
{
    public class GameOneController : ApiController
    {
        public async Task<string> Get(string id)
        {
            Trace.TraceInformation("Game Command Received: " + id);            
            string playerId = "";
            string playerName = "";
            Guid playerGuid = Guid.NewGuid();
            CookieHeaderValue cookie = Request.Headers.GetCookies("playerid").FirstOrDefault();
            if (cookie != null)
            {
                playerId = cookie["playerid"].Value;
                playerGuid = Guid.Parse(playerId);
            }
            else
            {
                HttpCookie cook = new HttpCookie("playerid", playerGuid.ToString());
                HttpContext.Current.Response.Cookies.Add(cook);
            }

            //var player = PlayerGrainFactory.GetGrain(playerGuid);
            //var player = GrainFactory.GetGrain<IPlayerGrain>(playerGuid.ToString());
            var player = GrainFactory.GetGrain<IPlayerGrain>(playerGuid);
            playerName = await player.Name();
            if (id == "begingame" && playerName == Constants.kPlayerSetupName)
            {
                //Kick off setup process
                AdventureSetup setup = new AdventureSetup();
                await setup.setup(player);
                await player.SetInfoGuid(playerGuid);
                return "What is your name?";
            }
            else if (id == "begingame")
            {
                playerName = await player.Name();
                return "Welcome back, " + playerName;
            }
            if (id == "hubtest")
            {
                //PlayerHub hub = new PlayerHub();
                var context = GlobalHost.ConnectionManager.GetHubContext<PlayerHub>();
                context.Clients.Group("BROWSERS").playerUpdate("This has been a hubtest!");
                //hub.PlayerUpdate("BLAH!");
                return "Hub should be tested";
            }
            if (id == "hubtest3")
            {
                //PlayerHub hub = new PlayerHub();
                var context = GlobalHost.ConnectionManager.GetHubContext<PlayerHub>();
                List<ClientMessage> messages = new List<ClientMessage>();
                messages.Add(new ClientMessage { Message = "Testone" });
                messages.Add(new ClientMessage { Message = "Testtwo" });
                context.Clients.Group("BROWSERS").objectTest(new ClientMessageBatch { Messages = messages.ToArray() });
                //hub.PlayerUpdate("BLAH!");
                return "Hub should be tested with ClientMessages";
            }
            
            if (playerName == Constants.kPlayerSetupName)
            {
                playerName = id;
                await player.SetName(playerName);
                
                string response = "Welcome, " + playerName + "\n";
                response += await player.Play("start");
                return response;
            }

            string result = await player.Play(id);

            if (result.StartsWith("COMMAND::"))
            {
                switch (result)
                {
                    case "COMMAND::RESTART":
                        await player.ClearGrainAndState();

                        AdventureSetup setup = new AdventureSetup();
                        await setup.setup(player);
                        await player.SetInfoGuid(playerGuid);
                        return "What is your name? (restart)";
                }
            }
            Trace.TraceInformation("GameCommand Result: " + result);
            return result;
        }
    }
}
