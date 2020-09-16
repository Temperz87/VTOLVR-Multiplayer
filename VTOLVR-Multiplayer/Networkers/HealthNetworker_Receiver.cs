using Oculus.Platform.Samples.VrHoops;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class HealthNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;
    private Message_Death lastMessage;
    public Health health;

    private Message_BulletHit bulletMessage;
    private void Awake()
    {
        lastMessage = new Message_Death(networkUID, false,"empty");
        Networker.Death += Death; 
        health = GetComponent<Health>();
        health.invincible = true;
    }

    public void Death(Packet packet)
    {
        lastMessage = (Message_Death)((PacketSingle)packet).message;
        if (lastMessage.UID != networkUID)
            return;
        
        // int player = PlayerManager.GetPlayerIDFromCSteamID(new Steamworks.CSteamID(PlayerManager.localUID));

        string name = Steamworks.SteamFriends.GetPersonaName();

        if (lastMessage.message.Contains(name))
        {
            PlayerManager.kills++;
            FlightLogger.Log("You got " + PlayerManager.kills + " Kill(s)");
        }

        FlightLogger.Log(lastMessage.message);

        if (lastMessage.immediate)
        {
            Destroy(gameObject);
        }
        else
        {
            health.invincible = false;
            health.Kill();
        }

    }
     

    public void OnDestroy()
    {
        Networker.Death -= Death;
        Debug.Log("Destroyed DeathUpdate");
        Debug.Log(gameObject.name);

        VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.Remove(GetComponent<Actor>());
        UnitIconManager.instance.UnregisterIcon(GetComponent<Actor>());
    }
}
