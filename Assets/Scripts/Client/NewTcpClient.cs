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

    public ClientEvent OnHostSet;

    [SerializeField] public string errorLog;

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
                writer.UnicodeEncoding = UnicodeEncoding.Utf8;
                writer.WriteInt64(messageType);
                //writer.StoreAsync();
                //writer.FlushAsync();
                switch(messageType)
                {
                    case 1:
                        writer.WriteInt64(writer.MeasureString(message));
                        writer.WriteString(message);
                        //writer.StoreAsync();
                        //writer.FlushAsync();
                        break;
                    case 2:
                        int step = 100;
                        int chunks = vertices.Count/step;
                        writer.WriteInt64(chunks);
                        writer.WriteInt64(step);
                        //writer.StoreAsync();
                        //writer.FlushAsync();
                        for(int i = 0; i < chunks*step; i+=step)
                        {
                            for(int j = i; j<i+step; j++)
                            {
                                writer.WriteDouble((double)vertices[j].x); writer.WriteDouble((double)vertices[j].y); writer.WriteDouble((double)vertices[j].z);
                                writer.WriteDouble((double)normals[j].x);  writer.WriteDouble((double)normals[j].y);  writer.WriteDouble((double)normals[j].z);
                            }
                            //writer.StoreAsync();
                            //writer.FlushAsync();
                            PromptError(string.Format("Chunk #{0} sent", i/step));
                        }
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
