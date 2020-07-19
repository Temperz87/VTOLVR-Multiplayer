using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class WingFoldNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_WingFold lastMessage;
    public RotationToggle wingController;
    bool lastFoldedState = false;

    private void Awake()
    {
        lastMessage = new Message_WingFold(false, networkUID);
    }

    void FixedUpdate() {
        bool foldedState = wingController.deployed;

        if (foldedState != lastFoldedState) {
            lastMessage.UID = networkUID;
            lastMessage.folded = foldedState;
            if (Networker.isHost)
                Networker.SendGlobalP2P(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
            else
                Networker.SendP2P(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
            
            lastFoldedState = foldedState;
        }
    }
}
