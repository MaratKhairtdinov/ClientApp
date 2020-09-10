using System;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Threading;
using System.Text;
using System.Linq;

#if !UNITY_EDITOR
using Windows.Storage.Streams;
#endif
public class TCPClient : MonoBehaviour
{
    public string host, inputGatePort, outputGatePort;
    public VertexCollector collector;
    public Transform modelGeometry;
    public ClientEvent OnHostSet;
    public DataTransferEvent onDataSent;
    public ClientEvent GUILog;    

    public InputGate inputGate;
    public OutputGate outputGate;

    private void Start()
    {
        inputGate = new InputGate();
        outputGate = new OutputGate();
        OnHostSet.Invoke("Host: " + host);
        inputGate.client  = this;
        outputGate.client = this;
        NetworkDataHandler.client = this;
        NetworkDataHandler.InitializePackages();
        NetworkDataHandler.modelGeometry = modelGeometry;
    }

    public void Connect()
    {
         inputGate.Connect(host,  inputGatePort);
        Thread.Sleep(3000);
        outputGate.Connect(host, outputGatePort);        
        inputGate.ReceiveData();
    }

    public void SendString(string strg)
    {
        outputGate.SendData(Encoding.UTF8.GetBytes(strg).ToList<byte>(), NetworkDataType.String);
    }

    public void SendPointCloud()
    {
        var vertices = collector.GetVertices();
        var normals = collector.GetNormals();
        List<byte> data = new List<byte>();
        data.AddRange(BitConverter.GetBytes(vertices.Count));
        for (int i = 0; i < vertices.Count; i++)
        {
            data.AddRange(BitConverter.GetBytes(vertices[i].x)); data.AddRange(BitConverter.GetBytes(vertices[i].y)); data.AddRange(BitConverter.GetBytes(vertices[i].z));
             data.AddRange(BitConverter.GetBytes(normals[i].x));  data.AddRange(BitConverter.GetBytes(normals[i].y));  data.AddRange(BitConverter.GetBytes(normals[i].z));
        }
        outputGate.SendData(data, NetworkDataType.PointCloud);
    }


    /*
     * Unity main thread functions, to interact with other gameobjects
     */
    #region Unity Main Thread
    int mainThreadInteger; string mainThreadString; float mainthreadFloat;
    bool mainThreadNotify = false;
    public void NotifyMainThread(int integer, string strg, float flt)
    {
        mainThreadNotify = true;
        mainThreadInteger = integer; mainThreadString = strg; mainthreadFloat = flt;
    }

    bool transform_set = false;
    Vector3 position, scale; Quaternion rotation;
    public void SetTransform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        transform_set = true;

        this.position = position; this.rotation = rotation; this.scale = scale;
    }

    private void Update()
    {
        if (mainThreadNotify)
        {
            mainThreadNotify = false;
            if (onDataSent != null)
            {
                onDataSent.Invoke(mainThreadInteger);
            }
            if (GUILog != null && mainThreadString != null && mainThreadString.Length > 0)
            {
                GUILog.Invoke(mainThreadString);
            }
        }

        if (transform_set)
        {
            transform_set = false;
            modelGeometry.position = position; modelGeometry.rotation = rotation; modelGeometry.localScale = scale;
        }
    }

    #endregion

    public class InputGate
    {
#if !UNITY_EDITOR
        Windows.Networking.Sockets.StreamSocket ClientSocket;
#endif
        public TCPClient client;
        public void Connect(string host, string port)
        {
            var connectTask = Task.Run(() => ConnectAsync(host, port)); connectTask.Wait();
            //client.NotifyMainThread(0, "Input Gate connected", 0);
        }

        async Task ConnectAsync(string host, string port)
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
                client.NotifyMainThread(0, "InputGate ConnectAsznc() ERROR\n"+e.ToString(), 0);
            }
        }

        Task receivingTask;

        public void ReceiveData()
        {
            receivingTask = new Task(() => ReceiveDataAsync());
            receivingTask.Start();            
        }

        public async Task ReceiveDataAsync()
        {
            byte[] inputBuffer = null;
#if !UNITY_EDITOR            
            while(true)
            {
                string errorLog = string.Empty;
                var stream = ClientSocket.InputStream.AsStreamForRead();
                byte[] inputDataTypeBuffer = new byte[2];
                byte[] inputLengthBuffer   = new byte[4];                
                
                try
                {
                    await stream.ReadAsync(inputDataTypeBuffer, 0, 2);
                }
                catch(Exception e)
                {
                    errorLog+="InputDataTypeBuffer read error\n";
                }

                NetworkDataType inputType = (NetworkDataType)BitConverter.ToInt16(inputDataTypeBuffer, 0);
                
                try
                {
                    await stream.ReadAsync(inputLengthBuffer, 0, 4);
                }
                catch(Exception e)
                {
                    errorLog+="InputLengthBuffer read error\n";
                }
                var inputLength = BitConverter.ToInt32(inputLengthBuffer, 0);
                try
                {
                    inputBuffer = new byte[inputLength];
                    await stream.ReadAsync(inputBuffer, 0, inputBuffer.Length);
                }
                catch(Exception e)
                {
                    errorLog+="InputBuffer read error\n";
                }
                try
                {
                    errorLog+="DataType: "+inputType+"\n";
                    NetworkDataHandler.HandleNetworkData(inputBuffer, inputType);
                }
                catch(Exception e)
                {
                    errorLog+="HandleData error\n";                    
                }
                if(errorLog.Length>0)
                {
                    client.NotifyMainThread(0,errorLog,0);
                    errorLog = string.Empty;
                }
                
            }
#endif            
        }
    }
    public class OutputGate
    {
#if !UNITY_EDITOR
        Windows.Networking.Sockets.StreamSocket ClientSocket;
#endif
        public TCPClient client;
        int chunkSize = 1024;
        int chunks, residual;
        public OutputGate()
        {
            NetworkDataHandler.outputGate = this;
        }
        public void Connect(string host, string port)
        {
            var connectTask = Task.Run(() => ConnectAsync(host, port)); connectTask.Wait();
            //client.NotifyMainThread(0, "Input Gate connected", 0);
        }

        async Task ConnectAsync(string host, string port)
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
                client.NotifyMainThread(0, e.ToString(), 0);
            }
        }

        Task sendingTask;

        public void SendData(List<byte> buffer, NetworkDataType type)
        {
            sendingTask = new Task(() => SendDataAsync(buffer, type));
            sendingTask.Start();
        }

        public async Task SendDataAsync(List<byte> buffer, NetworkDataType type)
        {
            try
            {
#if !UNITY_EDITOR
                using(var writer = new DataWriter(ClientSocket.OutputStream))
                {
                    NetworkDataHandler.lastBufferSent = buffer; NetworkDataHandler.lastTypeSent = type;

                    writer.ByteOrder = ByteOrder.BigEndian;
                    writer.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;

                    chunks = buffer.Count/chunkSize;
                    residual = buffer.Count%chunkSize;
                    
                    /*
                     *Header of the package so that the other side knows how much and what kind of data to receive
                     */
                    writer.WriteInt16((short)type);
                    writer.WriteInt32(chunkSize);
                    writer.WriteInt32(chunks);
                    writer.WriteInt32(residual);

                    var stream = ClientSocket.InputStream.AsStreamForRead();                    
                    byte[] response_buffer = new byte[2];
                    NetworkResponseType response;
                    int percentage = 0;
                    
                    for(int i = 0; i<chunks; i++)
                    {
                        bool chunk_received = false;
                        response = NetworkResponseType.DataCorrupt;
                        while(response == NetworkResponseType.DataCorrupt)
                        {
                            writer.WriteBytes(buffer.GetRange(i*chunkSize, chunkSize).ToArray());
                            await writer.StoreAsync();
                            await writer.FlushAsync();
                            await stream.ReadAsync(response_buffer,0,2);
                            stream.Flush();
                            response = (NetworkResponseType)BitConverter.ToInt16(response_buffer, 0);
                        }
                        percentage = ((int)(((float)(i+1))/((float)chunks)*100));
                        client.NotifyMainThread(percentage, "Sending data, Chunk#"+i+" of "+chunks, 0);
                    }
                    response = NetworkResponseType.DataCorrupt;
                    while(response == NetworkResponseType.DataCorrupt)
                    {
                        writer.WriteBytes(buffer.GetRange(chunks*chunkSize, residual).ToArray());
                        await writer.StoreAsync();
                        await writer.FlushAsync();
                        await stream.ReadAsync(response_buffer, 0, 2);
                        response = (NetworkResponseType)BitConverter.ToInt16(response_buffer, 0);
                    }
                    writer.DetachStream();
                }
#endif

            }
            catch (Exception e)
            {
                client.NotifyMainThread(0, "SendDataAsync Error"+e.ToString(), 0);
            }
        }
    }

    public class NetworkDataHandler
    {
        public static Transform modelGeometry;
        public static TCPClient client;
        public static OutputGate outputGate;
        public delegate void Handler(byte[] data);
        public static  Dictionary<int, Handler> handlers;

        public static List<byte> lastBufferSent; public static NetworkDataType lastTypeSent;

        public static void InitializePackages()
        {
            handlers = new Dictionary<int, Handler>()
            {
                {(int) NetworkDataType.Response,     HandleResponse},
                {(int) NetworkDataType.String,         HandleString},
                {(int) NetworkDataType.PointCloud, HandlePointcloud},
                {(int) NetworkDataType.Matrix4x4,      HandleMatrix}
            };
        }

        public static void HandleNetworkData(byte[] buffer, NetworkDataType type)
        {
            handlers[(int)type].Invoke(buffer);
        }

        private static void HandleMatrix(byte[] data)
        {
            /*
            Vector4 column0 = new Vector4((float)BitConverter.ToDouble(data,  0), (float)BitConverter.ToDouble(data, 8), (float)BitConverter.ToDouble(data, 16), (float)BitConverter.ToDouble(data, 24));
            Vector4 column1 = new Vector4((float)BitConverter.ToDouble(data, 32), (float)BitConverter.ToDouble(data, 40), (float)BitConverter.ToDouble(data, 48), (float)BitConverter.ToDouble(data, 56));
            Vector4 column2 = new Vector4((float)BitConverter.ToDouble(data, 64), (float)BitConverter.ToDouble(data, 72), (float)BitConverter.ToDouble(data, 80), (float)BitConverter.ToDouble(data, 88));
            Vector4 column3 = new Vector4((float)BitConverter.ToDouble(data, 96), (float)BitConverter.ToDouble(data, 104), (float)BitConverter.ToDouble(data, 112), (float)BitConverter.ToDouble(data, 128));
            Matrix4x4 mat = new Matrix4x4(column0, column1, column2, column3);
            */
            //client.SetTransform(mat.ExtractPosition(), mat.ExtractRotation(), mat.ExtractScale());
            client.NotifyMainThread(0, "MATRIX RECEIVED, buffer length: "+data.Length, 0);
        }

        private static void HandlePointcloud(byte[] data)
        {
            
        }

        private static void HandleString(byte[] data)
        {
            client.NotifyMainThread(0, Encoding.UTF8.GetString(data, 0, data.Length), 0);
        }
        static int counter = 0;
        static int trials = 5;
        public static void HandleResponse(byte[] data)
        {
            var response = (NetworkResponseType)BitConverter.ToInt16(data, 0);
            switch (response)
            {
                case NetworkResponseType.AllGood:
                    client.NotifyMainThread(0, "Server received data", 0);
                    break;
                case NetworkResponseType.DataCorrupt:
                    if (counter < trials)
                    {
                        counter += 1;
                        outputGate.SendData(lastBufferSent, lastTypeSent);
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
}

[Serializable]
public class ClientEvent : UnityEvent<string> { }
[Serializable]
public class DataTransferEvent : UnityEvent<int> { }

public enum NetworkDataType
{
    Response = 0,
    String = 1,
    PointCloud = 2,
    Matrix4x4 = 3
}

public enum NetworkResponseType
{
    AllGood = 0,
    DataCorrupt = 1
}

public static class MatrixExtensions
{
    public static Quaternion ExtractRotation(this Matrix4x4 matrix)
    {
        Vector3 forward;
        forward.x = matrix.m02;
        forward.y = matrix.m12;
        forward.z = matrix.m22;

        Vector3 upwards;
        upwards.x = matrix.m01;
        upwards.y = matrix.m11;
        upwards.z = matrix.m21;

        return Quaternion.LookRotation(forward, upwards);
    }

    public static Vector3 ExtractPosition(this Matrix4x4 matrix)
    {
        Vector3 position;
        position.x = matrix.m03;
        position.y = matrix.m13;
        position.z = matrix.m23;
        return position;
    }

    public static Vector3 ExtractScale(this Matrix4x4 matrix)
    {
        Vector3 scale;
        scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
        scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
        scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
        return scale;
    }
}