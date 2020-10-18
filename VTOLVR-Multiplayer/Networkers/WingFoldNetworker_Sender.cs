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

    void FixedUpdate()
    {
        bool foldedState = wingController.deployed;

        if (foldedState != lastFoldedState)
        {
            lastMessage.UID = networkUID;
            lastMessage.folded = foldedState;
            if (Networker.isHost)
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
            else
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);

            lastFoldedState = foldedState;
        }
    }
}
