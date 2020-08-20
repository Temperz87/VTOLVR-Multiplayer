using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class HealthNetworker_ReceiverHostEnforced : MonoBehaviour
{
    public ulong networkUID;
    private Message_Death lastMessage;
    public Health health;

    private Message_BulletHit bulletMessage;
    private void Awake()
    {
        lastMessage = new Message_Death(networkUID,false);
        Networker.Death += Death;

        Networker.BulletHit += this.BulletHit;
        health = GetComponent<Health>();
        //health.invincible = true;
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
        else
        {
            if (actor.unitSpawn != null)
            {
                if (actor.unitSpawn.unitSpawner == null)
                {
                    Debug.Log("unit spawner was null, adding one");
                    actor.unitSpawn.unitSpawner = actor.gameObject.AddComponent<UnitSpawner>();
                }
            }
        }

        health.invincible = false;
        health.Kill();
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
        a += vel * 0.2f;

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
    public void OnDestroy()
    {
        Networker.Death -= Death;
        Debug.Log("Destroyed DeathUpdate");
        Debug.Log(gameObject.name);
    }
}