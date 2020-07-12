# VTOL VR Multiplayer Mod
[![Discord](https://img.shields.io/discord/597153468834119710?label=VTOL%20VR%20Modding&logo=discord&style=flat-square)](https://discord.gg/XZeeafp "Discord Invite") [![](https://img.shields.io/badge/Steam-Networking-lightgrey?style=flat-square)](https://partner.steamgames.com/doc/api/ISteamNetworking "Steam Networking Docs") [![](https://img.shields.io/badge/Steamworks-.NET-blue?style=flat-square)](https://steamworks.github.io/installation/ "Steamworks C# Wrapper")

This is the repository for the modded multiplayer in VTOL VR. The multiplayer is currently sperate from the mod loader its self but once it is at a playable state, it will be merged in with the mod loader. 

The mod uses steams networking library meaning players don't need to install additional dependencies to play multiplayer, just the mod loader and multiplayer mod is needed.

## Installation
Once you have cloned the repository, you will need to head over to your VTOL VR games directory and copy the listed dlls from the `Steam\steamapps\common\VTOL VR\VTOLVR_Data\Managed` folder to the `Dependencies` folder. The list of what you need can be found inside of the `Dependencies` folder.

You also need to have .net 4.5 installed to build the mod.

To get pasted the super basic tester check, you just need to build the mod in debug mode, this should go past the check I put in place just to stop people sharing the dll with their friends and then complaining that things are buggy or keep asking how to use it. 

## Contributors

[Ketkev](https://github.com/ketkev "Ketkev's Github") for their code which creates a name tag above the player's heads with their steam name.
[MarshMellow0](https://github.com/MarshMello0 "MarshMello0's Github") for making the foundation of the multiplayer mod, so others can finish the fight.
Contributions are welcomed, if you would like to help out with creating multiplayer, fork the project, add your code then create a pull request.

## Useful Links

- [Steam Networking Documentation](https://partner.steamgames.com/doc/api/ISteamNetworking "https://partner.steamgames.com/doc/api/ISteamNetworking")
- [Modding Discord](https://discord.gg/XZeeafp "https://discord.gg/XZeeafp")