using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class Message_LoadingTextRequest : Message
{
    public string content;

    public Message_LoadingTextRequest()
    {
        type = MessageType.LoadingTextRequest;
    }
}