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
        NetworkHandler.client = this;
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
                    
                    Thread.Sleep(5);
                }

                writer.WriteBytes(outputBuffer.GetRange(chunks*chunkSize, residual).ToArray());

                NetworkHandler.SaveLastPackage(outputData, outputDataType);

                await writer.StoreAsync();
                await writer.FlushAsync();
                writer.DetachStream();
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
        string errorLog = string.Empty;

#if !UNITY_EDITOR

        var stream = ClientSocket.InputStream.AsStreamForRead();
            
        byte[] inputDataTypeBuffer = new byte[2];
        byte[] inputLengthBuffer = new byte[4];
        try
        {            
            await stream.ReadAsync(inputDataTypeBuffer, 0, 2);  
            inputType = (NetworkDataType)BitConverter.ToInt16(inputDataTypeBuffer, 0);
            errorLog += "message of type "+inputType+" received\n";
            //PromptMessage("message of type "+inputType+" received");
        }
        catch(Exception e)
        {
            errorLog +="InputDataType isn't received\n";
            //PromptMessage(errorLog);
            //return;
        }
        try
        {
            await stream.ReadAsync(inputLengthBuffer, 0, 4);
        }
        catch(Exception e)
        {
            errorLog +="InputDataLength isn't received\n";            
            //PromptMessage(errorLog);
            //return;
        }            
        try
        {
            var inputLength = BitConverter.ToInt32(inputLengthBuffer, 0);
            inputBuffer = new byte[inputLength];
            errorLog += "Input buffer is " + inputLength +" bytes long\n";
            //PromptMessage("Input buffer is " + inputLength +" bytes long");
        }
        catch(Exception e)
        {
            errorLog += e.ToString();
            //PromptMessage(errorLog);
            //return;
        }
        try
        {
            await stream.ReadAsync(inputBuffer, 0, inputBuffer.Length);
        }
        catch(Exception e)
        {
            errorLog +="Couldnt receive buffer";
            //PromptMessage(errorLog);
            //return;
        }
        try
        {
            var response = (NetworkResponseType)BitConverter.ToInt16(inputBuffer,0);
            errorLog+="Response is of type "+response+"\n";
            NetworkHandler.HandleData(inputBuffer, inputType);
        }
        catch(Exception e)
        {
            errorLog +="Couldn't handle data";
            //PromptMessage(errorLog);
            //return;
        }
#endif
        //PromptMessage(errorLog);
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
    public static int trials = 5;

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
        //handlers[(int)type].Invoke(data);
        switch (type)
        {
            case NetworkDataType.Response:
                HandleResponse(data);
                break;
            case NetworkDataType.String:
                HandleString(data);
                break;
        }
    }

    public static void HandleString(byte[] data)
    {
        client.PromptMessage(Encoding.UTF8.GetString(data, 0, data.Length));
    }


    static int counter = 0;
    public static void HandleResponse(byte[] data)
    {
        var response = (NetworkResponseType)BitConverter.ToInt16(data,0);
        switch (response)
        {
            case NetworkResponseType.AllGood:
                client.PromptMessage("Server received data");
                break;
            case NetworkResponseType.DataCorrupt:
                //for(int i = 0; i<trials; i++)
                //{
                if (counter<trials) 
                {
                    //client.SendData(lastBuffSent, lastDataTypeSent); 
                }
                else
                {
                    counter = 0;
                    //client.PromptMessage("Maximal amount of trials reached, some problem is in the network");
                }
                //}
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