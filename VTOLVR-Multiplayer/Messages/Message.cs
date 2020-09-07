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

    private static int BUFFER_SIZE = 64 * 1024; //64kB

    public static byte[] Compress(byte[] inputData)
    {
        if (inputData == null)
            throw new ArgumentNullException("inputData must be non-null");

        using (var compressIntoMs = new MemoryStream())
        {
            using (var gzs = new BufferedStream(new GZipStream(compressIntoMs,
             CompressionMode.Compress), BUFFER_SIZE))
            {
                gzs.Write(inputData, 0, inputData.Length);
            }
            return compressIntoMs.ToArray();
        }
    }

    public static byte[] Decompress(byte[] inputData)
    {
        if (inputData == null)
            throw new ArgumentNullException("inputData must be non-null");

        using (var compressedMs = new MemoryStream(inputData))
        {
            using (var decompressedMs = new MemoryStream())
            {
                using (var gzs = new BufferedStream(new GZipStream(compressedMs,
                 CompressionMode.Decompress), BUFFER_SIZE))
                {
                    gzs.CopyTo(decompressedMs);
                }
                return decompressedMs.ToArray();
            }
        }
    }
}
[Serializable]
    public class MessageCompressedBatch : Message
    {
        public byte[] compressedData;
        [NonSerialized] public List<Packet> packets;
        [NonSerialized] static BinaryFormatter binaryFormatter = new BinaryFormatter();
        [NonSerialized] public byte[] uncompressedData;
    [NonSerialized] public long uncompressedSize;
    [NonSerialized] public long compressedSize;
    public void addMessage(Packet packet)
        {
        packets.Add(packet);
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
            packets = binaryFormatter.Deserialize(serializationStream) as List<Packet>;
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
        public MessageCompressedBatch() { packets = new List<Packet>(); this.type = MessageType.CompressedBatch; }

      
    }

