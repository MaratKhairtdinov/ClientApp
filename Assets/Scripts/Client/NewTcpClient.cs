using System;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;


#if !UNITY_EDITOR
using Windows.Storage.Streams;
#endif

public class NewTcpClient : MonoBehaviour
{
    [SerializeField] public string messageType;
    [SerializeField] public string message;

    [SerializeField] public string receivedMessage = "";

    [SerializeField] public string host;
    [SerializeField] public string port;
#if !UNITY_EDITOR
    Windows.Networking.Sockets.StreamSocket ClientSocket;
#endif
    public ClientEvent GUILog;

    [SerializeField] public string errorLog;


    public void Start()
    {
        var connectTask = Task.Run(() => ConnectAsync()); connectTask.Wait();
        GUILog.Invoke("Connected to server");        
    }

    public void SendData()
    {
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
        await SendDataAsync(messageType);
        await SendDataAsync(message);
        await ReceiveDataAsync();
    }

    public async Task SendDataAsync(string data)
    {
        try
        {
#if !UNITY_EDITOR
        using(var writer = new DataWriter(ClientSocket.OutputStream))
        {
            writer.WriteString(writer.MeasureString(data).ToString().PadRight(64,' '));
            writer.WriteString(data);
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
            //GUILog.Invoke(errorLog);
            //errorPrompted = false;
        }
        if (messageReceived)
        {
            messageReceived = false;
            GUILog.Invoke(receivedMessage);
        }
    }
    
    
}
