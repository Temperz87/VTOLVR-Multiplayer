using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;

namespace VTOLVR_Multiplayer
{
    public class Networker : MonoBehaviour
    {
        private void Update() { }
        private void ReadP2P()
        {
            uint num;
            while (SteamNetworking.IsP2PPacketAvailable(out num))
            {
                ReadP2PPacket(num);
            }
        }

        private void ReadP2PPacket(uint num)
        {
            byte[] array = new byte[num];
            uint num2;
            CSteamID csteamID;
            if (SteamNetworking.ReadP2PPacket(array, num, out num2, out csteamID, 0))
            {

            }
        }
    }
}
