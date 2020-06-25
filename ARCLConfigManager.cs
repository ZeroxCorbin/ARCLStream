using ARCL;
using ARCLTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARCL
{
    public class ARCLConfigManager
    {
        public class ConfigManagerUpdateEventArgs : EventArgs
        {
            public string SectionName { get; private set; }
            public ConfigManagerUpdateEventArgs(string sectionName) => SectionName = sectionName;
        }
        //Public
        public delegate void ConfigManagerUpdateEventHandler(object sender, ConfigManagerUpdateEventArgs data);
        public event ConfigManagerUpdateEventHandler ConfigManagerUpdate;

        private ARCLConnection Connection { get; set; }
        public ARCLConfigManager(ARCLConnection connection) => Connection = connection;

        public Dictionary<string, List<ConfigSection>> Sections { get; private set; }

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
            if (Sections.ContainsKey(sectionName))
                Sections[sectionName].Clear();
            else
                Sections.Add(sectionName, new List<ConfigSection>());

            SectionName = sectionName;

            if (Connection.Write(string.Format("getconfigsectionvalues {0}\r\n", sectionName)))
                return true;
            else
                return false;
        }

        private void Connection_ConfigSectionUpdate(object sender, ARCLTypes.ConfigSectionUpdateEventArgs data)
        {
            if (SectionName == null) return;

            Sections[SectionName].AddRange(data.Sections);

            ConfigManagerUpdate?.BeginInvoke(this, new ConfigManagerUpdateEventArgs(SectionName), null, null);
        }
    }
}
