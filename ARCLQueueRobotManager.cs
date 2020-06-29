using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using ARCLTypes;

namespace ARCL
{
    public class ARCLQueueRobotManager
    {
        //Private
        private ARCLConnection Connection { get; }
        
        /// <summary>
        /// Raised when the Robots list is sycronized with the EM/LD robot queue.
        /// Raised when the connection is dropped.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="state"></param>
        public delegate void InSyncUpdateEventHandler(object sender, bool state);
        /// <summary>
        /// Raised when the Robots list is sycronized with the EM/LD robot queue.
        /// Raised when the connection is dropped.
        /// </summary>
        public event InSyncUpdateEventHandler InSync;
        /// <summary>
        /// True when the Robots list is sycronized with the EM/LD robot queue.
        /// False when the connection is dropped.
        /// </summary>
        public bool IsSynced { get; private set; } = false;

        public bool IsRunning { get; private set; } = false;

        /// <summary>
        /// Dictionary of Robots in the EM/LD queue.
        /// Not valid until InSync is true.
        /// </summary>
        public Dictionary<string, QueueRobotUpdateEventArgs> Robots
        {
            get
            {
                lock (RobotsDictLock)
                    return Robots_;
            }
        }
        private Dictionary<string, QueueRobotUpdateEventArgs> Robots_ { get; } = new Dictionary<string, QueueRobotUpdateEventArgs>();
        private object RobotsDictLock { get; set; } = new object();
        public int RobotCount
        {
            get
            {
                lock (RobotsDictLock)
                   return Robots_.Count();
            }
        }
        
        public bool IsRobotAvailable => RobotsAvailable > 0;
        public int RobotsAvailable
        {
            get
            {
                if (!IsSynced) return 0;

                lock (RobotsDictLock)
                {
                    int cnt = 0;
                    foreach(KeyValuePair<string, QueueRobotUpdateEventArgs> robot in Robots_)
                        if (robot.Value.Status == ARCLStatus.Available & robot.Value.SubStatus == ARCLSubStatus.Available)
                            cnt++;
                    return cnt;
                }
            }
        }
        public int RobotsUnAvailable => RobotCount - RobotsAvailable;
        /// <summary>
        /// Instantiate the class and store the ARCLConnection ref.
        /// </summary>
        /// <param name="connection"></param>
        public ARCLQueueRobotManager(ARCLConnection connection) => Connection = connection;

        /// <summary>
        /// Clears the Robots dictionary.
        /// Calls ReceiveAsync() on the ARCLConnection. **The connection must already be made.
        /// Initiates a QueueShowRobot command. 
        /// </summary>
        public void Start()
        {
            if (!Connection.IsConnected)
            {
                Stop();
                return;
            }

            lock (RobotsDictLock)
                Robots_.Clear();

            Connection.ConnectState += Connection_ConnectState;
            Connection.QueueRobotUpdate += Connection_QueueRobotUpdate;

            if (!Connection.IsReceivingAsync)
                Connection.ReceiveAsync();

            ThreadPool.QueueUserWorkItem(new WaitCallback(QueueShowRobotThread));
        }
        /// <summary>
        /// InSync is set to false.
        /// Calls StopReceiveAsync() on the ARCLConnection 
        /// </summary>
        public void Stop()
        {
            if (IsSynced)
                InSync?.BeginInvoke(this, false, null, null);
            IsSynced = false;

            Connection.ConnectState -= Connection_ConnectState;
            Connection.QueueRobotUpdate -= Connection_QueueRobotUpdate;

            Connection.StopReceiveAsync();
        }

        private void Connection_ConnectState(object sender, bool state)
        { 
            if (!state)
                Stop();
        }

        private void Connection_QueueRobotUpdate(object sender, QueueRobotUpdateEventArgs data)
        {
            if (data.IsEnd)
            {
                if (!IsSynced)
                {
                    IsSynced = true;
                    InSync?.BeginInvoke(this, true, null, null);
                }
                return;
            }

            lock (RobotsDictLock)
            {
                if (!Robots_.ContainsKey(data.Name))
                {
                    Robots_.Add(data.Name, data);
                    if(IsSynced) IsSynced = false;
                }
                else
                    Robots_[data.Name] = data;
            }
        }

        private void QueueShowRobot() => Connection.Write("queueShowRobot");

        private void QueueShowRobotThread(object sender)
        {
            while(!Connection.IsReceivingAsync) { };

            try
            {
                IsRunning = true;

                QueueShowRobot();

                Stopwatch sw = new Stopwatch();

                sw.Restart();
                while(sw.ElapsedMilliseconds < 1000)
                {
                    if (!Connection.IsReceivingAsync)
                    {
                        IsRunning = false;
                        return;
                    }
                    Thread.Sleep(10);
                }
            }
            catch
            {
                IsRunning = false;
            }
            finally
            {
                if(IsRunning)
                    ThreadPool.QueueUserWorkItem(new WaitCallback(QueueShowRobotThread));
            }
        }
    }
}
