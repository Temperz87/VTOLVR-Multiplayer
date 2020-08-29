using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class ExtLight_Receiver : MonoBehaviour
{
    public ulong networkUID;
    private Message_ExtLight lastMessage;
    public ExteriorLightsController lightsController;

    private void Awake()
    {
        lastMessage = new Message_ExtLight(false, false, false, networkUID);
        Networker.ExtLight += ChangeLights;
    }

    public void ChangeLights(Message message)
    {
        lastMessage = (Message_ExtLight)message;
        if (lastMessage.UID != networkUID)
            return;

        Debug.Log("The lights on " + networkUID + " have changed.");
        if (lastMessage.nav)
        {
            lightsController.SetNavLights(1);
        }
        else
        {
            lightsController.SetNavLights(0);
        }
        if (lastMessage.strobe)
        {
            lightsController.SetStrobeLights(1);
        }
        else
        {
            lightsController.SetStrobeLights(0);
        }
        if (lastMessage.land)
        {
            lightsController.SetLandingLights(1);
        }
        else
        {
            lightsController.SetLandingLights(0);
        }
    }

    public void OnDestroy()
    {
        Networker.ExtLight -= ChangeLights;
        Debug.Log("Destroyed ExtLight");
        Debug.Log(gameObject.name);
    }
}
