# VTOL VR Multiplayer Mod
[![Discord](https://img.shields.io/discord/597153468834119710?label=VTOL%20VR%20Modding&logo=discord&style=flat-square)](https://discord.gg/XZeeafp "Discord Invite") [![](https://img.shields.io/badge/Steam-Networking-lightgrey?style=flat-square)](https://partner.steamgames.com/doc/api/ISteamNetworking "Steam Networking Docs") [![](https://img.shields.io/badge/Steamworks-.NET-blue?style=flat-square)](https://steamworks.github.io/installation/ "Steamworks C# Wrapper")

This is the repository for the modded multiplayer in VTOL VR. The multiplayer is currently separate from the mod loader itself but once it is at a playable state, it will be merged in with the mod loader. 

The mod uses the steams networking library meaning players don't need to install additional dependencies to play multiplayer, just the mod loader and multiplayer mod is needed.

## Installation
The mod can be downloaded from [here](https://vtolvr-mods.com/mod/qs6jxkt2/) on the VTOLVR-Mods website, and that is recommended unless you want to make changes or build the dll yourself. However, to build the dll you first will need to clone the repository onto your computer. Once you have cloned the repository, you will need to head over to your VTOL VR games directory and copy the listed dlls from the `Steam\steamapps\common\VTOL VR\VTOLVR_Data\Managed` folder to the `Dependencies` folder. The list of what you need can be found inside of the `Dependencies` folder.

You also need to have .Net 4.5 installed to build the mod.

## Contributors

[Ketkev](https://github.com/ketkev "Ketkev's Github") for their code which creates a name tag above the player's heads with their steam name.

[MarshMellow0](https://github.com/MarshMello0 "MarshMello0's Github") for making the foundation of the multiplayer mod, so others can finish the fight.

[Dib](https://github.com/Nisenogen "Dib's Github") for being someone we can ask general C# questions about, and casually threading the networker class.

[THEGREATOVERLORDLORDOFALLCHEESE](https://github.com/THE-GREAT-OVERLORD-LORD-OF-ALL-CHEESE "Cheese's Github") for syncing features of the aircraft such as wingfold, and making the small details work, along with the aircraft carrier.

[mrdoctorsurgeon](https://github.com/omarehaly "surgeon's Github") for understanding how objectives work in game and helping us out with making it happen, he's the only reason objectives work.

[nebriv](https://github.com/nebriv "Nebriv's Github") for making the UI look pretty and general things with mods and hashes.

[Zaelix](https://github.com/Zaelix "Zaelix's Github") for also helping with the UI.

Contributions are welcomed, if you would like to help out with creating multiplayer, fork the project, add your code then create a pull request.

## Useful Links

- [Steam Networking Documentation](https://partner.steamgames.com/doc/api/ISteamNetworking "https://partner.steamgames.com/doc/api/ISteamNetworking")
- [Modding Discord](https://discord.gg/XZeeafp "https://discord.gg/XZeeafp")


Nebriv was here :)
