using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using ARCLTypes;

namespace ARCL
{
    public class ARCLConnection : IDisposable
    {
        //Public
        public delegate void ConnectedEventHandler();
        public event ConnectedEventHandler Connected;

        public delegate void DisconnectedEventHandler();
        public event DisconnectedEventHandler Disconnected;

        public delegate void DataReceivedEventHandler(object sender, ARCLEventArgs data);
        public event DataReceivedEventHandler DataReceived;

        public delegate void QueueUpdateEventHandler(object sender, QueueUpdateEventArgs data);
        public event QueueUpdateEventHandler QueueUpdate;

        public delegate void StatusUpdateEventHandler(object sender, StatusUpdateEventArgs data);
        public event StatusUpdateEventHandler StatusUpdate;

        public delegate void RangeDeviceCurrentUpdateEventHandler(object sender, RangeDeviceUpdateEventArgs data);
        public event RangeDeviceCurrentUpdateEventHandler RangeDeviceCurrentUpdate;

        public delegate void RangeDeviceCumulativeUpdateEventHandler(object sender, RangeDeviceUpdateEventArgs data);
        public event RangeDeviceCumulativeUpdateEventHandler RangeDeviceCumulativeUpdate;

        public delegate void ExternalIOUpdateEventHandler(object sender, ExternalIOUpdateEventArgs data);
        public event ExternalIOUpdateEventHandler ExternalIOUpdate;

        //Private
        private delegate void ARCLAsyncventHandler(object sender, ARCLEventArgs data);
        private event ARCLAsyncventHandler ARCLAsyncDataReceived;

        //Public
        public string ConnectionString { get; private set; }
        public string IPAddress
        {
            get
            {
                if (ConnectionString.Count(c => c == ':') != 2) return string.Empty;
                return ConnectionString.Split(':')[0];
            }
        }
        public int Port
        {
            get
            {
                if (ConnectionString.Count(c => c == ':') != 2) return 0;
                return int.Parse(ConnectionString.Split(':')[1]);
            }
        }
        public string Password
        {
            get
            {
                if (ConnectionString.Count(c => c == ':') != 2) return string.Empty;
                return ConnectionString.Split(':')[2];
            }
        }

        public int BufferSize { get; private set; } = 2048;
        public int SendTimeout { get; private set; } = 500;//ms
        public int ReceiveTimeout { get; private set; } = 500;//ms
        public bool IsConnected { get { return (Client != null) && Client.Connected; } }
        public bool IsAsyncReceiveRunning { get; private set; } = false;

        //Private
        private TcpClient Client;
        private NetworkStream ClientStream;
        private object LockObject { get; } = new object();

        //Public Static
        public static string GenerateConnectionString(string ip, int port, string pass) => ip + ":" + port.ToString() + ":" + pass;
        public static bool ValidateConnectionString(string connectionString)
        {
            if (connectionString.Count(c => c == ':') != 2) return false;
            string[] spl = connectionString.Split(':');

            if (!System.Net.IPAddress.TryParse(spl[0], out IPAddress ip)) return false;

            if (!int.TryParse(spl[1], out int port)) return false;

            if (spl[2].Length <= 0) return false;

            return true;
        }

        //Public
        public ARCLConnection(string connectionString)
        {
            ConnectionString = connectionString;
        }
        
        public bool Connect(bool withTimeout)
        {
            try
            {
                Client = new TcpClient
                {
                    SendTimeout = SendTimeout,
                    ReceiveTimeout = ReceiveTimeout
                };

                if (withTimeout)
                {
                    if (!ConnectWithTimeout(3))
                        return false;
                }
                    else
                        Client.Connect(IPAddress, Port);

                ClientStream = Client.GetStream();

                if (Login())
                {
                    Connected?.Invoke();
                    return true;
                }
                else
                {
                    Disconnect();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        public bool Disconnect()
        {
            StopReceiveAsync();

            Disconnected?.Invoke();

            try
            {
                if (ClientStream != null)
                {
                    ClientStream.Close();
                    Client.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        public void StartReceiveAsync()
        {
            IsAsyncReceiveRunning = true;

            ARCLAsyncDataReceived += AsyncReceiveThread_ARCLAsyncDataReceived;

            ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncReceiveThread_DoWork));
        }
        public void StopReceiveAsync()
        {
            ARCLAsyncDataReceived -= AsyncReceiveThread_ARCLAsyncDataReceived;
            IsAsyncReceiveRunning = false;
            Thread.Sleep(100);
        }

        public string Read()
        {
            int timeout = 45000; //ms
            Stopwatch sw = new Stopwatch();
            StringBuilder completeMessage = new System.Text.StringBuilder();

            try
            {
                sw.Start();
                lock (LockObject)
                {
                    if (ClientStream.CanRead && ClientStream.DataAvailable)
                    {
                        byte[] readBuffer = new byte[BufferSize];
                        int numberOfBytesRead = 0;

                        // Fill byte array with data from ARCL1 stream
                        numberOfBytesRead = ClientStream.Read(readBuffer, 0, readBuffer.Length);

                        // Convert the number of bytes received to a string and
                        // concatenate to complete message
                        completeMessage.AppendFormat("{0}", System.Text.Encoding.ASCII.GetString(readBuffer, 0, numberOfBytesRead));

                        sw.Stop();
                        if (sw.ElapsedMilliseconds >= timeout)
                            throw new TimeoutException();
                    }
                }
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine(ex);
                throw;
            }

            return completeMessage.ToString();
        }
        public string Read(string endString)
        {
            int timeout = 45000; //ms
            Stopwatch sw = new Stopwatch();
            StringBuilder completeMessage = new System.Text.StringBuilder();

            // Read until find the given string argument or hit timeout
            sw.Start();
            do
            {
                // Convert the number of bytes received to a string and
                // concatenate to complete message
                completeMessage.AppendFormat("{0}", Read());
            }
            while (!completeMessage.ToString().Contains(endString) &&
                   !completeMessage.ToString().Contains("Unknown command") &&
                   sw.ElapsedMilliseconds < timeout);
            sw.Stop();

            if (sw.ElapsedMilliseconds >= timeout)
                throw new TimeoutException();

            return completeMessage.ToString();
        }
        public string ReadLine()
        {
            int timeout = 45000; //ms
            Stopwatch sw = new Stopwatch();
            StringBuilder completeMessage = new System.Text.StringBuilder();

            try
            {
                sw.Start();
                lock (LockObject)
                {
                    if (ClientStream.CanRead && ClientStream.DataAvailable)
                    {
                        char singleChar = (char)ClientStream.ReadByte();

                        if (singleChar == '\n' | singleChar == '\f')
                            return completeMessage.ToString();
                        else if (singleChar != '\r')
                            completeMessage.AppendFormat("{0}", singleChar.ToString());
                    }
                    if (sw.ElapsedMilliseconds >= timeout)
                        throw new TimeoutException();
                }
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine(ex);
                throw;
            }

            return completeMessage.ToString();
        }
        public string ReadMessage()
        {
            int timeout = 45000; //ms
            Stopwatch sw = new Stopwatch();
            StringBuilder completeMessage = new System.Text.StringBuilder();

            sw.Start();
            // Read until find the given string argument or hit timeout
            do
            {
                // Convert the number of bytes received to a string and
                // concatenate to complete message
                completeMessage.AppendFormat("{0}", Read());
                Thread.Sleep(5);
            }
            while (ClientStream.DataAvailable &&
                   sw.ElapsedMilliseconds < timeout);
            sw.Stop();

            if (sw.ElapsedMilliseconds >= timeout)
                throw new TimeoutException();

            return completeMessage.ToString();
        }
        public string[] MessageParse(string message)
        {
            string[] messages = message.Split('\n', '\r');

            List<string> _messages = new List<string>();

            foreach (string item in messages)
            {
                if (!String.IsNullOrEmpty(item))
                {
                    _messages.Add(item);
                }
            }
            messages = _messages.ToArray();
            return messages;
        }

        public bool Write(string msg)
        {
            byte[] buffer_ot = new byte[BufferSize];
            msg += "\r\n";
            try
            {
                lock (LockObject)
                {
                    StringToBytes(msg, ref buffer_ot);
                    ClientStream.Write(buffer_ot, 0, buffer_ot.Length);
                    Bzero(buffer_ot);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }
        public bool Write(string msg, int waitTime)
        {
            if (Write(msg))
            {
                Thread.Sleep(waitTime);
                return true;
            }
            else
                return false; ;
        }

        //Private
        private bool ConnectWithTimeout(int timeout)
        {
            bool connected = false;
            IAsyncResult ar = Client.BeginConnect(IPAddress, Port, null, null);
            System.Threading.WaitHandle wh = ar.AsyncWaitHandle;
            try
            {
                if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(timeout), false))
                {
                    Client.Close();
                    connected = false;
                }
                else
                {
                    connected = true;
                }
                if (Client.Client != null)
                    Client.EndConnect(ar);
            }
            finally
            {
                wh.Close();
            }
            return connected;
        }
        private bool Login()
        {
            Read();

            Write(Password);
            System.Threading.Thread.Sleep(ReceiveTimeout);
            string rm = ReadMessage();

            if (rm.EndsWith("End of commands\r\n")) return true;
            else return false;
        }
        
        private void AsyncReceiveThread_DoWork(object sender)
        {
            try
            {
                string msg;
                while (IsAsyncReceiveRunning)
                {
                    msg = ReadMessage();
                    if (msg.Length > 0)
                        ARCLAsyncDataReceived?.Invoke(this, new ARCLEventArgs(msg));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void AsyncReceiveThread_ARCLAsyncDataReceived(object sender, ARCLEventArgs data)
        {
            string[] messages = MessageParse(data.Message);

            DataReceived?.Invoke(this, new ARCLEventArgs(data.Message));

            foreach (string message in messages)
            {
                if (message.StartsWith("queue", StringComparison.CurrentCultureIgnoreCase) &&
                    !message.StartsWith("queuerobot", StringComparison.CurrentCultureIgnoreCase))
                    QueueUpdate?.Invoke(this, new QueueUpdateEventArgs(message));

                if (message.StartsWith("extIOOutputUpdate") || message.Contains("extIOInputUpdate"))
                    ExternalIOUpdate?.Invoke(this, new ExternalIOUpdateEventArgs(message));

                if (message.StartsWith("Status:"))
                    StatusUpdate?.Invoke(this, new StatusUpdateEventArgs(message));

                if (message.StartsWith("RangeDeviceGetCurrent:"))
                    RangeDeviceCurrentUpdate?.Invoke(this, new RangeDeviceUpdateEventArgs(message));

                if (message.StartsWith("RangeDeviceGetCumulative:"))
                    RangeDeviceCumulativeUpdate?.Invoke(this, new RangeDeviceUpdateEventArgs(message));
            }
        }

        private void Bzero(byte[] buff)
        {
            for (int i = 0; i < buff.Length; i++)
            {
                buff[i] = 0;
            }
        }
        private byte[] StringToBytes(string msg) => ASCIIEncoding.ASCII.GetBytes(msg);
        private void StringToBytes(string msg, ref byte[] buffer)
        {
            Bzero(buffer);
            buffer = System.Text.ASCIIEncoding.ASCII.GetBytes(msg);
        }
        private string BytesToString(byte[] buffer) => ASCIIEncoding.ASCII.GetString(buffer, 0, buffer.Length);

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    Client?.Dispose();
                    ClientStream?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        //Public
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}