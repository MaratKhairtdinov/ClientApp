﻿using System;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Events;

#if !UNITY_EDITOR
using Windows.Storage.Streams;
#endif
public class TCPClient : MonoBehaviour
{
#region Unity Public Variables
#if !UNITY_EDITOR
    private Windows.Networking.Sockets.StreamSocket socket;
#endif
    public VertexCollector collector;
    public int chunkSize = 100;
    [SerializeField]
    private string hostAddress;
    [SerializeField]
    private string port;
    public string Host { get { return hostAddress; } set { hostAddress = value; } }
    public string Port { get { return port; } set { port = value; } }
    public ClientEvent OnClientNotifies;
    #endregion

#region Unity Main Thread Functions
    private void Awake()
    {
        NetworkDataHandler.client = this;
        NetworkDataHandler.InitializeNetworkPackages();
    }

    /*
     * Function from the main thread to be able to interact with game objects, and the common variables with the client thread
     */
    bool messagePrepared = false;
    string clientMessage;
    private void Update()
    {
        Listen();
        if (OnClientNotifies != null && messagePrepared)
        {
            messagePrepared = false;
            OnClientNotifies.Invoke(clientMessage);
        }
    }
    #endregion

#region Network Thread Functions


    public void PromptMessage(string message)
    {
        messagePrepared = true;
        clientMessage = message;
    }


    public void Connect()
    {
        Task connectTask = Task.Run(() => ConnectAsync()); connectTask.Wait();        
    }

    public async Task ConnectAsync()
    {
#if !UNITY_EDITOR
        try
        {
            socket = new Windows.Networking.Sockets.StreamSocket();
            Windows.Networking.HostName serverHost = new Windows.Networking.HostName(hostAddress);
            await socket.ConnectAsync(serverHost, port);
            PromptMessage("Connected to the server");
        }
        catch (Exception e)
        {
            PromptMessage(e.ToString());
        }
#endif
    }


    /*
     * Here are the shared variables of buffers and the data types sent over the net
     */
    protected byte[] inputBuffer, outputBuffer;
    private NetworkDataType outputMessageType, inputMessageType;
    private NetworkErrorType localErrorType = NetworkErrorType.NoError;
    private NetworkErrorType remoteErrorType;

    /*
     * Runs the async task of sending the data over the network,
     * Takes the shared byte[] outputBuffer and sends it, regardless what is there
     */
    public void SendData() 
    {
        var sendDataTask = Task.Run(() => SendDataAsync(outputBuffer, outputMessageType));
    }
    
    /*
     * Sends data asynchronously, takes byt[] as an argument along with the datatype being sent over the network, so that other side knows what to expect
     */

    private async Task<bool> SendDataAsync(byte[] data, NetworkDataType dataType)
    {
        try
        {
#if !UNITY_EDITOR
            using (var writer = new DataWriter(socket.OutputStream))
            {
                writer.ByteOrder = ByteOrder.BigEndian;
                writer.UnicodeEncoding = UnicodeEncoding.Utf8;
                writer.WriteInt16((short)dataType);
                writer.WriteInt64((long)data.Length);
                writer.WriteBytes(data);

                await writer.StoreAsync();
                await writer.FlushAsync();
                writer.DetachStream();
                outputBuffer = null;
            }
#endif
            return true;
        }
        catch(Exception e)
        {
            PromptMessage(e.ToString());
            return false;
        }
    }

    private async Task<bool> ReceiveDataAsync()
    {
        try
        {
#if !UNITY_EDITOR
            var stream = socket.InputStream.AsStreamForRead();
            var messageLengthBuffer = new byte[4];
            await stream.ReadAsync(messageLengthBuffer, 0, 4);

            int inputBufferLength = BitConverter.ToInt32(messageLengthBuffer, 0);
            inputBuffer = new byte[inputBufferLength];
            await stream.ReadAsync(inputBuffer,0, inputBufferLength);

            //NetworkDataHandler.HandleNetworkData(inputBuffer);
#endif
            return true;
        }
        catch (Exception e)
        {
            PromptMessage(e.ToString());
            return false;
        }
    }

    Task<bool> ReceiveDataTask;
    /*
     * Here is the initializer of the ReceiveData - thread
     * Has to be started from the beginning and each time, the new data is received, restarted
     */
    bool listening = false;
    private void Listen()
    {
        if (!listening)
        {
            listening = true;
            ReceiveDataTask = Task<bool>.Run(() => ReceiveDataAsync());
        }
        else
        {
            if (ReceiveDataTask.IsCompleted)
            {
                if (inputBuffer.Length == 0)
                {
                    OnClientNotifies.Invoke("ERROR! INPUT BUFFER IS EMPTY");
                    return;
                }
                NetworkDataHandler.HandleNetworkData(inputBuffer);
                inputBuffer = null;
                ReceiveDataTask = null;
                listening = false;
            }
            else
            {
                return;
            }
        }
    }
    #endregion

#region Pack the Data
    public void SendPointCloud()
    {
        List<Vector3> vertices = collector.GetVertices();
        List<Vector3> normals = collector.GetNormals();
        List<byte> buffer = new List<byte>();

        int step = chunkSize;
        int chunks = vertices.Count / step;

        for (int i = 0; i < chunks * step; i += step)
        {
            for (int j = i; j < i + step; j++)
            {
                buffer.AddRange(BitConverter.GetBytes(vertices[j].x)); buffer.AddRange(BitConverter.GetBytes(vertices[j].y)); buffer.AddRange(BitConverter.GetBytes(vertices[j].z));
                buffer.AddRange(BitConverter.GetBytes(normals[j].x)); buffer.AddRange(BitConverter.GetBytes(normals[j].y)); buffer.AddRange(BitConverter.GetBytes(normals[j].z));
            }
        }
        outputBuffer = buffer.ToArray(); outputMessageType = NetworkDataType.PointCloud;
        SendData();
    }
#endregion    
}
#region Public enums
/*
 *
 */
public enum NetworkDataType
{
    errorType = 0,
    String = 1,
    PointCloud = 2,
    Matrix = 3
}
public enum NetworkErrorType
{
    NoError = 0,
    DataCorrupt=1
}
#endregion

#region NetworkDataHandler
public class NetworkDataHandler
{
    public static TCPClient client;
    static NetworkDataType inputDataType;

    public delegate void Packet(byte[] data);
    private static Dictionary<int, Packet> packets;

    public static void InitializeNetworkPackages()
    {
        packets = new Dictionary<int, Packet>
        {
            { (int) NetworkDataType.errorType, HandleNetworkError},
            { (int) NetworkDataType.String, HandleString},
            { (int) NetworkDataType.Matrix, HandleMatrix}
        };
    }

    public static void HandleNetworkData(byte[]data)
    {
        var packetnum = BitConverter.ToInt16(data, 0);
        packets[packetnum].Invoke(data);
    }

    private static void HandleMatrix(byte[] data)
    {
        string inputString = BitConverter.ToString(data, 2);
        client.PromptMessage("Matrix Received");
    }

    private static void HandleString(byte[] data)
    {
        string inputString = BitConverter.ToString(data, 2);
        client.PromptMessage(inputString);
    }

    private static void HandleNetworkError(byte[] data)
    {
        int maxErrorCount = 5;
        int errorCounter = 1;
        switch ((NetworkErrorType)BitConverter.ToInt16(data, 2))
        {
            case NetworkErrorType.DataCorrupt:
                while (errorCounter < maxErrorCount)
                {
                    errorCounter+=1;
                    client.PromptMessage("Attempt #" + errorCounter);
                    /* Here, Client sends the data previously encoded into bytes only once,
                     * with the hope that encoding is correct and the data is lost only in the network, not while encoding
                     */
                    client.SendData();                    
                }
                break;
        }
    }
}
#endregion
