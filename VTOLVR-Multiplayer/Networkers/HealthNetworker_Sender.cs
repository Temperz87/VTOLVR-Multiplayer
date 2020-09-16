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
        lastMessage = new Message_Death(networkUID, false,"empty");

        health = GetComponent<Health>();

        if (health == null)
            Debug.LogError("health was null on vehicle " + gameObject.name);
        else
            health.OnDeath.AddListener(Death);
        Debug.LogError("found health on " + gameObject.name);
        ownerActor = GetComponentInParent<Actor>();
        ownerActor.hideDeathLog = true;
        Networker.BulletHit += this.BulletHit;
    }
    public void BulletHit(Packet packet)
    {
        bulletMessage = (Message_BulletHit)((PacketSingle)packet).message;

        Debug.Log("handling bullet hit");

        if (bulletMessage.destUID != networkUID)
            return;


        RaycastHit hitInfo;
        Vector3 pos = VTMapManager.GlobalToWorldPoint(bulletMessage.pos);
        Vector3 vel = bulletMessage.dir.toVector3;
        Vector3 a = pos;
        a += vel * 100.0f;

        bool flag = Physics.Linecast(pos, a, out hitInfo, 1025);
        Actor source = null;
        if (AIDictionaries.allActors.ContainsKey(bulletMessage.sourceActorUID))
        {
            source = AIDictionaries.allActors[bulletMessage.sourceActorUID];
        }
        Hitbox hitbox = null;
        if (flag)
        {

            hitbox = hitInfo.collider.GetComponent<Hitbox>();
            if ((bool)hitbox && (bool)hitbox.actor)
            {

                Debug.Log("found  target bullet hit");
                hitbox.Damage(bulletMessage.damage, hitInfo.point, Health.DamageTypes.Impact, source, "Bullet Impact");
                BulletHitManager.instance.CreateBulletHit(base.transform.position, -vel, true);


            }
        }
        else
        {
            health.Damage(bulletMessage.damage, hitInfo.point, Health.DamageTypes.Impact, source, "Bullet Impact");
            BulletHitManager.instance.CreateBulletHit(hitInfo.point, -vel, true);
        }
    }
    void Death()
    {
        lastMessage.UID = networkUID;
        lastMessage.immediate = immediateFlag;

        string killerName = "themselves";

        if (health.killedByActor != null)
        {
            killerName = health.killedByActor.name;

            string killerNameImproved = PlayerManager.GetPlayerNameFromActor(health.killedByActor);

            if (killerNameImproved.Length > 2)
            {
                killerName = killerNameImproved;
            }
        }

        FlightLogger.Log(PlayerManager.GetPlayerNameFromActor(ownerActor) + " was killed by " + killerName + " with a " + health.killMessage);
        lastMessage.message = PlayerManager.GetPlayerNameFromActor(ownerActor) + " was killed by " + killerName + " with a " + health.killMessage;

        if (Networker.isHost)
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
        else
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendReliable);
    }
}
