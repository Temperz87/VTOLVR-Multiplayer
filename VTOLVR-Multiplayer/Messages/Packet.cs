using Steamworks;
using System;

    public enum PacketType { Single,Multiple}
    [Serializable]
    public class Packet
    {
        public PacketType packetType;
        public EP2PSend sendType;
        public ulong networkUID = 0;
    }
    [Serializable]
    public class PacketSingle : Packet
    {
        public Message message;
        public PacketSingle(Message message, EP2PSend sendType)
        {
            this.message = message;
            this.sendType = sendType;
            packetType = PacketType.Single;
        }
    }
    [Serializable]
    public class PacketMultiple : Packet
    {
        public Message[] messages;

        public PacketMultiple(Message[] messages, EP2PSend sendType)
        {
            this.messages = messages;
            this.sendType = sendType;
            packetType = PacketType.Multiple;
        }
    }
