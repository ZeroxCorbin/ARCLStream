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

        public delegate void QueueUpdateEventHandler(object sender, QueueUpdateEventArgs data);
        public event QueueUpdateEventHandler QueueUpdate;

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
                if (message.StartsWith("Queue", StringComparison.CurrentCultureIgnoreCase) &
                    !message.StartsWith("QueueRobot", StringComparison.CurrentCultureIgnoreCase))
                    QueueUpdate?.BeginInvoke(this, new QueueUpdateEventArgs(message), null, null);

                if (message.StartsWith("extio", StringComparison.CurrentCultureIgnoreCase) | message.StartsWith("endextio", StringComparison.CurrentCultureIgnoreCase))
                    ExternalIOUpdate?.BeginInvoke(this, new ExternalIOUpdateEventArgs(message), null, null);

                if (message.StartsWith("getconfigsection", StringComparison.CurrentCultureIgnoreCase) | message.StartsWith("endofgetconfigsection", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (NewConfigSection.EndOfConfig)
                        NewConfigSection = new ConfigSectionUpdateEventArgs(message);
                    else
                        NewConfigSection.Update(message);

                    if (NewConfigSection.EndOfConfig)
                        ConfigSectionUpdate?.Invoke(this, NewConfigSection);
                }
                    
                if (message.StartsWith("Status:"))
                    StatusUpdate?.Invoke(this, new StatusUpdateEventArgs(message));

                if (message.StartsWith("RangeDeviceGetCurrent:"))
                    RangeDeviceCurrentUpdate?.BeginInvoke(this, new RangeDeviceUpdateEventArgs(message), null, null);

                if (message.StartsWith("RangeDeviceGetCumulative:"))
                    RangeDeviceCumulativeUpdate?.BeginInvoke(this, new RangeDeviceUpdateEventArgs(message), null, null);
            }
        }
    }
}