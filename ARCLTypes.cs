using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARCLTypes
{

    //queueShow [echoString]
    //QueueRobot: <robotName> <robotStatus> <robotSubstatus> <echoString>
    //QueueRobot: "21" InProgress Driving ""
    //QueueShow: <id> <jobId> <priority> <status> <substatus> Goal<"goalName"> <”robotName”> <queued date> <queued time> <completed date> <completed time> <echoString> <failed count>
    //QueueShow: PICKUP3 JOB3 10 Completed None Goal "1" "21" 11/14/2012 11:49:23 11/14/2012 11:49:23 "" 0
    //EndQueueShow

    //queueMulti <number of goals> <number of fields per goal> <goal1> <goal1 args> <goal2> <goal2 args> … <goalN> <goalN args> [jobid]
    //queuemulti 4 2 x pickup 10 y pickup 19 z dropoff 20 t dropoff 20

    //QueueMulti: goal "x" with priority 10 id PICKUP1 and jobid JOB1 successfully queued
    //QueueMulti: goal<"goal1"> with priority<goal1_priority> id<PICKUPid_or_DROPOFFid> jobid<jobId> successfully queued
    //QueueMulti: goal<"goal2"> with priority<goal2_priority> id <PICKUPid_or_DROPOFFid> jobid<jobId> successfully queued and linked to <goal1_PICKUPid_or_DROPOFFid>
    //:
    //:
    //QueueMulti: goal<"goaln"> with priority<goaln_priority> id <PICKUPid_or_DROPOFFid> jobid<jobId> successfully queued and linked to <goal(n-1)_PICKUPid_or_DROPOFFid>
    //EndQueueMulti


    //queuePickupDropoff <goal1Name> <goal2Name> [priority1 or "default"] [priority2 or "default"] [jobId]
    //queuepickupdropoff goals<"goal1"> and<"goal2"> with priorities<priority1> and <priority2> ids<PICKUPid> and <DROPOFFid> jobId<jobId> successfully queued and linked to jobId<jobid>
    //QueueUpdate: <id> <jobId> <priority> <status=Pending> <substatus=ID_<id>> Goal <”goal2”>
    //             <robotName> <queued date> <queued time> <completed date = None > < completed time=None>
    //             <failed count>

    public enum ARCLStatus
    {
        Pending,
        Available,
        Interrupted,
        InProgress,
        Completed,
        Cancelling,
        Cancelled,
        BeforeModify,
        InterruptedByModify,
        AfterModify,
        UnAvailable,
        Failed,
        Loading
    }
    public enum ARCLSubStatus
    {
        None,
        AssignedRobotOffLine,
        NoMatchingRobotForLinkedJob,
        NoMatchingRobotForOtherSegment,
        NoMatchingRobot,
        ID_PICKUP,
        ID_DROPOFF,
        Available,
        Parking,
        Parked,
        DockParking,
        DockParked,
        UnAllocated,
        Allocated,
        BeforePickup,
        BeforeDropoff,
        BeforeEvery,
        Before,
        Buffering,
        Buffered,
        Driving,
        After,
        AfterEvery,
        AfterPickup,
        AfterDropoff,
        NotUsingEnterpriseManager,
        UnknownBatteryType,
        ForcedDocked,
        Lost,
        EStopPressed,
        Interrupted,
        InterruptedButNotYetIdle,
        OutgoingARCLConnLost,
        ModeIsLocked,
        Cancelled_by_MobilePlanner
    }

    public class QueueJobUpdateEventArgs : EventArgs
    {
        public enum GoalTypes
        {
            pickup,
            dropoff
        }

        public string Message { get; }
        public string ID { get; }
        public GoalTypes GoalType { get; }
        public int Order { get; }
        public string JobID { get; }
        public int Priority { get; }
        public ARCLStatus Status { get; }
        public ARCLSubStatus SubStatus { get; }
        public string GoalName { get; }
        public string RobotName { get; }
        public DateTime StartedOn { get; }
        public DateTime CompletedOn { get; }
        public int FailCount { get; }
        public bool IsEnd { get; }

        public QueueJobUpdateEventArgs(string goalName, GoalTypes goalType, int priority = 10)
        {
            GoalName = goalName;
            Priority = priority;
            GoalType = goalType;

            Status = ARCLStatus.Pending;
            SubStatus = ARCLSubStatus.None;
        }

        public QueueJobUpdateEventArgs(string msg)
        {
            Message = msg;

            string[] spl = msg.Split(' ');

            if (spl[0].StartsWith("EndQueueShow", StringComparison.CurrentCultureIgnoreCase))
            {
                IsEnd = true;
                return;
            }

                //QueueMulti: goal "Goal1" with priority 10 id PICKUP12 and job_id OWBQYSXSGZ successfully queued
                if (spl[0].StartsWith("queuemulti", StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    if (spl.Length < 13)
                        throw new QueueUpdateParseException();

                    GoalName = spl[2].Replace("\"", "");

                    if (int.TryParse(spl[5], out int pri))
                        Priority = pri;
                    else
                        throw new QueueUpdateParseException();

                    ID = spl[7];

                    if (ID.StartsWith("PICKUP"))
                    {
                        GoalType = GoalTypes.pickup;
                        if (int.TryParse(ID.Replace("PICKUP", ""), out int val))
                            Order = val;
                        else
                            throw new QueueUpdateParseException();

                    }
                    else if (ID.StartsWith("DROPOFF"))
                    {
                        GoalType = GoalTypes.dropoff;
                        if (int.TryParse(ID.Replace("DROPOFF", ""), out int val))
                            Order = val;
                        else
                            throw new QueueUpdateParseException();
                    }
                    else
                    {
                        throw new QueueUpdateParseException();
                    }

                    JobID = spl[10];

                }
                catch (QueueUpdateParseException)
                {

                }

                return;
            }

            //QueueShow: <id> <jobId> <priority> <status> <substatus> Goal <"goalName"> <”robotName”>
            //           <queued date> <queued time> <completed date> <completed time> <echoString> <failed count>
            //QueueShow: PICKUP3 JOB3 10 Completed None Goal "1" "21" 11/14/2012 11:49:23 11/14/2012 11:49:23 "" 0
            if (spl[0].StartsWith("QueueShow", StringComparison.CurrentCultureIgnoreCase) || spl[0].StartsWith("QueueUpdate", StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    if (spl.Length != 15)
                        throw new QueueUpdateParseException();

                    ID = spl[1];

                    if (ID.StartsWith("PICKUP"))
                    {
                        GoalType = GoalTypes.pickup;
                        if (int.TryParse(ID.Replace("PICKUP", ""), out int val))
                            Order = val;
                        else
                            throw new QueueUpdateParseException();

                    }
                    else if (ID.StartsWith("DROPOFF"))
                    {
                        GoalType = GoalTypes.dropoff;
                        if (int.TryParse(ID.Replace("DROPOFF", ""), out int val))
                            Order = val;
                        else
                            throw new QueueUpdateParseException();
                    }
                    else
                    {
                        throw new QueueUpdateParseException();
                    }

                    JobID = spl[2];

                    if (int.TryParse(spl[3], out int pri))
                        Priority = pri;
                    else
                        throw new QueueUpdateParseException();

                    if (Enum.TryParse(spl[4], out ARCLStatus status))
                        Status = status;
                    else
                        throw new QueueUpdateParseException();

                    if (!spl[5].StartsWith("ID_"))
                    {
                        if (Enum.TryParse(spl[5], out ARCLSubStatus subStatus))
                            SubStatus = subStatus;
                        else
                            throw new QueueUpdateParseException();
                    }

                    GoalName = spl[7].Replace("\"", "");

                    RobotName = spl[8].Replace("\"", "");

                    if (!spl[9].Equals("None"))
                    {
                        if (DateTime.TryParse(spl[9] + " " + spl[10], out DateTime dt))
                            StartedOn = dt;
                    }
                    else
                        throw new QueueUpdateParseException();

                    if (!spl[11].Equals("None"))
                    {
                        if (DateTime.TryParse(spl[11] + " " + spl[12], out DateTime dt))
                            CompletedOn = dt;
                    }

                    int i = 14;
                    if (spl[0].StartsWith("QueueUpdate"))
                        i = 13;

                    if (int.TryParse(spl[i], out int fail))
                        FailCount = fail;
                    else
                        throw new QueueUpdateParseException();

                }
                catch (QueueUpdateParseException)
                {
                    throw;
                }
                catch (Exception)
                {

                }
                return;
            }
        }

    } 
    public class QueueUpdateParseException : Exception
    {
        public QueueUpdateParseException()
        {
        }

        public QueueUpdateParseException(string message)
            : base(message)
        {
        }
    }
    public class QueueManagerJob
    {
        public string ID { get; }
        public int Priority
        {
            get
            {
                if (Goals.Count > 0)
                    return Goals[0].Priority;
                else
                    return 0;
            }
        }

        public int GoalCount => Goals.Count;

        public QueueJobUpdateEventArgs CurrentGoal
        {
            get
            {
                if (Goals.Count > 0)
                {
                    foreach (QueueJobUpdateEventArgs que in Goals)
                    {
                        if (que.Status != ARCLStatus.Completed)
                            return que;
                    }
                    return Goals[Goals.Count - 1];
                }
                else
                    return null;
            }
        }
        public List<QueueJobUpdateEventArgs> Goals { get; private set; }

        public ARCLStatus Status
        {
            get
            {
                if (Goals.Count > 0)
                {
                    foreach (QueueJobUpdateEventArgs que in Goals)
                    {
                        if (que.Status != ARCLStatus.Completed)
                            return que.Status;
                    }
                    return ARCLStatus.Completed;
                }
                else
                    return ARCLStatus.Loading;
            }
        }
        public ARCLSubStatus SubStatus
        {
            get
            {
                if (Goals.Count > 0)
                {
                    foreach (QueueJobUpdateEventArgs que in Goals)
                    {
                        if (que.Status != ARCLStatus.Completed)
                            return que.SubStatus;
                    }
                    return ARCLSubStatus.None;
                }
                else
                    return ARCLSubStatus.None;
            }
        }
        public DateTime StartedOn
        {
            get
            {
                if (Goals.Count > 0)
                    return Goals[0].StartedOn;
                else
                    return new DateTime();
            }
        }
        public DateTime CompletedOn
        {
            get
            {
                if (Goals.Count > 0)
                    return Goals[Goals.Count - 1].CompletedOn;
                else
                    return new DateTime();
            }
        }

        public QueueManagerJob(string id) => ID = id;

        public QueueManagerJob(QueueJobUpdateEventArgs goal)
        {
            ID = goal.JobID;
            AddQueAndSort(goal);
        }

        public void AddGoal(QueueJobUpdateEventArgs goal) => AddQueAndSort(goal);
        private void AddQueAndSort(QueueJobUpdateEventArgs goal)
        {
            Goals.Add(goal);
            Goals.Sort((foo1, foo2) => foo2.Order.CompareTo(foo1.Order));
        }
    }
    public class QueueManagerJobCompleteEventArgs : EventArgs
    {
        public QueueManagerJob Job { get; }
        public QueueManagerJobCompleteEventArgs(QueueManagerJob job)
        {
            Job = job;
        }
    }


    public class QueueRobotUpdateEventArgs : EventArgs
    {
        //QueueRobot: "robotName" robotStatus robotSubstatus echoString
        public string Message { get; private set; }
        public string Name { get; private set; }
        public ARCLStatus Status { get; private set; }
        public ARCLSubStatus SubStatus { get; private set; }
        public bool IsEnd { get; private set; }
        public QueueRobotUpdateEventArgs(string msg)
        {
            if (msg.StartsWith("EndQueue", StringComparison.CurrentCultureIgnoreCase))
            {
                IsEnd = true;
                return;
            }

            Message = msg;

            string[] spl = msg.Split(' ');

            Name = spl[1].Trim('\"');

            if (Enum.TryParse(spl[2], out ARCLStatus status))
                Status = status;
            else
                throw new QueueRobotParseException();

            if (Enum.TryParse(spl[3], out ARCLSubStatus subStatus))
                SubStatus = subStatus;
            else
                throw new QueueRobotParseException();
        }
    }

    public class QueueRobotParseException : Exception
    {
        public QueueRobotParseException()
        {
        }
        public QueueRobotParseException(string message) : base(message)
        {
        }
    }
    public class StatusUpdateEventArgs : EventArgs
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

        public StatusUpdateEventArgs(string msg, bool isReplay = false)
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
                float val;

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
    public class StatusDelayedEventArgs : EventArgs
    {
        public bool Delayed = false;
        public StatusDelayedEventArgs(bool delayed)
        {
            Delayed = delayed;
        }
    }
    public class RangeDeviceUpdateEventArgs : EventArgs
    {
        public bool IsCurrent { get; set; } = false;
        public string Name { get; set; } = string.Empty;
        public string Message { get; set; }
        public List<float[]> Data { get; set; } = new List<float[]>();

        public float Timestamp = 0;

        public RangeDeviceUpdateEventArgs(string msg, bool isReplay = false)
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

            IsCurrent = rawData[0].Contains("RangeDeviceGetCurrent");

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

    public class ExtIOUpdateParseException : Exception
    {
        public ExtIOUpdateParseException()
        {
        }

        public ExtIOUpdateParseException(string message)
            : base(message)
        {
        }
    }


    public class ExtIOSet
    {
        public string Name { get; private set; }
        public List<byte> Inputs { get; set; } = new List<byte>();
        public List<byte> Outputs { get; set; } = new List<byte>();

        public int InputCount => Inputs.Count() * 8;
        public int OutputCount => Outputs.Count() * 8;
        public bool HasInputs => Inputs.Count() > 0;
        public bool HasOutputs => Outputs.Count() > 0;
        public bool IsDump => Inputs.Count() > 0 & Outputs.Count() > 0;
        public bool IsEnd { get; private set; }
        public bool IsRemove => Inputs.Count() == 0 & Outputs.Count() == 0;

        public bool AddedForPendingUpdate { get; set; }

        public ExtIOSet(string name, List<byte> inputs, List<byte> outputs)
        {
            if (inputs == null) inputs = new List<byte>();
            if (outputs == null) outputs = new List<byte>();

            Name = name;
            Inputs.AddRange(inputs);
            Outputs.AddRange(outputs);
        }
        public ExtIOSet(bool isEnd = false) => IsEnd = isEnd;

        //<name> <valueInHexOrDec>
        public string WriteInputCommand
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("extIOInputUpdate ");
                sb.Append(Name);
                sb.Append(" 0x");

                for (int i = Inputs.Count() - 1; i >= 0; i--)
                    sb.Append(Inputs[i].ToString("X"));

                return sb.ToString();
            }
            
        }
        public string WriteOutputCommand => $"extIOOutputUpdate {Name} {Inputs[0]:X}";
        public string CreateSetCommand => $"extIOAdd {Name} {InputCount} {OutputCount}";
    }

    public class ExternalIOUpdateEventArgs : EventArgs
    {
        public string Message { get; }
        public ExtIOSet ExtIOSet { get; }

        public ExternalIOUpdateEventArgs(string msg)
        {
            Message = msg;

            string[] spl = msg.Split(' ');

            //ExtIODump: Test with 4 input(s), value = 0x0 and 4 output(s), value = 0x00
            if (spl[0].StartsWith("extiodump", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!spl[4].Contains("input")) throw new ExtIOUpdateParseException();
                if (!spl[10].Contains("output")) throw new ExtIOUpdateParseException();

                if (!int.TryParse(spl[3], out int num_in)) throw new ExtIOUpdateParseException();
                if (!int.TryParse(spl[9], out int num_ot)) throw new ExtIOUpdateParseException();

                List<byte> input = new List<byte>();
                string txt = CleanHexString(spl[7]);
                for (int i =0;i< num_in / 8; i++)
                {
                    if (txt.Length < 2)
                        txt = txt.PadLeft(2, '0');
                    else
                        txt = txt.Substring(i, 2);
                    input.Add(byte.Parse(txt, System.Globalization.NumberStyles.HexNumber));

                }
                    

                List<byte> output = new List<byte>();
                txt = CleanHexString(spl[7]);
                for (int i = 0; i < num_ot / 8; i++)
                {
                    if (txt.Length < 2)
                        txt = txt.PadLeft(2, '0');
                    else
                        txt = txt.Substring(i, 2);
                    output.Add(byte.Parse(txt, System.Globalization.NumberStyles.HexNumber));
                }

                this.ExtIOSet = new ExtIOSet(spl[1].Trim(), input, output);

                return;
            }

            //extIOInputUpdate: input <name> updated with <IO value in Hex> from <valueInDecOrHex> (asentered in ARCL)
            if (spl[0].StartsWith("extIOInputUpdate", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!spl[1].Contains("input")) throw new ExtIOUpdateParseException();

                int cnt = (spl[7].Count() - 2) / 2;

                List<byte> input = new List<byte>();
                string txt = CleanHexString(spl[5]);
                for (int i = 0; i < cnt; i++)
                    input.Add(byte.Parse(txt.Substring(i, 2), System.Globalization.NumberStyles.HexNumber));

                this.ExtIOSet = new ExtIOSet(spl[2].Trim(), input, null);

                return;
            }

            if (spl[0].StartsWith("extIOOutputUpdate", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!spl[1].Contains("output")) throw new ExtIOUpdateParseException();

                if (!ulong.TryParse(CleanHexString(spl[5]), System.Globalization.NumberStyles.HexNumber, System.Globalization.NumberFormatInfo.InvariantInfo, out ulong val_out)) throw new ExtIOUpdateParseException();

                byte[] val8_out = BitConverter.GetBytes(val_out);

                List<byte> output = new List<byte>();

                foreach (byte b in val8_out)
                    output.Add(b);

                this.ExtIOSet = new ExtIOSet(spl[2].Trim(), null, output);

                return;
            }

            //extIORemove: <name> removed
            if (spl[0].StartsWith("extIORemove", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!spl[2].Contains("removed")) throw new ExtIOUpdateParseException();

                this.ExtIOSet = new ExtIOSet(spl[1].Trim(), null, null);

                return;
            }

            //EndExtIODump
            if (spl[0].StartsWith("EndExtIODump", StringComparison.CurrentCultureIgnoreCase))
            {
                this.ExtIOSet = new ExtIOSet(true);

                return;
            }
        }

        private string CleanHexString(string str)
        {
            int pos = str.IndexOf('x');
            if (pos == -1) return str;

            return str.Remove(0, pos + 1);
        }

    }

    public class ConfigSection
    {
        public string Name { get; private set; }
        public string Value { get; private set; }
        public string Other { get; private set; }
        public ConfigSection(string name, string value, string other)
        {
            Name = name;
            Value = value;
            Other = other;
        }
    }

    public class ConfigSectionUpdateEventArgs : EventArgs
    {
        public string Message { get; private set; }
        public ConfigSection Section { get; private set; }
        public bool IsEnd { get; private set; }
        public ConfigSectionUpdateEventArgs(string msg)
        {
            Message = msg;

            if (msg.StartsWith("endof", StringComparison.CurrentCultureIgnoreCase))
            {
                IsEnd = true;
                return;
            }

            string[] spl = msg.Split(' ');

            string other = "";
            for (int i = 3; i < spl.Length; i++)
                other += " " + spl[i];

            Section = new ConfigSection(spl[1], spl[2], other);
        }
        //public void Update(string msg)
        //{
        //    if (msg.StartsWith("endof", StringComparison.CurrentCultureIgnoreCase))
        //    {
        //        IsEnd = true;
        //        return;
        //    }

        //    string[] spl = msg.Split(' ');

        //    string other = "";
        //    for (int i = 3; i < spl.Length; i++)
        //        other += " " + spl[i];

        //    Sections.Add(new ConfigSection(spl[1], spl[2], other));
        //}
    }
}
