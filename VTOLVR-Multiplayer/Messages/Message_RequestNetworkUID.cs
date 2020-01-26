using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Message_RequestNetworkUID_Result : Message
{
    public ulong ID;

    public Message_RequestNetworkUID_Result(ulong iD)
    {
        ID = iD;
    }
}