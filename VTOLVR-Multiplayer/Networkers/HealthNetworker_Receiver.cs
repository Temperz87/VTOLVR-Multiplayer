using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class HealthNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;
    private Message_Death lastMessage;
    public Health health;
   

    private void Awake()
    {
        lastMessage = new Message_Death(networkUID);
        Networker.Death += Death;

        health = GetComponent<Health>();
    }

    public void Death(Packet packet)
    {
        lastMessage = (Message_Death)((PacketSingle)packet).message;
        if (lastMessage.UID != networkUID)
            return;

        Actor actor = GetComponent<Actor>();
        if (actor == null)
        {
            Debug.Log("actor was null");
        }
        else {
            if (actor.unitSpawn != null)
            {
                if (actor.unitSpawn.unitSpawner == null)
                {
                    Debug.Log("unit spawner was null, adding one");
                    actor.unitSpawn.unitSpawner = actor.gameObject.AddComponent<UnitSpawner>();
                }
            }
        }

        Debug.Log("Killing AI on clients");
        health.invincible = false;
        health.Kill();
    }

    public void OnDestroy()
    {
        Networker.Death -= Death;
        Debug.Log("Destroyed DeathUpdate");
        Debug.Log(gameObject.name);
    }
}
