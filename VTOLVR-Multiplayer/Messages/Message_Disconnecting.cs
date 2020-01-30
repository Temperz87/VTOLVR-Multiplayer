using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
[Serializable]
public class Message_Disconnecting : Message
{
    public ulong UID;
    public bool isHost;

    public Message_Disconnecting(ulong uID, bool isHost)
    {
        UID = uID;
        this.isHost = isHost;
        type = MessageType.Disconnecting;
    }
}