using Discord;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class DiscordRadioManager
    {
    public static Discord.Discord discord;
    public static bool connectedToDiscord;

    public static long userID;
    public static long lobbyID;
    public static string lobbySecret;
    public static bool connected;
    public static Dictionary<ulong, long> steamIDtoDiscordIDDictionary = new Dictionary<ulong, long>();
    public static int radioFreq;
    public static LobbyManager lobbyManager = null;
    public static void start()
        {
        UnityEngine.Debug.Log("loading discord");
        var dllDirectory = @"VTOLVR_ModLoader\mods\Multiplayer";
        Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + dllDirectory);
            var clientID = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID");
            if (clientID == null)
            {
                clientID = "418559331265675294";
            }
            discord = new Discord.Discord(Int64.Parse(clientID), (UInt64)Discord.CreateFlags.Default);
            userID = 0;
            lobbyID = 0;
            if (discord != null)
            {
                connectedToDiscord = true;
            UnityEngine.Debug.Log("loading discord worked");
        }
        lobbyManager = discord.GetLobbyManager();
        var userManager = discord.GetUserManager();
            // The auth manager fires events as information about the current user changes.
            // This event will fire once on init.
            //
            // GetCurrentUser will error until this fires once.
            userManager.OnCurrentUserUpdate += () =>
            {
                var currentUser = userManager.GetCurrentUser();
                //Console.WriteLine(currentUser.Username);
                userID=currentUser.Id;
            };
        }
    static void UpdateActivity(Discord.Discord discord, Discord.Lobby lobby)
    {
        var activityManager = discord.GetActivityManager();
        var lobbyManager = discord.GetLobbyManager();

        var activity = new Discord.Activity
        {
            State = "olleh",
            Details = "foo details",
            Timestamps =
            {
                Start = 5,
                End = 6,
            },
            Assets =
            {
                LargeImage = "foo largeImageKey",
                LargeText = "foo largeImageText",
                SmallImage = "foo smallImageKey",
                SmallText = "foo smallImageText",
            },
            Party = {
               Id = lobby.Id.ToString(),
               Size = {
                    CurrentSize = lobbyManager.MemberCount(lobby.Id),
                    MaxSize = (int)lobby.Capacity,
                },
            },
            Secrets = {
                Join = lobbyManager.GetLobbyActivitySecret(lobby.Id),
            },
            Instance = true,
        };

        activityManager.UpdateActivity(activity, result =>
        {
            Console.WriteLine("Update Activity {0}", result);

            // Send an invite to another user for this activity.
            // Receiver should see an invite in their DM.
            // Use a relationship user's ID for this.
            // activityManager
            //   .SendInvite(
            //       364843917537050624,
            //       Discord.ActivityActionType.Join,
            //       "",
            //       inviteResult =>
            //       {
            //           Console.WriteLine("Invite {0}", inviteResult);
            //       }
            //   );
        });
    }
    public static void addPlayer(ulong steamid, long discordid)
    {
        if(!steamIDtoDiscordIDDictionary.ContainsKey(steamid))
        steamIDtoDiscordIDDictionary.Add(steamid, discordid);
        else
        {
            steamIDtoDiscordIDDictionary.Remove(steamid);
            steamIDtoDiscordIDDictionary.Add(steamid, discordid);
        }
    }
    public static void joinLobby(long ilobbyid,string secret)
    {

        connected = true;
        lobbyID = ilobbyid;
      
        lobbyManager.ConnectLobby(ilobbyid, secret,(Discord.Result result, ref Discord.Lobby lobby) =>
        {
            if (result == Discord.Result.Ok)
            {
                Console.WriteLine("Connected to lobby {0}!", lobby.Id);
            }

            lobbyManager.ConnectVoice(lobby.Id, (Discord.Result voiceResult) => {

                if (voiceResult == Discord.Result.Ok)
                {
                    Console.WriteLine("New User Connected to Voice! Say Hello! Result: {0}", voiceResult);
                }
                else
                {
                    Console.WriteLine("Failed with Result: {0}", voiceResult);
                };
            });

            UpdateActivity(discord, lobby);
            discord.GetVoiceManager().SetSelfMute(false);
        });

        /*
    Console.WriteLine("Connected to lobby: {0}", lobby.Id);
    // Connect to voice chat, used in this case to actually know in overlay if your successful in connecting.
*/
    }

    public static void Update()
    {
        if (!connectedToDiscord)
            return;
        discord.RunCallbacks();

        lobbyManager.FlushNetwork();
    }

    public static void mutePlayer(ulong id, bool state)
    {
        var count = lobbyManager.MemberCount(lobbyID);
        for (int i = 0; i < count; i++)
        {
            var ids = lobbyManager.GetMemberUserId(lobbyID, i);

            if(steamIDtoDiscordIDDictionary.ContainsKey(id))
            {
                long discordid = steamIDtoDiscordIDDictionary[id];
            if (userID != ids)
                {
                    if (state)
                        discord.GetVoiceManager().SetLocalVolume(ids, 0);
                    else
                        discord.GetVoiceManager().SetLocalVolume(ids, 100);
                    Console.WriteLine("lobby muted");
                }
            }
            
        }
    }
    public static void makeLobby()
    {
        //lobbyManager = discord.GetLobbyManager();
        // Create the transaction
        var txn = lobbyManager.GetLobbyCreateTransaction();

        // Set lobby information
        txn.SetCapacity(30);
        txn.SetType(Discord.LobbyType.Private);
        txn.SetMetadata("a", "123");

        // Create it!
        lobbyManager.CreateLobby(txn, (Discord.Result result, ref Discord.Lobby lobby) =>
        {
            Console.WriteLine("lobby {0} created with secret {1}", lobby.Id, lobby.Secret);
           
            // We want to update the capacity of the lobby
            // So we get a new transaction for the lobby
            var newTxn = lobbyManager.GetLobbyUpdateTransaction(lobby.Id);
            newTxn.SetCapacity(5);
            lobbyManager.ConnectVoice(lobby.Id, (Discord.Result voiceResult) => {

                if (voiceResult == Discord.Result.Ok)
                {
                    Console.WriteLine("New User Connected to Voice! Say Hello! Result: {0}", voiceResult);
                }
                else
                {
                    Console.WriteLine("Failed with Result: {0}", voiceResult);
                };
            });

            lobbyID = lobby.Id;
            lobbySecret = lobby.Secret;
            connected = true;
            discord.GetVoiceManager().SetSelfMute(false);
            lobbyManager.UpdateLobby(lobby.Id, newTxn, (results) =>
            {
                Console.WriteLine("lobby updated");
            });
            UpdateActivity(discord, lobby);
        });
    
}
    

}
 
