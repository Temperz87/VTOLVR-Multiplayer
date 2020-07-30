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
    EMissionCanceled
}
[Serializable]
public class Message_ObjectiveSync : Message
{
    public ulong UID;
    public ObjSyncType status;
    public int objID;
    public Message_ObjectiveSync(ulong uid, int objID, ObjSyncType stat)
    {
        UID = uid;
        objID = this.objID;
        status = stat;
        type = MessageType.ObjectiveSync;
    }
}