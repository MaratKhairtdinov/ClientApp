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
using System.Linq;

#if !UNITY_EDITOR
using Windows.Storage.Streams;
#endif

public class TCPClient : MonoBehaviour
{   
    [SerializeField] public VertexCollector collector;        

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
        NetworkHandler.client = this;
    }

    public void Connect()
    {
        var connectTask = Task.Run(() => ConnectAsync()); connectTask.Wait();
        NetworkHandler.InitializePackages();
        NetworkHandler.trials = 5;
        GUILog.Invoke("Connected to server");
        SendString("Hello server");
    }

    public void SendString(string strg)
    {   
        SendData(Encoding.UTF8.GetBytes(strg).ToList<byte>(), NetworkDataType.String);
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
            NotifyMainThread(0, e.ToString(), 0);
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
    public void SendData(List<byte> data, NetworkDataType type)
    {
        //if (sendTask==null || sendTask.IsCompleted)
        //{
            outputData = data; outputDataType = type;
            sendTask = Task.Run(() => ExchangeDataAsync());
        //}
    }

    public void SendPointCloud()
    {
        var vertices = collector.GetVertices();
        var normals  = collector.GetNormals();
        List<byte> data = new List<byte>();
        data.AddRange(BitConverter.GetBytes(vertices.Count));
        for (int i = 0; i < vertices.Count; i++)
        {
            data.AddRange(BitConverter.GetBytes(vertices[i].x)); data.AddRange(BitConverter.GetBytes(vertices[i].y)); data.AddRange(BitConverter.GetBytes(vertices[i].z));
            data.AddRange(BitConverter.GetBytes(normals[i].x));  data.AddRange(BitConverter.GetBytes(normals[i].y));  data.AddRange(BitConverter.GetBytes(normals[i].z));
        }
        
        SendData(data, NetworkDataType.PointCloud);
        NotifyMainThread(0, "Point cloud prepared to send over the network", 0);
    }

    public async Task ExchangeDataAsync()
    {
        await SendDataAsync();        
        await ReceiveDataAsync();
    }

    List<byte> outputData;
    NetworkDataType outputDataType;
    byte[] chunk;
    int chunks, residual;
    int chunksSent = 0; int chunksSentPrev = 0;
    public DataTransferEvent onDataSent;
    

    public async Task SendDataAsync()
    {
        try
        {
#if !UNITY_EDITOR
            using(var writer = new DataWriter(ClientSocket.OutputStream))
            {
                writer.ByteOrder = ByteOrder.BigEndian;
                writer.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;

                chunks = outputData.Count/chunkSize;
                residual = outputData.Count%chunkSize;

                /*
                 *Header of the package so that the other side knows how much and what kind of data to receive
                 */
                writer.WriteInt16((short)outputDataType);
                writer.WriteInt32(chunkSize);
                writer.WriteInt32(chunks);
                writer.WriteInt32(residual);

                var stream = ClientSocket.InputStream.AsStreamForRead();
                NetworkHandler.SaveLastPackage(outputData, outputDataType);
                byte[] response_buffer = new byte[2];
                NetworkResponseType response;

                int percentage = 0;

                for(int i = 0; i < chunks; i++)
                {
                    bool chunk_received = false;
                    response = NetworkResponseType.DataCorrupt;
                    while(response == NetworkResponseType.DataCorrupt)
                    {
                        writer.WriteBytes(outputData.GetRange(i*chunkSize, chunkSize).ToArray());
                        await writer.StoreAsync();
                        await writer.FlushAsync();
                        await stream.ReadAsync(response_buffer, 0, 2);
                        stream.Flush();
                        response = (NetworkResponseType)BitConverter.ToInt16(response_buffer, 0);

                    }                    
                    percentage = ((int)(((float)(i+1))/((float)chunks)*100));
                    NotifyMainThread(percentage, string.Format("Sending chunk #{0} of {1}",i+1, chunks), 0f);
                }
                response = NetworkResponseType.DataCorrupt;
                while(response == NetworkResponseType.DataCorrupt)
                {
                    writer.WriteBytes(outputData.GetRange(chunks*chunkSize, residual).ToArray());
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                    await stream.ReadAsync(response_buffer, 0, 2);
                    response = (NetworkResponseType)BitConverter.ToInt16(response_buffer, 0);
                }
                outputData.Clear();
                writer.DetachStream();                
            }
#endif
        }
        catch (Exception e)
        {
            NotifyMainThread(0, e.ToString(), 0);
        }
    }



    NetworkDataType inputType;
    byte[] inputBuffer;
    public async Task ReceiveDataAsync()
    {
#if !UNITY_EDITOR
        var stream = ClientSocket.InputStream.AsStreamForRead();

        byte[] inputDataTypeBuffer = new byte[2];
        byte[] inputLengthBuffer = new byte[4];
     
        await stream.ReadAsync(inputDataTypeBuffer, 0, 2);
        inputType = (NetworkDataType)BitConverter.ToInt16(inputDataTypeBuffer, 0);
        await stream.ReadAsync(inputLengthBuffer, 0, 4);
        var inputLength = BitConverter.ToInt32(inputLengthBuffer, 0);
        inputBuffer = new byte[inputLength];
        await stream.ReadAsync(inputBuffer, 0, inputBuffer.Length);
        NetworkHandler.HandleData(inputBuffer, inputType);
#endif
    }
    private string mainThreadString;
    private int mainThreadInt;
    private float mainThreadFloat;
    bool mainThreadNotified = false;
    public void NotifyMainThread(int intgr, string strg, float flt)
    {
        mainThreadNotified = true;
        mainThreadString = strg; mainThreadInt = intgr; mainThreadFloat = flt;
    }


    
    bool messagePrompted = false;
    private string message;
    private void Update()
    {
        if (mainThreadNotified)
        {
            mainThreadNotified = false;

            if (mainThreadInt > 0)
            {
                onDataSent.Invoke(mainThreadInt);
            }
            if (mainThreadString.Length > 0)
            {
                GUILog.Invoke(mainThreadString);
            }
            if (mainThreadFloat > 0)
            {
                onDataSent.Invoke((int)mainThreadFloat);
            }
        }
        if (messagePrompted)
        {            
            GUILog.Invoke(message);
            messagePrompted = false;
        }
    }

    

}
[Serializable]
public class ClientEvent : UnityEvent<string> { }
[Serializable]
public class DataTransferEvent : UnityEvent<int> { }


public class NetworkHandler
{
    public static TCPClient client;
    public delegate void Packet(byte[] data);
    public static Dictionary<int, Packet> handlers;
    public static int trials = 5;

    private static List<byte> lastBuffSent;
    private static NetworkDataType lastDataTypeSent;

    private static bool lastPackageSentSaved = false;

    public static byte[] LastChunk
    {
        get ; set;
    }

    public static void SaveLastPackage(List<byte> data, NetworkDataType type)
    {
        if (!lastPackageSentSaved)
        {
            lastBuffSent = data; lastDataTypeSent = type; lastPackageSentSaved = true;
        }
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
        client.NotifyMainThread(0,Encoding.UTF8.GetString(data, 0, data.Length), 0);
    }


    static int counter = 0;
    public static void HandleResponse(byte[] data)
    {
        var response = (NetworkResponseType)BitConverter.ToInt16(data,0);
        switch (response)
        {
            case NetworkResponseType.AllGood:
                client.NotifyMainThread(0, "Server received data", 0);
                break;
            case NetworkResponseType.DataCorrupt:
                if (counter<trials)
                {
                    counter += 1;
                    client.SendData(lastBuffSent, lastDataTypeSent); 
                }
                else
                {
                    counter = 0;
                    client.NotifyMainThread(0, "Maximal amount of trials reached, some problem is in the network", 0);
                }                
                break;
        }
    }
}

public enum NetworkDataType
{
    Response = 0,
    String = 1,
    PointCloud = 2
}

public enum NetworkResponseType
{
    AllGood = 0,
    DataCorrupt = 1
}