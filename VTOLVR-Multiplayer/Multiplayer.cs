using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VTOLVR_Multiplayer
{
    public class Multiplayer : VTOLMOD
    {
        public override void ModLoaded()
        {
            SceneManager.sceneLoaded += SceneLoaded;
            base.ModLoaded();
            CreateUI();
        }

        private void SceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            switch (arg0.buildIndex)
            {
                case 2:
                    CreateUI();
                    break;
            }
        }

        private void CreateUI()
        {
            Log("Creating Multiplayer UI");
            Transform missionBriefingTemp = GameObject.Find("InteractableCanvas").transform.GetChild(0).GetChild(9).GetChild(2);

            //Creating the MP button
            Transform mpButton = Instantiate(missionBriefingTemp.GetChild(7).gameObject, missionBriefingTemp).transform;
            mpButton.name = "MPButton";
            mpButton.GetComponent<RectTransform>().position = new Vector3(631, -604);
            mpButton.GetComponent<RectTransform>().sizeDelta = new Vector2(129.1f, 84);
            mpButton.GetComponentInChildren<Text>().text = "MP";
            mpButton.GetComponentInChildren<Image>().color = Color.cyan; //Errors here
            VRInteractable mpInteractable = mpButton.GetComponentInChildren<VRInteractable>();
            mpInteractable.interactableName = "Multiplayer";
            mpInteractable.OnInteract = new UnityEngine.Events.UnityEvent();
            mpInteractable.OnInteract.AddListener(delegate { OpenMP(); Debug.Log("After Open MP"); });

            GameObject.Find("InteractableCanvas").GetComponent<VRPointInteractableCanvas>().RefreshInteractables();
        }

        public void OpenMP()
        {
            Log("Pressed Open Multiplayer Button\n" +
                PilotSaveManager.currentScenario + "\n" +
                PilotSaveManager.currentCampaign + "\n" +
                PilotSaveManager.currentVehicle);
        }
    }
}
