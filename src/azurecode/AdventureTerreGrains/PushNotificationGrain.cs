using AdventureTerreInterfaces;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.WindowsAzure.ServiceRuntime;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdventureTerreGrains
{
    [Reentrant]
    public class PushNotifierGrain : Orleans.Grain, IPushNotifierGrain
    {
        Dictionary<string, Tuple<HubConnection, IHubProxy>> hubs = new Dictionary<string, Tuple<HubConnection, IHubProxy>>();
        List<ClientMessage> messageQueue = new List<ClientMessage>();

        public override async Task ActivateAsync()
        {
            // set up a timer to regularly flush the message queue
            this.RegisterTimer(FlushQueue, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));

            if (RoleEnvironment.IsAvailable)
            {
                // in azure
                await RefreshHubs(null);
                // set up a timer to regularly refresh the hubs, to respond to azure infrastructure changes
                this.RegisterTimer(RefreshHubs, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
            }
            else
            {
                Trace.TraceError("PushNotGrain Issue, RoleEnvironment not available");
                // not in azure, the SignalR hub is running locally
                await AddHub("http://localhost:48777/");
            }

            await base.ActivateAsync();
        }

        Task IPushNotifierGrain.ClearGrainAndState()
        {
            //await this.State.ClearStateAsync();
            this.DeactivateOnIdle();
            return TaskDone.Done;
        }

        private async Task RefreshHubs(object _)
        {
            var addresses = new List<string>();
            var tasks = new List<Task>();

            // discover the current infrastructure
            foreach (var instance in RoleEnvironment.Roles["AdventureTerreWebRole"].Instances)
            {
                var endpoint = instance.InstanceEndpoints["InternalSignalR"];
                addresses.Add(string.Format("http://{0}", endpoint.IPEndpoint.ToString()));
            }
            var newHubs = addresses.Where(x => !hubs.Keys.Contains(x)).ToArray();
            var deadHubs = hubs.Keys.Where(x => !addresses.Contains(x)).ToArray();

            // remove dead hubs
            foreach (var hub in deadHubs)
            {
                hubs.Remove(hub);
            }

            // add new hubs
            foreach (var hub in newHubs)
            {
                tasks.Add(AddHub(hub));
            }

            await Task.WhenAll(tasks);
        }

        private Task FlushQueue(object _)
        {
            this.Flush();
            return TaskDone.Done;
        }

        private async Task AddHub(string address)
        {
            // create a connection to a hub
            var hubConnection = new HubConnection(address);
            hubConnection.Headers.Add("ORLEANS", "GRAIN");
            var hub = hubConnection.CreateHubProxy("PlayerHub");            
            await hubConnection.Start();
            hubs.Add(address, new Tuple<HubConnection, IHubProxy>(hubConnection, hub));
        }

        public Task SendMessage(string message)
        {
            return SendMessage(message, "BROWSERS");
        }
        public Task SendMessage(string message, string recipient)
        {
            // add a message to the send queue
            //messageQueue.Add(message);
            messageQueue.Add(new ClientMessage { Message = message, Recipient = recipient });
            if (messageQueue.Count > 25)
            {
                // if the queue size is greater than 25, flush the queue
                Flush();
            }
            return TaskDone.Done;
        }

        private void Flush()
        {
            if (messageQueue.Count == 0) return;

            // send all messages to all SignalR hubs
            var messagesToSend = messageQueue.ToArray();
            messageQueue.Clear();

            var promises = new List<Task>();
            foreach (var hub in hubs.Values)
            {
                try
                {
                    if (hub.Item1.State == ConnectionState.Connected)
                    {
                        //hub.Item2.Invoke("PlayerUpdate", "Hi there!" );
                        //hub.Item2.Invoke("PlayerTest1", "test");
                        //hub.Item2.Invoke("PlayerTest1", messagesToSend[0].Message);
                        //hub.Item2.Invoke("ObjectTest", new ClientMessageBatch { Messages = messagesToSend });
                        //hub.Item2.Invoke("PlayerUpdates", new { Messages = messagesToSend });
                        hub.Item2.Invoke("PlayerUpdates", new ClientMessageBatch { Messages = messagesToSend });
                    }
                    else
                    {
                        hub.Item1.Start();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

        }
    }
}
