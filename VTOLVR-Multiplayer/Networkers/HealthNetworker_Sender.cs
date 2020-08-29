using UnityEngine;

class HealthNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_Death lastMessage;
    public Health health;
    public bool immediateFlag;

    private Message_BulletHit bulletMessage;
    private void Awake()
    {
        lastMessage = new Message_Death(networkUID,false);

        health = GetComponent<Health>();
        if (health == null)
            Debug.LogError("health was null on vehicle " + gameObject.name);
        else
            health.OnDeath.AddListener(Death);
            Debug.LogError("found health on " + gameObject.name);

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
        Hitbox hitbox = null;
        if (flag)
        {

            hitbox = hitInfo.collider.GetComponent<Hitbox>();
            if ((bool)hitbox && (bool)hitbox.actor)
            {

                Debug.Log("found  target bullet hit");
                hitbox.Damage(bulletMessage.damage, hitInfo.point, Health.DamageTypes.Impact, hitbox.actor, "lol");
                BulletHitManager.instance.CreateBulletHit(hitInfo.point, -vel, true);


            }
        }
    }
    void Death()
    {
        lastMessage.UID = networkUID;
        lastMessage.immediate = immediateFlag;
        if (Networker.isHost)
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
        else
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliableNoDelay);
    }
}
