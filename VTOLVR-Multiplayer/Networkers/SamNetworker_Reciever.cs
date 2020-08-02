using Harmony;
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
        samLauncher.LoadAllMissiles();
        foreach (var missile in (Missile[])Traverse.Create(samLauncher).Field("missiles").GetValue())
        {
            
        }
    }
    private void SamUpdate(Packet packet)
    {
        lastMessage = (Message_SamUpdate)((PacketSingle)packet).message;
        if (lastMessage.senderUID != networkUID)
            return;
        Debug.Log("Got a sam update message.");
        if (VTOLVR_Multiplayer.AIDictionaries.allActors.TryGetValue(lastMessage.actorUID, out lastActor))
        {
            foreach (var radar in samLauncher.lockingRadars)
            {
                radar.ForceLock(lastActor, out lastData);
                if (lastData.locked)
                {
                    samLauncher.FireMissile(lastData);
                    Missile missile = (Missile)Traverse.Create(samLauncher).Field("firedMissile").GetValue();
                    MissileNetworker_Receiver reciever = missile.gameObject.AddComponent<MissileNetworker_Receiver>();
                    reciever.networkUID = lastMessage.missileUID;
                    Debug.Log($"Made new missile receiver with uID {reciever.networkUID}");
                    return;
                }
            }
        }
    }
}
