using ARCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ARCL.Arcl;

namespace ARCL
{
    public class ArclReplay : IDisposable
    {
        public enum Types
        {
            Status,
            Current,
            Cumulative
        }

        public delegate void StatusDataReceivedEventHandler(object sender, Arcl.StatusEventArgs data);
        public event StatusDataReceivedEventHandler StatusDataReceived;

        public delegate void RangeDeviceCurrentDataReceivedEventHandler(object sender, Arcl.RangeDeviceEventArgs data);
        public event RangeDeviceCurrentDataReceivedEventHandler RangeDeviceCurrentDataReceived;


        public int UpdateRate { get; private set; } = 5;

        public string FileFullPath { get; private set; }

        public bool IsRunning { get; private set; } = false;
        public bool IsPaused { get; private set; } = false;

        public void Start()
        {
            if (IsPaused) IsPaused = false;
            if (IsRunning) return;



            IsRunning = true;
            IsPaused = true;

            ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncThread_DoWork));
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        private IEnumerable<FileSearchResults> StartingEntries { get; set; } = Enumerable.Empty<FileSearchResults>();
        private IEnumerable<FileSearchResults> StoppingEntries { get; set; } = Enumerable.Empty<FileSearchResults>();
        private IEnumerable<FileSearchResults> EncoderTransformEntries { get; set; } = Enumerable.Empty<FileSearchResults>();
        private List<IEnumerable<FileSearchResults>> LaserEntries { get; set; } = new List<IEnumerable<FileSearchResults>>();

        private List<Arcl.RangeDeviceEventArgs> ReplayLaserEntries = new List<RangeDeviceEventArgs>();
        private List<Arcl.StatusEventArgs> ReplayStatusEntries { get; set; } = new List<Arcl.StatusEventArgs>();

        public long NumStartingEntries { get { return StartingEntries.Count(); } }
        public long NumEncoderTransformEntries { get { return EncoderTransformEntries.Count(); } }

        public bool LoadFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;

            FileFullPath = filePath;

            StartingEntries = FileSearch.Find(FileFullPath, "starting,");
            StoppingEntries = FileSearch.Find(FileFullPath, "stopping,");

            if (!LoadStartingData()) return false;
            if (!LoadStoppingData()) return false;

            foreach (string s in Devices)
            {
                IEnumerable<FileSearchResults> res = Enumerable.Empty<FileSearchResults>();
                res = FileSearch.Find(FileFullPath, $",{s},");
                LaserEntries.Add(res);
            }

            if (!LoadLaserData()) return false;

            EncoderTransformEntries = FileSearch.Find(FileFullPath, ",encoderTransform,");

            LoadStatusData();

            return true;
        }

        public string ConfigFile { get; private set; } = string.Empty;
        public string MapFile { get; private set; } = string.Empty;
        public string StartingDateTime { get; private set; } = string.Empty;
        public string StoppingDateTime { get; private set; } = string.Empty;
        public string StoppingElapsedTime { get; private set; } = string.Empty;
        public string StoppingMSecs= string.Empty;

        public List<string> Devices { get; private set; } = new List<string>();

        public StatusEventArgs StartingStatusEventArgs;
        public List<RangeDeviceEventArgs> StartingRangeDeviceCurrentReadings = new List<RangeDeviceEventArgs>();


        private bool LoadStartingData()
        {
            foreach(FileSearchResults fsr in StartingEntries)
            {
                if (fsr.Line.StartsWith("starting,config,"))
                    ConfigFile = fsr.Line.Replace("starting,config,", string.Empty);

                if (fsr.Line.StartsWith("starting,map,"))
                    MapFile = fsr.Line.Replace("starting,map,", string.Empty);

                if (fsr.Line.StartsWith("starting,startDateTime,"))
                    StartingDateTime = fsr.Line.Replace("starting,startDateTime,", string.Empty);

                if (fsr.Line.StartsWith("starting,encoderTransform,"))
                    StartingStatusEventArgs = new StatusEventArgs(fsr.Line, true);

                if (fsr.Line.StartsWith("starting,laser"))
                {
                    string[] spl = fsr.Line.Split(',');
                    if (!spl[1].Contains("Name"))
                    {
                        Devices.Add(spl[1]);
                        StartingRangeDeviceCurrentReadings.Add(new RangeDeviceEventArgs(fsr.Line, true));
                    }
                }
            }

            if (ConfigFile.Equals(string.Empty)) return false;
            if (MapFile.Equals(string.Empty)) return false;
            if (StartingDateTime.Equals(string.Empty)) return false;
            if (ConfigFile.Equals(string.Empty)) return false;

            return true;
        }

        private bool LoadStoppingData()
        {
            foreach (FileSearchResults fsr in StoppingEntries)
            {
                if (fsr.Line.StartsWith("stopping,stopElapsed,")) StoppingElapsedTime = fsr.Line.Replace("stopping,stopElapsed,", string.Empty);

                if (fsr.Line.StartsWith("stopping,stopDateTime,")) StoppingDateTime = fsr.Line.Replace("stopping,stopDateTime,", string.Empty);

                if (fsr.Line.StartsWith("stopping,stopMSecs,")) StoppingMSecs = fsr.Line.Replace("stopping,stopMSecs,", string.Empty);
            }

            if (StoppingElapsedTime.Equals(string.Empty)) return false;
            if (StoppingDateTime.Equals(string.Empty)) return false;
            if (StoppingMSecs.Equals(string.Empty)) return false;

            return true;
        }

        private bool LoadLaserData()
        {
            foreach(IEnumerable<FileSearchResults> ent in LaserEntries)
            {
                foreach(FileSearchResults res in ent)
                {
                    ReplayLaserEntries.Add(new RangeDeviceEventArgs(res.Line, true));
                }
            }

            ReplayLaserEntries.Sort(CompareTimes);

            return true;
        }

        private bool LoadStatusData()
        {
                   foreach (FileSearchResults res in EncoderTransformEntries)
                {
                    ReplayStatusEntries.Add(new StatusEventArgs(res.Line, true));
                }


            ReplayLaserEntries.Sort(CompareTimes);

            return true;
        }


        private static int CompareTimes(RangeDeviceEventArgs x, RangeDeviceEventArgs y)
        {

            if (x == null)
            {
                if (y == null)
                {
                    // If x is null and y is null, they're
                    // equal. 
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y
                    // is greater. 
                    return -1;
                }
            }
            else
            {
                // If x is not null...
                //
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    // ...and y is not null, compare the 
                    // lengths of the two strings.
                    //
                    //int retval = x.Timestamp.CompareTo(y.Timestamp);
                    return x.Timestamp.CompareTo(y.Timestamp);
                    //if (retval != 0)
                    //{
                    //    // If the strings are not of equal length,
                    //    // the longer string is greater.
                    //    //
                    //    return retval;
                    //}
                    //else
                    //{
                    //    // If the strings are of equal length,
                    //    // sort them with ordinary string comparison.
                    //    //
                    //    return x.Timestamp.CompareTo(y.Timestamp);
                    //}
                }
            }

        }

        private void AsyncThread_DoWork(object sender)
        {
            ReplayTimer Time = new ReplayTimer();

            float statusPrevTime = 0;
            float rangePrevTime = 0;
            RangeDeviceEventArgs rPrev = null;
            StatusEventArgs sPrev = null;

            Time.Reset();
            Time.Start();

            StatusDataReceived?.Invoke(new object(), StartingStatusEventArgs);
            foreach (RangeDeviceEventArgs e in StartingRangeDeviceCurrentReadings)
            {
                RangeDeviceCurrentDataReceived?.Invoke(new object(), e);
            }

            while (IsRunning)
            {
                if (IsPaused)
                {
                    Time.Stop();
                    Thread.Sleep(UpdateRate);
                    continue;
                }
                Time.Start();
                Time.Tick();

                
                foreach (RangeDeviceEventArgs r in ReplayLaserEntries)
                {
                    if(r.Timestamp >= Time.TotalTime)
                    {
                        if (r.Timestamp == rangePrevTime) break;
                        rangePrevTime = r.Timestamp;

                        if (rPrev == null) rPrev = r;

                        RangeDeviceCurrentDataReceived?.Invoke(new object(), rPrev);

                        rPrev = r;

                        break;
                    }
                }

                foreach (StatusEventArgs r in ReplayStatusEntries)
                {
                    if (r.Timestamp >= Time.TotalTime)
                    {
                        if (r.Timestamp == statusPrevTime) break;
                        statusPrevTime = r.Timestamp;

                        if (sPrev == null) sPrev = r;

                        StatusDataReceived?.Invoke(new object(), sPrev);

                        sPrev = r;

                        break;
                    }
                }

                Thread.Sleep(UpdateRate);

                //Robot.Write("onelinestatus");
                //foreach (string l in Devices)
                //{
                //    Robot.Write("rangeDeviceGetCurrent " + l);
                //}
                //foreach (string l in Devices)
                //{
                //    Robot.Write("rangeDeviceGetCumulative " + l);
                //}

            }

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).


                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~RobotLogging() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
