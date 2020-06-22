using ARCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace ARCLStream
{
    public class ARCLSychronousCommands : ARCLConnection
    {
        //Public
        public ARCLSychronousCommands(string connectionString) : base(connectionString) { }

        public List<string> GetRangeDevices()
        {
            List<string> dev = new List<string>();

            this.Write("rangeDeviceList");
            System.Threading.Thread.Sleep(500);

            string msg = this.Read();
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

            //List<string> dev = new List<string>();

            this.Write(string.Format("getconfigsectionvalues {0}\r\n", section));
            System.Threading.Thread.Sleep(500);

            string msg = this.ReadMessage();
            string[] rawDevices = msg.Split('\r');

            foreach (string s in rawDevices)
            {
                if (s.IndexOf("GetConfigSectionValue:") >= 0)
                {
                    SectionValues.Add(s.Split(':')[1].Trim());
                }
            }

            //string rawMessage = null;
            //string fullMessage = "";
            //string lastMessage = "";
            //string[] messages;
            //Thread.Sleep(100);
            //this.ReadMessage();
            //this.Write(string.Format("getconfigsectionvalues {0}\r\n", section));
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            //do
            //{
            //    while (String.IsNullOrEmpty(rawMessage))
            //    {
            //        rawMessage = this.ReadLine();

            //        fullMessage = rawMessage;
            //        if (sw.ElapsedMilliseconds > 1000)
            //        {
            //            throw new TimeoutException();
            //        }
            //    }
            //    sw.Restart();

            //    if (rawMessage.Contains("CommandError"))
            //    {
            //        Console.WriteLine("Config section \"{0}\" does not exist", section);
            //        rawMessage = "EndOfGetConfigSectionValues";
            //        fullMessage = rawMessage;
            //    }
            //    else
            //    {
            //        while (!rawMessage.Contains("EndOfGetConfigSectionValues"))
            //        {
            //            rawMessage = this.ReadLine();

            //            if (!string.IsNullOrEmpty(rawMessage))
            //            {
            //                fullMessage += rawMessage;
            //            }

            //        }
            //        sw.Stop();
            //    }

            //    messages = this.MessageParse(fullMessage);

            //    foreach (string message in messages)
            //    {
            //        if (message.Contains("GetConfigSectionValue:"))
            //        {
            //            SectionValues.Add(message.Split(':')[1].Trim());
            //        }
            //        if (message.Contains("EndOfGetConfigSectionValues"))
            //        {
            //            lastMessage = message;
            //            break;
            //        }
            //        if (message.Contains("CommandErrorDescription: No section of name"))
            //        {
            //            lastMessage = "EndOfGetConfigSectionValues";
            //        }
            //    }
            //} while (!lastMessage.Contains("EndOfGetConfigSectionValues"));

            return SectionValues;
        }

        public bool Goto(string goalname) => this.Write($"goto {goalname}");
        public bool GotoPoint(int x, int y, int heading) => this.Write($"gotopoint {x} {y} {heading}");
        public bool PatrolOnce(string routename) => this.Write($"patrolonce {routename}");
        public bool Patrol(string routename) => this.Write($"patrol {routename}");
        public bool Say(string message) => this.Write($"say {message}");
        public bool Stop() => this.Write("stop");
        public bool Dock() => this.Write("dock");
        public bool Undock() => this.Write("undock");
        public bool Localize(int x, int y, int heading) => this.Write($"localizeToPoint {x} {y} {heading}");





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
            List<string> sectionValues;


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

            if (!Count.ContainsKey(name + "_input"))
            {
                Count.Add(name + "_input", count);
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
            Write("\r\n");
            ReadMessage();
            Write(string.Format("extioAdd {0} {1} {2}\r\n", name, numIn.ToString(), numOut.ToString()));
            string message = Read();
            int attempts = 0;
            while (String.IsNullOrEmpty(message))
            {
                message = Read();
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
            Write("configStart\r\n");
            Write("configAdd Section External Digital Inputs\r\n");
            for (int i = 1; i <= numIn; i++)
            {
                Write("configAdd _beginList " + name + "_Input_" + i + "\r\n");
                Write("configAdd Alias " + name + "_i" + i + "\r\n");
                Write("configAdd _beginList OnList\r\n");
                Write("configAdd Count 1\r\n");
                Write("configAdd Type1 custom\r\n");
                Write("configAdd _endList OnList\r\n");
                Write("configAdd _endList " + name + "_Input_" + i + "\r\n");
            }

            Write("configAdd Section External Digital Outputs\r\n");
            for (int o = 1; o <= numIn; o++)
            {
                Write("configAdd _beginList " + name + "_Output_" + o + "\r\n");
                Write("configAdd Alias " + name + "_o" + o + "\r\n");
                Write("configAdd Type1 custom\r\n");
                Write("configAdd _endList " + name + "_Output_" + o + "\r\n");
            }

            Write("configParse\r\n");
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
                Write(string.Format("extioInputUpdate {0} {1}", ioName, List[ioName + "_output"]));
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
                    Write(string.Format("extioInputUpdate {0} {1}", ioName, List[ioName + "_output"]));
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

            Write(string.Format("extIOOutputUpdate {0} {1}\r\n", output, _value));
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

            Write(string.Format("extioOutputUpdate {0} {1}\r\n", name, value));
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
            Write(string.Format("extIOOutputUpdate {0} {1}\r\n", output, _value));

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

            Write(string.Format("extIOInputUpdate {0} {1}\r\n", input, _value));
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

            Write(string.Format("extIOInputUpdate {0} {1}\r\n", input, _value));
        }

        public double StateOfCharge()
        {
            this.Read();
            this.Write("status");
            Thread.Sleep(25);
            string status;
            do
            {
                status = this.Read();
            }
            while (!status.Contains("Temperature"));

            Regex regex = new Regex(@"StateOfCharge:");
            string[] output = regex.Split(status);
            string[] charge = output[1].Split(new char[] { '\n', '\r' });

            return Convert.ToDouble(charge[0]);
        }
        public string GetLocation()
        {
            this.Read();
            this.Write("status");
            Thread.Sleep(25);
            string status = Read("Temperature");


            Regex regex = new Regex(@"Location:");
            string[] output = regex.Split(status);
            string[] location = output[1].Split(new char[] { '\n', '\r' });

            return location[0].Trim();
        }

    }
}
