using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ARCL
{
    public class ArclQueue
    {
        private static int SegmentCount = 0;
        private static int SegmentCounted = 0;
        private static bool IsJobComplete = false;

        private string JobName = "OAT";

        private Arcl Robot;

        public delegate void JobDoneReceivedEventHandler(object sender, Arcl.JobDoneEventArgs data);
        public event JobDoneReceivedEventHandler JobDoneReceived;

        public ArclQueue(Arcl robot)
        {
            Robot = robot;
            Robot.QueueDataReceived += Robot_QueueDataReceived;
        }

        private void Robot_QueueDataReceived(object sender, Arcl.QueueEventArgs data)
        {
            /*When the QueueDataReceived event fires, we take the message and read it
            If the message has the job name, we queue a job
            SegmentCount can reset, so keeping SegmentCount > 0 helps prevents accidentally queueing too many jobs.*/
            //Console.WriteLine("{0} with {1} segments done out of {2}", data.Message, SegmentCounted, SegmentCount);
            if (data.Message.Contains(JobName) && SegmentCount > 0)
            {
                //If the message contains "Completed", we remember that a SEGMENT has been completed
                if (data.Message.Contains("Completed"))
                    SegmentCounted++;

                //When the number of completed segments equals the number of segments a job has, it is completed.
                //We change the property IsJobComplete to true, but also fire an event that says we finished
                if (SegmentCounted >= SegmentCount)
                {
                    SegmentCount = 0;
                    SegmentCounted = 0;
                    IsJobComplete = true;
                    string message = JobName;
                    JobDoneReceived?.Invoke(this, new Arcl.JobDoneEventArgs(message));
                }
            }
        }

        public void QueueJob(string jobToQueue)
        {
            #region Variables
            string msg = null;
            string[] messages = null;

            Stopwatch timeout = new Stopwatch();
            int segmentCount = 0;
            #endregion

            //Clear out message buffer
            Robot.ReadMessage();
            //Write to the queue
            Robot.Write(jobToQueue);

            #region Get ID and number of segments
            timeout.Start();
            while (timeout.ElapsedMilliseconds < 5000)
            {
                msg = Robot.Read();


                if (msg.Contains("QueueMulti:"))
                {
                    if (!msg.Contains("EndQueueMulti"))
                    {
                        msg += Robot.Read("EndQueueMulti");
                    }

                    timeout.Stop();
                    foreach (string line in msg.Split('\r', '\n'))
                    {
                        if (line.Contains("QueueMulti:"))
                        {
                            segmentCount++;
                        }
                    }

                    messages = msg.Split('\r', '\n')[0].Split(' ');

                    for (int i = 0; i < messages.Count(); i++)
                    {
                        if (messages[i].Contains("job_id"))
                        {
                            JobName = messages[i + 1];
                        }
                    }
                    break;

                }
                else
                {
                    if (msg.Contains("successfully queued"))
                    {
                        msg = msg.Split('\r', '\n')[0];

                        timeout.Stop();
                        messages = msg.Split(' ');

                        for (int i = 0; i < messages.Count(); i++)
                        {
                            if (messages[i].Contains("PICKUP") || messages[i].Contains("DROPOFF"))
                            {
                                segmentCount++;
                            }
                            if (messages[i].Contains("job_id"))
                            {
                                JobName = messages[i + 1];
                            }
                        }
                        break;
                    }
                    else
                    {
                        JobName = "Missing Job ID";
                    }
                }
            }

            if (timeout.ElapsedMilliseconds >= 10000)
            {
                timeout.Stop();
            }

            SegmentCount = segmentCount;

            Console.WriteLine("Queued job: {0}, with {1} segment(s)", JobName, SegmentCount);
            #endregion

        }
        public bool QueuePickup(string goal, out string o_jobid)
        {
            bool status = Robot.Write("queuepickup " + goal);
            Thread.Sleep(500);
            string result = Robot.Read();

            // Find the beginning of the message.
            int idx = result.IndexOf("queuepickup");
            if (idx < 0)
            {
                o_jobid = "null";
                return false;
            }

            // Find the job_id field
            string sub = result.Substring(idx);
            idx = sub.IndexOf("job_id");
            if (idx < 0)
            {
                o_jobid = "null";
                return false;
            }

            // Get the job id
            sub = sub.Substring(idx);
            string[] splt = sub.Split(' ');
            o_jobid = splt[1];
            if (o_jobid.Contains("JOB"))
                return true;

            o_jobid = "null";
            return false;
        }
        public bool QueueCancel(string jobID)
        {
            bool status = Robot.Write("queuecancel jobid " + jobID);
            return status;
        }

        public bool QueueQuery(string jobID, out string o_status)
        {
            string result = String.Empty;
            bool status = Robot.Write("queuequery jobID " + jobID);
            Thread.Sleep(500);
            result = Robot.Read();
            o_status = result;
            return status;
        }

        public string getJobName()
        {
            return JobName;
        }
    }

}
