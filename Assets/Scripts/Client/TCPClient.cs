using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

#if !UNITY_EDITOR
using System.Threading.Tasks;
using Windows.Storage.Streams;
#endif

public class TCPClient : MonoBehaviour
{

#if !UNITY_EDITOR
    private bool _useUWP = true;
    private Windows.Networking.Sockets.StreamSocket socket;
    private Task exchangeTask;
#endif

#if UNITY_EDITOR
    private bool _useUWP = false;
    System.Net.Sockets.TcpClient client;
    System.Net.Sockets.NetworkStream stream;
    private Thread exchangeThread;
#endif

    private Byte[] bytes = new Byte[256];
    private StreamWriter writer;
    private StreamReader reader;

    [SerializeField]
    private string host;
    [SerializeField]
    private string port;

    public string message, messageType;

    public string incomingMessage, incomingMessageType;

    public ClientEvent OnExceptionThrown;

    bool messageReceived = false;

    public void Start()
    {
        //Server ip address and port
        //Connect();
    }



    public void Connect()
    {
        if (_useUWP)
        {
            ConnectUWP(host, port);
        }
        else
        {
            ConnectUnity(host, port);
        }
    }



#if UNITY_EDITOR
    private void ConnectUWP(string host, string port)
#else
    private async void ConnectUWP(string host, string port)
#endif
    {
#if UNITY_EDITOR
        errorStatus = "UWP TCP client used in Unity!";
#else
        try
        {
            if (exchangeTask != null) StopExchange();

            socket = new Windows.Networking.Sockets.StreamSocket();
            Windows.Networking.HostName serverHost = new Windows.Networking.HostName(host);
            await socket.ConnectAsync(serverHost, port);           

            RestartExchange();
            //successStatus = "Connected!";
        }
        catch (Exception e)
        {
            errorStatus = e.ToString();
        }
#endif
    }

    private void ConnectUnity(string host, string port)
    {
#if !UNITY_EDITOR
        errorStatus = "Unity TCP client used in UWP!";
#else
        try
        {
            if (exchangeThread != null) StopExchange();

            client = new System.Net.Sockets.TcpClient(host, Int32.Parse(port));
            stream = client.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream) { AutoFlush = true };

            RestartExchange();
            //successStatus = "Connected!";
        }
        catch (Exception e)
        {
            errorStatus = e.ToString();
        }
#endif
    }

    private bool exchanging = false;
    private bool exchangeStopRequested = false;
    private string lastPacket = null;

    private string errorStatus = null;
    private string warningStatus = null;
    private string successStatus = null;
    private string unknownStatus = null;

    public void RestartExchange()
    {
#if UNITY_EDITOR
        if (exchangeThread != null) StopExchange();
        exchangeStopRequested = false;
        exchangeThread = new System.Threading.Thread(ExchangePackets);
        exchangeThread.Start();
#else
        if (exchangeTask != null) StopExchange();
        exchangeStopRequested = false;
        exchangeTask = Task.Run(() => ExchangePackets());
#endif
    }


#if !UNITY_EDITOR
    public async Task SendMessageToServerAsync(string message)
    {
        try 
        {
            using(var writer = new DataWriter(socket.OutputStream))
            {
                writer.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                writer.ByteOrder = Windows.Storage.Streams.ByteOrder.BigEndian;

                writer.WriteString(writer.MeasureString(message).ToString().PadRight(64));                
                await writer.StoreAsync();
                await writer.FlushAsync();

                writer.WriteString(message);                
                await writer.StoreAsync();
                await writer.FlushAsync();

                writer.DetachStream();
            }
        }
        catch (Exception e)
        {
            OnExceptionThrown.Invoke(e.ToString());
        }
    }

    public async Task<string> ReceiveMessageAsync()
    {
        try
        {
            using(var dataReader = new DataReader(socket.InputStream))
            {
                StringBuilder builder = new StringBuilder();
                dataReader.InputStreamOptions = Windows.Storage.Streams.InputStreamOptions.Partial;
                dataReader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                dataReader.ByteOrder = Windows.Storage.Streams.ByteOrder.BigEndian;

                await dataReader.LoadAsync(256);

                while(dataReader.UnconsumedBufferLength>0)
                {
                    builder.Append(dataReader.ReadString(dataReader.UnconsumedBufferLength));
                    await dataReader.LoadAsync(256);
                }

                dataReader.DetachStream();

                return builder.ToString();                                
            }
        }
        catch (Exception e)
        {
            OnExceptionThrown.Invoke(e.ToString());
            return null;
        }
    }

    public async Task ExchangePackets()
    {        
        exchanging = true;

        await SendMessageToServerAsync(this.messageType);
        await SendMessageToServerAsync(this.message);
            
        incomingMessageType = await ReceiveMessageAsync();
        //incomingMessage   = await ReceiveMessageAsync();
        
        messageReceived=true;
    }

#else

    public void SendMessageToServer(string message)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(message);        
        byte[] msgLengthBuffer = Encoding.UTF8.GetBytes(buffer.Length.ToString().PadRight(64));

        stream.Write(msgLengthBuffer, 0, msgLengthBuffer.Length); stream.Flush();
        stream.Write(buffer, 0, buffer.Length); stream.Flush();

    }

    public void ExchangePackets()
    {        
        exchanging = true;

        SendMessageToServer(messageType);
        SendMessageToServer(message);
        incomingMessageType = ReceiveMessage();
        incomingMessage = ReceiveMessage();
        Debug.Log(string.Format("[{0}], [{1}]", incomingMessageType, incomingMessage));
        exchanging = false;
        
    }

    private string ReceiveMessage()
    {
        byte[] messageLengthBuffer = new byte[64];
        int messageLength = stream.Read(messageLengthBuffer, 0, 64);        
        messageLength = Int32.Parse(Encoding.UTF8.GetString(messageLengthBuffer, 0, messageLength));
        byte[] messageBuffer = new byte[messageLength];
        int messageBufferLength = stream.Read(messageBuffer, 0, messageLength);
                
        return Encoding.UTF8.GetString(messageBuffer, 0, messageBufferLength);
    }

#endif

    private void Update()
    {
        if (messageReceived)
        {
            OnExceptionThrown.Invoke(string.Format("Server Sent: {0}, of type: {1} ", incomingMessage, incomingMessageType));
            //incomingMessage = null;
            messageReceived = false;
        }
    }

    private void ReportDataToTrackingManager(string data)
    {
        if (data == null)
        {
            Debug.Log("Received a frame but data was null");
            return;
        }

        var parts = data.Split(';');
        foreach (var part in parts)
        {
            ReportStringToTrackingManager(part);
        }
    }

    private void ReportStringToTrackingManager(string rigidBodyString)
    {
        var parts = rigidBodyString.Split(':');
        var positionData = parts[1].Split(',');
        var rotationData = parts[2].Split(',');

        int id = Int32.Parse(parts[0]);
        float x = float.Parse(positionData[0]);
        float y = float.Parse(positionData[1]);
        float z = float.Parse(positionData[2]);
        float qx = float.Parse(rotationData[0]);
        float qy = float.Parse(rotationData[1]);
        float qz = float.Parse(rotationData[2]);
        float qw = float.Parse(rotationData[3]);

        Vector3 position = new Vector3(x, y, z);
        Quaternion rotation = new Quaternion(qx, qy, qz, qw);


    }

    public void StopExchange()
    {
        exchangeStopRequested = true;

#if UNITY_EDITOR
        if (exchangeThread != null)
        {
            exchangeThread.Abort();
            stream.Close();
            client.Close();
            writer.Close();
            reader.Close();

            stream = null;
            exchangeThread = null;
        }
#else
        if (exchangeTask != null)
        {
            exchangeTask.Wait();
            socket.Dispose();
            writer.Dispose();
            reader.Dispose();

            socket = null;
            exchangeTask = null;
        }
#endif
        writer = null;
        reader = null;
    }

    public void OnDestroy()
    {
        StopExchange();
    }

    public void SendMesh()
    {

    }

}



[Serializable]
public class ClientEvent : UnityEvent <string> { }