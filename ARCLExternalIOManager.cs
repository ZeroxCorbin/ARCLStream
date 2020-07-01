using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using ARCLTypes;

namespace ARCL
{
    public class ARCLExternalIOManager
    {
        /// <summary>
        /// Raised when the External IO is sycronized with the EM.
        /// </summary>
        public delegate void InSyncEventHandler(object sender, bool state);
        public event InSyncEventHandler InSync;
        /// <summary>
        /// True when the External IO is sycronized with the EM.
        /// </summary>
        public bool IsSynced { get; private set; } = false;


        private readonly Dictionary<string, ExtIOSet> _ActiveSets = new Dictionary<string, ExtIOSet>();
        public ReadOnlyDictionary<string, ExtIOSet> ActiveSets { get { lock (ActiveSetsLock) return new ReadOnlyDictionary<string, ExtIOSet>(_ActiveSets); } }
        private object ActiveSetsLock { get; } = new object();

        public ReadOnlyDictionary<string, ExtIOSet> DesiredSets { get; }
        private Dictionary<string, ExtIOSet> InProcessSets { get; set; } = new Dictionary<string, ExtIOSet>();

        private ARCLConnection Connection { get; set; }

        public ARCLExternalIOManager(ARCLConnection connection, Dictionary<string, ExtIOSet> desiredSets)
        {
            Connection = connection;

            if (desiredSets == null) DesiredSets = new ReadOnlyDictionary<string, ExtIOSet>(new Dictionary<string, ExtIOSet>());
            else DesiredSets = new ReadOnlyDictionary<string, ExtIOSet>(desiredSets);
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

            if (inputs.Count() < _ActiveSets.Count()) return false;

            int i = 0;
            bool res = false;
            foreach (KeyValuePair<string, ExtIOSet> set in _ActiveSets)
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
            foreach (KeyValuePair<string, ExtIOSet> set in _ActiveSets)
                result |= Connection.Write(set.Value.WriteOutputCommand);

            return result;
        }

        private bool Dump() => Connection.Write("extIODump");

        private void Connection_ExternalIOUpdate(object sender, ExternalIOUpdateEventArgs data)
        {
            if (data.ExtIOSet == null) return;

            if (data.ExtIOSet.IsEnd)
            {
                SyncDesiredSets();
                return;
            }

            if (data.ExtIOSet.IsDump)
            {
                lock (ActiveSetsLock)
                {
                    if (_ActiveSets.ContainsKey(data.ExtIOSet.Name))
                        _ActiveSets[data.ExtIOSet.Name] = data.ExtIOSet;
                    else
                        _ActiveSets.Add(data.ExtIOSet.Name, data.ExtIOSet);
                }

                return;
            }

            if (data.ExtIOSet.HasInputs)
            {
                bool isSync = false;
                lock (ActiveSetsLock)
                {
                    if (_ActiveSets.ContainsKey(data.ExtIOSet.Name))
                    {
                        _ActiveSets[data.ExtIOSet.Name].Inputs = data.ExtIOSet.Inputs;
                        _ActiveSets[data.ExtIOSet.Name].AddedForPendingUpdate = false;
                    }
                    else
                        isSync = true;
                }

                foreach (KeyValuePair<string, ExtIOSet> set in _ActiveSets)
                    isSync |= set.Value.AddedForPendingUpdate;

                if (!isSync)
                    InSync?.BeginInvoke(this, true, null, null);

                return;
            }
        }

        private void SyncDesiredSets()
        {
            if (DesiredSets.Count() == 0)
            {
                if (!IsSynced)
                    InSync?.BeginInvoke(this, true, null, null);
                IsSynced = true;
                return;
            }

            foreach (KeyValuePair<string, ExtIOSet> set in DesiredSets)
            {
                if (_ActiveSets.ContainsKey(set.Key))
                {
                    if (InProcessSets.ContainsKey(set.Key))
                        InProcessSets.Remove(set.Key);
                    continue;
                }
                else
                {
                    if (!InProcessSets.ContainsKey(set.Key))
                        InProcessSets.Add(set.Key, set.Value);
                }
            }

            if (InProcessSets.Count() > 0)
                AddSets();
            else
            {
                if (!IsSynced)
                    InSync?.BeginInvoke(this, true, null, null);
                IsSynced = true;
            }
        }

        private void AddSets()
        {
            foreach (KeyValuePair<string, ExtIOSet> set in InProcessSets)
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
