using System;

    [Serializable]
    public class Message
    {
        public Message() { }
        public Message(MessageType type) { this.type = type; }
        public MessageType type;
    }
