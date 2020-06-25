using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ARCLTypes;

namespace ARCL
{
    public class ARCLQueueRobotManager
    {
        /// <summary>
        /// Fires when the Robots list is sycronized with the EM/LD robot queue.
        /// </summary>
        public delegate void InSyncUpdateEventHandler(object sender, bool data);
        public event InSyncUpdateEventHandler InSync;

        /// <summary>
        /// True when the Robots list is sycronized with the EM/LD robot queue.
        /// </summary>
        public bool IsSynced { get; private set; } = false;
        public Dictionary<string, QueueRobotUpdateEventArgs> Robots { get; private set; }

        //Private
        private ARCLConnection Connection { get; }

        //Public
        public ARCLQueueRobotManager(ARCLConnection connection) => Connection = connection;

        public void Start()
        {
            if (!Connection.IsReceivingAsync)
                Connection.ReceiveAsync();

            Connection.QueueRobotUpdate += Connection_QueueRobotUpdate;

            Robots = new Dictionary<string, QueueRobotUpdateEventArgs>();

            //Initiate the the load of the current queue
            QueueShowRobot();
        }
        public void Stop()
        {
            if (IsSynced)
                InSync?.BeginInvoke(this, false, null, null);
            IsSynced = false;

            Connection.QueueRobotUpdate -= Connection_QueueRobotUpdate;
            Connection.StopReceiveAsync();
        }

        private void Connection_QueueRobotUpdate(object sender, QueueRobotUpdateEventArgs data)
        {
            if (data.IsEnd & !IsSynced)
            {
                IsSynced = true;
                InSync?.BeginInvoke(this, true, null, null);
                return;
            }

            if (!Robots.ContainsKey(data.Name))
                Robots.Add(data.Name, data);
            else
                Robots[data.Name] = data;
        }

        private bool QueueShowRobot() => Connection.Write("queueShowRobot");
    }
}
