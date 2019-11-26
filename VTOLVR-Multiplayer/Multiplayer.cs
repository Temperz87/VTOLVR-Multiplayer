using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Harmony;

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
            Transform ScenarioDisplay = GameObject.Find("InteractableCanvas").transform.GetChild(0).GetChild(6).GetChild(0).GetChild(1);
            //Creating the MP button
            Transform mpButton = Instantiate(ScenarioDisplay.GetChild(6).gameObject, ScenarioDisplay).transform;
            Log("Multiplayer Button" + mpButton.name);
            mpButton.gameObject.SetActive(true);
            mpButton.name = "MPButton";
            mpButton.GetComponent<RectTransform>().localPosition = new Vector3(601, -325);
            mpButton.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 206.7f);
            mpButton.GetComponentInChildren<Text>().text = "MP";
            mpButton.GetComponent<Image>().color = Color.cyan;
            mpButton.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
            VRInteractable mpInteractable = mpButton.GetComponentInChildren<VRInteractable>();
            mpInteractable.interactableName = "Multiplayer";
            mpInteractable.OnInteract = new UnityEngine.Events.UnityEvent();
            mpInteractable.OnInteract.AddListener(delegate { Debug.Log("Before Opening MP");  OpenMP();});

            GameObject.Find("InteractableCanvas").GetComponent<VRPointInteractableCanvas>().RefreshInteractables();
        }

        public void OpenMP()
        {
            Log("Finding Mission");
            CampaignSelectorUI selectorUI = FindObjectOfType<CampaignSelectorUI>();
            int missionIdx = (int)Traverse.Create(selectorUI).Field("missionIdx").GetValue();
            PilotSaveManager.currentScenario = PilotSaveManager.currentCampaign.missions[missionIdx];
            Log("Pressed Open Multiplayer Button\n" +
                PilotSaveManager.currentScenario + "\n" +
                PilotSaveManager.currentCampaign + "\n" +
                PilotSaveManager.currentVehicle);
        }
    }
}
