using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using ARCLTypes;
using SocketManagerNS;

namespace ARCL
{
    public class ARCLConnection : SocketManager
    {
        //Public
        public delegate void ARCLConnectedEventHandler(object sender, SocketStateEventArgs data);
        public event ARCLConnectedEventHandler ARCLConnectState;

        public delegate void QueueJobEventHandler(object sender, QueueJobUpdateEventArgs data);
        public event QueueJobEventHandler QueueJobUpdate;

        public delegate void QueueRobotUpdateEventHandler(object sender, QueueRobotUpdateEventArgs data);
        public event QueueRobotUpdateEventHandler QueueRobotUpdate;

        public delegate void StatusUpdateEventHandler(object sender, StatusUpdateEventArgs data);
        public event StatusUpdateEventHandler StatusUpdate;

        public delegate void RangeDeviceCurrentUpdateEventHandler(object sender, RangeDeviceUpdateEventArgs data);
        public event RangeDeviceCurrentUpdateEventHandler RangeDeviceCurrentUpdate;

        public delegate void RangeDeviceCumulativeUpdateEventHandler(object sender, RangeDeviceUpdateEventArgs data);
        public event RangeDeviceCumulativeUpdateEventHandler RangeDeviceCumulativeUpdate;

        public delegate void ExternalIOUpdateEventHandler(object sender, ExternalIOUpdateEventArgs data);
        public event ExternalIOUpdateEventHandler ExternalIOUpdate;

        public delegate void ConfigSectionUpdateEventHandler(object sender, ConfigSectionUpdateEventArgs data);
        public event ConfigSectionUpdateEventHandler ConfigSectionUpdate;

        //Public
        public ARCLConnection(string connectionString) : base(connectionString) { }

        //Public Override SocketManager
        public new bool Connect(int timeout = 3000)
        {
            if (base.Connect(timeout))
            {
                if (Login())
                {
                    ARCLConnectState?.BeginInvoke(this, new SocketStateEventArgs(true), null, null);
                    base.DataReceived += Connection_DataReceived;
                    return true;
                }
            }

            ARCLConnectState?.BeginInvoke(this, new SocketStateEventArgs(false), null, null);
            return false;
        }
        public new bool Write(string msg) => base.Write(msg + "\r\n");

        //Public Extend SocketManager
        public string Password
        {
            get
            {
                if (ConnectionString.Count(c => c == ':') < 2) return string.Empty;
                return ConnectionString.Split(':')[2];
            }
        }
        private bool Login()
        {
            Read();

            Write(Password);
            string rm = Read("End of commands\r\n");

            if (rm.EndsWith("End of commands\r\n")) return true;
            else return false;
        }

        private ConfigSectionUpdateEventArgs NewConfigSection { get; set; } = new ConfigSectionUpdateEventArgs("endof");
        //Private
        private void Connection_DataReceived(object sender, SocketMessageEventArgs data)
        {
            string[] messages = MessageSplit(data.Message);

            foreach (string message in messages)
            {
                if ((message.StartsWith("Queue", StringComparison.CurrentCultureIgnoreCase) | message.StartsWith("EndQueue", StringComparison.CurrentCultureIgnoreCase)) & !message.Contains("Robot"))
                {
                    QueueJobUpdate?.BeginInvoke(this, new QueueJobUpdateEventArgs(message), null, null);
                    continue;
                }

                if ((message.StartsWith("Queue", StringComparison.CurrentCultureIgnoreCase) | message.StartsWith("EndQueue", StringComparison.CurrentCultureIgnoreCase)) & message.Contains("Robot"))
                {
                    QueueRobotUpdate?.BeginInvoke(this, new QueueRobotUpdateEventArgs(message), null, null);
                    continue;
                }

                if (message.StartsWith("ExtIO", StringComparison.CurrentCultureIgnoreCase) | message.StartsWith("EndExtIO", StringComparison.CurrentCultureIgnoreCase))
                {
                    ExternalIOUpdate?.BeginInvoke(this, new ExternalIOUpdateEventArgs(message), null, null);
                    continue;
                }

                if (message.StartsWith("getconfigsection", StringComparison.CurrentCultureIgnoreCase) | message.StartsWith("endofgetconfigsection", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (NewConfigSection.IsEnd)
                        NewConfigSection = new ConfigSectionUpdateEventArgs(message);
                    else
                        NewConfigSection.Update(message);

                    if (NewConfigSection.IsEnd)
                        ConfigSectionUpdate?.BeginInvoke(this, NewConfigSection, null, null);

                    continue;
                }

                if (message.StartsWith("Status:"))
                {
                    StatusUpdate?.BeginInvoke(this, new StatusUpdateEventArgs(message), null, null);
                    continue;
                }

                if (message.StartsWith("RangeDeviceGetCurrent:"))
                {
                    RangeDeviceCurrentUpdate?.BeginInvoke(this, new RangeDeviceUpdateEventArgs(message), null, null);
                    continue;
                }

                if (message.StartsWith("RangeDeviceGetCumulative:"))
                {
                    RangeDeviceCumulativeUpdate?.BeginInvoke(this, new RangeDeviceUpdateEventArgs(message), null, null);
                    continue;
                }

            }
        }
    }
}