using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;


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
public class PacketCompressedBatch : Packet
{

    public byte[] compressedData;
    public int messagesNum;
    public int[] messagesSize = new int[20];

    [NonSerialized] public List<Message> messages;
    [NonSerialized] public List<byte> uncompressedData;
    [NonSerialized] public byte[] decomperessedBuffer;

    [NonSerialized] BinaryFormatter binaryFormatter;

    public void addMessage(Message msg)
    {

        messages.Add(msg);
        MemoryStream memoryStream = new MemoryStream();
        binaryFormatter.Serialize(memoryStream, msg);
        //UnityEngine.Debug.Log("buffer " + messagesNum);
        /*foreach( byte b in memoryStream.ToArray())
        {
            uncompressedData.Add(b);
        }*/
        uncompressedData.AddRange(memoryStream.ToArray());
        messagesSize[messagesNum] = (int)memoryStream.Length;
        //UnityEngine.Debug.Log("serlized size" + messagesSize[messagesNum]);
        messagesNum += 1;

        // UnityEngine.Debug.Log("uncompressedData size" + uncompressedData.Count);
    }
    public void prepareForSend()
    {
        // UnityEngine.Debug.Log("messagesNum " + messagesNum);
        //UnityEngine.Debug.Log("messagesNumlist " + messages.Count);
        CompressMessages();
    }

    public void prepareForRead()
    {
        binaryFormatter = new BinaryFormatter(); uncompressedData = new List<byte>();
        messages = new List<Message>();
        if (uncompressedData != null) uncompressedData.Clear();
        DeCompressMessages();
        /*for (int i = 0; i < messagesNum; i++)
        {
            List<byte> data = new List<byte>();
     
            for (int j = 0; j < messagesSize[i]; j++)
            {
                int index = (i * messagesNum) + j;
                data.Add(uncompressedData[index]);
                 
            }

            uncompressedDataArray.Add(data.ToArray());
        }*/


    }
    public void generateMessageList()
    {

        // UnityEngine.Debug.Log("post uncompressedData size" + decomperessedBuffer.Length);
        messages.Clear();
        // UnityEngine.Debug.Log("messagesNum " + messagesNum);
        int index = 0;
        for (int i = 0; i < messagesNum; i++)
        {


            MemoryStream serializationStream = new MemoryStream(decomperessedBuffer, index, messagesSize[i]);

            Message newMessage = binaryFormatter.Deserialize(serializationStream) as Message;
            if (newMessage != null)
                messages.Add(newMessage);

            index += messagesSize[i];
        }

    }

    public void CompressMessages()
    {
        compressedData = ByteArrayCompressionUtility.Compress(uncompressedData.ToArray());

    }
    public void DeCompressMessages()
    {

        decomperessedBuffer = ByteArrayCompressionUtility.Decompress(compressedData);

    }
    /*  


      public void serializeBatchMessages()
      {

      }
      public void deserializeBatchMessages()
      {

      }

      public void prepareForSend()
      {
          serializeBatchMessages();
          //CompressMessages();
      }
      public void prepareForRead()
      {
      //DeCompressMessages();
      deserializeBatchMessages();  
      }*/
    public PacketCompressedBatch()
    {
        messages = new List<Message>(); packetType = PacketType.Batch; messagesNum = 0; binaryFormatter = new BinaryFormatter(); uncompressedData = new List<byte>();
        messages = new List<Message>();
    }

}




