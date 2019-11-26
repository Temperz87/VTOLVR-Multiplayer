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
            if (Networker._instance == null)
                gameObject.AddComponent<Networker>();
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
            VRInteractable mpInteractable = mpButton.GetComponent<VRInteractable>();
            mpInteractable.interactableName = "Multiplayer";
            mpInteractable.OnInteract = new UnityEngine.Events.UnityEvent();
            

            //Creating Mp Menu
            GameObject MPMenu = Instantiate(ScenarioDisplay.gameObject, ScenarioDisplay.parent);
            for (int i = 0; i < MPMenu.transform.childCount; i++)
            {
                Destroy(MPMenu.transform.GetChild(i).gameObject);
            }

            //Back Button
            GameObject BackButton = Instantiate(mpButton.gameObject, MPMenu.transform);
            BackButton.GetComponent<RectTransform>().localPosition = new Vector3(-508, -325);
            BackButton.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 256.3f);
            BackButton.GetComponentInChildren<Text>().text = "Back";
            BackButton.GetComponent<Image>().color = Color.red;
            VRInteractable BackInteractable = BackButton.GetComponent<VRInteractable>();
            BackInteractable.interactableName = "Back";
            BackInteractable.OnInteract = new UnityEngine.Events.UnityEvent();
            BackInteractable.OnInteract.AddListener(delegate { Debug.Log("Before Back"); MPMenu.SetActive(false); ScenarioDisplay.gameObject.SetActive(true); });
            //Host
            GameObject HostButton = Instantiate(mpButton.gameObject, MPMenu.transform);
            HostButton.GetComponent<RectTransform>().localPosition = new Vector3(-134, -325);
            HostButton.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 256.3f);
            HostButton.GetComponentInChildren<Text>().text = "Host";
            HostButton.GetComponent<Image>().color = Color.green;
            VRInteractable HostInteractable = HostButton.GetComponent<VRInteractable>();
            HostInteractable.interactableName = "Host Game";
            HostInteractable.OnInteract = new UnityEngine.Events.UnityEvent();
            HostInteractable.OnInteract.AddListener(delegate { Debug.Log("Before Host"); Host(); });
            //Join
            GameObject JoinButton = Instantiate(mpButton.gameObject, MPMenu.transform);
            JoinButton.GetComponent<RectTransform>().localPosition = new Vector3(297, -325);
            JoinButton.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 256.3f);
            JoinButton.GetComponentInChildren<Text>().text = "Join";
            JoinButton.GetComponent<Image>().color = Color.green;
            VRInteractable JoinInteractable = JoinButton.GetComponent<VRInteractable>();
            JoinInteractable.interactableName = "Join Game";
            JoinInteractable.OnInteract = new UnityEngine.Events.UnityEvent();
            JoinInteractable.OnInteract.AddListener(delegate { Debug.Log("Before Join"); Join(); });


            mpInteractable.OnInteract.AddListener(delegate { Debug.Log("Before Opening MP"); MPMenu.SetActive(true);ScenarioDisplay.gameObject.SetActive(false); OpenMP(); });
            GameObject.Find("InteractableCanvas").GetComponent<VRPointInteractableCanvas>().RefreshInteractables();
        }

        public void OpenMP()
        {
            CampaignSelectorUI selectorUI = FindObjectOfType<CampaignSelectorUI>();
            int missionIdx = (int)Traverse.Create(selectorUI).Field("missionIdx").GetValue();
            PilotSaveManager.currentScenario = PilotSaveManager.currentCampaign.missions[missionIdx];
            Log("Pressed Open Multiplayer with\n" +
                PilotSaveManager.currentScenario + "\n" +
                PilotSaveManager.currentCampaign + "\n" +
                PilotSaveManager.currentVehicle);
        }

        public void Host()
        {
            Networker.SendP2P(new Steamworks.CSteamID(0), new Message(), Steamworks.EP2PSend.k_EP2PSendReliable);
        }

        public void Join()
        {

        }
    }
}
