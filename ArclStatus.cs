using ARCLTypes;
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

        private readonly ARCLConnection ARCL;

        private Stopwatch Stopwatch = new Stopwatch();

        public int UpdateRate { get; private set; } = 50;
        public bool IsRunning { get; private set; } = false;
        public long TTL { get; private set; }
        public bool IsDelayed { get; private set; } = false;
        private bool Heartbeat = false;

        private List<string> Devices = new List<string>();

        public ArclStatus(ARCLConnection arcl)
        {
            ARCL = arcl;
        }

        public void Start(int updateRate, List<string> devices)
        {
            if (!ARCL.IsRunning)
                ARCL.StartRecieveAsync();

            ARCL.StatusReceived += Robot_StatusReceived;

            UpdateRate = updateRate;
            Devices = devices;
            IsRunning = true;
            ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncThread_DoWork));
        }

        public void Start(int updateRate)
        {
            if (!ARCL.IsRunning)
                ARCL.StartRecieveAsync();

            ARCL.StatusReceived += Robot_StatusReceived;

            UpdateRate = updateRate;
            Devices = new List<string>();
            IsRunning = true;
            ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncThread_DoWork));
        }

        private void Robot_StatusReceived(object sender, StatusEventArgs data)
        {
            Heartbeat = true;
            TTL = Stopwatch.ElapsedMilliseconds;
        }

        public void Stop()
        {
            ARCL.StatusReceived -= Robot_StatusReceived;

            IsRunning = false;
            Thread.Sleep(UpdateRate + 100);
        }

        private void AsyncThread_DoWork(object sender)
        {
            while (IsRunning)
            {
                if(!IsDelayed) Stopwatch.Reset();

                ARCL.Write("onelinestatus");
                foreach (string l in Devices)
                {
                    ARCL.Write("rangeDeviceGetCurrent " + l);
                }
                foreach (string l in Devices)
                {
                    ARCL.Write("rangeDeviceGetCumulative " + l);
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

