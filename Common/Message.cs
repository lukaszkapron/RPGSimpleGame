using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class Message
    {
        public string Type { get; set; } // "Connect", "Action", "Log", "Disconnect", "PlayerDisconnected"
        public string PlayerName { get; set; }
        public string Class { get; set; }
        public string Action { get; set; } // "attack", "heal", "move"
        public int Value { get; set; }
        public string TargetServer { get; set; } // Do komunikacji między serwerami
        public string PlayerDataSerialized { get; set; }
        public string Log { get; set; } // System message
        public string CurrentServer { get; set; } // Serwer gracza
    }
}
