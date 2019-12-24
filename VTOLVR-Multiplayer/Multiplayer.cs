using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Harmony;
using System.Reflection;
using Steamworks;
using System.Collections;

public class Multiplayer : VTOLMOD
{
    private static string TesterURL = "http://86.182.159.146/?id=";

    private struct FriendItem
    {
        public CSteamID steamID;
        public Transform transform;

        public FriendItem(CSteamID steamID, Transform transform)
        {
            this.steamID = steamID;
            this.transform = transform;
        }
    }
    //Friends
    private GameObject friendsTemplate, content, lableVTOL,lableInGame,lableOnline,lableOffline, JoinButton;
    private ScrollRect scrollRect;
    private float buttonHeight;
    private List<FriendItem> steamFriends = new List<FriendItem>();
    private CSteamID selectedFriend;
    private Transform selectionTF;

    private void Start()
    {
        HarmonyInstance harmony = HarmonyInstance.Create("marsh.vtolvr.multiplayer");      
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
    public override void ModLoaded()
    {
#if DEBUG
        Log("Running in Debug Mode");
#else
        Log("Running in Release Mode");
        System.Net.WebClient wc = new System.Net.WebClient();
        string webData = wc.DownloadString(TesterURL + SteamUser.GetSteamID().m_SteamID);
        if (webData != "Y")
            return;
#endif

        Log("Valid User " + SteamUser.GetSteamID().m_SteamID);

        SceneManager.sceneLoaded += SceneLoaded;
        base.ModLoaded();
        CreateUI();
        gameObject.AddComponent<Networker>();


        
    }

    private void SceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        switch (arg0.buildIndex)
        {
            case 2:
                CreateUI();
                break;
            case 7:
            case 12:
                StartCoroutine(WaitForMap());
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
        GameObject ScrollView = null;
        for (int i = 0; i < MPMenu.transform.childCount; i++)
        {
            if (MPMenu.transform.GetChild(i).name != "Scroll View")
                Destroy(MPMenu.transform.GetChild(i).gameObject);
            else
            {
                ScrollView = MPMenu.transform.GetChild(i).gameObject;
                scrollRect = ScrollView.GetComponent<ScrollRect>();
            }
        }
        content = ScrollView.transform.GetChild(0).GetChild(0).gameObject;
        selectionTF = content.transform.GetChild(0);
        selectionTF.GetComponent<Image>().color = new Color(0,0,0,0);
        //Copying the List from select Campaign for friends
        friendsTemplate = content.transform.GetChild(1).gameObject;
        buttonHeight = ((RectTransform)friendsTemplate.transform).rect.height;

        
        //Getting the headers from the campaign display
        GameObject lableTemplate = ScenarioDisplay.parent.GetChild(0).GetChild(5).GetChild(0).GetChild(0).GetChild(2).gameObject;
        lableVTOL = Instantiate(lableTemplate, content.transform);
        lableVTOL.GetComponentInChildren<Text>().text = "In VTOL VR";
        lableVTOL.SetActive(true);
        lableInGame = Instantiate(lableTemplate, content.transform);
        lableInGame.GetComponentInChildren<Text>().text = "In Game";
        lableInGame.SetActive(true);
        lableOnline = Instantiate(lableTemplate, content.transform);
        lableOnline.GetComponentInChildren<Text>().text = "Online";
        lableOnline.SetActive(true);
        lableOffline = Instantiate(lableTemplate, content.transform);
        lableOffline.GetComponentInChildren<Text>().text = "Offline";
        lableOffline.SetActive(true);

        //Back Button
        GameObject BackButton = Instantiate(mpButton.gameObject, MPMenu.transform);
        BackButton.GetComponent<RectTransform>().localPosition = new Vector3(-508, -325);
        BackButton.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 256.3f);
        BackButton.GetComponentInChildren<Text>().text = "Back";
        BackButton.GetComponent<Image>().color = Color.red;
        VRInteractable BackInteractable = BackButton.GetComponent<VRInteractable>();
        BackInteractable.interactableName = "Back";
        BackInteractable.OnInteract = new UnityEngine.Events.UnityEvent();
        BackInteractable.OnInteract.AddListener(delegate { Log("Before Back"); MPMenu.SetActive(false); ScenarioDisplay.gameObject.SetActive(true); });
        //Host
        GameObject HostButton = Instantiate(mpButton.gameObject, MPMenu.transform);
        HostButton.GetComponent<RectTransform>().localPosition = new Vector3(0, -325);
        HostButton.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 256.3f);
        HostButton.GetComponentInChildren<Text>().text = "Host";
        HostButton.GetComponent<Image>().color = Color.green;
        VRInteractable HostInteractable = HostButton.GetComponent<VRInteractable>();
        HostInteractable.interactableName = "Host Game";
        HostInteractable.OnInteract = new UnityEngine.Events.UnityEvent();
        HostInteractable.OnInteract.AddListener(delegate { Log("Before Host"); Host(); });
        //Join
        JoinButton = Instantiate(mpButton.gameObject, MPMenu.transform);
        JoinButton.GetComponent<RectTransform>().localPosition = new Vector3(489, -325);
        JoinButton.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 256.3f);
        JoinButton.GetComponentInChildren<Text>().text = "Join";
        JoinButton.GetComponent<Image>().color = Color.green;
        VRInteractable JoinInteractable = JoinButton.GetComponent<VRInteractable>();
        JoinInteractable.interactableName = "Join Game";
        JoinInteractable.OnInteract = new UnityEngine.Events.UnityEvent();
        JoinInteractable.OnInteract.AddListener(delegate { Log("Before Join"); Join(); });
        JoinButton.SetActive(false);

        mpInteractable.OnInteract.AddListener(delegate { Log("Before Opening MP"); RefershFriends(); MPMenu.SetActive(true); ScenarioDisplay.gameObject.SetActive(false); OpenMP(); });
        GameObject.Find("InteractableCanvas").GetComponent<VRPointInteractableCanvas>().RefreshInteractables();
    }

    public void RefershFriends()
    {
        Log("Refreshing Friends");
        int friendsCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
        if (friendsCount == -1)
        {
            LogError("Friendcount == -1, user isn't logged into steam");
            return;
        }

        /*
         * First we are going to sort the list into four sections
         * Friends playing vtol vr
         * Friends in game
         * Friends online
         * Friends offline
         */
        CSteamID lastFriendID;
        List<CSteamID> vtolvrFriends = new List<CSteamID>();
        List<CSteamID> inGameFriends = new List<CSteamID>();
        List<CSteamID> onlineFriends = new List<CSteamID>();
        List<CSteamID> offlineFriends = new List<CSteamID>();
        Log("Getting all friends");
        for (int i = 0; i < friendsCount; i++)
        {
            lastFriendID = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
            if (SteamFriends.GetFriendPersonaState(lastFriendID) == EPersonaState.k_EPersonaStateOffline)
            {
                offlineFriends.Add(lastFriendID);
                continue;
            }
            else if (SteamFriends.GetFriendGamePlayed(lastFriendID,out FriendGameInfo_t gameInfo))
            {
                if (gameInfo.m_gameID.AppID().m_AppId == 667970)
                {
                    //User is in VTOLVR
                    vtolvrFriends.Add(lastFriendID);
                    continue;
                }
                inGameFriends.Add(lastFriendID);
                continue;
            }
            //Just online
            onlineFriends.Add(lastFriendID);
        }
        Log("Adding friends to list");
        //Now we want to create the ingame list
        friendsTemplate.SetActive(true);
        GameObject lastFriendGO;
        VRUIListItemTemplate uiListItem;
        int totalFriends = 0;
        lableVTOL.transform.localPosition = new Vector3(0, -totalFriends * buttonHeight);
        for (int i = 0; i < vtolvrFriends.Count; i++)
        {
            totalFriends++;
            lastFriendGO = Instantiate(friendsTemplate, content.transform);
            steamFriends.Add(new FriendItem(vtolvrFriends[i],lastFriendGO.transform));
            lastFriendGO.transform.localPosition = new Vector3(0f, -totalFriends - 1 * buttonHeight);
            uiListItem = lastFriendGO.GetComponent<VRUIListItemTemplate>();
            uiListItem.Setup(SteamFriends.GetFriendPersonaName(vtolvrFriends[i]), totalFriends - 1, SelectFriend);
            uiListItem.labelText.color = Color.green;
        }
        totalFriends++;
        lableInGame.transform.localPosition = new Vector3(0, -totalFriends * buttonHeight);
        for (int i = 0; i < inGameFriends.Count; i++)
        {
            totalFriends++;
            lastFriendGO = Instantiate(friendsTemplate, content.transform);
            steamFriends.Add(new FriendItem(inGameFriends[i], lastFriendGO.transform));
            lastFriendGO.transform.localPosition = new Vector3(0f, -totalFriends * buttonHeight);
            uiListItem = lastFriendGO.GetComponent<VRUIListItemTemplate>();
            uiListItem.Setup(SteamFriends.GetFriendPersonaName(inGameFriends[i]), totalFriends - 2, SelectFriend);
            uiListItem.labelText.color = Color.green;
        }
        totalFriends++;
        lableOnline.transform.localPosition = new Vector3(0, -totalFriends * buttonHeight);
        for (int i = 0; i < onlineFriends.Count; i++)
        {
            totalFriends++;
            lastFriendGO = Instantiate(friendsTemplate, content.transform);
            steamFriends.Add(new FriendItem(onlineFriends[i], lastFriendGO.transform));
            lastFriendGO.transform.localPosition = new Vector3(0f, -totalFriends * buttonHeight);
            uiListItem = lastFriendGO.GetComponent<VRUIListItemTemplate>();
            uiListItem.Setup(SteamFriends.GetFriendPersonaName(onlineFriends[i]), totalFriends - 3, SelectFriend);
            uiListItem.labelText.color = Color.blue;
        }
        totalFriends++;
        lableOffline.transform.localPosition = new Vector3(0, -totalFriends * buttonHeight);
        for (int i = 0; i < offlineFriends.Count; i++)
        {
            totalFriends++;
            lastFriendGO = Instantiate(friendsTemplate, content.transform);
            steamFriends.Add(new FriendItem(offlineFriends[i], lastFriendGO.transform));
            lastFriendGO.transform.localPosition = new Vector3(0f, -totalFriends * buttonHeight);
            uiListItem = lastFriendGO.GetComponent<VRUIListItemTemplate>();
            uiListItem.Setup(SteamFriends.GetFriendPersonaName(offlineFriends[i]), totalFriends - 4, SelectFriend);
            uiListItem.labelText.color = Color.grey;
        }

        Log("Updating Scroll Rect");
        scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (2f + steamFriends.Count) * buttonHeight);
        scrollRect.ClampVertical();

        JoinButton.SetActive(false);
        friendsTemplate.SetActive(false);
        Log("Refreahing Interactables");
        GameObject.Find("InteractableCanvas").GetComponent<VRPointInteractableCanvas>().RefreshInteractables();
        Log($"Total Friends:{totalFriends} In VTOLVR:{vtolvrFriends.Count} In Game:{inGameFriends.Count} Online:{onlineFriends.Count} Offline:{offlineFriends.Count}");
        Networker.ResetNetworkUID();
    }

    public void SelectFriend(int index)
    {
        JoinButton.SetActive(true);
        selectedFriend = steamFriends[index].steamID;
        Log("User has selected " + SteamFriends.GetFriendPersonaName(steamFriends[index].steamID));

        selectionTF.position = steamFriends[index].transform.position;
        selectionTF.GetComponent<Image>().color = new Color(0.3529411764705882f, 0.196078431372549f, 0);
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
        Networker.HostGame();
    }

    public void Join()
    {
        if (Networker.hostID == new Steamworks.CSteamID(0))
            Networker.JoinGame(selectedFriend);
        else
            LogWarning("Already in a game with " + Networker.hostID.m_SteamID);
    }

    private IEnumerator WaitForMap()
    {
        Log("Started WaitForMap");
        while (VTMapManager.fetch == null || !VTMapManager.fetch.scenarioReady)
        {
            yield return null;
        }
        Log("Wait for map finished");
        PlayerManager.MapLoaded();
    }
}