using System;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;


#if !UNITY_EDITOR
using Windows.Storage.Streams;
#endif

public class NewTcpClient : MonoBehaviour
{
    [SerializeField] public VertexCollector collector;
    [SerializeField] public int messageType;
    [SerializeField] public string message;

    [SerializeField] public string receivedMessage = "";

    [SerializeField] public string host;
    [SerializeField] public string port;
#if !UNITY_EDITOR
    Windows.Networking.Sockets.StreamSocket ClientSocket;
#endif
    public ClientEvent GUILog;

    [SerializeField] public string errorLog;

    List<Vector3> vertices;
    List<Vector3> normals;

    public void Connect()
    {
        var connectTask = Task.Run(() => ConnectAsync()); connectTask.Wait();
        GUILog.Invoke("Connected to server");        
    }

    public void SendMessage()
    {
        messageType = 1;
        var exchangeTask = Task.Run(() => ExchangeDataAsync());
    }

    public void SendPCD()
    {
        messageType = 2;
        vertices = collector.GetVertices();
        normals = collector.GetNormals();
        var exchangeTask = Task.Run(() => ExchangeDataAsync());
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
            PromptError(e.ToString());
        }
    }

    void PromptError(string error)
    {
        errorLog = error;
        errorPrompted = true;
    }

    public async Task ExchangeDataAsync()
    {   
        await SendDataAsync();
        await ReceiveDataAsync();
    }

    public async Task SendDataAsync()
    {
        try
        {
#if !UNITY_EDITOR
        using(var writer = new DataWriter(ClientSocket.OutputStream))
        {
            writer.ByteOrder = ByteOrder.BigEndian;
            writer.WriteInt64(messageType);
            switch(messageType)
            {
                case 1:
                    writer.WriteInt64(writer.MeasureString(message));
                    writer.WriteString(message);
                    break;
                case 2:                    
                    int intChunks = vertices.Count / 100;
                    int modulo = vertices.Count % 100;

                    writer.WriteInt64(intChunks);
                    writer.WriteInt64(modulo);
                    for(int i = 0; i < intChunks; i++)
                    {
                        var step = i*100; 
                        for (int j = 0; j < 100; j++)
                        {
                            writer.WriteDouble(Convert.ToDouble(vertices[step+j].x));
                            writer.WriteDouble(Convert.ToDouble(vertices[step+j].y));
                            writer.WriteDouble(Convert.ToDouble(vertices[step+j].z));
                            writer.WriteDouble(Convert.ToDouble( normals[step+j].x));
                            writer.WriteDouble(Convert.ToDouble( normals[step+j].y));
                            writer.WriteDouble(Convert.ToDouble( normals[step+j].z));
                        }
                        using(var stream = ClientSocket.InputStream.AsStreamForRead();)
                        {
                            byte[] buffer = new byte[1];

                        }
                    }
                    for (int j = 0; j < modulo; j++)
                    {
                        writer.WriteDouble(Convert.ToDouble(vertices[intChunks*100+j].x));
                        writer.WriteDouble(Convert.ToDouble(vertices[intChunks*100+j].y));
                        writer.WriteDouble(Convert.ToDouble(vertices[intChunks*100+j].z));
                        writer.WriteDouble(Convert.ToDouble( normals[intChunks*100+j].x));
                        writer.WriteDouble(Convert.ToDouble( normals[intChunks*100+j].y));
                        writer.WriteDouble(Convert.ToDouble( normals[intChunks*100+j].z));
                    }

                    /*
                    writer.WriteInt64(vertices.Count);
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        writer.WriteDouble(Convert.ToDouble(vertices[i].x));
                        writer.WriteDouble(Convert.ToDouble(vertices[i].y));
                        writer.WriteDouble(Convert.ToDouble(vertices[i].z));
                        writer.WriteDouble(Convert.ToDouble(normals[i].x));
                        writer.WriteDouble(Convert.ToDouble(normals[i].y));
                        writer.WriteDouble(Convert.ToDouble(normals[i].z));
                    }
                    */

                    break;
            }
            
            await writer.StoreAsync();
            await writer.FlushAsync();
            writer.DetachStream();
        }
#endif
        }
        catch (Exception e)
        {
            PromptError(e.ToString());
        }
    }

    
    bool messageReceived = false;

    public async Task ReceiveDataAsync()
    {
        try
        {
#if !UNITY_EDITOR
/*
            using(var reader = new DataReader(ClientSocket.InputStream))
            {
                await reader.LoadAsync(2048);

                var bytes = uint.Parse(reader.ReadString(64));
                receivedMessage = reader.ReadString(bytes);

                reader.DetachStream();

                messageReceived = true;
            }
*/
            var stream = ClientSocket.InputStream.AsStreamForRead();            
            byte[] buffer = new byte[64];

            await stream.ReadAsync(buffer, 0, 64);
            byte[] messageBuffer = new byte[int.Parse(System.Text.Encoding.UTF8.GetString(buffer))];
            await stream.ReadAsync(messageBuffer, 0, messageBuffer.Length);

            receivedMessage = System.Text.Encoding.UTF8.GetString(messageBuffer);
            messageReceived = true;
#endif
        }
        catch (Exception e)
        {            
            PromptError(e.ToString());           
        }
    }

    bool errorPrompted = false;
    private void Update()
    {
        if (errorPrompted)
        {
            GUILog.Invoke(errorLog);
            errorPrompted = false;
        }
        if (messageReceived)
        {
            messageReceived = false;
            GUILog.Invoke(receivedMessage);
        }
    }
    
    
}
