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
        lastMessage = new Message_Death(networkUID,false);
        Networker.Death += Death;

        health = GetComponent<Health>();
        health.invincible = true;
        Networker.BulletHit += this.BulletHit;
    }

    public void Death(Message message)
    {
        lastMessage = (Message_Death)message;
        if (lastMessage.UID != networkUID)
            return;

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

    public void BulletHit(Message message)
    {
        bulletMessage = (Message_BulletHit)message;

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

        VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.Remove(GetComponent<Actor>());
        UnitIconManager.instance.UnregisterIcon(GetComponent<Actor>());
    }
}
