using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks;
using UnityEngine;

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

    public static RoundelLayout[] layouts = {
        new RoundelLayout(VTOLVehicles.None, new RoundelPosition[0]),//Roundel layout for no aircraft, incase somthing goes wrong i guess
    new RoundelLayout(VTOLVehicles.AV42C, new RoundelPosition[0]),//roundel layout for the AV-42C
    new RoundelLayout(VTOLVehicles.FA26B, new RoundelPosition[] {//roundel layout for the F/A-26
        new RoundelPosition(new Vector3(-4.31f, 0.38f, -3.26f), Quaternion.Euler(new Vector3(90,0,0)), new Vector3(2,2,2)),//roundel on the top of the left wing
        new RoundelPosition(new Vector3(4.31f, 0.38f, -3.26f), Quaternion.Euler(new Vector3(90,0,0)), new Vector3(2,2,2)),//roundel on the top of the right wing
        new RoundelPosition(new Vector3(-4.51f, 0.144f, -2.498f), Quaternion.Euler(new Vector3(-90,180,0)), new Vector3(1,1,1)),//roundel on the bottom of the left wing
        new RoundelPosition(new Vector3(4.51f, 0.144f, -2.498f), Quaternion.Euler(new Vector3(-90,180,0)), new Vector3(1,1,1)),//roundel on the bottom of the right wing
        new RoundelPosition(new Vector3(-0.894f, 0.729f, 5.239f), Quaternion.Euler(new Vector3(41.881f,97.04301f,4.823f)), new Vector3(0.7f,0.7f,0.7f)),//roundel on the left of the cockpit
        new RoundelPosition(new Vector3(0.894f, 0.729f, 5.239f), Quaternion.Euler(new Vector3(41.881f,-97.04301f,-4.823f)), new Vector3(0.7f,0.7f,0.7f)),//roundel on the right of the cockpit
        new RoundelPosition(new Vector3(-2.849f, 1.596f, -6.718f), Quaternion.Euler(new Vector3(-26.271f,90,0)), new Vector3(1,1,1)),//roundel on the left tail
        new RoundelPosition(new Vector3(2.849f, 1.596f, -6.718f), Quaternion.Euler(new Vector3(-26.271f,-90,0)), new Vector3(1,1,1))//roundel on the right tail
    }),
    new RoundelLayout(VTOLVehicles.F45A, new RoundelPosition[0])//roundel layout for the F-45
    };

    public static void SetupAircraftRoundels(Transform aircraft, VTOLVehicles type, CSteamID steamID, Vector3 offset)
    {
        Texture2D pfpTexture = GetAvatar(steamID);

        RoundelLayout layout = layouts[(int)type];

        foreach (RoundelPosition roundelPosition in layout.roundels) {
            GameObject roundel = GameObject.CreatePrimitive(PrimitiveType.Quad);
            roundel.transform.parent = aircraft;
            roundel.transform.localPosition = roundelPosition.position;
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

        if (success && ImageWidth > 0 && ImageHeight > 0)
        {
            byte[] Image = new byte[ImageWidth * ImageHeight * 4];
            Texture2D returnTexture = new Texture2D((int)ImageWidth, (int)ImageHeight, TextureFormat.RGBA32, false, true);
            success = SteamUtils.GetImageRGBA(FriendAvatar, Image, (int)(ImageWidth * ImageHeight * 4));
            if (success)
            {
                returnTexture.LoadRawTextureData(Image);
                returnTexture.Apply();
            }
            return returnTexture;
        }
        else
        {
            Debug.LogError("Couldn't get avatar.");
            return new Texture2D(0, 0);
        }
    }
}
