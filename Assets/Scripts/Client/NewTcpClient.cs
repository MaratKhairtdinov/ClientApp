using System;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Events;

#if !UNITY_EDITOR
using Windows.Storage.Streams;
#endif

public class NewTcpClient : MonoBehaviour
{   
    [SerializeField] public VertexCollector collector;
    [SerializeField] public int messageType;
    [SerializeField] public string message;

    [SerializeField] public string receivedMessage = "";

    string serverErrorLog = string.Empty;

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
            PromptMessage(e.ToString());
        }
    }

    void PromptMessage(string message)
    {
        errorLog = message;
        errorPrompted = true;
    }

    public async Task ExchangeDataAsync()
    {
        int counter = 0;
        while (counter < 5)
        {
            counter += 1;
            await SendDataAsync();
            serverErrorLog = await ReceiveDataAsync();
            PromptMessage("Sending the data, attempt #" + counter);
            if (serverErrorLog.Length == 0)
            {
                break;
            }
        }
        PromptMessage(await ReceiveDataAsync());
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

    

    public async Task<string> ReceiveDataAsync()
    {
        string toReturn = string.Empty;
        try
        {
#if !UNITY_EDITOR
            var stream = ClientSocket.InputStream.AsStreamForRead();
            byte[] buffer = new byte[64];

            await stream.ReadAsync(buffer, 0, 64);
            byte[] messageBuffer = new byte[int.Parse(System.Text.Encoding.UTF8.GetString(buffer))];
            await stream.ReadAsync(messageBuffer, 0, messageBuffer.Length);

            toReturn = System.Text.Encoding.UTF8.GetString(messageBuffer);            
#endif
        }
        catch (Exception e)
        {
            PromptMessage(e.ToString());           
        }
        return toReturn;
    }

    bool errorPrompted = false;
    bool messagePrompted = false;
    private void Update()
    {
        if (errorPrompted)
        {
            GUILog.Invoke(errorLog);
            errorPrompted = false;
        }
        if (receivedMessage.Length!=0 && !messagePrompted)
        {
            messagePrompted = true;
            GUILog.Invoke(receivedMessage);
        }
    }
    
}
[Serializable]
public class ClientEvent : UnityEvent<string> { }
