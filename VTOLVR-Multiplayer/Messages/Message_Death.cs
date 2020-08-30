using System;
[Serializable]
public class Message_Death : Message
{
    public ulong UID;
    public bool immediate;

    public Message_Death(ulong uID,bool Immediate)
    {
        UID = uID;
        this.immediate = Immediate;
        type = MessageType.Death;
    }
}
[Serializable]
public class Message_BulletHit : Message
{
    public ulong UID;
    public ulong destUID;
    public Vector3D pos;
    public Vector3D dir;
    public float damage;
    public Message_BulletHit(ulong uID, ulong dest, Vector3D apos, Vector3D adir, float adam)
    {
        this.UID = uID;
        this.destUID = dest;
        this.pos = apos;
        this.dir = adir;
        this.damage = adam;

        type = MessageType.BulletHit;
    }
}
