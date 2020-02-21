using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class Message_LoadingTextUpdate : Message
{
    public string content;

    public Message_LoadingTextUpdate(string content)
    {
        this.content = content;
        type = MessageType.LoadingTextUpdate;
    }
}