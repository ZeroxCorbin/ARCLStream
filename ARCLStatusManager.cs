using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ARCLTypes;

namespace ARCL
{
    public class ARCLStatusManager
    {
        //Public
        public delegate void StatusUpdateEventHandler(object sender, StatusUpdateEventArgs data);
        public event StatusUpdateEventHandler StatusUpdate;

        public delegate void RangeDeviceCurrentReceivedEventHandler(object sender, RangeDeviceUpdateEventArgs data);
        public event RangeDeviceCurrentReceivedEventHandler RangeDeviceCurrentUpdate;

        public delegate void RangeDeviceCumulativeUpdateEventHandler(object sender, RangeDeviceUpdateEventArgs data);
        public event RangeDeviceCumulativeUpdateEventHandler RangeDeviceCumulativeUpdate;

        public delegate void StatusDelayedEventHandler(StatusDelayedEventArgs data);
        public event StatusDelayedEventHandler StatusDelayed;

        //Private
        private ARCLConnection Connection { get; }
        private Stopwatch Stopwatch { get; } = new Stopwatch();

        //Public Read-only
        public int UpdateRate { get; private set; } = 50;
        public bool IsRunning { get; private set; } = false;
        public long TTL { get; private set; }
        public bool IsDelayed { get; private set; } = false;

        //Private
        private bool Heartbeat = false;
        public List<string> Devices { get; private set; } = new List<string>();

        //Public
        public ARCLStatusManager(ARCLConnection connection)
        {
            Connection = connection;
        }

        public void Start(int updateRate, List<string> devices)
        {
            if (!Connection.IsReceivingAsync)
                Connection.ReceiveAsync();

            UpdateRate = updateRate;
            Devices = devices;

            IsRunning = true;
            ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncThread_DoWork));

            Connection.StatusUpdate += Connection_StatusUpdate;
            Connection.RangeDeviceCurrentUpdate += Connection_RangeDeviceCurrentUpdate;
            Connection.RangeDeviceCumulativeUpdate += Connection_RangeDeviceCumulativeUpdate;
        }
        private void Connection_StatusUpdate(object sender, StatusUpdateEventArgs data)
        { 
            Heartbeat = true;
            TTL = Stopwatch.ElapsedMilliseconds;

            StatusUpdate?.Invoke(sender, data);
        }


        public void Start(int updateRate)
        {
            if (!Connection.IsReceivingAsync)
                Connection.ReceiveAsync();

            UpdateRate = updateRate;
            Devices.Clear();

            IsRunning = true;
            ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncThread_DoWork));

            Connection.StatusUpdate += Connection_StatusUpdate;
            Connection.RangeDeviceCurrentUpdate += Connection_RangeDeviceCurrentUpdate;
            Connection.RangeDeviceCumulativeUpdate += Connection_RangeDeviceCumulativeUpdate;
        }
        public void Stop()
        {
            Connection.StatusUpdate -= Connection_StatusUpdate;
            Connection.RangeDeviceCurrentUpdate -= Connection_RangeDeviceCurrentUpdate;
            Connection.RangeDeviceCumulativeUpdate -= Connection_RangeDeviceCumulativeUpdate;

            IsRunning = false;
            Thread.Sleep(UpdateRate + 100);

            Devices.Clear();
        }

        //Private
        private void AsyncThread_DoWork(object sender)
        {
            while (IsRunning)
            {
                if(!IsDelayed) Stopwatch.Reset();

                Connection.Write("onelinestatus");
                foreach (string l in Devices)
                {
                    Connection.Write("rangeDeviceGetCurrent " + l);
                }
                foreach (string l in Devices)
                {
                    Connection.Write("rangeDeviceGetCumulative " + l);
                }

                Heartbeat = false;

                Thread.Sleep(UpdateRate);

                if (Heartbeat)
                {
                    if(IsDelayed) StatusDelayed?.Invoke(new StatusDelayedEventArgs(false));
                    IsDelayed = false;
                }
                else
                {
                    if (!IsDelayed) StatusDelayed?.Invoke(new StatusDelayedEventArgs(true));
                    IsDelayed = true;
                }
            }
        }
        private void Connection_RangeDeviceCurrentUpdate(object sender, RangeDeviceUpdateEventArgs data) => RangeDeviceCurrentUpdate?.Invoke(sender, data);
        private void Connection_RangeDeviceCumulativeUpdate(object sender, RangeDeviceUpdateEventArgs data)=> RangeDeviceCumulativeUpdate?.Invoke(sender, data);
    }
}

