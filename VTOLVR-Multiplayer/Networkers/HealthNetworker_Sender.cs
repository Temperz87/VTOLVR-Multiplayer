using UnityEngine;

class HealthNetworker_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_Death lastMessage;
    public Health health;

    private void Awake()
    {
        lastMessage = new Message_Death(networkUID);

        health = GetComponent<Health>();
        if (health == null)
            Debug.LogError("health was null on vehicle " + gameObject.name);
        else
            health.OnDeath.AddListener(Death);
            Debug.LogError("found health on " + gameObject.name);
    }

    void Death()
    {
        lastMessage.UID = networkUID;
        if (Networker.isHost)
            NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
        else
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
    }
}
