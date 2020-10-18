using UnityEngine;
using VTOLVR_Multiplayer;
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
        lastMessage = new Message_Death(networkUID, false, "empty");
        ownerActor = GetComponentInParent<Actor>();
        health = ownerActor.health;

        if (health == null)
            Debug.LogError("health was null on vehicle " + gameObject.name);
        else
            health.OnDeath.AddListener(Death);
        Debug.LogError("found health on " + gameObject.name);

        ownerActor.hideDeathLog = true;
        Networker.BulletHit += BulletHit;

    }

    public void BulletHit(Packet packet)
    {
        bulletMessage = (Message_BulletHit)((PacketSingle)packet).message;



        if (bulletMessage.destUID != networkUID)
            return;


        RaycastHit hitInfo;
        Vector3 pos = VTMapManager.GlobalToWorldPoint(bulletMessage.pos);
        Vector3 vel = bulletMessage.dir.toVector3;
        Vector3 a = pos;
        a += vel * 100.0f;
        Actor source = null;
        if (AIDictionaries.allActors.ContainsKey(bulletMessage.sourceActorUID))
        {
            source = AIDictionaries.allActors[bulletMessage.sourceActorUID];
        }
        if (bulletMessage.sourceActorUID == networkUID)
            return;
        bool storage = health.invincible;
        health.invincible = false;
        health.Damage(bulletMessage.damage * 1.0f, pos, Health.DamageTypes.Impact, source, "Bullet Impact");
        BulletHitManager.instance.CreateBulletHit(pos, -vel, true);
        health.invincible = storage;

    }
    public void Death()
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

        if (selfName.Length < 2)
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
