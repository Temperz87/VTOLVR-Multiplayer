/// <summary>
/// This is the type of message which has been sent with the packet,
/// this is so that we know what class to convert it to.
/// </summary>
public enum MessageType
{
    None,
    LobbyInfoRequest, //When a client wants to know the info about a lobby to display
    LobbyInfoRequest_Result,//The information about the lobby
    JoinRequest, //When the client asks if they can join
    JoinRequestAccepted_Result, //Responce from the host if the client can join
    JoinRequestRejected_Result, //Responce from the host if the client is rejected
    Ready, //Clients telling the host that they are ready
    AllPlayersReady, //Host telling everyone we are starting
    LoadingTextUpdate, //To Update the loading text to show who is ready.
    RequestSpawn, //Requesting a location to spawn at to the host
    RequestSpawn_Result, //The Result of the host sending to client where they can spawn
    SpawnPlayerVehicle, //When someone is telling everyone to spawn a new vehicle on their game
    SpawnAiVehicle, // When the host is telling the clients about an AI vehicle
    RequestAllCurrrentPlayers, //When someone joins the host needs to send them all the current players in the game.
    RigidbodyUpdate, //When a RigidbodyNetworker is updating
    PlaneUpdate, //This is when the base plane script is updating
    EngineTiltUpdate, //This is the angle of an engine when its tilted
    RequestNetworkUID, //A client wants a ID for one of it's object which no one else has.
    Disconnecting, //When a user is disconnecting from the server
    WeaponsSet, //This is when the host asks someone what their current weapons are for a new guy
    WeaponsSet_Result, //This is the weapons of the person who we asked
    WeaponFiring, //This is saying that the weapon is now firing on this vehicle
    WeaponStoppedFiring, //This is when they have finished firing
    MissileUpdate, //This is when a missile is updating its state across the network.
    FireCountermeasure, //This is when a player fires a countermeasure
    Death, //This is when a player dies
    Respawn, //This is when a player respawns
    HostLoaded, // This is when the host has loaded and the clients can load
    WingFold, //this is when a player folds or unfolds their wings
    ActorSync, // This updates actors
    WorldData, // This is timescale sync data
    ExtLight, //this is when a player changes their external lights
    ShipUpdate, //this is when a player changes their external lights
    RadarUpdate, //this is when the radar is turned on or off, or the fov is changed, make a messsage called LockingRadarUpdate in the future to deal with locks
    LockingRadarUpdate, //See above nerd
    TurretUpdate, //This is turret aiming data
    JettisonUpdate, // Used when weapons are jettisoned
    ServerHeartbeat, // Ping
    ServerHeartbeat_Response, // Pong
    ServerReportingPingTime, // Ping
    LoadingTextRequest, //Clients request for loading text
    ObjectiveSync, // To sync objectives
    ScenarioAction, // To sync scenario actions which have their hand in objectives
    SamUpdate, // To sync sams
    AAAUpdate, // To sync A's x 3
    BulletHit, // Obvious
    MissileDamage // Obvious
    //AWACSComms, // To update awacs comms state
    //AWACSCommsRequest // To request the initial state
}