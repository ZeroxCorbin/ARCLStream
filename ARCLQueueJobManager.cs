﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ARCLTypes;

namespace ARCL
{
    public class ARCLQueueJobManager
    {
        //Public
        public delegate void JobCompleteEventHandler(object sender, QueueJobUpdateEventArgs data);
        public event JobCompleteEventHandler JobComplete;

        /// <summary>
        /// Raised when the Jobs list is sycronized with the EM job queue.
        /// </summary>
        public delegate void InSyncEventHandler(object sender, bool state);
        public event InSyncEventHandler InSync;
        /// <summary>
        /// True when the Jobs list is sycronized with the EM job queue.
        /// </summary>
        public bool IsSynced { get; private set; } = false;

        public Dictionary<string, QueueManagerJob> _Jobs { get; private set; }
        public ReadOnlyDictionary<string, QueueManagerJob> Jobs
        {
            get
            {
                lock (JobsLock)
                    return new ReadOnlyDictionary<string, QueueManagerJob>(_Jobs);
            }
        }
        private object JobsLock { get; set; } = new object();
        //Private
        private ARCLConnection Connection { get; }

        //Public
        public ARCLQueueJobManager(ARCLConnection connection) => Connection = connection;

        /// <summary>
        /// Starts the QueueManager.
        /// This will clear and reload the Jobs list.
        /// Stop() must be called before Nulling QM or callling Start() again.
        /// </summary>
        public void Start()
        {
            if (!Connection.IsReceivingAsync)
                Connection.ReceiveAsync();

            Connection.QueueJobUpdate += Connection_QueueJobUpdate;

            _Jobs = new Dictionary<string, QueueManagerJob>();

            //Initiate the the load of the current queue
            QueueShow();
        }
        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            Connection.QueueJobUpdate -= Connection_QueueJobUpdate;
            Connection.StopReceiveAsync();
        }

        public string QueueMulti(List<QueueJobUpdateEventArgs> goals)
        {
            StringBuilder msg = new StringBuilder();
            string space = " ";

            msg.Append("QueueMulti");
            msg.Append(space);

            msg.Append(goals.Count.ToString());
            msg.Append(space);

            msg.Append("2");
            msg.Append(space);

            foreach (QueueJobUpdateEventArgs g in goals)
            {
                msg.Append(g.GoalName);
                msg.Append(space);

                msg.Append(Enum.GetName(typeof(QueueJobUpdateEventArgs.GoalTypes), g.GoalType));
                msg.Append(space);

                msg.Append(g.Priority.ToString());
                msg.Append(space);
            }

            string id;
            if (goals[0].JobID == "")
                id = GetNewJobID();
            else
                id = goals[0].JobID;
            msg.Append(id);

            Connection.Write(msg.ToString());

            return id;
        }

        public bool UpdateJobSegment(ref QueueManagerJob job, string newGoalName, int segmentIndex = 1)
        {
            switch (job.Goals[segmentIndex].Status)
            {
                case ARCLStatus.Pending:
                    break;
                case ARCLStatus.InProgress:
                    switch (job.Goals[segmentIndex].SubStatus)
                    {
                        case ARCLSubStatus.UnAllocated:
                        case ARCLSubStatus.Allocated:
                        case ARCLSubStatus.Driving:
                            break;
                        default:
                            return false;
                    }
                    break;
                default:
                    return false;
            }

            QueueModify(job.Goals[segmentIndex].ID, "goal", newGoalName);

            return true;
        }

        public bool CancelJob(ref QueueManagerJob job)
        {
            return true;
        }
        //Private
        private bool QueueShow() => Connection.Write("QueueShow");
        private bool QueueModify(string id, string type, string value) => Connection.Write($"queueModify {id} {type} {value}");
        private void Connection_QueueJobUpdate(object sender, QueueJobUpdateEventArgs data)
        {
            lock (JobsLock)
            {
                if (data.IsEnd & !IsSynced)
                {
                    IsSynced = true;
                    InSync?.BeginInvoke(this, true, null, null);
                    return;
                }

                if (!_Jobs.ContainsKey(data.JobID))
                {
                    QueueManagerJob job = new QueueManagerJob(data);
                    _Jobs.Add(job.ID, job);
                }
                else
                {
                    int i = 0;
                    bool found = false;
                    foreach (QueueJobUpdateEventArgs currentQue in _Jobs[data.JobID].Goals.ToList())
                    {
                        if (currentQue.ID.Equals(data.ID))
                        {
                            _Jobs[data.JobID].Goals[i] = data;
                            found = true;
                        }

                        i++;
                    }
                    if (!found) _Jobs[data.JobID].AddGoal(data);
                }

                if (_Jobs[data.JobID].Status == ARCLStatus.Completed || _Jobs[data.JobID].Status == ARCLStatus.Cancelled)
                    JobComplete?.Invoke(new object(), data);
            }

        }
        private string GetNewJobID()
        {
            string newID = RandomString(10, false);

            while (_Jobs.ContainsKey(newID))
                newID = RandomString(10, false);

            return newID;
        }
        private string RandomString(int size, bool lowerCase)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            if (lowerCase)
                return builder.ToString().ToLower();
            return builder.ToString();
        }

        //public void QueueJob(string jobToQueue)
        //{
        //    #region Variables
        //    string msg;
        //    string[] messages;

        //    Stopwatch timeout = new Stopwatch();
        //    int segmentCount = 0;
        //    #endregion

        //    //Clear out message buffer
        //    Connection.Read();
        //    //Write to the queue
        //    Connection.Write(jobToQueue);

        //    #region Get ID and number of segments
        //    timeout.Start();
        //    while (timeout.ElapsedMilliseconds < 5000)
        //    {
        //        msg = Connection.Read();

        //        if (msg.Contains("QueueMulti:"))
        //        {
        //            if (!msg.Contains("EndQueueMulti"))
        //            {
        //                msg += Connection.Read("EndQueueMulti");
        //            }

        //            timeout.Stop();
        //            foreach (string line in msg.Split('\r', '\n'))
        //            {
        //                if (line.Contains("QueueMulti:"))
        //                {
        //                    segmentCount++;
        //                }
        //            }

        //            messages = msg.Split('\r', '\n')[0].Split(' ');

        //            for (int i = 0; i < messages.Count(); i++)
        //            {
        //                if (messages[i].Contains("job_id"))
        //                {
        //                    //JobName = messages[i + 1];
        //                }
        //            }
        //            break;

        //        }
        //        else
        //        {
        //            if (msg.Contains("successfully queued"))
        //            {
        //                msg = msg.Split('\r', '\n')[0];

        //                timeout.Stop();
        //                messages = msg.Split(' ');

        //                for (int i = 0; i < messages.Count(); i++)
        //                {
        //                    if (messages[i].Contains("PICKUP") || messages[i].Contains("DROPOFF"))
        //                    {
        //                        segmentCount++;
        //                    }
        //                    if (messages[i].Contains("job_id"))
        //                    {
        //                        //JobName = messages[i + 1];
        //                    }
        //                }
        //                break;
        //            }
        //            else
        //            {
        //                //JobName = "Missing Job ID";
        //            }
        //        }
        //    }

        //    if (timeout.ElapsedMilliseconds >= 10000)
        //    {
        //        timeout.Stop();
        //    }

        //    //SegmentCount = segmentCount;

        //    //Console.WriteLine("Queued job: {0}, with {1} segment(s)", JobName, SegmentCount);
        //    #endregion

        //}

        //public bool QueuePickup(string goal, out string o_jobid)
        //{
        //    bool status = Connection.Write("queuepickup " + goal);
        //    Thread.Sleep(500);
        //    string result = Connection.Read();

        //    // Find the beginning of the message.
        //    int idx = result.IndexOf("queuepickup");
        //    if (idx < 0)
        //    {
        //        o_jobid = "null";
        //        return false;
        //    }

        //    // Find the job_id field
        //    string sub = result.Substring(idx);
        //    idx = sub.IndexOf("job_id");
        //    if (idx < 0)
        //    {
        //        o_jobid = "null";
        //        return false;
        //    }

        //    // Get the job id
        //    sub = sub.Substring(idx);
        //    string[] splt = sub.Split(' ');
        //    o_jobid = splt[1];
        //    if (o_jobid.Contains("JOB"))
        //        return true;

        //    o_jobid = "null";
        //    return false;
        //}
        //public bool QueuePickupDropoff(string goalPick, string goalDrop, out string o_jobid)
        //{
        //    bool status = Connection.Write("queuepickup " + goalPick);
        //    Thread.Sleep(500);
        //    string result = Connection.Read();

        //    // Find the beginning of the message.
        //    int idx = result.IndexOf("queuepickup");
        //    if (idx < 0)
        //    {
        //        o_jobid = "null";
        //        return false;
        //    }

        //    // Find the job_id field
        //    string sub = result.Substring(idx);
        //    idx = sub.IndexOf("job_id");
        //    if (idx < 0)
        //    {
        //        o_jobid = "null";
        //        return false;
        //    }

        //    // Get the job id
        //    sub = sub.Substring(idx);
        //    string[] splt = sub.Split(' ');
        //    o_jobid = splt[1];
        //    if (o_jobid.Contains("JOB"))
        //        return true;

        //    o_jobid = "null";
        //    return false;
        //}
        //public bool QueueCancel(string jobID)
        //{
        //    bool status = Connection.Write("queuecancel jobid " + jobID);
        //    return status;
        //}

        //public bool QueueQuery(string jobID, out string o_status)
        //{
        //    string result = String.Empty;
        //    bool status = Connection.Write("queuequery jobID " + jobID);
        //    Thread.Sleep(500);
        //    result = Connection.Read();
        //    o_status = result;
        //    return status;
        //}
    }
}
