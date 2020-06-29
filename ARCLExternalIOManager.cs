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
        public bool WriteAllInputs(List<byte> inputs)
        {
            if (!IsSynced) return false;

            if (inputs.Count() < _ActiveExtIOSets.Count()) return false;

            int i = 0;
            bool res = false;
            foreach (KeyValuePair<string, ExtIOSet> set in _ActiveExtIOSets)
            {
                set.Value.Inputs = new List<byte> { inputs[i++] };
                set.Value.AddedForPendingUpdate = true;
                res &= Connection.Write(set.Value.WriteInputCommand);
            }

            return res ^= true;
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
            if (data.ExtIOSet == null) return;

            if (data.ExtIOSet.IsEnd)
            {
                SyncDesiredExtIO();
                return;
            }

            if (data.ExtIOSet.IsDump)
            {
                lock (ActiveLockObject)
                {
                    if (_ActiveExtIOSets.ContainsKey(data.ExtIOSet.Name))
                        _ActiveExtIOSets[data.ExtIOSet.Name] = data.ExtIOSet;
                    else
                        _ActiveExtIOSets.Add(data.ExtIOSet.Name, data.ExtIOSet);
                }

                return;
            }

            if (data.ExtIOSet.HasInputs)
            {
                bool isSync = false;
                lock (ActiveLockObject)
                {
                    if (_ActiveExtIOSets.ContainsKey(data.ExtIOSet.Name))
                    {
                        _ActiveExtIOSets[data.ExtIOSet.Name].Inputs = data.ExtIOSet.Inputs;
                        _ActiveExtIOSets[data.ExtIOSet.Name].AddedForPendingUpdate = false;
                    }
                    else
                        isSync = true;
                }

                foreach (KeyValuePair<string, ExtIOSet> set in _ActiveExtIOSets)
                    isSync |= set.Value.AddedForPendingUpdate;

                if(!isSync)
                    InSync?.BeginInvoke(this, true, null, null);

                return;
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

            foreach (KeyValuePair<string, ExtIOSet> set in DesiredExtIOSets)
            {
                if (_ActiveExtIOSets.ContainsKey(set.Key))
                {
                    if (InProcessExtIOSets.ContainsKey(set.Key))
                        InProcessExtIOSets.Remove(set.Key);
                    continue;
                }
                else
                {
                    if (!InProcessExtIOSets.ContainsKey(set.Key))
                        InProcessExtIOSets.Add(set.Key, set.Value);
                }
            }

            if (InProcessExtIOSets.Count() > 0)
                CreateIO();
            else
            {
                if (!IsSynced)
                    InSync?.BeginInvoke(this, true, null, null);
                IsSynced = true;
            }
        }

        private void CreateIO()
        {
            foreach(KeyValuePair<string, ExtIOSet> set in InProcessExtIOSets)
            {
                if (set.Value.AddedForPendingUpdate) continue;
                else
                    Connection.Write(set.Value.CreateSetCommand);
            }

            ThreadPool.QueueUserWorkItem(new WaitCallback(CreateIOWaitThread));
        }

        private void CreateIOWaitThread(object sender)
        {
            Thread.Sleep(10000);

            Dump();
        }
    }
}
