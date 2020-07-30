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
        public static Dictionary<ulong, UnitSpawner> objectiveSpawners = new Dictionary<ulong, UnitSpawner>();
        public static Dictionary<UnitSpawner, ulong> reverseObjectiveSpawners = new Dictionary<UnitSpawner, ulong>();
    }
}
