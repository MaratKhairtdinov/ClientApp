using System;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Networking;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Text;

#if !UNITY_EDITOR
using Windows.Storage.Streams;
#endif

public class TCPClient : MonoBehaviour
{   
    [SerializeField] public VertexCollector collector;    
    //[SerializeField] public string message;

    //[SerializeField] public string receivedMessage = "";

    string serverErrorLog = string.Empty;

    [SerializeField] public string host;
    [SerializeField] public string port;
#if !UNITY_EDITOR
    Windows.Networking.Sockets.StreamSocket ClientSocket;
#endif

    public int chunkSize = 1024;

    public ClientEvent GUILog;

    public ClientEvent OnHostSet;

    


    List<Vector3> vertices;
    List<Vector3> normals;

    private void Start()
    {
        if (OnHostSet != null)
        {
            OnHostSet.Invoke("Host: " + host);
        }
    }

    public void Connect()
    {
        var connectTask = Task.Run(() => ConnectAsync()); connectTask.Wait();
        NetworkHandler.InitializePackages();
        NetworkHandler.trials = 5;
        GUILog.Invoke("Connected to server");
    }

    public void SendString(string strg)
    {                
        SendData(Encoding.UTF8.GetBytes(strg), NetworkDataType.String);
    }   


    public async Task ConnectAsync()
    {
        try
        {
#if !UNITY_EDITOR
            ClientSocket = new Windows.Networking.Sockets.StreamSocket();
            Windows.Networking.HostName serverHost = new Windows.Networking.HostName(host);
            await ClientSocket.ConnectAsync(serverHost, port);
#endif
        }
        catch (Exception e)
        {
            PromptMessage(e.ToString());
        }
    }
    

    Task receiveTask;
    public void ReceiveData()
    {
        if (receiveTask != null)
        {
            if (receiveTask.IsCompleted)
            {
                receiveTask.Dispose();
                receiveTask = null;
                ReceiveData();
            }            
        }
        else
        {
            receiveTask = Task.Run(() => ReceiveDataAsync());
        }
    }

    Task sendTask;
    public void SendData(byte[] data, NetworkDataType type)
    {
        if (sendTask==null || sendTask.IsCompleted)
        {
            outputData = data; outputDataType = type;
            //sendTask = Task.Run(() => SendDataAsync());            
            sendTask = Task.Run(() => ExchangeDataAsync());
        }
    }

    public async Task ExchangeDataAsync()
    {
        await SendDataAsync();
        await ReceiveDataAsync();
    }


    byte[] outputData;
    NetworkDataType outputDataType;
    
    public async Task SendDataAsync()
    {
        try
        {
#if !UNITY_EDITOR
            using(var writer = new DataWriter(ClientSocket.OutputStream))
            {
                writer.ByteOrder = ByteOrder.BigEndian;
                writer.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;

                int chunks = outputData.Length/chunkSize;
                int residual = outputData.Length%chunkSize;                


                /*
                 *Header of the package so that the other side knows how much and what kind of data to receive
                 */
                writer.WriteInt16((short)outputDataType);
                writer.WriteInt32(chunkSize);
                writer.WriteInt32(chunks);
                writer.WriteInt32(residual);

                var outputBuffer = new List<byte>(); outputBuffer.AddRange(outputData);

                for(int i = 0; i < chunks; i+=chunkSize)
                {
                    writer.WriteBytes(outputBuffer.GetRange(i, chunkSize).ToArray());
                    
                    //Thread.Sleep(5);
                }

                writer.WriteBytes(outputBuffer.GetRange(chunks*chunkSize, residual).ToArray());

                NetworkHandler.SaveLastPackage(outputData, outputDataType);

                await writer.StoreAsync();
                await writer.FlushAsync();
                writer.DetachStream();

                /*
                switch(messageType)
                {
                    case 1:
                        writer.WriteInt64(writer.MeasureString(message));
                        writer.WriteString(message);
                        break;
                    case 2:
                        int step = 100;
                        int chunks = vertices.Count/step;
                        writer.WriteInt64(chunks);
                        writer.WriteInt64(step);
                        for(int i = 0; i < chunks*step; i+=step)
                        {
                            for(int j = i; j<i+step; j++)
                            {
                                writer.WriteDouble((double)vertices[j].x); writer.WriteDouble((double)vertices[j].y); writer.WriteDouble((double)vertices[j].z);
                                writer.WriteDouble((double)normals[j].x);  writer.WriteDouble((double)normals[j].y);  writer.WriteDouble((double)normals[j].z);
                            }
                            //PromptMessage(string.Format("Chunk #{0} sent", i/step));
                        }
                        break;
                }            
                */
            }
#endif
        }
        catch (Exception e)
        {
            PromptMessage(e.ToString());            
        }
    }



    NetworkDataType inputType;
    byte[] inputBuffer;
    public async Task ReceiveDataAsync()
    {        
        try
        {
#if !UNITY_EDITOR
            var stream = ClientSocket.InputStream.AsStreamForRead();
            
            byte[] inputDataTypeBuffer = new byte[2];
            byte[] inputLengthBuffer = new byte[8];
            await stream.ReadAsync(inputDataTypeBuffer, 0, 2);
            await stream.ReadAsync(inputLengthBuffer, 0, 8);
            
            inputBuffer = new byte[BitConverter.ToInt64(inputLengthBuffer, 8)];
            await stream.ReadAsync(inputBuffer, 0, inputBuffer.Length);
            NetworkHandler.HandleData(inputBuffer, inputType);
#endif
        }
        catch (Exception e)
        {
            PromptMessage(e.ToString());
        }        
    }

    bool messagePrompted = false;
    private string message;
    private void Update()
    {
        if (messagePrompted)
        {
            GUILog.Invoke(message);
            messagePrompted = false;
        }

    }

    public void PromptMessage(string message)
    {
        this.message = message;
        messagePrompted = true;
    }

}
[Serializable]
public class ClientEvent : UnityEvent<string> { }


public class NetworkHandler
{
    public static TCPClient client;
    public delegate void Packet(byte[] data);
    public static Dictionary<int, Packet> handlers;
    public static int trials;

    private static byte[] lastBuffSent;
    private static NetworkDataType lastDataTypeSent;

    private static bool lastPackageSentSaved = false;
    public static void SaveLastPackage(byte[] data, NetworkDataType type)
    { 
        lastBuffSent = data; lastDataTypeSent = type; lastPackageSentSaved = true;
    }

    public static void InitializePackages()
    {
        handlers = new Dictionary<int, Packet>()
        {
            {(int) NetworkDataType.Response,  HandleResponse},
            {(int) NetworkDataType.String, HandleString}
        };
    }

    public static void HandleData(byte[] data, NetworkDataType type)
    {
        handlers[(int)type].Invoke(data);
    }

    public static void HandleString(byte[] data)
    {
        client.PromptMessage(Encoding.UTF8.GetString(data, 0, data.Length));
    }

    public static void HandleResponse(byte[] data)
    {
        var response = (NetworkResponseType)BitConverter.ToInt16(data,0);
        switch (response)
        {
            case NetworkResponseType.AllGood:
                client.PromptMessage("Server received data");
                break;
            case NetworkResponseType.DataCorrupt:
                for(int i = 0; i<trials; i++)
                {
                    client.SendData(lastBuffSent, lastDataTypeSent);
                }
                break;
        }
    }

    
}

public enum NetworkDataType
{
    Response = 0,
    String = 1
}

public enum NetworkResponseType
{
    AllGood = 0,
    DataCorrupt = 1
}