using Discord;
using System;
using System.Collections.Generic;

public static class DiscordRadioManager
{
    public static Discord.Discord discord;
    public static bool connectedToDiscord;

    public static long userID;
    public static long lobbyID;
    public static string lobbySecret;
    public static bool connected;
    public static Dictionary<string, int> steamIDtoFreq = new Dictionary<string, int>();
    public static int radioFreq;
    public static LobbyManager lobbyManager = null;
    public static string PersonaName = " ";
    public static float tick = 0;
    public static float tickrate = 4.0f;
    public static List<string> frequencyTable = new List<string>();
    public static List<string> frequencyTableLabels = new List<string>();
    private static int freqSelection = 0;
    private static bool pttCommand = false;
    public static string freqTableNetworkString = "UNICOM";
    public static string freqLabelTableNetworkString = "122.8";
    public static void start()
    {
        UnityEngine.Debug.Log("loading discord");
        var dllDirectory = @"VTOLVR_ModLoader\mods\Multiplayer\discordsdk";
        Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + dllDirectory);
        var clientID = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID");
        if (clientID == null)
        {
            clientID = "759675844971986975";
        }
        connectedToDiscord = true; 
        PersonaName = Steamworks.SteamFriends.GetPersonaName();

        radioFreq = 0;
        reloadFrequencyTextFiles();
        try
        {
            discord = new Discord.Discord(Int64.Parse(clientID), (UInt64)Discord.CreateFlags.NoRequireDiscord, out connectedToDiscord);
        }
        catch (ResultException e)
        {
            connectedToDiscord = false;
        }
        if (!connectedToDiscord)
            return;
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
            userID = currentUser.Id;
        };

  
    }

    public static void reloadFrequencyTextFiles()
    {

        var dllDirectory = @"VTOLVR_ModLoader\mods\Multiplayer\discordsdk";
        frequencyTable.Clear();

        frequencyTableLabels.Clear();
        freqSelection = 0;

        freqLabelTableNetworkString = "UNICOM";
        freqTableNetworkString = "122.8";
        frequencyTableLabels.Add("UNICOM");
        frequencyTable.Add("122.8");

        string readText = "";
        // This text is added only once to the file.
        if (System.IO.File.Exists(dllDirectory + @"\freq.txt"))
        {
            readText = System.IO.File.ReadAllText(dllDirectory + @"\freq.txt");
            string[] values = readText.Split(',');
            frequencyTable.Add(values);
            UnityEngine.Debug.Log("loading freqs");
            freqTableNetworkString += "," + readText;
        }

        if (System.IO.File.Exists(dllDirectory + @"\freqlabels.txt"))
        {
            readText = System.IO.File.ReadAllText(dllDirectory + @"\freqlabels.txt");
            UnityEngine.Debug.Log("loading freqlabels");
            string[] values = readText.Split(',');
            frequencyTableLabels.Add(values);
            freqLabelTableNetworkString += "," + readText;
        }

    }
    public static void parseFrequencyList()
    {
        frequencyTable.Clear();

        frequencyTableLabels.Clear();
        freqSelection = 0;

        //freqTableNetworkString = "UNICOM";
        //freqLabelTableNetworkString = "140.1";
        //frequencyTableLabels.Add("UNICOM");
        //frequencyTable.Add("140.1");


        string[] values = freqTableNetworkString.Split(',');
        frequencyTable.Add(values);

        string[] values2 = freqLabelTableNetworkString.Split(',');
        frequencyTableLabels.Add(values2);
    }
    public static string getFrequencyTableString()
    {
        string frequencyLegLookup = "Frequencies:";
        int counter = 0;
        foreach (var freq in DiscordRadioManager.frequencyTable)
        {

            string Label = "\n";
            if (counter < frequencyTableLabels.Count)
            {
                Label += frequencyTableLabels[counter];
            }

            frequencyLegLookup += Label;
            frequencyLegLookup += ": " + freq;
            counter += 1;
        }
        return frequencyLegLookup;
    }
    public static string getNextFrequency()
    {
        freqSelection += 1;
        if (freqSelection > frequencyTable.Count - 1)
            freqSelection = 0;
        return frequencyTable[freqSelection];
    }
    public static void disconnect()
    {
        if (!connectedToDiscord)
            return;
        reloadFrequencyTextFiles();
        connected = false;
        radioFreq = 0;
    
        lobbyManager.DisconnectVoice(lobbyID, (result) =>
        {
            if (result == Discord.Result.Ok)
            {
                Console.WriteLine("Left voice lobby!");
            }
        });
        lobbyManager.DisconnectLobby(lobbyID, (result) =>
        {
            if (result == Discord.Result.Ok)
            {
                Console.WriteLine("Left lobby!");
            }
        });
        lobbyID = 0;
    }
    static void UpdateActivity(Discord.Discord discord, Discord.Lobby lobby)
    {
        if (!connectedToDiscord)
            return;
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
    public static void addPlayer(string name, long discordid)
    {
        if (!connectedToDiscord)
            return;
     

        if (!steamIDtoFreq.ContainsKey(name))
            steamIDtoFreq.Add(name, 0);

    }

    public static void setFreq(string name, int freq)
    {
        if (!connectedToDiscord)
            return;
        // UnityEngine.Debug.Log("setting freq of player "+ name + "to"+ freq);
        if (!steamIDtoFreq.ContainsKey(name))
            steamIDtoFreq.Add(name, freq);
        else
        {
            //steamIDtoFreq.Remove(name);
            steamIDtoFreq[name] = freq;
        }
    }
    public static void joinLobby(long ilobbyid, string secret)
    {
        if (!connectedToDiscord)
            return;
        parseFrequencyList();
        connected = true;
        lobbyID = ilobbyid;

        lobbyManager.ConnectLobby(ilobbyid, secret, (Discord.Result result, ref Discord.Lobby lobby) =>
         {
             if (result == Discord.Result.Ok)
             {
                 Console.WriteLine("Connected to lobby {0}!", lobby.Id);
             }

             lobbyManager.ConnectVoice(lobby.Id, (Discord.Result voiceResult) =>
             {

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

             //discord.GetVoiceManager().SetSelfDeaf(true);
         });

        /*
    Console.WriteLine("Connected to lobby: {0}", lobby.Id);
    // Connect to voice chat, used in this case to actually know in overlay if your successful in connecting.
*/
    }
    public static void selfMute(bool value)
    {
        discord.GetVoiceManager().SetSelfMute(value);

    }
    public static void Update()
    {
        if (!connectedToDiscord)
            return;
        discord.RunCallbacks();


        if (!connected)
            return;
        lobbyManager.FlushNetwork();
        tick += UnityEngine.Time.deltaTime;

        if (tick > 1.0f / tickrate)
        {
            tick = 0.0f;
            Message_SetFrequency freqMsg = new Message_SetFrequency(PersonaName, radioFreq);
            if (Networker.isHost)
                NetworkSenderThread.Instance.SendPacketAsHostToAllClients(freqMsg, Steamworks.EP2PSend.k_EP2PSendUnreliable);
            else
                NetworkSenderThread.Instance.SendPacketToSpecificPlayer(Networker.hostID, freqMsg, Steamworks.EP2PSend.k_EP2PSendUnreliable);

            foreach (var play in PlayerManager.players)
            {
                if (steamIDtoFreq.ContainsKey(play.nameTag))
                {
                    if(steamIDtoFreq[play.nameTag] == 9999)
                    {
                        mutePlayer(play, play.nameTag, false);
                        FlightLogger.Log("command");
                    }
                    else
                    if (steamIDtoFreq[play.nameTag] != radioFreq)
                    {
                        mutePlayer(play,play.nameTag, true);
                    }
                    else
                    {
                        mutePlayer(play, play.nameTag, false);
                    }
                }
            }
        }
    }

    public static void mutePlayer(PlayerManager.Player play,string name, bool state)
    {
        if (!connectedToDiscord)
            return;
        var count = lobbyManager.MemberCount(lobbyID);
        for (int i = 0; i < count; i++)
        {
            var ids = lobbyManager.GetMemberUserId(lobbyID, i);

            if (play.discordID == ids)
            {
               
                if (userID != ids)
                {
                    if (state)
                    {
                         discord.GetVoiceManager().SetLocalVolume(ids, 0);
                        //discord.GetVoiceManager().SetLocalMute(discordid, true);
                    }

                    else
                    {
                    //    discord.GetVoiceManager().SetLocalMute(discordid, false);
                    discord.GetVoiceManager().SetLocalVolume(ids, 100);
                    }

                }
            }

        }
    }
    public static void makeLobby()
    {
        if (!connectedToDiscord)
            return;
        //lobbyManager = discord.GetLobbyManager();
        // Create the transaction
        var txn = lobbyManager.GetLobbyCreateTransaction();

        // Set lobby information
        txn.SetCapacity(50);
        txn.SetType(Discord.LobbyType.Public);
        txn.SetMetadata("VTOL MP", "In a Lobby");

        // Create it!
        lobbyManager.CreateLobby(txn, (Discord.Result result, ref Discord.Lobby lobby) =>
        {
            Console.WriteLine("lobby {0} created with secret {1}", lobby.Id, lobby.Secret);

            // We want to update the capacity of the lobby
            // So we get a new transaction for the lobby
            var newTxn = lobbyManager.GetLobbyUpdateTransaction(lobby.Id);
            newTxn.SetCapacity(50);
            lobbyManager.ConnectVoice(lobby.Id, (Discord.Result voiceResult) =>
            {

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
            //discord.GetVoiceManager().SetSelfDeaf(true);
            lobbyManager.UpdateLobby(lobby.Id, newTxn, (results) =>
            {
                Console.WriteLine("lobby updated");
            });
            UpdateActivity(discord, lobby);
        });

    }


}

