using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ARCL
{
    public class ArclStatus
    {
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

        private readonly Arcl Robot;

        private Stopwatch Stopwatch = new Stopwatch();

        public int UpdateRate { get; private set; } = 50;
        public bool IsRunning { get; private set; } = false;
        public long TTL { get; private set; }
        public bool IsDelayed { get; private set; } = false;
        private bool Heartbeat = false;

        private List<string> Devices = new List<string>();

        public ArclStatus(Arcl robot)
        {
            Robot = robot;
        }

        public void Start(int updateRate, List<string> devices)
        {
            Robot.StartRecieveAsync();
            Robot.StatusDataReceived += Robot_StatusDataReceived;

            UpdateRate = updateRate;
            Devices = devices;
            IsRunning = true;
            ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncThread_DoWork));
        }

        private void Robot_StatusDataReceived(object sender, Arcl.StatusEventArgs data)
        {
            Heartbeat = true;
            TTL = Stopwatch.ElapsedMilliseconds;
        }

        public void Stop()
        {
            Robot.StopRecieveAsync();
            Robot.StatusDataReceived -= Robot_StatusDataReceived;

            IsRunning = false;
            Thread.Sleep(UpdateRate + 100);
        }

        private void AsyncThread_DoWork(object sender)
        {
            while (IsRunning)
            {
                if(!IsDelayed) Stopwatch.Reset();

                Robot.Write("onelinestatus");
                foreach (string l in Devices)
                {
                    Robot.Write("rangeDeviceGetCurrent " + l);
                }
                foreach (string l in Devices)
                {
                    Robot.Write("rangeDeviceGetCumulative " + l);
                }

                Heartbeat = false;

                Thread.Sleep(UpdateRate);

                if (Heartbeat)
                {
                    if(IsDelayed) Delayed?.Invoke(new DelayedEventArgs(false));
                    IsDelayed = false;
                }
                else
                {
                    if (!IsDelayed) Delayed?.Invoke(new DelayedEventArgs(true));
                    IsDelayed = true;
                }
            }
        }
    }
}

