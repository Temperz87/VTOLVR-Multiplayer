using System;
[Serializable]
public class Message_Death : Message
{
    public ulong UID;
    public bool immediate;
    public string message;
    public Message_Death(ulong uID, bool Immediate, string msg)
    {
        UID = uID;
        immediate = Immediate;
        message = msg;
        type = MessageType.Death;
    }
}
[Serializable]
public class Message_BulletHit : Message
{
    public ulong UID;
    public ulong destUID;
    public ulong sourceActorUID;
    public Vector3D pos;
    public Vector3D dir;
    public float damage;
    public Message_BulletHit(ulong uID, ulong dest, ulong sourceActor, Vector3D apos, Vector3D adir, float adam)
    {
        UID = uID;
        destUID = dest;
        pos = apos;
        dir = adir;
        damage = adam;
        sourceActorUID = sourceActor;
        type = MessageType.BulletHit;
    }
}
