using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class WingFoldNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;
    private Message_WingFold lastMessage;
    public RotationToggle wingController;

    private void Awake()
    {
        lastMessage = new Message_WingFold(false, networkUID);
        Networker.WingFold += WingFold;
    }

    public void WingFold(Message message)
    {
        lastMessage = (Message_WingFold)message;
        if (lastMessage.UID != networkUID)
            return;
        if (lastMessage.folded)
        {
            wingController.SetDeployed();
        }
        else
        {
            wingController.SetDefault();
        }
    }

    public void OnDestroy()
    {
        Networker.WingFold -= WingFold;
        Debug.Log("Destroyed WingFold");
        Debug.Log(gameObject.name);
    }
}
