using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class VTTextProperties
{

    public VTTextProperties(string text, float fontSize, float lineHeight, VTText.AlignmentModes align, VTText.VerticalAlignmentModes vertAlign)
    {
        this.text = text;
        this.fontSize = fontSize;
        this.lineHeight = lineHeight;
        this.align = align;
        this.vertAlign = vertAlign;
    }

    public VTTextProperties(string text, float fontSize, float lineHeight, VTText.AlignmentModes align, VTText.VerticalAlignmentModes vertAlign, Color color, Color emission, bool useEmission, float emissionMult)
    {
        this.text = text;
        this.fontSize = fontSize;
        this.lineHeight = lineHeight;
        this.align = align;
        this.vertAlign = vertAlign;
        this.color = color;
        this.emission = emission;
        this.useEmission = useEmission;
        this.emissionMult = emissionMult;
    }
    public string text;
    public float fontSize;
    public float lineHeight;
    public VTText.AlignmentModes align;
    public VTText.VerticalAlignmentModes vertAlign;
    public Color color;
    public Color emission;
    public bool useEmission;
    public float emissionMult;

}
static class FileLoader
{
    //PUBLIC LOADING METHODS
    public static GameObject GetAssetBundleAsGameObject(string path, string name)
    {
        Debug.Log("AssetBundleLoader: Attempting to load AssetBundle...");
        UnityEngine.AssetBundle bundle = null;
        try
        {
            bundle = AssetBundle.LoadFromFile(path);
            Debug.Log("AssetBundleLoader: Success.");
        }
        catch (Exception e)
        {
            Debug.Log("AssetBundleLoader: Couldn't load AssetBundle from path: '" + path + "'. Exception details: e: " + e.Message);
        }

        Debug.Log("AssetBundleLoader: Attempting to retrieve: '" + name + "' as type: 'GameObject'.");
        try
        {
            var temp = bundle.LoadAsset(name, typeof(GameObject));
            Debug.Log("AssetBundleLoader: Success.");
            return (GameObject)temp;
        }
        catch (Exception e)
        {
            Debug.Log("AssetBundleLoader: Couldn't retrieve GameObject from AssetBundle.");
            return null;
        }
    }
}
public static class CUSTOM_API
{

    private static GameObject hudDash;

    private static GameObject switchObject;
    private static GameObject flareDumpSwitch;
    private static GameObject button1;
    private static GameObject button2;
    private static GameObject button3;
    private static GameObject button4;
    private static GameObject button5;
    private static GameObject button6;
    private static GameObject button7;
    private static GameObject button8;
    private static GameObject button9;
    private static GameObject button0;
    private static GameObject buttonClr;
    private static GameObject vmaxSwitch;
    private static GameObject newDisplayPrefab;
    private static GameObject newDisplay;
    private static string PathToBundle;
    private static bool AssetLoaded = false;
    private static GameObject radioInput;
    private static VTText radioText;

    public static string currentFreq = "xxx.x";
    private static int freqIndex = 0;
    private static StringBuilder sb;
    private static GameObject paper;
    private static GameObject paperLabel;

    private static GameObject swap;
    private static GameObject objectToMove;
    private static GameObject labelObject;
    public static bool onFreq = false;
    public static GameObject manprefab = null;

    private static bool lastFreq = false;
    public static void loadDisplayPrefab()
    {
        PathToBundle = Directory.GetCurrentDirectory() + @"\VTOLVR_ModLoader\mods\Multiplayer\display";
        if (!AssetLoaded)
        {
            newDisplayPrefab = FileLoader.GetAssetBundleAsGameObject(PathToBundle, "Display.prefab");
            PathToBundle = Directory.GetCurrentDirectory() + @"\VTOLVR_ModLoader\mods\Multiplayer\ikmanfinal";
            manprefab = FileLoader.GetAssetBundleAsGameObject(PathToBundle, "IKMANMOUSE.prefab");

            AssetLoaded = true;
            Debug.Log("Prefab is " + newDisplayPrefab);

        }

    }


    private static void updateFreq1()
    {
        updateFreq('1');

    }

    private static void updateFreq2()
    {
        updateFreq('2');

    }

    private static void updateFreq3()
    {
        updateFreq('3');

    }

    private static void updateFreq4()
    {
        updateFreq('4');

    }

    private static void updateFreq5()
    {
        updateFreq('5');

    }

    private static void updateFreq6()
    {
        updateFreq('6');

    }

    private static void updateFreq7()
    {
        updateFreq('7');

    }

    private static void updateFreq8()
    {
        updateFreq('8');

    }

    private static void updateFreq9()
    {
        updateFreq('9');

    }

    private static void updateFreq0()
    {
        updateFreq('0');

    }
    private static void editIndex()
    {
        freqIndex = Math.Min(4, freqIndex);
        char letter = currentFreq[freqIndex];

        if (letter == '.')
        {

            freqIndex -= 1;
        }

        currentFreq = currentFreq.ReplaceAt(freqIndex, 'X');
        freqIndex -= 1;
        radioText.text = currentFreq;
        radioText.ApplyText();


        DiscordRadioManager.radioFreq = radioText.text.GetHashCode();
        Debug.Log("discord freq " + DiscordRadioManager.radioFreq);
        if (PlayerManager.FrequenceyButton != null)
        {
            UnityEngine.UI.Text text = PlayerManager.FrequenceyButton.GetComponentInChildren<UnityEngine.UI.Text>();
            //text.transform.localScale = text.transform.localScale * 0.75f;
            text.text = "Freq: " + currentFreq;

        }
        freqIndex = Math.Max(0, freqIndex);

    }
    public static string ReplaceAt(this string input, int index, char newChar)
    {
        if (input == null)
        {
            throw new ArgumentNullException("input");
        }
        char[] chars = input.ToCharArray();
        chars[index] = newChar;
        return new string(chars);
    }
    private static void updateFreq(char input)
    {

        freqIndex = Math.Min(3, freqIndex);

        char letter = currentFreq[freqIndex];

        if (letter == '.')
        {

            freqIndex++;
        }
        letter = currentFreq[freqIndex];
        if (letter == 'X')
        {
            currentFreq = currentFreq.ReplaceAt(freqIndex, input);
            freqIndex++;
        }
        radioText.text = currentFreq;
        radioText.ApplyText();


        DiscordRadioManager.radioFreq = radioText.text.GetHashCode();
        Debug.Log("discord freq " + DiscordRadioManager.radioFreq);
        if (PlayerManager.FrequenceyButton != null)
        {
            UnityEngine.UI.Text text = PlayerManager.FrequenceyButton.GetComponentInChildren<UnityEngine.UI.Text>();
            //text.transform.localScale = text.transform.localScale * 0.75f;
            text.text = "Freq: " + currentFreq;

        }

        freqIndex = Math.Min(4, freqIndex);
    }


    public static void forceSetFreq(string ins)
    {
        currentFreq = ins;
        freqIndex = 4;
        if (radioText != null)
        {
            radioText.text = currentFreq;
            radioText.ApplyText();
            radioText.SetEmission(true);
            radioText.SetEmissionMultiplier(3);
        }

    }
    public static void setupLeg(GameObject go)
    {
        GameObject leg = GetChildWithName(go, "femur.left");
        paper = GameObject.CreatePrimitive(PrimitiveType.Cube);
        paper.transform.SetParent(leg.transform);
        paper.transform.localScale = new Vector3(0.18f, 0.001f, 0.13f);
        GameObject.Destroy(paper.GetComponent<Collider>());
        GameObject.Destroy(paper.GetComponent<Rigidbody>());
        paper.transform.SetParent(leg.transform);
        paper.transform.localPosition = new Vector3(0.1003f, 0.1629f, -0.0089f);
        paper.transform.localEulerAngles = new Vector3(4.13f, 188.92f, 87.24f);

        TextMeshPro textMesh;
        GameObject paperLabel;
        paperLabel = new GameObject();

        textMesh = paperLabel.AddComponent<TextMeshPro>();
        textMesh.alignment = TextAlignmentOptions.Left;
        textMesh.overflowMode = TextOverflowModes.Overflow;
        textMesh.enableWordWrapping = false;
        textMesh.fontSize *= 0.5f;
        textMesh.color = new Color32(0, 0, 0, 255);
        paperLabel.transform.SetParent(leg.transform);
        paperLabel.transform.localPosition = new Vector3(0.11f, 0.115f, 0);
        paperLabel.transform.localEulerAngles = new Vector3(3.24f, 280.42f, 185.08f);
        paperLabel.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);

        textMesh.SetText(DiscordRadioManager.getFrequencyTableString());
        CUSTOM_API.forceSetFreq("122.8");
        DiscordRadioManager.radioFreq = currentFreq.GetHashCode();
    }
    public static void setupFA26(GameObject go)
    {
        switchObject = GetChildWithName(go, "APUSwitch");

        Debug.Log(" hudDash = GetChildWithName(go, ");
        hudDash = GetChildWithName(go, "HUDDash");

        Debug.Log("switchObject != null");
        if (switchObject != null)
        {
            //gets engine objects
            FindSwitchObjects(go);
            if (DiscordRadioManager.frequencyTable.Count > 0)
                currentFreq = DiscordRadioManager.frequencyTable[0];
            else
                currentFreq = "122.8";
            DiscordRadioManager.radioFreq = currentFreq.GetHashCode();


            sb = new StringBuilder(currentFreq);
            lastFreq = false;

            Debug.Log("SetupNewDisplay");

            SetupNewDisplay();
            //newDisplay.GetComponent<MeshRenderer>().material.EnableKeyword("_EMISSION");
            newDisplay.transform.SetParent(go.transform);

            newDisplay.transform.localPosition = new Vector3(0.05f, 1.289f, 5.84f);
            newDisplay.transform.localEulerAngles = new Vector3(273, 0, 0);
            newDisplay.transform.localScale = Vector3.Scale(newDisplay.transform.localScale, new Vector3(0.065f, 0.065f, 0.065f));


            Debug.Log(" Color darkGreen = new Color32(26, 102, 11, 255);");
            Color darkGreen = new Color32(26, 102, 11, 255);
            VTTextProperties radioProp = new VTTextProperties("radioInput", 30, 1, VTText.AlignmentModes.Center, VTText.VerticalAlignmentModes.Middle, darkGreen, Color.green, true, 3.0f);
            radioInput = createText(currentFreq, newDisplay.transform, new Vector3(0, 0.007f, 0.007f), new Vector3(0, 0, 0), new Vector3(0.0005f, 0.0005f, 0.0005f), radioProp, true);
            radioInput.transform.localEulerAngles = new Vector3(90, 0, 0);
            radioInput.name = "Radio Input";


            Debug.Log("Radio input pos: " + radioInput.transform.localPosition);
            Debug.Log("Radio input scale " + radioInput.transform.localScale);
            radioText = radioInput.GetComponent<VTText>();

            Debug.Log("createAPButton");
            selectedOBJ = GetChildWithName(go, "MFD1");
            //TODO assign audio listener
            //Debug.Log("Found event: " + cloneLever.OnSetState.GetPersistentTarget(0));

            //-0.0460, 1.2385, 5.8246
            //Creates radio buttons
            VTTextProperties button1Properties = new VTTextProperties("1", 36, 1, VTText.AlignmentModes.Center, VTText.VerticalAlignmentModes.Middle, Color.black, Color.green, true, 1.0f);
            button1 = createAPButton("1", null, "1Bound", go.transform, new Vector3(-0.0510f, 1.2435f, 5.8246f), new Vector3(0, 0, 0), button1Properties);
            VRInteractable button1Int = button1.GetComponentInChildren<VRInteractable>();
            button1Int.OnInteract = new UnityEvent();
            button1Int.OnInteract.AddListener(updateFreq1);

            Debug.Log("createAPButton2");
            VTTextProperties button2Properties = new VTTextProperties("2", 36, 1, VTText.AlignmentModes.Center, VTText.VerticalAlignmentModes.Middle, Color.black, Color.green, true, 1.0f);
            button2 = createAPButton("2", null, "2Bound", go.transform, new Vector3(-0.0280f, 1.2435f, 5.8246f), new Vector3(0, 0, 0), button2Properties);
            VRInteractable button2Int = button2.GetComponentInChildren<VRInteractable>();
            button2Int.OnInteract = new UnityEvent();
            button2Int.OnInteract.AddListener(updateFreq2);

            VTTextProperties button3Properties = new VTTextProperties("3", 36, 1, VTText.AlignmentModes.Center, VTText.VerticalAlignmentModes.Middle, Color.black, Color.green, true, 1.0f);
            button3 = createAPButton("3", null, "3Bound", go.transform, new Vector3(-0.0050f, 1.2435f, 5.8246f), new Vector3(0, 0, 0), button3Properties);
            VRInteractable button3Int = button3.GetComponentInChildren<VRInteractable>();
            button3Int.OnInteract = new UnityEvent();
            button3Int.OnInteract.AddListener(updateFreq3);

            //v.1284, 1.2165, 5.8176

            VTTextProperties button4Properties = new VTTextProperties("4", 36, 1, VTText.AlignmentModes.Center, VTText.VerticalAlignmentModes.Middle, Color.black, Color.green, true, 1.0f);
            button4 = createAPButton("4", null, "4Bound", go.transform, new Vector3(-0.0510f, 1.2275f, 5.8196f), new Vector3(0, 0, 0), button4Properties);
            VRInteractable button4Int = button4.GetComponentInChildren<VRInteractable>();
            button4Int.OnInteract = new UnityEvent();
            button4Int.OnInteract.AddListener(updateFreq4);


            VTTextProperties button5Properties = new VTTextProperties("5", 36, 1, VTText.AlignmentModes.Center, VTText.VerticalAlignmentModes.Middle, Color.black, Color.green, true, 1.0f);
            button5 = createAPButton("5", null, "5Bound", go.transform, new Vector3(-0.0280f, 1.2275f, 5.8196f), new Vector3(0, 0, 0), button5Properties);
            VRInteractable button5Int = button5.GetComponentInChildren<VRInteractable>();
            button5Int.OnInteract = new UnityEvent();
            button5Int.OnInteract.AddListener(updateFreq5);


            VTTextProperties button6Properties = new VTTextProperties("6", 36, 1, VTText.AlignmentModes.Center, VTText.VerticalAlignmentModes.Middle, Color.black, Color.green, true, 1.0f);
            button6 = createAPButton("6", null, "6Bound", go.transform, new Vector3(-0.0050f, 1.2275f, 5.8196f), new Vector3(0, 0, 0), button6Properties);
            VRInteractable button6Int = button6.GetComponentInChildren<VRInteractable>();
            button6Int.OnInteract = new UnityEvent();
            button6Int.OnInteract.AddListener(updateFreq6);

            //0.1244, 1.1925, 5.8116
            VTTextProperties button7Properties = new VTTextProperties("7", 36, 1, VTText.AlignmentModes.Center, VTText.VerticalAlignmentModes.Middle, Color.black, Color.green, true, 1.0f);
            button7 = createAPButton("7", null, "7Bound", go.transform, new Vector3(-0.0510f, 1.2105f, 5.8156f), new Vector3(0, 0, 0), button7Properties);
            VRInteractable button7Int = button7.GetComponentInChildren<VRInteractable>();
            button7Int.OnInteract = new UnityEvent();
            button7Int.OnInteract.AddListener(updateFreq7);

            VTTextProperties button8Properties = new VTTextProperties("8", 36, 1, VTText.AlignmentModes.Center, VTText.VerticalAlignmentModes.Middle, Color.black, Color.green, true, 1.0f);
            button8 = createAPButton("8", null, "8Bound", go.transform, new Vector3(-0.0280f, 1.2105f, 5.8156f), new Vector3(0, 0, 0), button8Properties);
            VRInteractable button8Int = button8.GetComponentInChildren<VRInteractable>();
            button8Int.OnInteract = new UnityEvent();
            button8Int.OnInteract.AddListener(updateFreq8);

            VTTextProperties button9Properties = new VTTextProperties("9", 36, 1, VTText.AlignmentModes.Center, VTText.VerticalAlignmentModes.Middle, Color.black, Color.green, true, 1.0f);
            button9 = createAPButton("9", null, "9Bound", go.transform, new Vector3(-0.0050f, 1.2105f, 5.8156f), new Vector3(0, 0, 0), button9Properties);
            VRInteractable button9Int = button9.GetComponentInChildren<VRInteractable>();
            button9Int.OnInteract = new UnityEvent();
            button9Int.OnInteract.AddListener(updateFreq9);

            VTTextProperties buttonClrProperties = new VTTextProperties("Clr", 35, 1, VTText.AlignmentModes.Center, VTText.VerticalAlignmentModes.Middle, Color.black, Color.green, true, 1.0f);
            buttonClr = createAPButton("Clr", null, "ClrBound", go.transform, new Vector3(-0.0510f, 1.1935f, 5.8106f), new Vector3(0, 0, 0), buttonClrProperties);
            VRInteractable buttonClrInt = buttonClr.GetComponentInChildren<VRInteractable>();
            buttonClrInt.OnInteract = new UnityEvent();
            buttonClrInt.OnInteract.AddListener(editIndex);

            VTTextProperties button0Properties = new VTTextProperties("0", 36, 1, VTText.AlignmentModes.Center, VTText.VerticalAlignmentModes.Middle, Color.black, Color.green, true, 1.0f);
            button0 = createAPButton("0", null, "0Bound", go.transform, new Vector3(-0.0280f, 1.1935f, 5.8106f), new Vector3(0, 0, 0), button0Properties);
            VRInteractable button0Int = button0.GetComponentInChildren<VRInteractable>();
            button0Int.OnInteract = new UnityEvent();
            button0Int.OnInteract.AddListener(updateFreq0);


            setupLeg(go);
            Vector3 scaler = new Vector3(0.83f, 0.83f, 0.83f);
            //23.5153
            GameObject alt = GetChildWithName(go, "Altitude");
            alt.transform.localScale = Vector3.Scale(alt.transform.localScale, scaler);
            alt.transform.localPosition = new Vector3(55.3942f, -18.7209f, 24.3522f);

            GameObject spd = GetChildWithName(go, "Speed");
            spd.transform.localScale = Vector3.Scale(spd.transform.localScale, scaler);
            spd.transform.localPosition = new Vector3(55.3942f, -42.2362f, 26.1551f);

            GameObject hdg = GetChildWithName(go, "Heading");
            hdg.transform.localScale = Vector3.Scale(hdg.transform.localScale, scaler);
            hdg.transform.localPosition = new Vector3(55.3942f, -65.7515f, 26.1551f);

            GameObject nav = GetChildWithName(go, "Nav");
            nav.transform.localScale = Vector3.Scale(nav.transform.localScale, scaler);
            nav.transform.localPosition = new Vector3(55.3942f, -89.2668f, 26.1551f);

            GameObject off = GetChildWithName(go, "APOff");
            off.transform.localScale = Vector3.Scale(off.transform.localScale, scaler);
            off.transform.localPosition = new Vector3(55.3942f, -112.2782f, 26.1551f);
            objectToMove = off;



            GameObject brtKnob = GetChildWithName(go, "MFDBrightnessKnob");
            brtKnob.transform.localEulerAngles = new Vector3(343.54f, 0, 180);
            brtKnob.transform.localPosition = new Vector3(111.6f, -84.4f, 56.90f);


            swap = GetChildWithName(go, "MFDSwapButton");
            swap.transform.localPosition = new Vector3(-155.5f, -452.8f, 146.7f);
            swap.transform.localEulerAngles = new Vector3(275.54f, 359, 180);
            swap.transform.localScale = Vector3.Scale(swap.transform.localScale, scaler);
            VRInteractable swapInt = swap.transform.GetComponentInChildren<VRInteractable>();

            GameObject newBounds = GameObject.Instantiate(GetChildWithName(go, "MasterArmPoseBounds"), go.transform);

            newBounds.transform.position = swapInt.transform.position;
            newBounds.transform.eulerAngles = GetChildWithName(go, "MasterArmPoseBounds").transform.eulerAngles;
            swapInt.poseBounds = newBounds.GetComponent<PoseBounds>(); //Assigns bounds for switch


            forceSetFreq("122.8");
            freqIndex = 4;

            GameObject.Destroy(hudDash.transform.Find("AutopilotLabel").gameObject);
            GameObject.Destroy(hudDash.transform.Find("lrRectangle").gameObject);
            GameObject.Destroy(hudDash.transform.Find("lrRectangle (1)").gameObject);
            GameObject.Destroy(hudDash.transform.Find("lrRectangle (2)").gameObject);

        }
    }
    public static void SetupNewDisplay()
    {
        bool displayEnabled = false;
        if (newDisplayPrefab != null)
        {
            VTOLVehicles cv = VTOLVehicles.FA26B;
            if (cv == PlayerManager.getPlayerVehicleType())
            {
                displayEnabled = true;
            }

            if (displayEnabled)
            {
                newDisplay = GameObject.Instantiate(newDisplayPrefab);
                //manobject = GameObject.Instantiate(manprefab);
            }
        }
    }
    /// <summary>
    /// Spawns the apu switch inside the original one
    /// </summary>
    /// <param name="switchName"> Name of switch</param>
    /// <param name="bound"> What posebound to assign it to. Set to "null" to automatically create a new posebound just for the new switch</param>
    /// <param name="boundName">The name of the new posebound</param>
    /// <returns></returns>
    public static GameObject createAPUSwitch(string switchName, PoseBounds bound, string boundName)
    {
        GameObject newSwitch = GameObject.Instantiate(APU_ORIGINAL);
        newSwitch.name = switchName;
        newSwitch.transform.position = APU_ORIGINAL.transform.position;

        VRInteractable switchInteractable = newSwitch.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<VRInteractable>();
        VRInteractable coverInteractable = newSwitch.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<VRInteractable>();
        switchInteractable.interactableName = switchName;
        coverInteractable.interactableName = switchName + " cover";
        if (bound == null)
        {
            GameObject newBounds = GameObject.Instantiate(SEAT_ADJUST_POSE_BOUNDS, playerGameObject.transform);
            newBounds.name = boundName;
            newBounds.transform.position = newSwitch.transform.position;
            newBounds.transform.eulerAngles = SEAT_ADJUST_POSE_BOUNDS.transform.eulerAngles;
            switchInteractable.poseBounds = newBounds.GetComponent<PoseBounds>(); //Assigns bounds for switch
            coverInteractable.poseBounds = newBounds.GetComponent<PoseBounds>(); //Assigns bounds for cover

        }
        else
        {
            GameObject newBounds = GameObject.Instantiate(SEAT_ADJUST_POSE_BOUNDS, playerGameObject.transform);
            newBounds.name = boundName;
            newBounds.transform.eulerAngles = SEAT_ADJUST_POSE_BOUNDS.transform.eulerAngles;
            newBounds.transform.position = newSwitch.transform.position;
            switchInteractable.poseBounds = bound; //Assigns bounds for cover switch
            coverInteractable.poseBounds = bound; //Assigns bounds for cover


        }

        return newSwitch;
    }


    //spawns switch where ya want relative to the original location
    public static GameObject createAPUSwitch(string switchName, PoseBounds bound, string boundName, Transform parent, Vector3 localPosition)
    {

        GameObject newSwitch = GameObject.Instantiate(APU_ORIGINAL, APU_ORIGINAL.transform.parent);
        newSwitch.transform.localPosition = APU_ORIGINAL.transform.localPosition;
        newSwitch.transform.SetParent(parent);
        newSwitch.name = switchName;
        //newSwitch.transform.position = APU_ORIGINAL.transform.position;
        newSwitch.transform.localPosition = localPosition;
        Debug.Log("New apu switch is at: " + newSwitch.transform.position);
        Debug.Log("OG switch is at " + APU_ORIGINAL.transform.position);

        VRInteractable switchInteractable = newSwitch.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<VRInteractable>();
        VRInteractable coverInteractable = newSwitch.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<VRInteractable>();

        switchInteractable.interactableName = switchName;
        coverInteractable.interactableName = switchName + " cover";

        Debug.Log("Set the interactables to: " + switchInteractable + " and " + coverInteractable);
        if (bound == null)
        {
            GameObject newBounds = GameObject.Instantiate(SEAT_ADJUST_POSE_BOUNDS, playerGameObject.transform);
            newBounds.name = boundName;
            newBounds.transform.position = newSwitch.transform.position;
            newBounds.transform.eulerAngles = SEAT_ADJUST_POSE_BOUNDS.transform.eulerAngles;
            switchInteractable.poseBounds = newBounds.GetComponent<PoseBounds>(); //Assigns bounds for switch
            coverInteractable.poseBounds = newBounds.GetComponent<PoseBounds>(); //Assigns bounds for cover


        }
        else
        {
            GameObject newBounds = GameObject.Instantiate(SEAT_ADJUST_POSE_BOUNDS, playerGameObject.transform);
            newBounds.name = boundName;
            newBounds.transform.eulerAngles = SEAT_ADJUST_POSE_BOUNDS.transform.eulerAngles;
            newBounds.transform.position = newSwitch.transform.position;
            switchInteractable.poseBounds = bound; //Assigns bounds for cover switch
            coverInteractable.poseBounds = bound; //Assigns bounds for cover


        }



        Debug.Log("THE FUCKING NEW SWITCH INSIDE CREATEAPU IS " + newSwitch);
        return newSwitch;
    }

    public static GameObject createAPUSwitch(string switchName, PoseBounds bound, string boundName, Transform parent, Vector3 localPosition, Vector3 locaEulerAngles)
    {

        GameObject newSwitch = GameObject.Instantiate(APU_ORIGINAL, APU_ORIGINAL.transform.parent);
        newSwitch.transform.localPosition = APU_ORIGINAL.transform.localPosition;
        newSwitch.transform.SetParent(parent);
        newSwitch.name = switchName;
        newSwitch.transform.localPosition = localPosition;
        newSwitch.transform.localEulerAngles = locaEulerAngles;

        GameObject newSwitchLabel = newSwitch.transform.GetChild(4).gameObject;

        GameObject customSwitchLabel = createText(switchName, newSwitch.transform, newSwitchLabel.transform.localPosition, newSwitchLabel.transform.localEulerAngles, newSwitchLabel.transform.localScale);

        customSwitchLabel.transform.SetParent(newSwitch.transform);

        GameObject.Destroy(newSwitchLabel);
        Debug.Log("New apu switch is at: " + newSwitch.transform.position);
        Debug.Log("OG switch is at " + APU_ORIGINAL.transform.position);

        VRInteractable switchInteractable = newSwitch.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<VRInteractable>();
        VRInteractable coverInteractable = newSwitch.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<VRInteractable>();

        switchInteractable.interactableName = switchName;
        coverInteractable.interactableName = switchName + " cover";

        Debug.Log("Set the interactables to: " + switchInteractable + " and " + coverInteractable);
        if (bound == null)
        {
            GameObject newBounds = GameObject.Instantiate(SEAT_ADJUST_POSE_BOUNDS, playerGameObject.transform);
            newBounds.name = boundName;
            newBounds.transform.position = newSwitch.transform.position;
            newBounds.transform.eulerAngles = SEAT_ADJUST_POSE_BOUNDS.transform.eulerAngles;
            switchInteractable.poseBounds = newBounds.GetComponent<PoseBounds>(); //Assigns bounds for switch
            coverInteractable.poseBounds = newBounds.GetComponent<PoseBounds>(); //Assigns bounds for cover


        }
        else
        {
            GameObject newBounds = GameObject.Instantiate(SEAT_ADJUST_POSE_BOUNDS, playerGameObject.transform);
            newBounds.name = boundName;
            newBounds.transform.eulerAngles = SEAT_ADJUST_POSE_BOUNDS.transform.eulerAngles;
            newBounds.transform.position = newSwitch.transform.position;
            switchInteractable.poseBounds = bound; //Assigns bounds for cover switch
            coverInteractable.poseBounds = bound; //Assigns bounds for cover


        }



        Debug.Log("THE FUCKING NEW SWITCH INSIDE CREATEAPU IS " + newSwitch);
        return newSwitch;
    }

    public static GameObject createAPUSwitch(string switchName, PoseBounds bound, string boundName, Transform parent, Vector3 localPosition, Vector3 locaEulerAngles, VTTextProperties properties)
    {

        GameObject newSwitch = GameObject.Instantiate(APU_ORIGINAL, APU_ORIGINAL.transform.parent);
        newSwitch.transform.localPosition = APU_ORIGINAL.transform.localPosition;
        newSwitch.transform.SetParent(parent);
        newSwitch.name = switchName;
        newSwitch.transform.localPosition = localPosition;
        newSwitch.transform.localEulerAngles = locaEulerAngles;

        GameObject newSwitchLabel = newSwitch.transform.GetChild(4).gameObject;

        GameObject customSwitchLabel = createText(switchName, newSwitch.transform, newSwitchLabel.transform.localPosition, newSwitchLabel.transform.localEulerAngles, newSwitchLabel.transform.localScale, properties, false);



        GameObject.Destroy(newSwitchLabel);
        Debug.Log("New apu switch is at: " + newSwitch.transform.position);
        Debug.Log("OG switch is at " + APU_ORIGINAL.transform.position);

        VRInteractable switchInteractable = newSwitch.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<VRInteractable>();
        VRInteractable coverInteractable = newSwitch.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<VRInteractable>();

        switchInteractable.interactableName = switchName;
        coverInteractable.interactableName = switchName + " cover";

        Debug.Log("Set the interactables to: " + switchInteractable + " and " + coverInteractable);
        if (bound == null)
        {
            GameObject newBounds = GameObject.Instantiate(SEAT_ADJUST_POSE_BOUNDS, playerGameObject.transform);
            newBounds.name = boundName;
            newBounds.transform.position = newSwitch.transform.position;
            newBounds.transform.eulerAngles = SEAT_ADJUST_POSE_BOUNDS.transform.eulerAngles;
            switchInteractable.poseBounds = newBounds.GetComponent<PoseBounds>(); //Assigns bounds for switch
            coverInteractable.poseBounds = newBounds.GetComponent<PoseBounds>(); //Assigns bounds for cover


        }
        else
        {
            GameObject newBounds = GameObject.Instantiate(SEAT_ADJUST_POSE_BOUNDS, playerGameObject.transform);
            newBounds.name = boundName;
            newBounds.transform.eulerAngles = SEAT_ADJUST_POSE_BOUNDS.transform.eulerAngles;
            newBounds.transform.position = newSwitch.transform.position;
            switchInteractable.poseBounds = bound; //Assigns bounds for cover switch
            coverInteractable.poseBounds = bound; //Assigns bounds for cover


        }



        Debug.Log("THE FUCKING NEW SWITCH INSIDE CREATEAPU IS " + newSwitch);
        return newSwitch;
    }



    public static GameObject createAPButton(string buttonName, PoseBounds bound, string boundName, Transform parent, Vector3 localPosition, Vector3 locaEulerAngles, VTTextProperties properties)
    {
        GameObject newButton = GameObject.Instantiate(APOFF_ORIGINAL, APOFF_ORIGINAL.transform.parent);
        newButton.transform.localPosition = APOFF_ORIGINAL.transform.localPosition;
        newButton.transform.localEulerAngles = APOFF_ORIGINAL.transform.localEulerAngles;
        newButton.transform.SetParent(parent);
        newButton.name = buttonName;
        newButton.transform.localPosition = localPosition;
        //newButton.transform.localEulerAngles = locaEulerAngles;

        newButton.transform.localScale = Vector3.Scale(newButton.transform.localScale, new Vector3(0.8f, 1, 1));
        newButton.transform.localScale = Vector3.Scale(newButton.transform.localScale, new Vector3(0.9f, 0.9f, 0.9f));
        GameObject newButtonLabel = newButton.GetComponentInChildren<VTText>().gameObject;

        GameObject customButtonLabel = createText(buttonName, newButton.transform.GetChild(1).GetChild(0), newButtonLabel.transform.localPosition, newButtonLabel.transform.localEulerAngles, newButtonLabel.transform.localScale, properties, true);



        GameObject.Destroy(newButtonLabel);


        VRInteractable buttonInteractable = newButton.transform.GetComponentInChildren<VRInteractable>();
        buttonInteractable.interactableName = buttonName;
        if (bound == null)
        {
            GameObject newBounds = GameObject.Instantiate(SEAT_ADJUST_POSE_BOUNDS, playerGameObject.transform);
            newBounds.name = boundName;
            newBounds.transform.position = newButton.transform.position;
            newBounds.transform.eulerAngles = SEAT_ADJUST_POSE_BOUNDS.transform.eulerAngles;
            buttonInteractable.poseBounds = newBounds.GetComponent<PoseBounds>(); //Assigns bounds for switch



        }
        else
        {
            GameObject newBounds = GameObject.Instantiate(SEAT_ADJUST_POSE_BOUNDS, playerGameObject.transform);
            newBounds.name = boundName;
            newBounds.transform.eulerAngles = SEAT_ADJUST_POSE_BOUNDS.transform.eulerAngles;
            newBounds.transform.position = newButton.transform.position;
            buttonInteractable.poseBounds = bound; //Assigns bounds for cover switch



        }

        Debug.Log("THE FUCKING NEW BUTTON INSIDE CREATEAPU IS " + newButton);
        return newButton;
    }

    /// <summary>
    /// Creates VTText and puts it into a new empty "label" object. However, it sets the properties of the text to default sizes and alignments
    /// </summary>
    /// <param name="text"></param>
    /// <param name="parent"></param>
    /// <param name="localPosition"></param>
    /// <param name="localEuler"></param>
    /// <param name="localScale"></param>
    /// <returns></returns>
    public static GameObject createText(string text, Transform parent, Vector3 localPosition, Vector3 localEuler, Vector3 localScale)
    {
        GameObject textObject = new GameObject(text + "Label");
        textObject.AddComponent<VTText>();
        VTText textRef = textObject.GetComponent<VTText>();

        GameObject objectClone = APU_ORIGINAL.transform.GetChild(4).gameObject;
        Debug.Log("Cloned label is: " + objectClone);

        VTText textClone = objectClone.GetComponentInChildren<VTText>();

        textObject.transform.SetParent(parent);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localEulerAngles = localEuler;
        textObject.transform.localScale = localScale;
        textRef.font = textClone.font;

        textRef.text = text;
        textRef.fontSize = 40;
        textRef.lineHeight = 1;
        textRef.align = VTText.AlignmentModes.Center;
        textRef.vertAlign = VTText.VerticalAlignmentModes.Middle;
        textRef.ApplyText();

        return textObject;

    }

    /// <summary>
    /// Creates VTText and puts it into a new empty "label" gameobject. Is able to set custom properites to the VTText
    /// </summary>
    /// <param name="text">What you want the text to say</param>
    /// <param name="parent">The parent of the label gameobject</param>
    /// <param name="localPosition">Local position of the label gameobject</param>
    /// <param name="localEuler">Local euler angle of the label gameobject</param>
    /// <param name="localScale">local scale of the label gameobject</param>
    /// <param name="properties">The properties of the actual text. Need to instantiate the VTTextProperties class and fill in the nessecary information</param>
    /// <param name="extendedProp">Adds aditional properties such as color, emission color, useEmssion, and emssionMultiplier. Set to false if you just want to make blank white text</param>
    /// <returns></returns>
    public static GameObject createText(string text, Transform parent, Vector3 localPosition, Vector3 localEuler, Vector3 localScale, VTTextProperties properties, bool extendedProp)
    {
        GameObject textObject = new GameObject(text + "Label");
        textObject.AddComponent<VTText>();
        VTText textRef = textObject.GetComponent<VTText>();

        GameObject objectClone = APU_ORIGINAL.transform.GetChild(4).gameObject;
        Debug.Log("Cloned label is: " + objectClone);

        VTText textClone = objectClone.GetComponentInChildren<VTText>();

        textObject.transform.SetParent(parent);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localEulerAngles = localEuler;
        textObject.transform.localScale = localScale;
        textRef.font = textClone.font;

        textRef.text = text;
        textRef.fontSize = properties.fontSize;
        textRef.lineHeight = properties.lineHeight;
        textRef.align = properties.align;
        textRef.vertAlign = properties.vertAlign;

        if (extendedProp)
        {
            textRef.color = properties.color;
            textRef.emission = properties.emission;
            textRef.emissionMult = properties.emissionMult;
            textRef.useEmission = properties.useEmission;
        }

        textRef.ApplyText();
        textRef.ApplyText(); textRef.ApplyText();
        textRef.SetEmission(false);
        return textObject;

    }
    public static GameObject GetChildWithName(GameObject obj, string name)
    {


        Transform[] children = obj.GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child.name == name || child.name.Contains(name + "(clone"))
            {
                return child.gameObject;
            }
        }


        return null;

    }

    public static Transform GetChildTransformWithName(GameObject obj, string name)
    {


        Transform[] children = obj.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == name || child.name.Contains(name + "(clone"))
            {
                return child;
            }
        }


        return null;

    }
    public static void moveObjectByKeyboard(GameObject objectMoved, float increment)
    {
        Vector3 objectTemp = objectMoved.transform.localPosition;



        if (Input.GetKey("w"))
        {
            objectTemp.y += increment;
            moveCounterY++;
            objectMoved.transform.localPosition = objectTemp;
            Debug.Log(objectMoved.name + ": " + objectMoved.transform.localPosition.ToString("F4"));

        }

        if (Input.GetKey("s"))
        {
            objectTemp.y -= increment;

            moveCounterY--;
            objectMoved.transform.localPosition = objectTemp;
            Debug.Log(objectMoved.name + ": " + objectMoved.transform.localPosition.ToString("F4"));
        }

        if (Input.GetKey("a"))
        {
            objectTemp.x -= increment;

            moveCounterX--;
            objectMoved.transform.localPosition = objectTemp;
            Debug.Log(objectMoved.name + ": " + objectMoved.transform.localPosition.ToString("F4"));
        }

        if (Input.GetKey("d"))
        {
            objectTemp.x += increment;

            moveCounterX++;
            objectMoved.transform.localPosition = objectTemp;
            Debug.Log(objectMoved.name + ": " + objectMoved.transform.localPosition.ToString("F4"));
        }

        if (Input.GetKey("r"))
        {
            objectTemp.z += increment;

            moveCounterZ++;
            objectMoved.transform.localPosition = objectTemp;
            Debug.Log(objectMoved.name + ": " + objectMoved.transform.localPosition.ToString("F4"));
        }

        if (Input.GetKey("f"))
        {
            objectTemp.z -= increment;

            moveCounterZ--;
            objectMoved.transform.localPosition = objectTemp;
            Debug.Log(objectMoved.name + ": " + objectMoved.transform.localPosition.ToString("F4"));
        }
    }

    private static int moveCounterX;
    private static int moveCounterY;
    private static int moveCounterZ;
    public static void rotateObjectByKeyboard(GameObject rotatedObject, float increment)
    {
        Quaternion objectTemp = rotatedObject.transform.localRotation;

        Quaternion incrementx = Quaternion.Euler(new Vector3(increment, 0.0f, 0.0f)).normalized;

        Quaternion incrementy = Quaternion.Euler(new Vector3(0.0f, increment, 0.0f)).normalized;

        Quaternion incrementz = Quaternion.Euler(new Vector3(0.0f, 0.0f, increment)).normalized;
        if (Input.GetKey("u"))
        {
            objectTemp *= incrementy;

            rotatedObject.transform.localRotation = objectTemp.normalized;
            Debug.Log("Switch clone new angle: " + rotatedObject.transform.localEulerAngles.ToString("F2"));
        }

        if (Input.GetKey("j"))
        {
            objectTemp *= Quaternion.Inverse(incrementy);


            rotatedObject.transform.localRotation = objectTemp.normalized;
            Debug.Log("Switch clone new angle: " + rotatedObject.transform.localEulerAngles.ToString("F2"));
        }

        if (Input.GetKey("h"))
        {
            objectTemp *= incrementx;

            rotatedObject.transform.localRotation = objectTemp.normalized;
            Debug.Log("Switch clone new angle: " + rotatedObject.transform.localEulerAngles.ToString("F2"));
        }


        if (Input.GetKey("k"))
        {
            objectTemp *= Quaternion.Inverse(incrementx);


            rotatedObject.transform.localRotation = objectTemp.normalized;
            Debug.Log("Switch clone new angle: " + rotatedObject.transform.localEulerAngles.ToString("F2"));
        }


        if (Input.GetKey("o"))
        {
            objectTemp *= incrementz;


            rotatedObject.transform.localRotation = objectTemp.normalized;
            Debug.Log("Switch clone new angle: " + rotatedObject.transform.localEulerAngles.ToString("F2"));
        }

        if (Input.GetKey("l"))
        {
            objectTemp *= Quaternion.Inverse(incrementz);


            rotatedObject.transform.localRotation = objectTemp.normalized;
            Debug.Log("Switch clone new angle: " + rotatedObject.transform.localEulerAngles.ToString("F2"));
        }
    }

    static void getObjectByClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit[] hit = Physics.RaycastAll(ray.origin, ray.direction, 50.0f);

            {
                foreach (var Raycs in hit)
                {
                    GameObject obj = Raycs.collider.gameObject;
                    if (obj != null)
                        if (obj.GetComponent<VRInteractable>() != null)
                        {
                            Debug.Log("You selected the " + obj.name);
                        }
                }
                // ensure you picked right object
            }
        }
    }

    static GameObject selectedOBJ;
    public static void Update()
    {

        //getObjectByClick();
        if(selectedOBJ!=null)
        { 
           foreach( var objo in selectedOBJ.GetComponentsInChildren<Rigidbody>())
                {
                objo.isKinematic = true;
                 
            }
        if (selectedOBJ != null)
            moveObjectByKeyboard(selectedOBJ, 0.15f);
        }

    }
    /// <summary>
    /// Must be called at the beggining of the program in order to retrieve all the objects that will be cloned
    /// So far only works on the fa-26b. 
    /// </summary>
    public static void FindSwitchObjects(GameObject go)
    {
        APU_ORIGINAL = GetChildWithName(go, "APUSwitch");
        Debug.Log("APU Original found: " + APU_ORIGINAL);

        //TODO start using the seat adjust posebounds
        SEAT_ADJUST_POSE_BOUNDS = GetChildWithName(go, "MasterArmPoseBounds");
        Debug.Log("pose bound found: " + SEAT_ADJUST_POSE_BOUNDS);
        playerGameObject = VTOLAPI.GetPlayersVehicleGameObject();
        //APOFF_ORIGINAL = GetChildWithName(go,"VisorButton");
        APOFF_ORIGINAL = GetChildWithName(go, "APOff");
    }




    private static GameObject APU_ORIGINAL;

    private static GameObject selectedObject = null;
    private static GameObject SEAT_ADJUST_POSE_BOUNDS;
    private static GameObject playerGameObject;
    private static GameObject APOFF_ORIGINAL;

}

