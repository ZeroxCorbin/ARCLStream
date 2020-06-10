using ARCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ARCLStream
{
    public class ARCLSychronousCommands
    {
        //Private
        public ARCLConnection Connection { get; private set; }

        //Public
        public ARCLSychronousCommands(string connectionString) => Connection = new ARCLConnection(connectionString);

        public List<string> GetRangeDevices()
        {
            List<string> dev = new List<string>();

            Connection.Write("rangeDeviceList");
            System.Threading.Thread.Sleep(500);

            string msg = Connection.ReadMessage();
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

            Connection.Write("getgoals");
            System.Threading.Thread.Sleep(500);

            string goalsString = Connection.ReadMessage();
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
            Connection.ReadMessage();
            Connection.Write("getroutes");
            System.Threading.Thread.Sleep(500);

            string routesString = Connection.ReadMessage();
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
            Connection.ReadMessage();
            Connection.Write("inputlist");
            System.Threading.Thread.Sleep(500);

            string inputsString = Connection.ReadMessage();
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
            Connection.ReadMessage();
            Connection.Write("outputlist");
            System.Threading.Thread.Sleep(500);

            string outputsString = Connection.ReadMessage();
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
            Connection.ReadMessage();
            Connection.Write("inputQuery " + inputname);
            System.Threading.Thread.Sleep(50);

            string status = Connection.ReadMessage();
            string input = status.Replace("InputList: ", String.Empty);
            input = input.Trim(new char[] { '\n', '\r' });

            if (input.Contains("on"))
                return true;
            else
                return false;
        }
        public bool CheckOutput(string outputname)
        {
            Connection.ReadMessage();
            Connection.Write("outputQuery " + outputname);
            System.Threading.Thread.Sleep(50);

            string status = Connection.ReadMessage();
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
                Connection.Write("outputOn " + outputname);
            else
                Connection.Write("outputOff " + outputname);

            System.Threading.Thread.Sleep(50);

            string status = Connection.ReadMessage();
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

            Connection.Write(string.Format("getconfigsectionvalues {0}\r\n", section));
            System.Threading.Thread.Sleep(500);

            string msg = Connection.ReadMessage();
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

        public bool Goto(string goalname) => Connection.Write($"goto {goalname}");
        public bool GotoPoint(int x, int y, int heading) => Connection.Write($"gotopoint {x} {y} {heading}");
        public bool PatrolOnce(string routename) => Connection.Write($"patrolonce {routename}");
        public bool Patrol(string routename) => Connection.Write($"patrol {routename}");
        public bool Say(string message) => Connection.Write($"say {message}");
        public bool Stop() => Connection.Write("stop");
        public bool Dock() => Connection.Write("dock");
        public bool Undock() => Connection.Write("undock");
        public bool Localize(int x, int y, int heading) => Connection.Write($"localizeToPoint {x} {y} {heading}");


        public double StateOfCharge()
        {
            Connection.ReadMessage();
            Connection.Write("status");
            Thread.Sleep(25);
            string status;
            do
            {
                status = Connection.ReadMessage();
            }
            while (!status.Contains("Temperature"));

            Regex regex = new Regex(@"StateOfCharge:");
            string[] output = regex.Split(status);
            string[] charge = output[1].Split(new char[] { '\n', '\r' });

            return Convert.ToDouble(charge[0]);
        }
        public string GetLocation()
        {
            Connection.ReadMessage();
            Connection.Write("status");
            Thread.Sleep(25);
            string status;
            do
            {
                status = Connection.ReadMessage();
            }
            while (!status.Contains("Temperature"));

            Regex regex = new Regex(@"Location:");
            string[] output = regex.Split(status);
            string[] location = output[1].Split(new char[] { '\n', '\r' });

            return location[0].Trim();
        }

    }
}
