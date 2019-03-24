using ARCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ARCLStream
{
    public class EXTIO
    {
        /*public void extioAdd(string extName, int extIn, int extOut)
{
    string confAdd = "configAdd ";
    string beginList = "_beginList ";
    string endList = "_endList ";
    string linefeed = "\r\n";

    Write(String.Format("extioAdd {0} {1} {2}", extName, extIn, extOut));

    Thread.Sleep(100);

    string extioUpdate = "configStart\r\nconfigAdd Section External Digital Inputs\r\n";

    for(int i = 0; i<extIn; i++)
    {
        extioUpdate += String.Format("{0}{1}{4}_Input{1}{2}configAdd Alias {0}_i1\r\n_beginList OnList\r\nconfigAdd")
    }


}*/

        public delegate void ExtIODataReceivedEventHandler(object sender, ExtIOEventArgs data);
        public event ExtIODataReceivedEventHandler ExtIODataReceived;

        /// <summary>
        /// Custom event args for External IO events.
        /// </summary>
        public class ExtIOEventArgs : EventArgs
        {

            /// <summary>
            /// The message.
            /// </summary>
            private string message;

            /// <summary>
            /// Gets the message.
            /// </summary>
            public string Message
            {
                get { return message; }
            }

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="msg">The EXTIO message.</param>
            public ExtIOEventArgs(string msg)
            {
                message = msg;
            }
        }

        /// <summary>
        /// Dictionary of all the External Digital Inputs and Outputs created.
        /// Key is exio name + "_input" or "_output".
        /// Value is current state in hex.
        /// </summary>
        private Dictionary<string, int> ioList;


        #region Data handling
        /// <summary>
        /// Fire this method when we receive anything from ArclDataReceived event to fire some events depending on what we received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        private void ArclStream_ArclDataReceived(object sender, ARCL.Arcl.ArclEventArgs data)
        {
            string[] messages = RobotParse(data.Message);

            foreach (string message in messages)
            {
                if (message.Contains("extIOOutputUpdate") || message.Contains("extIOInputUpdate"))
                {
                    ExtIODataReceived?.Invoke(this, new ExtIOEventArgs(message));
                }
            }
        }

        /// <summary>
        /// This method will take anything read from ARCL and split it by line. This prevents messages being clumped together.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public string[] RobotParse(string message)
        {
            string[] messages;

            List<string> _messages = new List<string>();

            foreach (string item in message.Split('\n', '\r'))
            {
                if (!String.IsNullOrEmpty(item))
                {
                    _messages.Add(item);
                }
            }
            messages = _messages.ToArray();
            return messages;
        }

        #endregion

        /// <summary>
        /// Grab the ioList
        /// </summary>
        /// <returns>Key ExtIO name +_input/_output and Value state in hex</returns>
        public Dictionary<string, int> getIOList()
        {
            return ioList;
        }

        /// <summary>
        /// Dictionary of number of inputs or outputs associated with the key, which is shared with ioList.
        /// </summary>
        private Dictionary<string, int> ioCount = new Dictionary<string, int>();

        /// <summary>
        /// Which extio to use as softsignals if running in the background.
        /// </summary>
        public string SoftIO { get; set; }

        private Arcl Robot;

        public EXTIO(Arcl robot)
        {
            Robot = robot;
            ioList = new Dictionary<string, int>();
        }

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

            sectionValues = GetConfigSectionValue("external digital inputs");
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

            if (!ioCount.ContainsKey(name + "_input"))
            {
                ioCount.Add(name + "_input", count);
            }

            Thread.Sleep(100);

            count = 0;
            sectionValues = GetConfigSectionValue("external digital outputs");
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

            if (!ioCount.ContainsKey(name + "_output"))
            {
                ioCount.Add(name + "_output", count);
            }

            return (existsIn && existsOut);
        }

        /// <summary>
        /// See the config section and values.
        /// </summary>
        /// <param name="section">Section to read</param>
        /// <returns>Returns a list of every line</returns>
        public List<string> GetConfigSectionValue(string section)
        {
            List<string> SectionValues = new List<string>();

            string rawMessage = null;
            string fullMessage = "";
            string lastMessage = "";
            string[] messages;
            Thread.Sleep(100);
            Robot.ReadMessage();
            Robot.Write(string.Format("getconfigsectionvalues {0}\r\n", section));
            Stopwatch sw = new Stopwatch();
            sw.Start();

            do
            {
                while (String.IsNullOrEmpty(rawMessage))
                {
                    rawMessage = Robot.ReadLine();

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
                        rawMessage = Robot.ReadLine();

                        if (!string.IsNullOrEmpty(rawMessage))
                        {
                            fullMessage += rawMessage;
                        }

                    }
                    sw.Stop();
                }

                messages = Robot.MessageParse(fullMessage);

                foreach (string message in messages)
                {
                    if (message.Contains("GetConfigSectionValue:"))
                    {
                        SectionValues.Add(message.Split(':')[1]);

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
            Robot.Write("\r\n");
            Robot.ReadMessage();
            Robot.Write(string.Format("extioAdd {0} {1} {2}\r\n", name, numIn.ToString(), numOut.ToString()));
            string message = Robot.Read();
            int attempts = 0;
            while (String.IsNullOrEmpty(message))
            {
                message = Robot.Read();
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
            Robot.Write("configStart\r\n");
            Robot.Write("configAdd Section External Digital Inputs\r\n");
            for (int i = 1; i <= numIn; i++)
            {
                Robot.Write("configAdd _beginList " + name + "_Input_" + i + "\r\n");
                Robot.Write("configAdd Alias " + name + "_i" + i + "\r\n");
                Robot.Write("configAdd _beginList OnList\r\n");
                Robot.Write("configAdd Count 1\r\n");
                Robot.Write("configAdd Type1 custom\r\n");
                Robot.Write("configAdd _endList OnList\r\n");
                Robot.Write("configAdd _endList " + name + "_Input_" + i + "\r\n");
            }

            Robot.Write("configAdd Section External Digital Outputs\r\n");
            for (int o = 1; o <= numIn; o++)
            {
                Robot.Write("configAdd _beginList " + name + "_Output_" + o + "\r\n");
                Robot.Write("configAdd Alias " + name + "_o" + o + "\r\n");
                Robot.Write("configAdd Type1 custom\r\n");
                Robot.Write("configAdd _endList " + name + "_Output_" + o + "\r\n");
            }

            Robot.Write("configParse\r\n");
            Thread.Sleep(500);

        }

        /// <summary>
        /// Add the new external IO to the IOList
        /// </summary>
        /// <param name="name"></param>
        public void ioListAdd(string name, int numIn, int numOut)
        {
            if (!ioList.Keys.Contains(name + "_input"))
            {
                ioList.Add(string.Format("{0}_input", name), 0);
            }

            if (!ioList.Keys.Contains(name + "_output"))
            {
                ioList.Add(string.Format("{0}_output", name), 0);
            }

            if (!ioCount.ContainsKey(name + "_input"))
            {
                ioCount.Add(name, numIn);
            }

            if (!ioCount.ContainsKey(name + "_output"))
            {
                ioCount.Add(name, numOut);
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


            ioList[name + "_" + type] = value;
        }

        /// <summary>
        /// Method to turn an external IO into a soft signal.
        /// </summary>
        /// <param name="ioName">Name of the external IO to turn into soft signal</param>
        public void softSignal(string ioName)
        {
            if (ioList[ioName + "_output"] != ioList[ioName + "_input"])
            {
                Robot.Write(string.Format("extioInputUpdate {0} {1}", ioName, ioList[ioName + "_output"]));
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

                foreach (string item in ioList.Keys)
                {
                    //Console.WriteLine(item);
                }
                if (ioList[ioName + "_output"] != ioList[ioName + "_input"])
                {
                    Robot.Write(string.Format("extioInputUpdate {0} {1}", ioName, ioList[ioName + "_output"]));
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
            int _valuePrev = ioList[output + "_output"];

            _value |= _valuePrev;

            Robot.Write(string.Format("extIOOutputUpdate {0} {1}\r\n", output, _value));
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

            Robot.Write(string.Format("extioOutputUpdate {0} {1}\r\n", name, value));
        }

        /// <summary>
        /// Method to Turn Off an output.
        /// </summary>
        /// <param name="output">Name of EXTIO</param>
        /// <param name="value">Which bits to turn off (in hex)</param>
        public void OutputOff(string output, int value)
        {
            int _value = value;
            int Length = ioCount[output + "_output"];
            int _valuePrev = ioList[output + "_output"];

            _value = (Convert.ToInt32(Convert.ToString(_value, 2).PadLeft(Length, '0')));
            _value = ~value & 0xf;
            _value &= _valuePrev;
            Console.WriteLine("Writing: " + string.Format("extIOOutputUpdate {0} {1}\r\n", output, _value));
            Robot.Write(string.Format("extIOOutputUpdate {0} {1}\r\n", output, _value));

        }

        /// <summary>
        /// Method to Turn On an input.
        /// </summary>
        /// <param name="input">Name of EXTIO</param>
        /// <param name="value">Which bits to turn off (in hex)</param>
        public void InputOn(string input, int value)
        {
            int _value = value;
            int _valuePrev = ioList[input + "_input"];

            _value |= _valuePrev;

            Robot.Write(string.Format("extIOInputUpdate {0} {1}\r\n", input, _value));
        }

        /// <summary>
        /// Method to Turn Off an input.
        /// </summary>
        /// <param name="input">Name of EXTIO</param>
        /// <param name="value">Which bits to turn off (in hex)</param>
        public void InputOff(string input, int value)
        {
            int _value = value;
            int Length = ioCount[input + "_input"];
            int _valuePrev = ioList[input + "_input"];

            _value = (Convert.ToInt32(Convert.ToString(_value, 2).PadLeft(Length, '0')));
            _value = ~value & 0xf;
            _value &= _valuePrev;

            Robot.Write(string.Format("extIOInputUpdate {0} {1}\r\n", input, _value));
        }

    }
}
