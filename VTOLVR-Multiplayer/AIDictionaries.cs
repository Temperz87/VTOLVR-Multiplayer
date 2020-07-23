using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTOLVR_Multiplayer
{
    public static class AIDictionaries
    {
        public static Dictionary<ulong, Actor> allActors = new Dictionary<ulong, Actor>();
        public static Dictionary<Actor, ulong> reverseAllActors = new Dictionary<Actor, ulong>();
        public static Dictionary<ulong, AIManager.AI> allAIByNetworkId = new Dictionary<ulong, AIManager.AI>();
        public static Dictionary<ulong, PlayerManager.Player> allPlayersByNetworkId = new Dictionary<ulong, PlayerManager.Player>();
    }
}
