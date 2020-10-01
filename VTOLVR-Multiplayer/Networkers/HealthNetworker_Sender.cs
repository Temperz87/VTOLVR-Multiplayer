using UnityEngine;
using VTOLVR_Multiplayer;
using Harmony;
class HealthNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_Death lastMessage;
    public Health health;
    public bool immediateFlag;
    public Actor ownerActor;
    private Message_BulletHit bulletMessage;
  
    private void Awake()
    {
        lastMessage = new Message_Death(networkUID, false,"empty");
        ownerActor = GetComponentInParent<Actor>();
        health = ownerActor.health;
      
        if (health == null)
            Debug.LogError("health was null on vehicle " + gameObject.name);
        else
            health.OnDeath.AddListener(Death);
        Debug.LogError("found health on " + gameObject.name);
       
        ownerActor.hideDeathLog = true;
        Networker.BulletHit += this.BulletHit;
    }
    public void BulletHit(Packet packet)
    {
        bulletMessage = (Message_BulletHit)((PacketSingle)packet).message;

        Debug.Log("handling bullet hit");

        if (bulletMessage.destUID != networkUID)
            return;


        Collider[] hitInfo;
        Vector3 pos =  ownerActor.gameObject.transform.position + bulletMessage.pos; 
        

        hitInfo = Physics.OverlapSphere(pos, 5.0f, 1025);


        Actor source = null;
        if (AIDictionaries.allActors.ContainsKey(bulletMessage.sourceActorUID))
        {
            source = AIDictionaries.allActors[bulletMessage.sourceActorUID];
        }


        int hits = 0;
        foreach(Collider col in hitInfo)
        {
            Hitbox hitbox = null;
            hitbox =  col.GetComponent<Hitbox>();
            if ((bool)hitbox && (bool)hitbox.actor)
            {
                Debug.Log("found  target bullet hit");
                hitbox.Damage(bulletMessage.damage * 2.0f, pos, Health.DamageTypes.Impact, source, "Bullet Impact");
                BulletHitManager.instance.CreateBulletHit(pos, new Vector3(0.0f,1.0f,0.0f), true);
                hits++;
            }
        }

        if(hits == 0)
        health.Damage(bulletMessage.damage * 2.0f, ownerActor.gameObject.transform.position, Health.DamageTypes.Impact, source, "Bullet Impact");
        
         
    }
    void Death()
    {
        lastMessage.UID = networkUID;
        lastMessage.immediate = immediateFlag;

        string killerName = "themselves";
        Actor killer = null;

        
        if (health.killedByActor != null)
        {
            killerName = health.killedByActor.name;

            string killerNameImproved = PlayerManager.GetPlayerNameFromActor(health.killedByActor);

            if (killerNameImproved.Length > 2)
            {
                killerName = killerNameImproved;
            }
        }
        string selfName = PlayerManager.GetPlayerNameFromActor(ownerActor);

        if(selfName.Length<2)
        {
            selfName = ownerActor.name;
        }
        FlightLogger.Log(selfName + " was killed by " + killerName + " with a " + health.killMessage);
        lastMessage.message = selfName + " was killed by " + killerName + " with a " + health.killMessage;

        if (Networker.isHost)
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
        else
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
    }
}
