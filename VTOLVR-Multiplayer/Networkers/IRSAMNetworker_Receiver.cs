using Harmony;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class IRSAMNetworker_Reciever : MonoBehaviour
{
    public ulong networkUID;
    public ulong[] radarUIDS;
    private Message_SamUpdate lastMessage;
    private SAMLauncher samLauncher;
    private RadarLockData lastData;
    private Actor lastActor;
    private void Awake()
    {
        samLauncher = GetComponentInChildren<SAMLauncher>();
        Networker.SAMUpdate += SamUpdate;
        samLauncher.LoadAllMissiles();
        if (samLauncher.lockingRadars == null)
        {

            List<LockingRadar> lockingRadars = new List<LockingRadar>();
            Actor lastActor;
            foreach (var uID in radarUIDS)
            {
                Debug.Log($"Try adding uID {uID} to SAM's radars.");
                if (VTOLVR_Multiplayer.AIDictionaries.allActors.TryGetValue(uID, out lastActor))
                {
                    Debug.Log("Got the actor.");
                    foreach (var radar in lastActor.gameObject.GetComponentsInChildren<LockingRadar>())
                    {
                        lockingRadars.Add(radar);
                        Debug.Log("Added radar to a sam launcher!");
                    }
                }
                else
                {
                    Debug.LogError($"Could not resolve actor from uID {uID}.");
                }
            }
            samLauncher.lockingRadars = lockingRadars.ToArray();
        }
    }
    private void SamUpdate(Message message)
    {
        if (samLauncher.lockingRadars == null)
        {

            List<LockingRadar> lockingRadars = new List<LockingRadar>();
            Actor lastActor;
            foreach (var uID in radarUIDS)
            {
                Debug.Log($"Try adding uID {uID} to SAM's radars.");
                if (VTOLVR_Multiplayer.AIDictionaries.allActors.TryGetValue(uID, out lastActor))
                {
                    Debug.Log("Got the actor.");
                    foreach (var radar in lastActor.gameObject.GetComponentsInChildren<LockingRadar>())
                    {
                        lockingRadars.Add(radar);
                        Debug.Log("Added radar to a sam launcher!");
                    }
                }
                else
                {
                    Debug.LogError($"Could not resolve actor from uID {uID}.");
                }
            }
            samLauncher.lockingRadars = lockingRadars.ToArray();
        }
        lastMessage = (Message_SamUpdate)message;
        if (lastMessage.senderUID != networkUID)
            return;
        Debug.Log("Got a sam update message.");
        if (VTOLVR_Multiplayer.AIDictionaries.allActors.TryGetValue(lastMessage.actorUID, out lastActor))
        {
            foreach (var radar in samLauncher.lockingRadars)
            {
                Debug.Log("Found a suitable radar for this sam.");
                radar.ForceLock(lastActor, out lastData);
                if (lastData.locked)
                {
                    Debug.Log("Beginning sam launch routine for reciever.");
                    int j = 0;
                    Missile[] missiles = (Missile[])Traverse.Create(samLauncher).Field("missiles").GetValue();
                    for (int i = 0; i < missiles.Length; i++)
                    {
                        if (missiles[i] != null)
                        {
                            Debug.Log("Found a suitable missile to attach a reciever to.");
                            MissileNetworker_Receiver missileReciever = missiles[i].gameObject.AddComponent<MissileNetworker_Receiver>();
                            missileReciever.networkUID = lastMessage.missileUID;
                            Debug.Log($"Made new missile receiver with uID {missileReciever.networkUID}");
                            break;
                        }
                    }
                    Debug.Log("Firing sam.");
                    samLauncher.FireMissile(lastData);
                    /*Missile missile = (Missile)Traverse.Create(samLauncher).Field("firedMissile").GetValue();
                    MissileNetworker_Receiver reciever = missile.gameObject.AddComponent<MissileNetworker_Receiver>();
                    reciever.networkUID = lastMessage.missileUID;*/
                    return;
                }
                else
                {
                    Debug.Log("Couldn't force a lock, trying with another radar.");
                }
            }
        }
        else
        {
            Debug.Log($"Could not resolve lock for sam {networkUID}.");
        }
    }

    public void OnDestroy()
    {
        Networker.SAMUpdate -= SamUpdate;
        Debug.Log("Destroyed SamUpdate");
        Debug.Log(gameObject.name);
    }
}
