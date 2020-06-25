using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using ARCLTypes;

namespace ARCL
{
    public class ARCLExternalIOManager
    {
        public delegate void ExtIOUpdateEventHandler(object sender, ExternalIOUpdateEventArgs data);
        public event ExtIOUpdateEventHandler ExtIOUpdate;

        public delegate void InputUpdateEventHandler(object sender, ExternalIOUpdateEventArgs data);
        public event InputUpdateEventHandler InputUpdate;

        public delegate void OutputUpdateEventHandler(object sender, ExternalIOUpdateEventArgs data);
        public event OutputUpdateEventHandler OutputUpdate;

        public delegate void InSyncUpdateEventHandler(object sender, bool data);
        public event InSyncUpdateEventHandler InSync;


        private readonly Dictionary<string, ExtIOSet> _ActiveExtIOSets = new Dictionary<string, ExtIOSet>();
        public Dictionary<string, ExtIOSet> ActiveExtIOSets
        {
            get
            {
                lock (ActiveLockObject)
                    return _ActiveExtIOSets;
            }
        }
        private Dictionary<string, ExtIOSet> DesiredExtIOSets { get; }
        private Dictionary<string, ExtIOSet> InProcessExtIOSets { get; set; } = new Dictionary<string, ExtIOSet>();

        public bool IsSynced { get; private set; } = false;

        private ARCLConnection Connection { get; set; }
        private object ActiveLockObject { get; } = new object();
        public ARCLExternalIOManager(ARCLConnection connection, Dictionary<string, ExtIOSet> desiredExtIOSets)
        {
            Connection = connection;

            if (desiredExtIOSets == null) DesiredExtIOSets = new Dictionary<string, ExtIOSet>();
            else DesiredExtIOSets = desiredExtIOSets;
        }

        public void Start()
        {
            if (!Connection.IsReceivingAsync)
                Connection.ReceiveAsync();

            Connection.ExternalIOUpdate += Connection_ExternalIOUpdate;

            //Initiate the the load of the current ExtIO
            Dump();
        }
        public void Stop()
        {
            if (IsSynced)
                InSync?.BeginInvoke(this, false, null, null);
            IsSynced = false;

            Connection.ExternalIOUpdate -= Connection_ExternalIOUpdate;
            Connection?.StopReceiveAsync();
        }

        public bool UpdateAllIO() => Dump();
        public bool WriteAllInputs()
        {
            if (!IsSynced) return false;

            foreach (KeyValuePair<string, ExtIOSet> set in _ActiveExtIOSets)
                Connection.Write(set.Value.WriteInputCommand);

            return true;
        }
        public bool WriteAllOutputs_Uncommon()
        {
            if (!IsSynced) return false;

            bool result = false;
            foreach (KeyValuePair<string, ExtIOSet> set in _ActiveExtIOSets)
                result |= Connection.Write(set.Value.WriteOutputCommand);

            return result;
        }

        private bool Dump() => Connection.Write("extIODump");

        private void Connection_ExternalIOUpdate(object sender, ExternalIOUpdateEventArgs data)
        {
            ExtIOUpdate?.Invoke(this, data);

            if (data.ExtIOSet == null) return;

            if (data.ExtIOSet.IsEnd)
                SyncDesiredExtIO();

            if (data.ExtIOSet.IsDump)
            {
                lock (ActiveLockObject)
                {
                    if (_ActiveExtIOSets.ContainsKey(data.ExtIOSet.Name))
                        _ActiveExtIOSets[data.ExtIOSet.Name] = data.ExtIOSet;
                    else
                        _ActiveExtIOSets.Add(data.ExtIOSet.Name, data.ExtIOSet);
                }
            }
        }

        private void SyncDesiredExtIO()
        {
            if (DesiredExtIOSets.Count() == 0)
            {
                if (!IsSynced)
                    InSync?.BeginInvoke(this, true, null, null);
                IsSynced = true;
                return;
            }

            foreach (KeyValuePair<string, ExtIOSet> set in _ActiveExtIOSets)
            {
                if (DesiredExtIOSets.ContainsKey(set.Key)) continue;
                else InProcessExtIOSets.Add(set.Key, set.Value);
            }

            if (InProcessExtIOSets.Count() > 0)
            {

            }
            else
            {
                if (!IsSynced)
                    InSync?.BeginInvoke(this, true, null, null);
                IsSynced = true;
            }
        }
    }
}
