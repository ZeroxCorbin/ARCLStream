using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ARCL
{
    public class Arcl : IDisposable
    {

        public class ArclEventArgs : EventArgs
        {
            public string Message { get; }
            public ArclEventArgs(string msg)
            {
                Message = msg;
            }
        }

        public class ArclStreamEventArgs : EventArgs
        {
            public string Message { get; }
            public ArclStreamEventArgs(string msg)
            {
                Message = msg;
            }
        }

        public class QueueEventArgs : EventArgs
        {
            public string Message { get; }
            public string Segment { get; }
            public string TimeStarted { get; }
            public string TimeFinished { get; }
            public int FailCount { get; }
            public QueueEventArgs(string msg)
            {
                Message = msg;
                Segment = msg.Split(' ')[7];
                TimeStarted = msg.Split(' ')[10];
                TimeFinished = msg.Split(' ')[12];
                FailCount = Convert.ToInt32(msg.Split(' ')[13]);
            }
        }

        public class RangeDeviceEventArgs : EventArgs
        {
            public bool IsCurrent { get; set; } = false;
            public string Name { get; set; } = string.Empty;
            public string Message { get; set; }
            public List<float[]> Data { get; set; } = new List<float[]>();

            public float Timestamp = 0;

            public RangeDeviceEventArgs(string msg, bool isReplay = false)
            {
                Message = msg;
                string[] rawData;

                if (isReplay)
                {
                    string[] spl = msg.Split(',');
                    rawData = spl[2].Split();
                    Name = spl[1];

                    IsCurrent = true;

                    if (float.TryParse(spl[0], out float res))
                        Timestamp = res;
                }
                else
                {
                    rawData = msg.Split();
                    Name = rawData[1];
                }

                IsCurrent = rawData[0].Contains("RangeDeviceGetCurrent") ? true : false;

                int i = 3;
                for (; i < rawData.Length - 3; i += 3)
                {
                    float[] fl = new float[2];
                    fl[0] = float.Parse(rawData[i]);
                    fl[1] = float.Parse(rawData[i + 1]);
                    Data.Add(fl);
                }
            }
        }

        public class StatusEventArgs : EventArgs
        {
            public string Message { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string DockingState { get; set; } = string.Empty;
            public string ForcedState { get; set; } = string.Empty;
            public float ChargeState { get; set; }
            public float StateOfCharge { get; set; }
            public string Location { get; set; } = string.Empty;
            public float X { get; set; }
            public float Y { get; set; }
            public float Heading { get; set; }
            public float Temperature { get; set; }

            public float Timestamp { get; set; }

            public StatusEventArgs(string msg, bool isReplay = false)
            {
                if (isReplay)
                {
                    //0.361,encoderTransform,82849.2 -33808.8 140.02
                    Message = msg;
                    string[] spl = msg.Split(',');
                    string[] loc = spl[2].Split();

                        Location = String.Format("{0},{1},{2}", loc[0], loc[1], loc[2]);

                        X = float.Parse(loc[0]);
                        Y = float.Parse(loc[1]);
                        Heading = float.Parse(loc[2]);

                    if (float.TryParse(spl[0], out float res))
                    {
                        Timestamp = res;
                    }
                    else
                    {
                        if (!spl[0].Equals("starting"))
                        {

                        }
                    }

                }
                else
                {
                    Message = msg;
                    string[] spl = msg.Split();
                    int i = 0;
                    float val = 0.0f;

                    while (true)
                    {
                        switch (spl[i])
                        {
                            case "Status:":
                                while (true)
                                {
                                    if (spl[i + 1].Contains(":") & !spl[i + 1].Contains("Error")) break;
                                    Status += spl[++i] + ' ';
                                }
                                break;

                            case "DockingState:":
                                if (!spl[i + 1].Contains(':'))
                                    DockingState = spl[++i];
                                break;

                            case "ForcedState:":
                                if (!spl[i + 1].Contains(':'))
                                    ForcedState = spl[++i];
                                break;

                            case "ChargeState:":
                                if (!spl[i + 1].Contains(':'))
                                    if (float.TryParse(spl[++i], out val))
                                        ChargeState = val;
                                break;

                            case "StateOfCharge:":
                                if (!spl[i + 1].Contains(':'))
                                    if (float.TryParse(spl[++i], out val))
                                        StateOfCharge = val;
                                break;

                            case "Location:":
                                if (!spl[i + 1].Contains(':'))
                                {
                                    Location = String.Format("{0},{1},{2}", spl[++i], spl[++i], spl[++i]);
                                    string[] spl1 = Location.Split(',');
                                    X = float.Parse(spl1[0]);
                                    Y = float.Parse(spl1[1]);
                                    Heading = float.Parse(spl1[2]);
                                }

                                break;
                            case "Temperature:":
                                if (!spl[i + 1].Contains(':'))
                                    if (float.TryParse(spl[++i], out val))
                                        Temperature = val;
                                break;

                            default:
                                break;
                        }

                        i++;
                        if (spl.Length == i) break;
                    }
                }

            }
        }

        public class JobDoneEventArgs : EventArgs
        {
            public string Message { get; }
            public JobDoneEventArgs(string msg)
            {
                Message = msg;
            }
        }

        public class ExtIOEventArgs : EventArgs
        {
            public string Message { get; }
            public ExtIOEventArgs(string msg)
            {
                Message = msg;
            }
        }

        public delegate void ArclDataReceivedEventHandler(object sender, ArclEventArgs data);
        public event ArclDataReceivedEventHandler ArclDataReceived;

        private delegate void ArclAsyncventHandler(object sender, ArclEventArgs data);
        private event ArclAsyncventHandler ArclAsyncDataReceived;

        public delegate void QueueDataReceivedEventHandler(object sender, QueueEventArgs data);
        public event QueueDataReceivedEventHandler QueueDataReceived;

        public delegate void StatusDataReceivedEventHandler(object sender, StatusEventArgs data);
        public event StatusDataReceivedEventHandler StatusDataReceived;

        public delegate void RangeDeviceCurrentDataReceivedEventHandler(object sender, RangeDeviceEventArgs data);
        public event RangeDeviceCurrentDataReceivedEventHandler RangeDeviceCurrentDataReceived;

        public delegate void RangeDeviceCumulativeDataReceivedEventHandler(object sender, RangeDeviceEventArgs data);
        public event RangeDeviceCumulativeDataReceivedEventHandler RangeDeviceCumulativeDataReceived;

        public delegate void ExtIODataReceivedEventHandler(object sender, ExtIOEventArgs data);
        public event ExtIODataReceivedEventHandler ExtIODataReceived;


        public string GenerateConnectionString(string ip, int port, string pass) => ip + ":" + port.ToString() + pass;
        public static bool ValidateConnectionString(string connectionString)
        {
            if (connectionString.Count(c => c == ':') != 2) return false;
            string[] spl = connectionString.Split(':');

            if (!System.Net.IPAddress.TryParse(spl[0], out IPAddress ip)) return false;

            if (!int.TryParse(spl[1], out int port)) return false;

            if (spl[2].Length <= 0) return false;

            return true;
        }
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

        private TcpClient Client;
        private NetworkStream ClientStream;
        public int BufferSize { get; private set; } = 1024;
        public int SendTimeout { get; private set; } = 500;
        public int RecieveTimeout { get; private set; } = 500;

        public bool IsConnected { get { return (Client != null) ? Client.Connected : false; } }

        public bool IsRunning { get; private set; } = true;
        public int UpdateRate { get; private set; } = 50;
        private object LockObject = new object();


        public Arcl(string connectionString)
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
                    ReceiveTimeout = RecieveTimeout
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
                    return true;
                else
                {
                    Disconnect();
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
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
            System.Threading.Thread.Sleep(RecieveTimeout);
            string r = Read();
            string rm = ReadMessage();

            if (rm.EndsWith("End of commands\r\n")) return true;
            else return false;
        }
        public bool Disconnect()
        {
            StopRecieveAsync();
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
                return false;
            }
            return true;
        }

        public void StartRecieveAsync(int rate = 20)
        {
            UpdateRate = rate;
            IsRunning = true;

            ArclAsyncDataReceived += AsyncRecieveThread_ArclAsyncDataReceived;
            ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncRecieveThread_DoWork));
        }
        public void StopRecieveAsync()
        {
            ArclAsyncDataReceived -= AsyncRecieveThread_ArclAsyncDataReceived;
            IsRunning = false;
            Thread.Sleep(UpdateRate + 100);
        }
        private void AsyncRecieveThread_DoWork(object sender)
        {
            try
            {
                string msg;
                while (IsRunning)
                {
                    msg = ReadMessage();
                    if (msg.Length > 0)
                        ArclAsyncDataReceived?.Invoke(this, new ArclEventArgs(msg));
                    Thread.Sleep(UpdateRate);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void AsyncRecieveThread_ArclAsyncDataReceived(object sender, ArclEventArgs data)
        {
            string[] messages = MessageParse(data.Message);

            ArclDataReceived?.Invoke(this, new ArclEventArgs(data.Message));

            foreach (string message in messages)
            {
                if (message.StartsWith("QueueUpdate"))
                    QueueDataReceived?.Invoke(this, new QueueEventArgs(message));

                if (message.StartsWith("extIOOutputUpdate") || message.Contains("extIOInputUpdate"))
                    ExtIODataReceived?.Invoke(this, new ExtIOEventArgs(message));

                if (message.StartsWith("Status:"))
                    StatusDataReceived?.Invoke(this, new StatusEventArgs(message));

                if (message.StartsWith("RangeDeviceGetCurrent:"))
                    RangeDeviceCurrentDataReceived?.Invoke(this, new RangeDeviceEventArgs(message));

                if (message.StartsWith("RangeDeviceGetCumulative:"))
                    RangeDeviceCumulativeDataReceived?.Invoke(this, new RangeDeviceEventArgs(message));

            }
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
                        int readBuffer = 0;
                        char singleChar = '~';

                        while (singleChar != '\n' && singleChar != '\r')
                        {
                            // Read the first byte of the stream
                            readBuffer = ClientStream.ReadByte();

                            //Start converting and appending the bytes into a message
                            singleChar = (char)readBuffer;

                            completeMessage.AppendFormat("{0}", singleChar.ToString());

                            if (sw.ElapsedMilliseconds >= timeout)
                                throw new TimeoutException();

                        }
                        sw.Stop();
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
            string[] messages = message.Split('\n','\r');

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
            msg += '\r';
            try
            {
                lock (LockObject)
                {
                    StringToBytes(msg, ref buffer_ot);
                    ClientStream.Write(buffer_ot, 0, buffer_ot.Length);
                    bzero(buffer_ot);
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

        private void bzero(byte[] buff)
        {
            for (int i = 0; i < buff.Length; i++)
            {
                buff[i] = 0;
            }
        }
        public byte[] StringToBytes(string msg)
        {
            byte[] buffer = new byte[msg.Length];
            buffer = System.Text.ASCIIEncoding.ASCII.GetBytes(msg);
            return buffer;
        }
        public void StringToBytes(string msg, ref byte[] buffer)
        {
            bzero(buffer);
            buffer = System.Text.ASCIIEncoding.ASCII.GetBytes(msg);
        }

        public string BytesToString(byte[] buffer)
        {
            string msg = System.Text.ASCIIEncoding.ASCII.GetString(buffer, 0, buffer.Length);
            return msg;
        }

        public void Log(string msg)
        {
            // Strip quotes
            msg.Replace((char)32, ' ');

            // Replace CRs and LFs with spaces
            msg.Replace('\r', ' ');
            msg.Replace('\f', ' ');

            Write("log \"" + msg + "\"");
        }


        public List<string> GetRangeDevices()
        {
            List<string> dev = new List<string>();

            Write("rangeDeviceList");
            System.Threading.Thread.Sleep(500);

            string msg = ReadMessage();
            string[] rawDevices = msg.Split('\r');

            foreach (string s in rawDevices)
            {
                if (s.IndexOf("RangeDeviceCumulativeDrawingData: ") >= 0)
                {
                    string devStr = s.Replace("RangeDeviceCumulativeDrawingData: ", String.Empty);
                    devStr = devStr.Trim(new char[] { '\n', '\r' });
                    string[] devSpl = devStr.Split();

                    dev.Add(devSpl[0]);
                }
            }

            return dev;
        }

        public List<string> GetGoals()
        {
            List<string> goals = new List<string>();

            this.Write("getgoals");
            System.Threading.Thread.Sleep(500);

            string goalsString = this.ReadMessage();
            string[] rawGoals = goalsString.Split('\r');

            foreach (string s in rawGoals)
            {
                if (s.IndexOf("Goal: ") >= 0)
                {
                    string goal = s.Replace("Goal: ", String.Empty);
                    goal = goal.Trim(new char[] { '\n', '\r' });
                    goals.Add(goal);
                }
            }
            goals.Sort();
            return goals;
        }

        public List<string> GetRoutes()
        {
            List<string> routes = new List<string>();
            this.ReadMessage();
            this.Write("getroutes");
            System.Threading.Thread.Sleep(500);

            string routesString = this.ReadMessage();
            string[] rawRoutes = routesString.Split('\r');

            foreach (string s in rawRoutes)
            {
                if (s.IndexOf("Route: ") >= 0)
                {
                    string route = s.Replace("Route: ", String.Empty);
                    route = route.Trim(new char[] { '\n', '\r' });
                    routes.Add(route);
                }
            }
            routes.Sort();
            return routes;
        }

        public List<string> GetInputs()
        {
            List<string> inputs = new List<string>();
            this.ReadMessage();
            this.Write("inputlist");
            System.Threading.Thread.Sleep(500);

            string inputsString = this.ReadMessage();
            string[] rawInputs = inputsString.Split('\r');

            foreach (string s in rawInputs)
            {
                if (s.IndexOf("InputList: ") >= 0)
                {
                    string route = s.Replace("InputList: ", String.Empty);
                    route = route.Trim(new char[] { '\n', '\r' });
                    inputs.Add(route);
                }
            }
            inputs.Sort();
            return inputs;
        }
        public List<string> GetOutputs()
        {
            List<string> outputs = new List<string>();
            this.ReadMessage();
            this.Write("outputlist");
            System.Threading.Thread.Sleep(500);

            string outputsString = this.ReadMessage();
            string[] rawOutputs = outputsString.Split('\r');

            foreach (string s in rawOutputs)
            {
                if (s.IndexOf("OutputList: ") >= 0)
                {
                    string route = s.Replace("OutputList: ", String.Empty);
                    route = route.Trim(new char[] { '\n', '\r' });
                    outputs.Add(route);
                }
            }
            outputs.Sort();
            return outputs;
        }
        public bool CheckInput(string inputname)
        {
            this.ReadMessage();
            this.Write("inputQuery " + inputname);
            System.Threading.Thread.Sleep(50);

            string status = this.ReadMessage();
            string input = status.Replace("InputList: ", String.Empty);
            input = input.Trim(new char[] { '\n', '\r' });

            if (input.Contains("on"))
                return true;
            else
                return false;
        }
        public bool CheckOutput(string outputname)
        {
            this.ReadMessage();
            this.Write("outputQuery " + outputname);
            System.Threading.Thread.Sleep(50);

            string status = this.ReadMessage();
            string output = status.Replace("OutputList: ", String.Empty);
            output = output.Trim(new char[] { '\n', '\r' });

            if (output.Contains("on"))
                return true;
            else
                return false;
        }
        public bool SetOutput(string outputname, bool state)
        {
            if (state)
                this.Write("outputOn " + outputname);
            else
                this.Write("outputOff " + outputname);

            System.Threading.Thread.Sleep(50);

            string status = this.ReadMessage();
            string output = status.Replace("Output: ", String.Empty);
            output = output.Trim(new char[] { '\n', '\r' });

            if (output.Contains("on"))
                return true;
            else
                return false;
        }

        public List<string> GetConfigSectionValue(string section)
        {
            List<string> SectionValues = new List<string>();

            string rawMessage = null;
            string fullMessage = "";
            string lastMessage = "";
            string[] messages;
            Thread.Sleep(100);
            this.ReadMessage();
            this.Write(string.Format("getconfigsectionvalues {0}\r\n", section));
            Stopwatch sw = new Stopwatch();
            sw.Start();

            do
            {
                while (String.IsNullOrEmpty(rawMessage))
                {
                    rawMessage = this.ReadLine();

                    fullMessage = rawMessage;
                    if (sw.ElapsedMilliseconds > 1000)
                    {
                        throw new TimeoutException();

                    }
                }
                sw.Restart();

                if (rawMessage.Contains("CommandError"))
                {
                    Console.WriteLine("Config section \"{0}\" does not exist", section);
                    rawMessage = "EndOfGetConfigSectionValues";
                    fullMessage = rawMessage;
                }
                else
                {
                    while (!rawMessage.Contains("EndOfGetConfigSectionValues"))
                    {
                        rawMessage = this.ReadLine();

                        if (!string.IsNullOrEmpty(rawMessage))
                        {
                            fullMessage += rawMessage;
                        }

                    }
                    sw.Stop();
                }

                messages = this.MessageParse(fullMessage);

                foreach (string message in messages)
                {
                    if (message.Contains("GetConfigSectionValue:"))
                    {
                        SectionValues.Add(message.Split(':')[1].Trim());
                    }
                    if (message.Contains("EndOfGetConfigSectionValues"))
                    {
                        lastMessage = message;
                        break;
                    }
                    if (message.Contains("CommandErrorDescription: No section of name"))
                    {
                        lastMessage = "EndOfGetConfigSectionValues";
                    }
                }
            } while (!lastMessage.Contains("EndOfGetConfigSectionValues"));

            return SectionValues;
        }

        public void Goto(string goalname) => this.Write($"goto {goalname}");
        public void GotoPoint(int x, int y, int heading) => this.Write($"gotopoint {x} {y} {heading}");
        public void PatrolOnce(string routename) => this.Write($"patrolonce {routename}");
        public void Patrol(string routename) => this.Write($"patrol {routename}");
        public void Say(string message) => this.Write($"say {message}");
        public void Stop() => this.Write("stop");
        public void Dock() => this.Write("dock");
        public void Undock() => this.Write("undock");
        public void Localize(int x, int y, int heading) => this.Write($"localizeToPoint {x} {y} {heading}");


        public double StateOfCharge()
        {
            this.ReadMessage();
            this.Write("status");
            Thread.Sleep(25);
            string status = "";
            do
            {
                status = this.ReadMessage();
            }
            while (!status.Contains("Temperature"));

            Regex regex = new Regex(@"StateOfCharge:");
            string[] output = regex.Split(status);
            string[] charge = output[1].Split(new char[] { '\n', '\r' });

            return Convert.ToDouble(charge[0]);
        }
        public string GetLocation()
        {
            this.ReadMessage();
            this.Write("status");
            Thread.Sleep(25);
            string status = "";
            do
            {
                status = this.ReadMessage();
            }
            while (!status.Contains("Temperature"));

            Regex regex = new Regex(@"Location:");
            string[] output = regex.Split(status);
            string[] location = output[1].Split(new char[] { '\n', '\r' });

            return location[0].Trim();
        }

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

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Arcl() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
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