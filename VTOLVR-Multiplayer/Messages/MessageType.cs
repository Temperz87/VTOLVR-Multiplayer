/// <summary>
/// This is the type of message which has been sent with the packet,
/// this is so that we know what class to convert it to.
/// </summary>
public enum MessageType
{
    None,
    JoinRequest, //When the client asks if they can join
    JoinRequest_Result, //Responce from the host if the client can join
    Ready, //Clients telling the host that they are ready
    Ready_Result, //Host telling everyone we are starting
    RequestSpawn, //Requesting a location to spawn at to the host
    RequestSpawn_Result, //The Result of the host sending to client where they can spawn
    SpawnVehicle, //When someone is telling everyone to spawn a new vehicle on their game
    RigidbodyUpdate, //When a RigidbodyNetworker is updating
}