using System;




public enum ObjSyncType
{
    EMissionCompleted,
    EMissionFailed,
    EMissionBegin,
    EMissionCanceled,
    EVTBegin
}
[Serializable]
public class Message_ObjectiveSync : Message
{
    public ulong UID;
    public ObjSyncType status;
    public int objID;
    public Message_ObjectiveSync(ulong uid, int objid, ObjSyncType stat)
    {
        UID = uid;
        objID = objid;
        status = stat;
        type = MessageType.ObjectiveSync;
    }
}



public enum AudioMsgType
{
    ERadio,
    EMusic
}
[Serializable]
public class Message_AudioCommand : Message
{
    public ulong UID;
    public AudioMsgType typeAud;
    public string path;
    public bool Loop;
    public float Pos;
    public Message_AudioCommand(ulong uid, string path, AudioMsgType t)
    {
        UID = uid;
        this.path = path;
        typeAud = t;
        type = MessageType.ObjectiveSync;
    }
}
