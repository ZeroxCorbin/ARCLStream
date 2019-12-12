using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ARCLTypes;

namespace ARCL
{
    public class ARCLExtIO
    {
        public delegate void ExtIOUpdateEventHandler(object sender, ExtIOEventArgs data);
        public event ExtIOUpdateEventHandler ExtIOUpdate;

        public class DelayedEventArgs : EventArgs
        {
            public bool Delayed = false;
            public DelayedEventArgs(bool delayed)
            {
                Delayed = delayed;
            }
        }
        public delegate void DelayedEventHandler(DelayedEventArgs data);
        public event DelayedEventHandler Delayed;

        /// <summary>
        /// Dictionary of all the External Digital Inputs and Outputs created.
        /// Key is exio name + "_input" or "_output".
        /// Value is current state in hex.
        /// </summary>
        public Dictionary<string, int> List { get; private set; }
        /// <summary>
        /// Dictionary of number of inputs or outputs associated with the key, which is shared with List.
        /// </summary>
        public Dictionary<string, int> Count { get; private set; }


        private Stopwatch Stopwatch = new Stopwatch();

        public int UpdateRate { get; private set; } = 50;
        public bool IsRunning { get; private set; } = false;
        public long TTL { get; private set; }
        public bool IsDelayed { get; private set; } = false;

        private bool Heartbeat = false;

        private ARCLConnection ARCL;
        public ARCLExtIO(ARCLConnection arcl)
        {
            ARCL = arcl;

            List = new Dictionary<string, int>();
            Count = new Dictionary<string, int>();
        }

        public void Start(int updateRate)
        {
            if (!ARCL.IsRunning)
                ARCL.StartRecieveAsync();

            ARCL.ExtIOReceived += ARCL_ExtIOReceived;

            UpdateRate = updateRate;
            IsRunning = true;
            ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncThread_DoWork));
        }

        private void ARCL_ExtIOReceived(object sender, ExtIOEventArgs data)
        {

                if (data.Message.Contains("extIOOutputUpdate") || data.Message.Contains("extIOInputUpdate"))
                {
                    ExtIOUpdate?.Invoke(this, data);
                }
            
        }

        public void Stop()
        {
            ARCL.ExtIOReceived -= ARCL_ExtIOReceived;

            IsRunning = false;
            Thread.Sleep(UpdateRate + 100);
        }

        private void AsyncThread_DoWork(object sender)
        {
            while (IsRunning)
            {
                if (!IsDelayed) Stopwatch.Reset();

                //Send status request here

                Heartbeat = false;

                Thread.Sleep(UpdateRate);

                if (Heartbeat)
                {
                    if (IsDelayed) Delayed?.Invoke(new DelayedEventArgs(false));
                    IsDelayed = false;
                }
                else
                {
                    if (!IsDelayed) Delayed?.Invoke(new DelayedEventArgs(true));
                    IsDelayed = true;
                }
            }
        }


        /// <summary>
        /// Which extio to use as softsignals if running in the background.
        /// </summary>
        public string SoftIO { get; set; }

        /// <summary>
        /// Use to see if a specific external IO already exists.
        /// </summary>
        /// <param name="name">Name of the ExtIO (not Alias)</param>
        /// <param name="numIn">Number of Inputs</param>
        /// <param name="numOut">Number of Outputs</param>
        /// <returns>True if external IO exists</returns>
        public bool CheckExtIO(string name, int numIn, int numOut)
        {
            bool existsIn = false;
            bool existsOut = false;
            List<string> sectionValues = new List<string>();


            int count = 0;

            sectionValues = ARCL.GetConfigSectionValue("external digital inputs");
            foreach (string item in sectionValues)
            {
                if (item.Contains(name + "_Input"))
                {
                    count++;
                }
            }
            count = ((count + 1) / 2);
            Console.WriteLine("Num of extins: " + count);


            if (count == numIn)
            {
                existsIn = true;
            }

            if (!Count.ContainsKey(name + "_input"))
            {
                Count.Add(name + "_input", count);
            }

            Thread.Sleep(100);

            count = 0;
            sectionValues = ARCL.GetConfigSectionValue("external digital outputs");
            foreach (string item in sectionValues)
            {
                if (item.Contains(name + "_Output"))
                {
                    count++;
                }
            }
            count = ((count + 1) / 2);
            Console.WriteLine("Num of extouts: " + count);

            if (count == numOut)
            {
                existsOut = true;
            }

            if (!Count.ContainsKey(name + "_output"))
            {
                Count.Add(name + "_output", count);
            }

            return (existsIn && existsOut);
        }

        /// <summary>
        /// Use to create a new external IO
        /// </summary>
        /// <param name="name">Name of the ExtIO (not Alias)</param>
        /// <param name="numIn">Number of Inputs</param>
        /// <param name="numOut">Number of Outputs</param>
        /// <returns>True if external IO created successfully</returns>
        public bool CreateExtIO(string name, int numIn, int numOut)
        {
            bool success = false;
            ARCL.Write("\r\n");
            ARCL.ReadMessage();
            ARCL.Write(string.Format("extioAdd {0} {1} {2}\r\n", name, numIn.ToString(), numOut.ToString()));
            string message = ARCL.Read();
            int attempts = 0;
            while (String.IsNullOrEmpty(message))
            {
                message = ARCL.Read();
                if (attempts > 1000)
                {
                    break;
                }
                Thread.Sleep(10);
                attempts++;
            }

            //Console.WriteLine("Num Attempts = " + attempts);
            //Console.WriteLine("Extio Message: "+ message);

            if (message.Contains(name + " added") || message.Contains("extioAdd " + name) || message.Contains("CommandErrorDescription:"))
            {
                success = true;
            }
            if (success)
            {
                defaultExtIO(name, numIn, numOut);
                ioListAdd(name, numIn, numOut);
            }


            return success;
        }

        /// <summary>
        /// Use set a specific external IO to default values.
        /// Default values is:
        /// Alias of [name]_[Input/Output]_[i/o][number]
        /// Count 1
        /// Type1 custom
        /// </summary>
        /// <param name="name">Name of the ExtIO (not Alias)</param>
        /// <param name="numIn">Number of Inputs</param>
        /// <param name="numOut">Number of Outputs</param>
        /// <returns>True if external IO exists</returns>
        public void defaultExtIO(string name, int numIn, int numOut)
        {
            Console.WriteLine("Setting IO to defaults.");
            ARCL.Write("configStart\r\n");
            ARCL.Write("configAdd Section External Digital Inputs\r\n");
            for (int i = 1; i <= numIn; i++)
            {
                ARCL.Write("configAdd _beginList " + name + "_Input_" + i + "\r\n");
                ARCL.Write("configAdd Alias " + name + "_i" + i + "\r\n");
                ARCL.Write("configAdd _beginList OnList\r\n");
                ARCL.Write("configAdd Count 1\r\n");
                ARCL.Write("configAdd Type1 custom\r\n");
                ARCL.Write("configAdd _endList OnList\r\n");
                ARCL.Write("configAdd _endList " + name + "_Input_" + i + "\r\n");
            }

            ARCL.Write("configAdd Section External Digital Outputs\r\n");
            for (int o = 1; o <= numIn; o++)
            {
                ARCL.Write("configAdd _beginList " + name + "_Output_" + o + "\r\n");
                ARCL.Write("configAdd Alias " + name + "_o" + o + "\r\n");
                ARCL.Write("configAdd Type1 custom\r\n");
                ARCL.Write("configAdd _endList " + name + "_Output_" + o + "\r\n");
            }

            ARCL.Write("configParse\r\n");
            Thread.Sleep(500);

        }

        /// <summary>
        /// Add the new external IO to the IOList
        /// </summary>
        /// <param name="name"></param>
        public void ioListAdd(string name, int numIn, int numOut)
        {
            if (!List.Keys.Contains(name + "_input"))
            {
                List.Add(string.Format("{0}_input", name), 0);
            }

            if (!List.Keys.Contains(name + "_output"))
            {
                List.Add(string.Format("{0}_output", name), 0);
            }

            if (!Count.ContainsKey(name + "_input"))
            {
                Count.Add(name, numIn);
            }

            if (!Count.ContainsKey(name + "_output"))
            {
                Count.Add(name, numOut);
            }
        }

        /// <summary>
        /// Parse the message and update dictionary IOList. Use the message from the event.
        /// </summary>
        /// <param name="msg">Message to parse.</param>
        public void extIOUpdate(string msg)
        {
            string type = msg.Split(' ')[1];
            string name = msg.Split(' ')[2];
            string bit = msg.Split(' ')[5];
            bit = bit.Split('x')[1];

            int value = int.Parse(bit, System.Globalization.NumberStyles.AllowHexSpecifier);


            List[name + "_" + type] = value;
        }

        /// <summary>
        /// Method to turn an external IO into a soft signal.
        /// </summary>
        /// <param name="ioName">Name of the external IO to turn into soft signal</param>
        public void softSignal(string ioName)
        {
            if (List[ioName + "_output"] != List[ioName + "_input"])
            {
                ARCL.Write(string.Format("extioInputUpdate {0} {1}", ioName, List[ioName + "_output"]));
            }
        }

        /// <summary>
        /// Method to run soft signals in a background thread. Set SoftIO for this to work.
        /// </summary>
        /// <param name="sender"></param>
        public void softSignal(object sender)
        {
            string ioName = SoftIO;

            while (true)
            {

                foreach (string item in List.Keys)
                {
                    //Console.WriteLine(item);
                }
                if (List[ioName + "_output"] != List[ioName + "_input"])
                {
                    ARCL.Write(string.Format("extioInputUpdate {0} {1}", ioName, List[ioName + "_output"]));
                }

                Thread.Sleep(20);
            }

        }

        /// <summary>
        /// Method to Turn On an output.
        /// </summary>
        /// <param name="output">Name of EXTIO</param>
        /// <param name="value">Which bits to turn on (in hex)</param>
        public void OutputOn(string output, int value)
        {
            int _value = value;
            int _valuePrev = List[output + "_output"];

            _value |= _valuePrev;

            ARCL.Write(string.Format("extIOOutputUpdate {0} {1}\r\n", output, _value));
        }

        /// <summary>
        /// Turn an output on by feeding it the full raw data from the EXTIO event handler
        /// </summary>
        /// <param name="msg">Message to parse.</param>
        public void OutputOn(string msg)
        {
            string name = msg.Split(' ')[1];
            string bit = msg.Split(' ')[2];
            bit = bit.Split('x')[1];

            int value = int.Parse(bit, System.Globalization.NumberStyles.AllowHexSpecifier);

            ARCL.Write(string.Format("extioOutputUpdate {0} {1}\r\n", name, value));
        }

        /// <summary>
        /// Method to Turn Off an output.
        /// </summary>
        /// <param name="output">Name of EXTIO</param>
        /// <param name="value">Which bits to turn off (in hex)</param>
        public void OutputOff(string output, int value)
        {
            int _value = value;
            int Length = Count[output + "_output"];
            int _valuePrev = List[output + "_output"];

            _value = (Convert.ToInt32(Convert.ToString(_value, 2).PadLeft(Length, '0')));
            _value = ~value & 0xf;
            _value &= _valuePrev;
            Console.WriteLine("Writing: " + string.Format("extIOOutputUpdate {0} {1}\r\n", output, _value));
            ARCL.Write(string.Format("extIOOutputUpdate {0} {1}\r\n", output, _value));

        }

        /// <summary>
        /// Method to Turn On an input.
        /// </summary>
        /// <param name="input">Name of EXTIO</param>
        /// <param name="value">Which bits to turn off (in hex)</param>
        public void InputOn(string input, int value)
        {
            int _value = value;
            int _valuePrev = List[input + "_input"];

            _value |= _valuePrev;

            ARCL.Write(string.Format("extIOInputUpdate {0} {1}\r\n", input, _value));
        }

        /// <summary>
        /// Method to Turn Off an input.
        /// </summary>
        /// <param name="input">Name of EXTIO</param>
        /// <param name="value">Which bits to turn off (in hex)</param>
        public void InputOff(string input, int value)
        {
            int _value = value;
            int Length = Count[input + "_input"];
            int _valuePrev = List[input + "_input"];

            _value = (Convert.ToInt32(Convert.ToString(_value, 2).PadLeft(Length, '0')));
            _value = ~value & 0xf;
            _value &= _valuePrev;

            ARCL.Write(string.Format("extIOInputUpdate {0} {1}\r\n", input, _value));
        }

    }
}
