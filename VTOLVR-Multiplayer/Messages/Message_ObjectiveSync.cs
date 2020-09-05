using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;




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
        this.objID = objid;
        status = stat;
        type = MessageType.ObjectiveSync;
    }
}
