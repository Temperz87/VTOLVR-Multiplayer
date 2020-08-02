using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class SamNetworker_Reciever : MonoBehaviour
{
    public ulong networkUID;
    private Message_SamUpdate lastMessage;
    private SAMLauncher samLauncher;
    private RadarLockData lastData;
    private Actor lastActor;
    private void Awake()
    {
        samLauncher = GetComponentInChildren<SAMLauncher>();
        Networker.SAMUpdate += SamUpdate;
    }
    private void SamUpdate(Packet packet)
    {
        lastMessage = (Message_SamUpdate)((PacketSingle)packet).message;
        if (lastMessage.senderUID != networkUID)
            return;

        if (VTOLVR_Multiplayer.AIDictionaries.allActors.TryGetValue(lastMessage.actorUID, out lastActor))
        {
            foreach (var radar in samLauncher.lockingRadars)
            {
                radar.ForceLock(lastActor, out lastData);
                if (lastData.locked)
                {
                    samLauncher.FireMissile(lastData);
                    return;
                }
            }
        }
    }
}
