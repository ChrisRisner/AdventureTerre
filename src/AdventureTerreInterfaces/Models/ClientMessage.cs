using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdventureTerreInterfaces.Models
{
    public class ClientMessage
    {
        public string Message { get; set; }
        public string Recipient { get; set; }
    }

    public class ClientMessageBatch
    {
        public ClientMessage[] Messages;
    }
}
