using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class Message_ActorSync : Message
{
    public string allActors;
    public Message_ActorSync(string allActors)
    {
        this.allActors = allActors;
        type = MessageType.ActorSync;
    }
}