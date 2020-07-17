using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class Message_HostLoaded : Message
{
    public bool isReady;
    public Message_HostLoaded(bool isReady)
    {
        this.isReady = isReady;
        type = MessageType.HostLoaded;
    }
}