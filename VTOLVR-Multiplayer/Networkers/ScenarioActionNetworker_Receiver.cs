using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class ScenarioActionNetworker_Receiver : MonoBehaviour
{
    public ulong networkUID;
    private Message_ScenarioAction lastMessage;
   
   

    private void Awake()
    {
        lastMessage = new Message_ScenarioAction(networkUID,0);
        Networker.runScenarioAction += runScenarioAction;
    }

    public void runScenarioAction(Packet packet)
    {
        lastMessage = (Message_ScenarioAction)((PacketSingle)packet).message;

        Debug.Log("recieved action from other");
        // do not run scenarios on self
        if (lastMessage.UID == PlayerManager.localUID)
            return;
        PlayerManager.runScenarioAction(lastMessage.scenarioActionHash);
    }

    public void OnDestroy()
    {
        Debug.Log("Destroyed action syncer");
        Debug.Log(gameObject.name);
    }
}
