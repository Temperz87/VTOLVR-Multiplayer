using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using System.IO.Compression;
[Serializable]
public class Message
{
    public Message() { }
    public Message(MessageType type) { this.type = type; }
    /// <summary>
    /// The type of message we are sending, this needs to be set 
    /// otherwise we won't know what to convert the message to.
    /// </summary>
    public MessageType type;
}
public static class ByteArrayCompressionUtility
{
     

    public static byte[] Compress(byte[] data)
    {
        using (var compressedStream = new MemoryStream())
        using (var zipStream = new DeflateStream(compressedStream, CompressionMode.Compress))
        {
            zipStream.Write(data, 0, data.Length);
            zipStream.Close();
            return compressedStream.ToArray();
        }

    }
    public static byte[] Decompress(byte[] data)
    {
        using (var compressedStream = new MemoryStream(data))
        using (var zipStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
        using (var resultStream = new MemoryStream())
        {
            zipStream.CopyTo(resultStream);
            return resultStream.ToArray();
        }
    }
     
}
[Serializable]
    public class MessageCompressedBatch : Message
    {
        public byte[] compressedData;
        [NonSerialized] public List<Message> packets;
        [NonSerialized] static BinaryFormatter binaryFormatter = new BinaryFormatter();
        [NonSerialized] public byte[] uncompressedData;
        [NonSerialized] public long uncompressedSize;
        [NonSerialized] public long compressedSize;
    public void addMessage(Message msg)
        {
        packets.Add(msg);

        }
        
    public List<Message> getHalfSizePart1()
    {
        return packets.GetRange(0, (packets.Count / 2)-1);
    }

    public List<Message> getHalfSizePart2()
    {
        return packets.GetRange((packets.Count / 2), packets.Count-1);
    }
    public void CompressMessages()
        {
            compressedData = ByteArrayCompressionUtility.Compress(uncompressedData);
        
        }
        public void DeCompressMessages()
        {
         
        uncompressedData = ByteArrayCompressionUtility.Decompress(compressedData);
        
         }

        public void serializeBatchMessages()
        {
            MemoryStream memoryStream = new MemoryStream();
            binaryFormatter.Serialize(memoryStream, packets);
            uncompressedData = memoryStream.ToArray();
        }
        public void deserializeBatchMessages()
        {
            MemoryStream serializationStream = new MemoryStream(uncompressedData);
            packets = binaryFormatter.Deserialize(serializationStream) as List<Message>;
        }

        public void prepareForSend()
        {
            serializeBatchMessages();
            CompressMessages();
        }
        public void prepareForRead()
        {
        DeCompressMessages();
        deserializeBatchMessages();  
        }
        public MessageCompressedBatch() { packets = new List<Message>(); this.type = MessageType.CompressedBatch; }

      
    }

