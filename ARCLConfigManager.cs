using ARCL;
using ARCLTypes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARCL
{
    public class ARCLConfigManager
    {
        //Public
        /// <summary>
        /// Raised when the config Section is sycronized with the EM.
        /// </summary>
        public delegate void InSyncEventHandler(object sender, string sectionName);
        public event InSyncEventHandler InSync;
        /// <summary>
        /// True when the config Section is sycronized with the EM.
        /// </summary>
        public bool IsSynced { get; private set; } = false;

        private ARCLConnection Connection { get; set; }
        public ARCLConfigManager(ARCLConnection connection) => Connection = connection;

        private Dictionary<string, List<ConfigSection>> _Sections { get; set; }
        public ReadOnlyDictionary<string, List<ConfigSection>> Sections { get { lock (SectionsLockObject) return new ReadOnlyDictionary<string, List<ConfigSection>>(_Sections); } }
        private object SectionsLockObject { get; set; } = new object();

        public void Start()
        {
            if (!Connection.IsReceivingAsync)
                Connection.ReceiveAsync();

            Connection.ConfigSectionUpdate += Connection_ConfigSectionUpdate;
        }
        public void Stop()
        {
            Connection.ConfigSectionUpdate -= Connection_ConfigSectionUpdate;
            Connection?.StopReceiveAsync();
        }

        private string SectionName { get; set; } = null;
        public bool GetConfigSectionValues(string sectionName)
        {
            IsSynced = false;

            if (_Sections.ContainsKey(sectionName))
                _Sections[sectionName].Clear();
            else
                _Sections.Add(sectionName, new List<ConfigSection>());

            SectionName = sectionName;

            return Connection.Write($"getconfigsectionvalues {sectionName}\r\n");
        }

        private void Connection_ConfigSectionUpdate(object sender, ConfigSectionUpdateEventArgs data)
        {
            if (SectionName == null) return;

            lock (SectionsLockObject)
                _Sections[SectionName].Add(data.Section);

            if (data.IsEnd)
            {
                IsSynced = true;
                InSync?.BeginInvoke(this, SectionName, null, null);
                SectionName = null;
            }
        }
    }
}
