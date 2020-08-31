/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class Message_AWACSComms : Message
{
    public bool isActive;
    public ulong networkUID;
    public Message_AWACSComms(bool isActive, ulong networkUID)
    {
        this.isActive = isActive;
        this.networkUID = networkUID;
        type = MessageType.AWACSComms;
    }
}
[Serializable]
public class Message_AWACSCommsRequest : Message
{
    public Message_AWACSCommsRequest()
    {
        type = MessageType.AWACSCommsRequest;
    }
}*/