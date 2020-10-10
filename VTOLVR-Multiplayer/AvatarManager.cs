using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using Harmony;
using System.Reflection;

public static class AvatarManager
{
    public class RoundelLayout
    {
        public VTOLVehicles vehicleType;
        public RoundelPosition[] roundels;

        public RoundelLayout(VTOLVehicles vehicleType, RoundelPosition[] roundels)
        {
            this.vehicleType = vehicleType;
            this.roundels = roundels;
        }
    }

    public class RoundelPosition
    {
        public RoundelPosition(Vector3 position, Quaternion rotation, Vector3 scale) {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    public class Skin
    {
        public Skin(string name, bool av42, bool fa26, bool f45, List<string> textureNames, List<Texture> texture)
        {
            this.name = name;
            this.av42 = av42;
            this.fa26 = fa26;
            this.f45 = f45;
            this.textureNames = textureNames;
            this.texture = texture;
        }

        public string name;
        public bool av42;
        public bool fa26;
        public bool f45;
        public List<string> textureNames;
        public List<Texture> texture;
    }

    public static RoundelLayout[] layouts = {//roundel layouts for all the playable aircraft, get ready for some hardcoded schenanigins
        new RoundelLayout(VTOLVehicles.None, new RoundelPosition[0]),//Roundel layout for no aircraft, incase somthing goes wrong i guess
        new RoundelLayout(VTOLVehicles.AV42C, new RoundelPosition[] {//roundel layout for the AV-42C
            new RoundelPosition(new Vector3(6.18f, 1.589f, -0.04f), Quaternion.Euler(new Vector3(85.091f,-35.389f,-35.488f)), new Vector3(0.9f,0.9f,0.9f)),//roundel on the top of the left engine
            new RoundelPosition(new Vector3(-6.18f, 1.589f, -0.04f), Quaternion.Euler(new Vector3(85.091f,35.389f,35.488f)), new Vector3(0.9f,0.9f,0.9f)),//roundel on the top of the right engine
            new RoundelPosition(new Vector3(1.388f, -0.001f, 3.117f), Quaternion.Euler(new Vector3(2.585f,-99.12501f,0f)), new Vector3(1.0f,1.0f,1.0f)),//roundel on the left of the body
            new RoundelPosition(new Vector3(-1.388f, -0.001f, 3.117f), Quaternion.Euler(new Vector3(2.585f,99.12501f,0f)), new Vector3(1.0f,1.0f,1.0f)),//roundel on the right of the body
            new RoundelPosition(new Vector3(2.001f, 0.92f, 0.619f), Quaternion.Euler(new Vector3(-53.8f,-90f,0f)), new Vector3(0.6f,0.6f,0.6f)),//roundel on the bottom of the left nacelle
            new RoundelPosition(new Vector3(-2.001f, 0.92f, 0.619f), Quaternion.Euler(new Vector3(-53.8f,90f,0f)), new Vector3(0.6f,0.6f,0.6f)),//roundel on the bottom of the right nacelle
            new RoundelPosition(new Vector3(0f, 2.221f, -1.786f), Quaternion.Euler(new Vector3(90f,0f,0f)), new Vector3(1.0f,1.0f,1.0f)),//roundel on the roof
            new RoundelPosition(new Vector3(0f, -1.406f, 2.225f), Quaternion.Euler(new Vector3(-88.108f,180f,0f)), new Vector3(1.0f,1.0f,1.0f)),//roundel on the belly
            new RoundelPosition(new Vector3(0f, -0.118f, -1.801f), Quaternion.Euler(new Vector3(0f,180f,0f)), new Vector3(1.5f,1.5f,1.5f)),//roundel in the cargo bay
        }),
        new RoundelLayout(VTOLVehicles.FA26B, new RoundelPosition[] {//roundel layout for the F/A-26
            new RoundelPosition(new Vector3(-4.31f, 0.38f, -3.26f), Quaternion.Euler(new Vector3(90,0,0)), new Vector3(2.0f,2.0f,2.0f)),//roundel on the top of the left wing
            new RoundelPosition(new Vector3(4.31f, 0.38f, -3.26f), Quaternion.Euler(new Vector3(90,0,0)), new Vector3(2.0f,2.0f,2.0f)),//roundel on the top of the right wing
            new RoundelPosition(new Vector3(-4.51f, 0.18f, -2.498f), Quaternion.Euler(new Vector3(-90,180,0)), new Vector3(1.0f,1.0f,1.0f)),//roundel on the bottom of the left wing
            new RoundelPosition(new Vector3(4.51f, 0.18f, -2.498f), Quaternion.Euler(new Vector3(-90,180,0)), new Vector3(1.0f,1.0f,1.0f)),//roundel on the bottom of the right wing
            new RoundelPosition(new Vector3(-0.894f, 0.729f, 5.239f), Quaternion.Euler(new Vector3(41.881f,97.04301f,4.823f)), new Vector3(0.7f,0.7f,0.7f)),//roundel on the left of the cockpit
            new RoundelPosition(new Vector3(0.894f, 0.729f, 5.239f), Quaternion.Euler(new Vector3(41.881f,-97.04301f,-4.823f)), new Vector3(0.7f,0.7f,0.7f)),//roundel on the right of the cockpit
            new RoundelPosition(new Vector3(-2.849f, 1.596f, -6.718f), Quaternion.Euler(new Vector3(-26.271f,90,0)), new Vector3(1.0f,1.0f,1.0f)),//roundel on the left tail
            new RoundelPosition(new Vector3(2.849f, 1.596f, -6.718f), Quaternion.Euler(new Vector3(-26.271f,-90,0)), new Vector3(1.0f,1.0f,1.0f))//roundel on the right tail
        }),
        new RoundelLayout(VTOLVehicles.F45A, new RoundelPosition[] {//roundel layout for the F-45
            new RoundelPosition(new Vector3(-2.801f, 0.457f, -1.617f), Quaternion.Euler(new Vector3(92.80899f,-90,-90)), new Vector3(1.8f,1.8f,1.8f)),//roundel on the top of the left wing
            new RoundelPosition(new Vector3(2.801f, 0.457f, -1.617f), Quaternion.Euler(new Vector3(92.80899f,90,90)), new Vector3(1.8f,1.8f,1.8f)),//roundel on the top of the right wing
            new RoundelPosition(new Vector3(-1.723f, 1.008f, -3.852f), Quaternion.Euler(new Vector3(-19.71f,90,0)), new Vector3(0.8f,0.8f,0.8f)),//roundel on the left tail
            new RoundelPosition(new Vector3(1.723f, 1.008f, -3.852f), Quaternion.Euler(new Vector3(-19.71f,-90,0)), new Vector3(0.8f,0.8f,0.8f)),//roundel on the right tail
            new RoundelPosition(new Vector3(-2.801f, 0.18f, -1.617f), Quaternion.Euler(new Vector3(-96.71f,270,-90)), new Vector3(1.0f,1.0f,1.0f)),//roundel on the bottom of the left wing
            new RoundelPosition(new Vector3(2.801f, 0.18f, -1.617f), Quaternion.Euler(new Vector3(-96.71f,-270,90)), new Vector3(1.0f,1.0f,1.0f)),//roundel on the bottom of the right wing
            new RoundelPosition(new Vector3(-0.609f, 0.018f, 5.622f), Quaternion.Euler(new Vector3(-21.99f,96.01501f,-2.259f)), new Vector3(0.6f,0.6f,0.6f)),//roundel on the left of the cockpit
            new RoundelPosition(new Vector3(0.609f, 0.018f, 5.622f), Quaternion.Euler(new Vector3(-21.99f,-96.01501f,2.259f)), new Vector3(0.6f,0.6f,0.6f))//roundel on the right of the cockpit
        })
    };
    public static bool hideAvatars = false;//set this when we implement an option to dissable avatars

    public static Dictionary<string, Skin> skins = new Dictionary<string, Skin>();

    public static void SetupAircraftRoundels(Transform aircraft, VTOLVehicles type, CSteamID steamID, Vector3 offset)
    {
        if (hideAvatars)
            return;

        Texture2D pfpTexture = GetAvatar(steamID);

        RoundelLayout layout = layouts[(int)type];

        foreach (RoundelPosition roundelPosition in layout.roundels) {
            GameObject roundel = GameObject.CreatePrimitive(PrimitiveType.Quad);
            roundel.transform.parent = aircraft;
            roundel.transform.localPosition = roundelPosition.position + offset;
            roundel.transform.localRotation = roundelPosition.rotation;
            roundel.transform.localScale = roundelPosition.scale;

            GameObject.Destroy(roundel.GetComponent<Collider>());

            roundel.GetComponent<Renderer>().material.mainTexture = pfpTexture; 
            roundel.GetComponent<Renderer>().material.mainTextureScale = new Vector2(1, -1);
        }
    }

    public static Texture2D GetAvatar(CSteamID user)
    {
        int FriendAvatar = SteamFriends.GetLargeFriendAvatar(user);
        uint ImageWidth;
        uint ImageHeight;
        bool success = SteamUtils.GetImageSize(FriendAvatar, out ImageWidth, out ImageHeight);

        Debug.LogError("Loading avatar for " + user.m_SteamID);

        if (success && ImageWidth > 0 && ImageHeight > 0)
        {
            byte[] Image = new byte[ImageWidth * ImageHeight * 4];
            Texture2D returnTexture = new Texture2D((int)ImageWidth, (int)ImageHeight, TextureFormat.RGBA32, false, true);
            success = SteamUtils.GetImageRGBA(FriendAvatar, Image, (int)(ImageWidth * ImageHeight * 4));
            if (success)
            {
                returnTexture.LoadRawTextureData(Image);
                returnTexture.Apply();
                Debug.LogError("Loaded avatar!");
            }
            else {
                Debug.LogError("Avatar loading failed!");
            }
            return returnTexture;
        }
        else
        {
            Debug.LogError("Couldn't get avatar.");
            return new Texture2D(0, 0);
        }
    }

    public static void LoadAllSkins()
    {
        skins = new Dictionary<string, Skin>();

        string address = Directory.GetCurrentDirectory() + @"\VTOLVR_ModLoader\skins\";
        Debug.Log("Checking for: " + address);

        if (Directory.Exists(address))
        {
            Debug.Log(address + " exists!");
            DirectoryInfo info = new DirectoryInfo(address);
            foreach (DirectoryInfo item in info.GetDirectories())
            {
                Debug.Log("Checking for: " + address + item.Name);

                bool av42 = false;
                bool fa26 = false;
                bool f45 = false;

                List<string> fileNames = new List<string>();
                foreach (FileInfo file in item.GetFiles())
                {
                    Debug.Log("Found file " + file.Name);
                    switch (file.Name) {
                        case "0.png":
                            Debug.Log("This skin is for the AV-42");
                            av42 = true;
                            break;
                        case "1.png":
                            Debug.Log("This skin is for the FA-26");
                            fa26 = true;
                            break;
                        case "2.png":
                            Debug.Log("This skin is for the F-45");
                            f45 = true;
                            break;
                        default:
                            if (file.Name.Contains(".png")) {
                                fileNames.Add(file.Name);
                            }
                            break;
                    }
                }

                skins.Add(item.Name, new Skin(item.Name, av42, fa26, f45, fileNames, new List<Texture>()));
            }
        }
        else
        {
            Debug.Log(address + " doesn't exist.");
        }
        Debug.Log("Loading textures");
        Multiplayer._instance.StartCoroutine(LoadSkins());
    }

    static IEnumerator LoadSkins()
    {
        Debug.Log("Loading textures for skins coroutine");
        string address = Directory.GetCurrentDirectory() + @"\VTOLVR_ModLoader\skins\";
        foreach (Skin skin in skins.Values)
        {
            Debug.Log("Loading textures for " + skin.name);
            for (int x = 0; x < skin.textureNames.Count; x++)
            {
                
                using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(address + skin.textureNames[x]))
                {
                    yield return www.SendWebRequest();
                    while (www.isDone == false) {
                        yield return 0;
                    }
                    if (www.isNetworkError)
                    {
                        Debug.Log("Error: " + www.error);
                    }
                    else
                    {
                        skin.texture[x] = DownloadHandlerTexture.GetContent(www);
                        Debug.Log("Loaded " + skin.textureNames[x]);
                    }
                }
            }
        }
    }

    public static string GetLocalPlayerSkin() {
        return "";
    }
}
