using UnityEngine;

class ExtLight_Sender : MonoBehaviour
{
    public ulong networkUID;
    private Message_ExtLight lastMessage;
    public StrobeLightController strobeLight;
    public VRLever navLever;
    public VRLever landingLever;

    bool lastStrobe;
    bool lastNav;
    bool lastLanding;

    private void Awake()
    {
        lastMessage = new Message_ExtLight(false, false, false, networkUID);
        strobeLight = GetComponentInChildren<StrobeLightController>();
        VRInteractable navObject = FindInteractableWithName("Navigation Lights");
        if (navObject == null)
            navObject = FindInteractableWithName("Nav Lights");
        VRInteractable landingObject = FindInteractableWithName("Landing Lights");

        if (navObject != null)
        {
            navLever = navObject.gameObject.GetComponent<VRLever>();
            Debug.Log("Got navlight lever");
        }
        else
        {
            Debug.Log("Could not get navlight lever");
        }
        if (landingObject != null)
        {
            landingLever = landingObject.gameObject.GetComponent<VRLever>();
            Debug.Log("Got landing lever");
        }
        else
        {
            Debug.Log("Could not get landing lever");
        }
    }

    void FixedUpdate()
    {
        lastMessage.UID = networkUID;
        if (strobeLight.onByDefault != lastStrobe || (navLever.currentState == 1) != lastNav || (landingLever.currentState == 1) != lastLanding)
        {
            //Debug.Log("The lights on " + networkUID + " have changed, sending");

            lastMessage.strobe = strobeLight.onByDefault;
            lastMessage.nav = (navLever.currentState == 1);
            lastMessage.land = (landingLever.currentState == 1);

            if (Networker.isHost)
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);
            else
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, lastMessage, Steamworks.EP2PSend.k_EP2PSendUnreliable);

            lastStrobe = strobeLight.onByDefault;
            lastNav = (navLever.currentState == 1);
            lastLanding = (landingLever.currentState == 1);
        }
    }

    VRInteractable FindInteractableWithName(string name)
    {
        foreach (VRInteractable interactable in GameObject.FindObjectsOfType<VRInteractable>())
        {
            if (interactable.interactableName == name)
            {
                return interactable;
            }
        }
        return null;
    }
}
