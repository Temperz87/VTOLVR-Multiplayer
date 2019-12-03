public enum MessageType
{
    None,
    JoinRequest, //When the client asks if they can join
    JoinRequest_Result, //Responce from the host if the client can join
    Ready, //Clients telling the host that they are ready
    Ready_Result, //Host telling everyone we are starting
    RequestSpawn, //Requesting a location to spawn at to the host
    RequestSpawn_Result, //The Result of the host sending to client where they can spawn
    SpawnVehicle
}