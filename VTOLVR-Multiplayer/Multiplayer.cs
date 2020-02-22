using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HarmonyLib;
using System.Reflection;
using Steamworks;
using System.Collections;
using TMPro;

public class Multiplayer : VTOLMOD
{
    private static string TesterURL = "http://marsh.vtolvr-mods.com/?id=";
    public static bool SoloTesting = true;
    public static Multiplayer _instance;

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
    private float buttonHeight;
    private List<FriendItem> steamFriends = new List<FriendItem>();
    private CSteamID selectedFriend;
    private Transform selectionTF;

    private Coroutine waitingForJoin;
    private Text joinButtonText;
    public Text lobbyInfoText;

    private void Start()
    {
        _instance = this;
        Harmony harmony = new Harmony("marsh.vtolvr.multiplayer");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
    public override void ModLoaded()
    {
#if DEBUG
        Log("Running in Debug Mode");
#else
        SoloTesting = false;
        Log("Running in Release Mode");
        System.Net.WebClient wc = new System.Net.WebClient();
        string webData = wc.DownloadString(TesterURL + SteamUser.GetSteamID().m_SteamID);
        if (webData != "Y")
            return;
#endif

        Log("Valid User " + SteamUser.GetSteamID().m_SteamID);

        VTOLAPI.SceneLoaded += SceneLoaded;
        base.ModLoaded();
        CreateUI();
        gameObject.AddComponent<Networker>();
    }

    private void SceneLoaded(VTOLScenes scene)
    {
        switch (scene)
        {
            case VTOLScenes.ReadyRoom:
                CreateUI();
                break;
            case VTOLScenes.Akutan:
            case VTOLScenes.CustomMapBase:
                Log("Map Loaded");
                PlayerManager.MapLoaded();
                break;
            case VTOLScenes.LoadingScene:
                CreateLoadingSceneObjects();
                break;
        }
    }

    private void CreateUI()
    {
        Log("Creating Multiplayer UI");
        Transform ScenarioDisplay = GameObject.Find("InteractableCanvas").transform.GetChild(0).GetChild(6).GetChild(0).GetChild(1);
        if (ScenarioDisplay.name != "ScenarioDisplay")
        {
            Log($"ScenarioDisplay was wrong ({ScenarioDisplay.name}), trying other method");
            ScenarioDisplay = GameObject.Find("InteractableCanvas").transform.GetChild(0).GetChild(7).GetChild(0).GetChild(1);
            Log($"ScenarioDisplay now == {ScenarioDisplay.name}");
        }
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
        selectionTF.GetComponent<Image>().color = new Color(0,0,0,0);
        Log("Copying the List from select Campaign for friends");//Copying the List from select Campaign for friends
        friendsTemplate = content.transform.GetChild(1).gameObject;
        buttonHeight = ((RectTransform)friendsTemplate.transform).rect.height;


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
        JoinButton.GetComponent<Image>().color = Color.green;
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
        lobbyInfoText.transform.localRotation =  Quaternion.Euler(lobbyInfoText.transform.localRotation.eulerAngles.x + 90,
            lobbyInfoText.transform.localRotation.y,
            lobbyInfoText.transform.localRotation.z);
        Log("Last one");
        mpInteractable.OnInteract.AddListener(delegate { Log("Before Opening MP"); RefershFriends(); MPMenu.SetActive(true); ScenarioDisplay.gameObject.SetActive(false); OpenMP(); });
        GameObject.Find("InteractableCanvas").GetComponent<VRPointInteractableCanvas>().RefreshInteractables();
        Log("Finished");
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
        int totalFriends = 0;
        lableVTOL.transform.localPosition = new Vector3(0, -totalFriends * buttonHeight);
        for (int i = 0; i < vtolvrFriends.Count; i++)
        {
            totalFriends++;
            lastFriendGO = Instantiate(friendsTemplate, content.transform);
            steamFriends.Add(new FriendItem(vtolvrFriends[i],lastFriendGO.transform));
            lastFriendGO.transform.localPosition = new Vector3(0f, -totalFriends * buttonHeight);
            uiListItem = lastFriendGO.GetComponent<VRUIListItemTemplate>();
            uiListItem.Setup(SteamFriends.GetFriendPersonaName(vtolvrFriends[i]), totalFriends - 1, SelectFriend);
            uiListItem.labelText.color = Color.green;
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
        Networker.SendP2P(steamFriends[index].steamID, new Message_LobbyInfoRequest(), EP2PSend.k_EP2PSendReliable); //Getting lobby info.
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
        if (Networker.hostID == new Steamworks.CSteamID(0) && waitingForJoin == null)
        {
            Networker.JoinGame(selectedFriend);
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
        Transform cube = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        cube.position = new Vector3(-0.485f, 1.153f, 0.394f);
        cube.rotation = Quaternion.Euler(0,-53.038f,0);
        cube.localScale = new Vector3(0.5f,0.5f,0.01f);
        cube.name = "Multiplayer Player List";

        GameObject Text = new GameObject("Text", typeof(TextMeshPro), typeof(RectTransform));
        Text.transform.SetParent(cube,false);
        RectTransform rect = Text.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1, 1);
        rect.localPosition = new Vector3(0, 0, -1);
        Networker.loadingText = Text.GetComponent<TextMeshPro>();
        Networker.loadingText.enableAutoSizing = true;
        Networker.loadingText.fontSizeMin = 0;
        Networker.loadingText.color = Color.black;
        Networker.UpdateLoadingText();
    }

    public void OnDestory()
    {
        VTOLAPI.SceneLoaded -= SceneLoaded;
    }
}