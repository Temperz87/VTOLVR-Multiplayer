using System;
using System.Collections.Generic;
using System.Linq;
[Serializable]
public class Message_RequestNetworkUID : Message
{
    /*
     * The client will generate a local UID. Create a message and send that with it.
     * The host will create a UID for this object and put it with "resultUID"
     * Then when the client receives the messge, it first checks its local UID 
     * to tell who this needs to go to then applys the resultUID to it.
     */
    public ulong clientsUID, resultUID;

    public Message_RequestNetworkUID(ulong clientsUID)
    {
        this.clientsUID = clientsUID;
        type = MessageType.RequestNetworkUID;
    }

    public Message_RequestNetworkUID(ulong clientsUID, ulong resultUID)
    {
        this.clientsUID = clientsUID;
        this.resultUID = resultUID;
        type = MessageType.RequestNetworkUID;
    }
}