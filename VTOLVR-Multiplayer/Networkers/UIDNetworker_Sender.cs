using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class UIDNetworker_Sender : MonoBehaviour //this class is attached to actors so we can give them a UID, can also be used for teams in the future
{
    public ulong networkUID;
    private Actor actor;
    private void Awake()
    {
        actor = base.GetComponent<Actor>();
        if (actor != null)
        {
            if (!VTOLVR_Multiplayer.AIDictionaries.allActors.ContainsKey(networkUID))
            {
                VTOLVR_Multiplayer.AIDictionaries.allActors.Add(networkUID, actor);
            }
            if (!VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.ContainsKey(actor))
            {
                VTOLVR_Multiplayer.AIDictionaries.reverseAllActors.Add(actor, networkUID);
            }
        }
    }
}