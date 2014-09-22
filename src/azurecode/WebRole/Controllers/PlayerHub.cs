using AdventureTerreInterfaces;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Web;

namespace AdventureTerreWebRole.Controllers
{
    public class PlayerHub : Hub
    {
        public void PlayerUpdate(ClientMessage message)
        {
            Clients.Group(message.Recipient).playerUpdate(message);
        }

        public void PlayerUpdates(ClientMessageBatch messages)
        {            
            Clients.Group(messages.Messages[0].Recipient).playerUpdates(messages);
        }

        public override System.Threading.Tasks.Task OnConnected()
        {
            if (Context.Headers.Get("ORLEANS") != "GRAIN")
            {
                // This connection does not have the GRAIN header, so it must be a browser
                // Therefore add this connection to the browser group
                Groups.Add(Context.ConnectionId, "BROWSERS");
                Cookie playerIdCookie;
                bool foundCookie = Context.RequestCookies.TryGetValue("playerid", out playerIdCookie);

                if (foundCookie)
                {
                    Groups.Add(Context.ConnectionId, playerIdCookie.Value);
                }
            }
            return base.OnConnected();
        }
    }
}