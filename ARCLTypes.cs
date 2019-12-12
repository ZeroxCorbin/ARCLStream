using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARCLTypes
{
    public class ARCLEventArgs : EventArgs
    {
        public string Message { get; }
        public ARCLEventArgs(string msg)
        {
            Message = msg;
        }
    }

    public class ARCLStreamEventArgs : EventArgs
    {
        public string Message { get; }
        public ARCLStreamEventArgs(string msg)
        {
            Message = msg;
        }
    }

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


    public enum QueueStatus
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

    //ID_PICKUPxx<where PICKUPxx is the jobSegment ID for which this Job Segment is waiting>
    //ID_DROPOFFxx <where DROPOFFxx is the jobSegment ID for which this Job Segment is waiting>
    public enum QueueSubStatus
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

        public int GoalCount
        {
            get
            {
                return _Goals.Count;
            }
        }

        public QueueUpdateEventArgs CurrentGoal {
            get
            {
                if (Goals.Count > 0)
                {
                    foreach (QueueUpdateEventArgs que in _Goals)
                    {
                        if (que.Status != QueueStatus.Completed)
                            return que;
                    }
                    return _Goals[_Goals.Count - 1];
                }
                else
                    return null;
            }
        }

        private List<QueueUpdateEventArgs> _Goals = new List<QueueUpdateEventArgs>();
        public List<QueueUpdateEventArgs> Goals
        {
            get
            {
                return _Goals;
            }
        }

        public QueueStatus Status
        {
            get
            {
                if (Goals.Count > 0)
                {
                    foreach(QueueUpdateEventArgs que in _Goals)
                    {
                        if (que.Status != QueueStatus.Completed)
                            return que.Status;
                    }
                    return QueueStatus.Completed;
                }
                else
                    return QueueStatus.Loading;
            }
        }
        public QueueSubStatus SubStatus {
            get
            {
                if (Goals.Count > 0)
                {
                    foreach (QueueUpdateEventArgs que in _Goals)
                    {
                        if (que.Status != QueueStatus.Completed)
                            return que.SubStatus;
                    }
                    return QueueSubStatus.None;
                }
                else
                    return QueueSubStatus.None;
            }
        }
        public DateTime StartedOn {
            get
            {
                if (Goals.Count > 0)
                    return _Goals[0].StartedOn;
                else
                    return new DateTime();
            }
        }
        public DateTime CompletedOn {
            get
            {
                if (Goals.Count > 0)
                    return _Goals[_Goals.Count-1].CompletedOn;
                else
                    return new DateTime();
            }
        }

        public QueueManagerJob(string id)
        {
            ID = id;
        }

        public QueueManagerJob(QueueUpdateEventArgs goal)
        {
            ID = goal.JobID;
            AddQueAndSort(goal);
        }

        public void AddGoal(QueueUpdateEventArgs goal)
        {
            AddQueAndSort(goal);
        }

        private void AddQueAndSort(QueueUpdateEventArgs goal)
        {
            _Goals.Add(goal);
            _Goals.Sort((foo1, foo2) => foo2.Order.CompareTo(foo1.Order));
        }
    }

    public class JobDoneEventArgs : EventArgs
    {
        public QueueManagerJob Job { get; }
        public JobDoneEventArgs(QueueManagerJob job)
        {
            Job = job;
        }
    }

    public class QueueUpdateEventArgs : EventArgs
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
        public QueueStatus Status { get; }
        public QueueSubStatus SubStatus { get; }
        public string GoalName { get; }
        public string RobotName { get; }
        public DateTime StartedOn { get; }
        public DateTime CompletedOn { get; }
        public int FailCount { get; }
        public QueueUpdateEventArgs(string goalName, GoalTypes goalType, int priority = 10)
        {
            GoalName = goalName;
            Priority = priority;
            GoalType = goalType;

            Status = QueueStatus.Pending;
            SubStatus = QueueSubStatus.None;
        }
        public QueueUpdateEventArgs(string msg)
        {
            Message = msg;

            string[] spl = msg.Split(' ');

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
            }

            //QueueShow: <id> <jobId> <priority> <status> <substatus> Goal <"goalName"> <”robotName”>
            //           <queued date> <queued time> <completed date> <completed time> <echoString> <failed count>
            //QueueShow: PICKUP3 JOB3 10 Completed None Goal "1" "21" 11/14/2012 11:49:23 11/14/2012 11:49:23 "" 0
            if (spl[0].StartsWith("QueueShow") || spl[0].StartsWith("QueueUpdate"))
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

                    if (Enum.TryParse(spl[4], out QueueStatus status))
                        Status = status;
                    else
                        throw new QueueUpdateParseException();

                    if (!spl[5].StartsWith("ID_"))
                    {
                        if (Enum.TryParse(spl[5], out QueueSubStatus subStatus))
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
            }
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

    public class ExtIOEventArgs : EventArgs
    {
        public string Message { get; }
        public ExtIOEventArgs(string msg)
        {
            Message = msg;
        }
    }
}
