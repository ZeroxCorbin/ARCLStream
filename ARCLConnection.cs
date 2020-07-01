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
    /// <summary>

    /// The intention of this class is to process ARCL messages and fire events based on the type of message.
    /// All messsages are parsed by the respective EventArgs classes and passed with the event.
    /// Inherits: SocketManager
    /// Hides: Connect(), Write(), ConnectState Event
    /// Extends: Password, Login()
    /// </summary>
    public class ARCLConnection : SocketManager
    {
        /// <summary>
        /// Raised when the connection to the ARCL Server changes.
        /// </summary>
        public new event ConnectedEventHandler ConnectState;

        /// <summary>
        /// A message that starts with "Queue" or "EndQueue" and does not contain "Robot".
        /// </summary>
        /// <param name="sender">Will always be "this".</param>
        /// <param name="data">Job information.</param>
        public delegate void QueueJobEventHandler(object sender, QueueJobUpdateEventArgs data);
        /// <summary>
        /// A message that starts with "Queue" or "EndQueue" and does not contain "Robot".
        /// </summary>
        public event QueueJobEventHandler QueueJobUpdate;

        /// <summary>
        /// A message that starts with "Queue" or "EndQueue" and does contain "Robot".
        /// </summary>
        /// <param name="sender">Will always be "this".</param>
        /// <param name="data">Robot information.</param>
        public delegate void QueueRobotUpdateEventHandler(object sender, QueueRobotUpdateEventArgs data);
        /// <summary>
        /// A message that starts with "Queue" or "EndQueue" and does contain "Robot".
        /// </summary>
        public event QueueRobotUpdateEventHandler QueueRobotUpdate;

        public delegate void StatusUpdateEventHandler(object sender, StatusUpdateEventArgs data);
        public event StatusUpdateEventHandler StatusUpdate;

        public delegate void RangeDeviceCurrentUpdateEventHandler(object sender, RangeDeviceUpdateEventArgs data);
        public event RangeDeviceCurrentUpdateEventHandler RangeDeviceCurrentUpdate;

        public delegate void RangeDeviceCumulativeUpdateEventHandler(object sender, RangeDeviceUpdateEventArgs data);
        public event RangeDeviceCumulativeUpdateEventHandler RangeDeviceCumulativeUpdate;

        /// <summary>
        /// A message that starts with "ExtIO" or "EndExtIO".
        /// </summary>
        /// <param name="sender">Will always be "this".</param>
        /// <param name="data">External IO information.</param>
        public delegate void ExternalIOUpdateEventHandler(object sender, ExternalIOUpdateEventArgs data);
        /// <summary>
        /// A message that starts with "ExtIO" or "EndExtIO".
        /// </summary>
        public event ExternalIOUpdateEventHandler ExternalIOUpdate;

        public delegate void ConfigSectionUpdateEventHandler(object sender, ConfigSectionUpdateEventArgs data);
        public event ConfigSectionUpdateEventHandler ConfigSectionUpdate;

        //Public
        public ARCLConnection(string connectionString) : base(connectionString) { }

        /// <summary>
        /// Connect and Login to an ARCL server.
        /// Hides SocketManager.Connect()
        /// </summary>
        /// <param name="timeout">How long to wait for a connection.</param>
        /// <returns>Conenction or Login failed / succeeded</returns>
        public new bool Connect(int timeout = 3000)
        {
            if (base.Connect(timeout))
            {
                if (Login())
                {
                    ConnectState?.BeginInvoke(this, true, null, null);
                    base.DataReceived += Connection_DataReceived;
                    return true;
                }
            }
            ConnectState?.BeginInvoke(this, false, null, null);
            return false;
        }

        /// <summary>
        /// Writes the message to the ARCL server.
        /// Appends "\r\n" to the message.
        /// Hides SocketManager.Write()
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public new bool Write(string msg) => base.Write(msg + "\r\n");

        /// <summary>
        /// Gets the password portion of the connection string.
        /// Extends SocketManager.ConnectionString to support a password.
        /// </summary>
        public string Password
        {
            get
            {
                if (ConnectionString.Count(c => c == ':') < 2) return string.Empty;
                return ConnectionString.Split(':')[2];
            }
        }

        /// <summary>
        /// Send password and wait for "End of commands\r\n"
        /// </summary>
        /// <returns>Success/Fail</returns>
        private bool Login()
        {
            Read();

            Write(Password);
            string rm = Read("End of commands\r\n");

            if (rm.EndsWith("End of commands\r\n")) return true;
            else return false;
        }

        //Private
        private void Connection_DataReceived(object sender, string data)
        {
            string[] messages = MessageSplit(data);

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
                    ConfigSectionUpdate?.BeginInvoke(this, new ConfigSectionUpdateEventArgs(message), null, null);
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