using System;
using UnityEngine;

[Serializable]
public class Message_Death : Message
{
    public ulong UID;
    public bool immediate;
    public string message;
    public Message_Death(ulong uID, bool Immediate,string msg)
    {
        UID = uID;
        this.immediate = Immediate;
        this.message = msg;
        type = MessageType.Death;
    }
}
[Serializable]
public class Message_BulletHit : Message
{
    public ulong UID;
    public ulong destUID;
    public ulong sourceActorUID;
    public SerializableVector3 pos;
     
    public float damage;
    public Message_BulletHit(ulong uID, ulong dest, ulong sourceActor, Vector3 apos , float adam)
    {
        this.UID = uID;
        this.destUID = dest;
        this.pos = apos;
       
        this.damage = adam;
        this.sourceActorUID = sourceActor;
        type = MessageType.BulletHit;
    }
}
