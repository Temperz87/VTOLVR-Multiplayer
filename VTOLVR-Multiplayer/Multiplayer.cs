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
using TMPro;
using UnityEngine.Events;

public class Multiplayer : VTOLMOD
{
    private static string TesterURL = "http://marsh.vtolvr-mods.com/?id=";
    public static bool SoloTesting = true;
    public static Multiplayer _instance = null;

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
    private GameObject friendsTemplate, content, lableVTOL, JoinButton;
    private ScrollRect scrollRect;
    private ScrollRect scrollRectJoinLog;
    private GameObject lableVTOLJoinLog, lableVTOLMPHeader, lableVTOLMPIntro;
    public Text contentJoinLog;
    private float buttonHeight;
    private List<FriendItem> steamFriends = new List<FriendItem>();

    private List<GameObject> friendListItems = new List<GameObject>();


    private CSteamID selectedFriend;
    private Transform selectionTF;

    private Coroutine waitingForJoin;
    private Text joinButtonText;
    public Text lobbyInfoText;

    // Fixing singleplayer functionality with MP mod
    public bool playingMP;

    //Create a host setting for these instead of a variable!
    public Settings settings;

    public bool spawnRemainingPlayersAtAirBase = false;
    public UnityAction<bool> spawnRemainingPlayersAtAirBase_changed;

    public bool replaceWingmenWithClients = true;
    private UnityAction<bool> replaceWingmenWithClients_changed;

    public bool restrictToHostMods = true;
    private UnityAction<bool> restrictToHostMods_changed; 

    public bool forceWinds = false; // not implemented
    private UnityAction<bool> forceWinds_changed;

    public bool FreeForAllMode = false; // not implemented
    private UnityAction<bool> FreeForAllMode_changed;

    public bool displayPing = false;
    private UnityAction<bool> DisplayPing_changed;

    private void Start()
    {
        if (_instance == null)
        {
            HarmonyInstance harmony = HarmonyInstance.Create("marsh.vtolvr.multiplayer.temperzFork");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        _instance = this;
        Networker.SetMultiplayerInstance(this);
    }

    public override void ModLoaded()
    {
        Log($"VTOL VR Multiplayer v{ ModVersionString.ModVersionNumber } - branch: { ModVersionString.ReleaseBranch }");


#if DEBUG
        Log("Running in Debug Mode");
#else
        SoloTesting = false;
        Log("Running in Release Mode");
        System.Net.WebClient wc = new System.Net.WebClient();
        /*string webData = wc.DownloadString(TesterURL + SteamUser.GetSteamID().m_SteamID);
        if (webData != "Y") Relying on the honor system for this
            return;*/
#endif
        SoloTesting = false;
        Log("Valid User " + SteamUser.GetSteamID().m_SteamID);


        VTOLAPI.SceneLoaded += SceneLoaded;
        CreateSettingsPage();
        base.ModLoaded();
        CreateUI();
        gameObject.AddComponent<Networker>();
    }


    private void CreateSettingsPage()
    {
        settings = new Settings(this);


        //public bool spawnRemainingPlayersAtAirBase = false;
        //private bool spawnRemainingPlayersAtAirBase_changed;

        //public bool replaceWingmenWithClients = true;
        //private bool replaceWingmenWithClients_changed;

        //public bool restrictToHostMods = true;
        //private bool restrictToHostMods_changed;

        //public bool forceWinds = false; // not implemented
        //private bool forceWinds_changed;

        //public bool FreeForAllMode = false; // not implemented
        //private bool FreeForAllMode_changed;
        settings.CreateCustomLabel("Host Settings");
        spawnRemainingPlayersAtAirBase_changed += spawnRemainingPlayersAtAirBase_Setting;
        settings.CreateCustomLabel("Spawn players at airbase if there are no wingmen available.");
        settings.CreateBoolSetting("Default = False", spawnRemainingPlayersAtAirBase_changed, spawnRemainingPlayersAtAirBase);


        replaceWingmenWithClients_changed += replaceWingmenWithClients_Setting;
        settings.CreateCustomLabel("Replace AI wingmen (with the same flight designation) with clients.");
        settings.CreateBoolSetting("Default = True", replaceWingmenWithClients_changed, replaceWingmenWithClients);

        restrictToHostMods_changed += restrictToHostMods_Settings;
        settings.CreateCustomLabel("Require clients to use the same mods as host.");
        settings.CreateBoolSetting("Default = True", restrictToHostMods_changed, restrictToHostMods);

        forceWinds_changed += forceWinds_Settings;
        settings.CreateCustomLabel("Force winds on for clients (Not functional).");
        settings.CreateBoolSetting("Default = True", forceWinds_changed, forceWinds);

        FreeForAllMode_changed += FreeForAllMode_Settings;
        settings.CreateCustomLabel("Free For All Mode! Sets all clients to enemies (Not functional).");
        settings.CreateBoolSetting("Default = True", FreeForAllMode_changed, FreeForAllMode);

        DisplayPing_changed += DisplayPing_Settings;
        settings.CreateCustomLabel("Show ping on the screen. (Not vr)");
        settings.CreateBoolSetting("Default = False", DisplayPing_changed, displayPing);

        VTOLAPI.CreateSettingsMenu(settings);
    }

    public void spawnRemainingPlayersAtAirBase_Setting(bool newval)
    {
        spawnRemainingPlayersAtAirBase = newval;
    }

    public void replaceWingmenWithClients_Setting(bool newval)
    {
        spawnRemainingPlayersAtAirBase = newval;
    }

    public void restrictToHostMods_Settings(bool newval)
    {
        restrictToHostMods = newval;
    }

    public void forceWinds_Settings(bool newval)
    {
        forceWinds = newval;
    }

    public void FreeForAllMode_Settings(bool newval)
    {
        FreeForAllMode = newval;
    }

    public void DisplayPing_Settings(bool newval)
    {
        displayPing = newval;
    }

    void OnGUI()//the 2d ping display, feel free to move elsewhere
    {
        if (displayPing) {
            string temp = "";
            foreach (PlayerManager.Player player in PlayerManager.players)
            {
                temp += player.cSteamID + ": " + Mathf.Round(player.ping * 1000f) + "\n";
            }
            GUI.TextArea(new Rect(100, 100, 800, 800), temp);
        }
    }

    private void SceneLoaded(VTOLScenes scene)
    {
        UnityEngine.CrashReportHandler.CrashReportHandler.enableCaptureExceptions = false;

        Debug.Log($"Scene Switch! { scene.ToString() }");

        switch (scene)
        {
            case VTOLScenes.ReadyRoom:
                CreateUI();
                break;
            case VTOLScenes.Akutan:
                Log("Map Loaded from vtol scenes akutan");
                waitingForJoin = null;
                DestroyLoadingSceneObjects();
                StartCoroutine(PlayerManager.MapLoaded());
                break;
            case VTOLScenes.CustomMapBase:
                Log("Map Loaded from vtol scenes custom map base");
                waitingForJoin = null;
                DestroyLoadingSceneObjects();
                StartCoroutine(PlayerManager.MapLoaded());
                break;
            case VTOLScenes.LoadingScene:
                if (playingMP)
                {
                    Log("Create Loading Scene");
                    CreateLoadingSceneObjects();
                    break;
                }
                break;
        }
    }

    public void displayError(string errorText)
    {
        try
        {
            contentJoinLog.text = errorText;
            contentJoinLog.color = new Color32(255, 0, 0, 255);
        }
        catch (Exception err)
        {
            Debug.Log("Got an error trying to update the contentJoinLog");
            Debug.Log(err.ToString());
        }
    }

    public void displayInfo(string infoText)
    {
        try
        {
            contentJoinLog.text = infoText;
            contentJoinLog.color = new Color32(255, 255, 255, 255);
        }
        catch (Exception err)
        {
            Debug.Log("Got an error trying to update the contentJoinLog");
            Debug.Log(err.ToString());
        }
    }

    public void clearJoinLog()
    {
        try
        {
            contentJoinLog.text = "";
            contentJoinLog.color = new Color32(255, 255, 255, 255);

        }
        catch (Exception err)
        {
            Debug.Log("Got an error trying to clear the contentJoinLog");
            Debug.Log(err.ToString());
        }


    }


    private void CreateUI()
    {

        while (!SceneManager.GetActiveScene().isLoaded){
            Debug.Log("Waiting for scene to be loaded");
        }

        Log("Creating Multiplayer UI");
        
        Transform ScenarioDisplay = null;
        bool foundDisplay = false;
        bool foundCampaginDisplay = false;
        int? campaignDisplayCount = null;

        Debug.Log("Looping through canvases to find the Scenario Display");


        // Get the interactable canvas
        for (int i = 0; i < GameObject.Find("InteractableCanvas").transform.GetChild(0).childCount; i++)
        {
            // Loop through each child to find the Campaign Select Canavas
            ScenarioDisplay = GameObject.Find("InteractableCanvas").transform.GetChild(0).GetChild(i);
            if (ScenarioDisplay.name == "CampaignSelector")
            {
                foundCampaginDisplay = true;
                campaignDisplayCount = i;
                // Get the next page in the campaign selector (The scenario display)
                ScenarioDisplay = ScenarioDisplay.GetChild(0).GetChild(1);

                // If the name is ScenarioDisplay, we found it! Breaking out of the for loop to continue on...
                if (ScenarioDisplay.name == "ScenarioDisplay")
                {
                    foundDisplay = true;
                    break;
                }
            }
        }
        Debug.Log($"Found Campaign Display? { foundCampaginDisplay.ToString() }");

        if (campaignDisplayCount != null)
        {
            Debug.Log($"Found Campaign Display { campaignDisplayCount.ToString() } canvases down.");
        }

        Debug.Log($"Found Scenario Display? { foundDisplay.ToString() }");

        //Creating the MP button
        Transform mpButton = Instantiate(ScenarioDisplay.GetChild(10).gameObject, ScenarioDisplay).transform;


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


        Log("Creating Mp Menu");//Creating Mp Menu
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
        selectionTF.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        Log("Copying the List from select Campaign for friends"); //Copying the List from select Campaign for friends
        friendsTemplate = content.transform.GetChild(1).gameObject;
        buttonHeight = ((RectTransform)friendsTemplate.transform).rect.height;

        Log("Getting the headers from the campaign display for the join log"); //Getting the headers from the campaign display
        GameObject lableTemplateHeader = ScenarioDisplay.Find("Title").gameObject;
        lableVTOLMPHeader = Instantiate(lableTemplateHeader, MPMenu.transform);

        lableVTOLMPHeader.GetComponent<RectTransform>().localPosition = new Vector3(-200, 320);
        lableVTOLMPHeader.GetComponentInChildren<Text>().text = "Welcome to VTOL VR Multiplayer!";
        lableVTOLMPHeader.GetComponentInChildren<Text>().resizeTextForBestFit = true;
        lableVTOLMPHeader.GetComponentInChildren<Text>().color = new Color32(252, 183, 34, 255);
        lableVTOLMPHeader.GetComponentInChildren<Text>().fontSize = 55;
        lableVTOLMPHeader.SetActive(true);


        Log("Getting the headers from the campaign display for the join log"); //Getting the headers from the campaign display
        GameObject lableTemplateIntro = ScenarioDisplay.Find("Title").gameObject;
        lableVTOLMPIntro = Instantiate(lableTemplateIntro, MPMenu.transform);

        lableVTOLMPIntro.GetComponent<RectTransform>().localPosition = new Vector3(-200, 200);
        lableVTOLMPIntro.GetComponent<RectTransform>().sizeDelta = new Vector2(850, 500.3f);
        lableVTOLMPIntro.GetComponentInChildren<Text>().text = $"Hello and welcome to multiplayer version {ModVersionString.ModVersionNumber}!\n\nThis is an alpha release and very much so a work in progress. Expect bugs!\n\nPlease report any issues at https://vtolvr-mods.com or on the modding discord here: https://discord.gg/pW4rkYf";
        //lableVTOLJoinLog.GetComponentInChildren<Text>().resizeTextForBestFit = true;
        lableVTOLMPIntro.GetComponentInChildren<Text>().color = new Color32(255, 255, 255, 255);
        lableVTOLMPIntro.GetComponentInChildren<Text>().fontSize = 20;
        lableVTOLMPIntro.SetActive(true);

        Log("Getting the headers from the campaign display for the join log"); //Getting the headers from the campaign display
        GameObject lableTemplateLog = ScenarioDisplay.Find("Title").gameObject;
        lableVTOLJoinLog = Instantiate(lableTemplateLog, MPMenu.transform);

        lableVTOLJoinLog.GetComponent<RectTransform>().localPosition = new Vector3(-200, 00);
        lableVTOLJoinLog.GetComponent<RectTransform>().sizeDelta = new Vector2(850, 300f);
        lableVTOLJoinLog.GetComponentInChildren<Text>().text = "";
        //lableVTOLJoinLog.GetComponentInChildren<Text>().resizeTextForBestFit = true;
        lableVTOLJoinLog.GetComponentInChildren<Text>().color = new Color32(255, 0, 0, 255);
        lableVTOLJoinLog.GetComponentInChildren<Text>().fontSize = 20;
        lableVTOLJoinLog.SetActive(true);

        contentJoinLog = lableVTOLJoinLog.GetComponentInChildren<Text>();

        Log("Getting the headers from the campaign display"); //Getting the headers from the campaign display
        GameObject lableTemplate = ScenarioDisplay.parent.GetChild(0).GetChild(5).GetChild(0).GetChild(0).GetChild(2).gameObject;
        lableVTOL = Instantiate(lableTemplate, content.transform);
        lableVTOL.GetComponentInChildren<Text>().text = "In VTOL VR";
        lableVTOL.SetActive(true);

        Log("Back Button");//Back Button
        GameObject BackButton = Instantiate(mpButton.gameObject, MPMenu.transform);
        BackButton.GetComponent<RectTransform>().localPosition = new Vector3(-508, -325);
        BackButton.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 256.3f);
        BackButton.GetComponentInChildren<Text>().text = "Back";
        BackButton.GetComponent<Image>().color = Color.red;
        VRInteractable BackInteractable = BackButton.GetComponent<VRInteractable>();
        BackInteractable.interactableName = "Back";
        BackInteractable.OnInteract = new UnityEngine.Events.UnityEvent();
        BackInteractable.OnInteract.AddListener(delegate { Log("Before Back"); MPMenu.SetActive(false); ScenarioDisplay.gameObject.SetActive(true); });
        Log("Host");//Host
        GameObject HostButton = Instantiate(mpButton.gameObject, MPMenu.transform);
        HostButton.GetComponent<RectTransform>().localPosition = new Vector3(0, -325);
        HostButton.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 256.3f);
        HostButton.GetComponentInChildren<Text>().text = "Host";
        HostButton.GetComponent<Image>().color = Color.green;
        VRInteractable HostInteractable = HostButton.GetComponent<VRInteractable>();
        HostInteractable.interactableName = "Host Game";
        HostInteractable.OnInteract = new UnityEngine.Events.UnityEvent();
        HostInteractable.OnInteract.AddListener(delegate { Log("Before Host"); Host(); });
        Log("Join");//Join
        JoinButton = Instantiate(mpButton.gameObject, MPMenu.transform);
        JoinButton.GetComponent<RectTransform>().localPosition = new Vector3(489, -325);
        JoinButton.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 256.3f);
        joinButtonText = JoinButton.GetComponentInChildren<Text>();
        joinButtonText.text = "Join";
        joinButtonText.resizeTextForBestFit = true;
        JoinButton.GetComponent<Image>().color = Color.blue;
        VRInteractable JoinInteractable = JoinButton.GetComponent<VRInteractable>();
        JoinInteractable.interactableName = "Join Game";
        JoinInteractable.OnInteract = new UnityEngine.Events.UnityEvent();
        JoinInteractable.OnInteract.AddListener(delegate { Log("Before Join"); Join(); });
        JoinButton.SetActive(false);
        Log("Lobby Info Text");
        GameObject lobbyInfoGO = Instantiate(mpButton.transform.GetChild(0).gameObject, MPMenu.transform);
        lobbyInfoGO.GetComponent<RectTransform>().localPosition = new Vector3(-168.3f, -30.9f);
        lobbyInfoGO.GetComponent<RectTransform>().sizeDelta = new Vector2(942.9f, 469.8f);
        lobbyInfoText = lobbyInfoGO.GetComponent<Text>();
        lobbyInfoText.text = "Select a friend or host a lobby.";
        lobbyInfoText.alignment = TextAnchor.UpperLeft;
        lobbyInfoText.transform.localRotation = Quaternion.Euler(lobbyInfoText.transform.localRotation.eulerAngles.x + 90,
            lobbyInfoText.transform.localRotation.y,
            lobbyInfoText.transform.localRotation.z);
        Log("Last one");
        mpInteractable.OnInteract.AddListener(delegate { Log("Before Opening MP"); RefershFriends(); MPMenu.SetActive(true); ScenarioDisplay.gameObject.SetActive(false); OpenMP(); });
        GameObject.Find("InteractableCanvas").GetComponent<VRPointInteractableCanvas>().RefreshInteractables();
        Log("Finished");

        Log("Refresh");//Host
        GameObject RefreshButton = Instantiate(mpButton.gameObject, MPMenu.transform);
        RefreshButton.GetComponent<RectTransform>().localPosition = new Vector3(510, 322);
        RefreshButton.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 250f);
        RefreshButton.GetComponentInChildren<Text>().text = "Refresh";
        RefreshButton.GetComponent<Image>().color = Color.red;
        VRInteractable RefreshInteractable = RefreshButton.GetComponent<VRInteractable>();
        RefreshInteractable.interactableName = "Refresh Friends";
        RefreshInteractable.OnInteract = new UnityEngine.Events.UnityEvent();
        RefreshInteractable.OnInteract.AddListener(delegate { Log("Before Host"); RefershFriends(); });


    }

    public void RefershFriends()
    {
        Log("Refreshing Friends");
        steamFriends.Clear();
        int totalFriends = 0;
        Debug.Log($"UI List Item Count: {friendListItems.Count}");

        foreach(GameObject uiItem in friendListItems)
        {
            if (uiItem != null) {
                Debug.Log($"Destroying {uiItem.name}");
            }
            else
            {
                Debug.Log("UI Item is null");
            }
            
            Destroy(uiItem);
        }

        friendListItems.Clear();

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
        Log("Getting all friends");
        for (int i = 0; i < friendsCount; i++)
        {
            lastFriendID = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
             if (SteamFriends.GetFriendGamePlayed(lastFriendID,out FriendGameInfo_t gameInfo))
            {
                if (gameInfo.m_gameID.AppID().m_AppId == 667970)
                {
                    //User is in VTOLVR
                    vtolvrFriends.Add(lastFriendID);
                    continue;
                }
            }
        }
        Log("Adding friends to list");
        //Now we want to create the ingame list
        friendsTemplate.SetActive(true);
        GameObject lastFriendGO;
        VRUIListItemTemplate uiListItem;
        totalFriends = 0;
        lableVTOL.transform.localPosition = new Vector3(0, -totalFriends * buttonHeight);
        for (int i = 0; i < vtolvrFriends.Count; i++)
        {
            totalFriends++;
            lastFriendGO = Instantiate(friendsTemplate, content.transform);
            lastFriendGO.name = SteamFriends.GetFriendPersonaName(vtolvrFriends[i]);
            steamFriends.Add(new FriendItem(vtolvrFriends[i],lastFriendGO.transform));
            lastFriendGO.transform.localPosition = new Vector3(0f, -totalFriends * buttonHeight);
            uiListItem = lastFriendGO.GetComponent<VRUIListItemTemplate>();
            uiListItem.Setup(SteamFriends.GetFriendPersonaName(vtolvrFriends[i]), totalFriends - 1, SelectFriend);
            uiListItem.labelText.color = Color.green;
            friendListItems.Add(lastFriendGO);
        }

        Log("Updating Scroll Rect");
        scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (2f + steamFriends.Count) * buttonHeight);
        scrollRect.ClampVertical();

        JoinButton.SetActive(false);
        friendsTemplate.SetActive(false);
        Log("Refreahing Interactables");
        GameObject.Find("InteractableCanvas").GetComponent<VRPointInteractableCanvas>().RefreshInteractables();
        Log($"Total Friends:{totalFriends} In VTOLVR:{vtolvrFriends.Count}");
        Networker.ResetNetworkUID();
    }

    public void SelectFriend(int index)
    {
        JoinButton.SetActive(true);
        joinButtonText.text = $"Join {SteamFriends.GetFriendPersonaName(steamFriends[index].steamID)}";
        selectedFriend = steamFriends[index].steamID;
        Log("User has selected " + SteamFriends.GetFriendPersonaName(steamFriends[index].steamID));
        NetworkSenderThread.Instance.SendPacketToSpecificPlayer(steamFriends[index].steamID, new Message_LobbyInfoRequest(), EP2PSend.k_EP2PSendReliable); //Getting lobby info.
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
        Networker._instance.DisconnectionTasks();
        Debug.Log("Dictionaries cleared just in case.");
        playingMP = true;
        MapAndScenarioVersionChecker.CreateHashes();
        Networker._instance.HostGame();
    }

    public void Join()
    {
        playingMP = true;
        if (Networker.hostID == new Steamworks.CSteamID(0) && waitingForJoin == null)
        {
            Networker.JoinGame(selectedFriend);
            Debug.Log($"Joining friend {selectedFriend.m_SteamID}");
            waitingForJoin = StartCoroutine(WaitingForJoiningRequestResult());
        }
        else
            LogWarning("Already in a game with " + Networker.hostID.m_SteamID);
    }
    private IEnumerator WaitingForJoiningRequestResult()
    {
        for (int i = 10; i > 0; i--)
        {
            joinButtonText.text = $"Joining [{i}]";
            yield return new WaitForSeconds(1);
        }
        joinButtonText.text = "Join";
        waitingForJoin = null;
    }

    private void CreateLoadingSceneObjects()
    {
        Debug.Log("Creating Loading Screen Object.");

        //Transform cube = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        //cube.position = new Vector3(-0.485f, 1.153f, 0.394f);
        //cube.rotation = Quaternion.Euler(0,-53.038f,0);
        //cube.localScale = new Vector3(0.5f,0.5f,0.01f);
        //cube.name = "Multiplayer Player List";

        Debug.Log("Looking for Canvas and Info Transforms");
        GameObject canvas_go = GameObject.Find("Canvas").gameObject;

        Transform infoCanvas = canvas_go.transform.Find("Info").transform;

        //Transform canvas = canvas_go.transform;

        //Text missionText = canvas.Find("MissionText").GetComponent<Text>();

        Debug.Log("Removing old text");
        infoCanvas.Find("Ready").GetComponent<Text>().text = "";
        infoCanvas.Find("MissionText").GetComponent<Text>().text = "";
        infoCanvas.Find("MissionText").gameObject.SetActive(false);
        infoCanvas.Find("progressGameObject").gameObject.SetActive(false);
        infoCanvas.Find("progressGameObject").transform.Find("LoadingText").gameObject.SetActive(false);
        infoCanvas.Find("progressGameObject").transform.Find("LoadingText (1)").gameObject.SetActive(false);

        //Destroy(missionText);

        //missionText.text = "HELLO!";

        //for (int i = 0; i < GameObject.Find("Canvas").transform.GetChild(0).childCount; i++)
        //{
        //    // Loop through each child to find the Campaign Select Canavas
        //    Transform cube = GameObject.Find("InteractableCanvas").transform.GetChild(0).GetChild(i);
        //    if (cube.name == "CampaignSelector")
        //    {
        //        // Get the next page in the campaign selector (The scenario display)
        //        cube = cube.GetChild(0).GetChild(1);

        //        // If the name is ScenarioDisplay, we found it! Breaking out of the for loop to continue on...
        //        if (cube.name == "ScenarioDisplay")
        //        {
        //            break;
        //        }
        //    }
        //}Adding loading text

        //Can't add component 'RectTransform' to Text because such a component is already added to the game object!
        //Loaded scene: LoadingScene

        Debug.Log("Adding loading text");

        Networker.loadingText = canvas_go.AddComponent<TextMeshPro>();

        //GameObject Text = new GameObject("Text", typeof(TextMeshPro));
        //Text.transform.SetParent(canvas, false);
        RectTransform rect = Networker.loadingText.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(676, 350);
        //rect.position = new Vector3(0, -99, 0);
        //Networker.loadingText = Text.GetComponent<TextMeshPro>();
        Networker.loadingText.enableAutoSizing = true;
        Networker.loadingText.fontSizeMin = 100;
        Networker.loadingText.fontSizeMax = 450;
        Networker.loadingText.color = Color.white;
        if (!Networker.isHost)
        {
            NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, new Message_LoadingTextRequest(), EP2PSend.k_EP2PSendReliable); // Getting Loading Text

        }
        Networker.UpdateLoadingText();
    }

    public void CleanUpOnDisconnect() {
        selectedFriend = new CSteamID(0);
        steamFriends?.Clear();
        playingMP = false;
    }

    private void DestroyLoadingSceneObjects()
    {
        GameObject cube = GameObject.Find("Multiplayer Player List");
        if (cube != null)
        {
            Debug.Log("Destroying MP list cube");
            Destroy(cube);
        }
        else
        {
            Debug.Log("Could not find MP list cube");
        }
    }

    public void OnDestory()
    {
        VTOLAPI.SceneLoaded -= SceneLoaded;
        Networker.OnMultiplayerDestroy();
    }
}