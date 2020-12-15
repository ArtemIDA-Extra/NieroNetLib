using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using NieroNetLib.Types;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace NieroNetLib
{
    public class LocalNetScan : INotifyPropertyChanged
    {
        //Fields
        private NetScanStatus p_ScanStatus;
        private TimeSpan? p_CompleteElapsedTime;
        private long p_TotalIpsForScan;
        private long p_SentPingsCount;
        private long p_CompletedPingsCount;
        private long p_SuccessfulPingsCount;
        private IPAddress p_LastReplyIp;

        //UI prop. fields
        string p_Status_str;
        string p_LastReplyIp_str;

        //Properties for code-behind
        public IPAddress NetMask { get; private set; }
        public IPAddress Gateway { get; private set; }
        public int Timeout { get; private set; }
        public NetScanStatus ScanStatus
        {
            private set
            {
                p_ScanStatus = value;
                switch (value)
                {
                    case NetScanStatus.Ready: Status_str = "Ready"; break;
                    case NetScanStatus.ScanStarted: Status_str = "In the process"; break;
                    case NetScanStatus.GeneratingIpList: Status_str = "IP-list generation..."; break;
                    case NetScanStatus.SendingPings: Status_str = "Sending pings..."; break;
                    case NetScanStatus.CompilingPingResponse: Status_str = "Compiling responses..."; break;
                    case NetScanStatus.Completed: Status_str = "Сompleted!"; OnCompleted(); break;
                }
                ScanStatusUpdatedEventArgs e = new ScanStatusUpdatedEventArgs()
                {
                    ActualStatus = ScanStatus
                };
                OnPropertyChanged();
                OnStatusUpdated(e);
            }
            get { return p_ScanStatus; }
        }
        public TimeSpan? ElapsedTime
        {
            get
            {
                if (ScanStatus != NetScanStatus.Completed && ScanStatus != NetScanStatus.Ready)
                {
                    return StartData - DateTime.Now;
                }
                else if (ScanStatus == NetScanStatus.Ready)
                {
                    return null;
                }
                else
                {
                    return p_CompleteElapsedTime;
                }
            }
        }
        public DateTime? StartData { get; private set; }
        public DateTime? FinishedData { get; private set; }
        public long TotalIpsForScan
        {
            private set
            {
                p_TotalIpsForScan = value;
                OnPropertyChanged();
            }
            get { return p_TotalIpsForScan; }
        }
        public long SentPingsCount
        {
            private set
            {
                p_SentPingsCount = value;
                SentPingsCountUpdatedEventArgs e = new SentPingsCountUpdatedEventArgs
                {
                    ActualElapsedTime = ElapsedTime,
                    ActualSentPingsCount = SentPingsCount,
                    TotalNeededPings = TotalIpsForScan
                };
                OnPropertyChanged();
                OnSentPingsCountUpdated(e);
            }
            get { return p_SentPingsCount; }
        }
        public long CompletedPingsCount
        {
            private set
            {
                p_CompletedPingsCount = value;
                CompletedPingsCountUpdatedEventArgs e = new CompletedPingsCountUpdatedEventArgs
                {
                    ActualElapsedTime = ElapsedTime,
                    ActualCompletedPingsCount = CompletedPingsCount,
                    TotalNeededPings = TotalIpsForScan
                };
                OnPropertyChanged();
                OnCompletedPingsCountUpdated(e);
            }
            get { return p_CompletedPingsCount; }
        }
        public long SuccessfulPingsCount
        {
            private set 
            { 
                p_SuccessfulPingsCount = value;
                OnPropertyChanged();
            }
            get{ return p_SuccessfulPingsCount; }
        }
        public IPAddress LastReplyIp
        {
            set
            {
                p_LastReplyIp = value;
                LastReplyIp_str = value.ToString();
                OnPropertyChanged();
            }
            get { return p_LastReplyIp; }
        }
        public bool Locked { get; private set; }
        public List<(IPAddress, PingReply)> ScanResult { get; private set; }

        //Properties for UI
        public string Status_str
        {
            private set
            {
                p_Status_str = value;
                OnPropertyChanged();
            }
            get { return p_Status_str; }
        }
        public string LastReplyIp_str
        {
            private set
            {
                p_LastReplyIp_str = value;
                OnPropertyChanged();
            }
            get { return p_LastReplyIp_str; }
        }

        public LocalNetScan(IPAddress gateway, IPAddress netMask, int timeout = 100)
        {
            ScanResult = null;
            Gateway = gateway;
            NetMask = netMask;
            if (timeout >= 0)
                Timeout = timeout;
            else
                throw new Exception("Timeout cannot be lower than 0");
            p_CompleteElapsedTime = null;
            StartData = FinishedData = null;
            ScanStatus = NetScanStatus.Ready;
            TotalIpsForScan = NetworkTools.CalculateNumberOfIPs(netMask);
            SentPingsCount = CompletedPingsCount = SuccessfulPingsCount = 0;
            p_LastReplyIp = null;
            p_LastReplyIp_str = "Not found:(";
            ScanResult = new List<(IPAddress, PingReply)>();
            Locked = false;
        }

        public async Task StartScanningAsync()
        {
            if (!Locked)
            {
                StartData = DateTime.Now;
                ScanStatus = NetScanStatus.ScanStarted;

                ScanStatus = NetScanStatus.GeneratingIpList;
                List<IPAddress> IpAddressesForScan = NetworkTools.GenerateIpList(Gateway, NetMask);
                List<Task<PingReply>> AsyncPingTasks = new List<Task<PingReply>>();

                TotalIpsForScan = IpAddressesForScan.Count;
                ScanStatus = NetScanStatus.SendingPings;

                string data = "YoRHa. For the Glory of Mankind!";
                byte[] buffer = Encoding.ASCII.GetBytes(data);

                foreach (IPAddress ip in IpAddressesForScan)
                {
                    Ping AsyncPingSender = new Ping();
                    Ping AsyncPingSenderAlt = new Ping();
                    AsyncPingSender.PingCompleted += PingCompleted;
                    AsyncPingSender.SendAsync(ip, Timeout);
                    AsyncPingTasks.Add(AsyncPingSenderAlt.SendPingAsync(ip, Timeout));
                    SentPingsCount++;
                }

                ScanStatus = NetScanStatus.CompilingPingResponse;

                PingReply[] pingsReplies = await Task.WhenAll(AsyncPingTasks);

                for (int i = 0; i < pingsReplies.Length; i++)
                {
                    ScanResult.Add((IpAddressesForScan[i], pingsReplies[i]));
                }

                FinishedData = DateTime.Now;
                p_CompleteElapsedTime = FinishedData - StartData;
                ScanStatus = NetScanStatus.Completed;
                Locked = true;
            }
        }
        public async void StartScanningOnBackground(object sender, DoWorkEventArgs e)
        {
            if (!Locked)
            {
                StartData = DateTime.Now;
                ScanStatus = NetScanStatus.ScanStarted;
                (sender as BackgroundWorker).ReportProgress(10);

                ScanStatus = NetScanStatus.GeneratingIpList;
                List<IPAddress> IpAddressesForScan = NetworkTools.GenerateIpList(Gateway, NetMask);
                List<Task<PingReply>> AsyncPingTasks = new List<Task<PingReply>>();

                TotalIpsForScan = IpAddressesForScan.Count;
                ScanStatus = NetScanStatus.SendingPings;
                (sender as BackgroundWorker).ReportProgress(30);

                string data = "YoRHa. For the Glory of Mankind!";
                byte[] buffer = Encoding.ASCII.GetBytes(data);

                foreach (IPAddress ip in IpAddressesForScan)
                {
                    Ping AsyncPingSender = new Ping();
                    Ping AsyncPingSenderAlt = new Ping();
                    AsyncPingSender.PingCompleted += PingCompleted;
                    AsyncPingSender.SendAsync(ip, Timeout);
                    AsyncPingTasks.Add(AsyncPingSenderAlt.SendPingAsync(ip, Timeout));
                    SentPingsCount++;
                }

                ScanStatus = NetScanStatus.CompilingPingResponse;
                (sender as BackgroundWorker).ReportProgress(80);

                PingReply[] pingsReplies = await Task.WhenAll(AsyncPingTasks);

                for (int i = 0; i < pingsReplies.Length; i++)
                {
                    ScanResult.Add((IpAddressesForScan[i], pingsReplies[i]));
                }

                FinishedData = DateTime.Now;
                p_CompleteElapsedTime = FinishedData - StartData;
                ScanStatus = NetScanStatus.Completed;
            }
        }

        //Receiver
        private void PingCompleted(object sender, PingCompletedEventArgs e)
        {
            CompletedPingsCount++;
            if(e.Reply.Status == IPStatus.Success)
            {
                SuccessfulPingsCount++;
                LastReplyIp = e.Reply.Address;
                NewSuccessfullyPingReplyEventArgs args = new NewSuccessfullyPingReplyEventArgs
                {
                    Address = e.Reply.Address,
                    Reply = e.Reply
                };
                OnNewSuccessfullyPingReply(args);
            }
        }

        public List<(IPAddress, PingReply)> FilterResults(IPStatus ipStatus)
        {
            if (ScanResult != null)
            {
                List<(IPAddress, PingReply)> FiltredResult = new List<(IPAddress, PingReply)>();
                foreach ((IPAddress ip, PingReply reply) it in ScanResult)
                {
                    if (it.reply.Status == ipStatus)
                        FiltredResult.Add(it);
                }
                return FiltredResult;
            }
            else return null;
        }
        public List<(IPAddress, PingReply)> FilterResults(params IPAddress[] ipAdress)
        {
            if (ScanResult != null)
            {
                List<(IPAddress, PingReply)> FiltredResult = new List<(IPAddress, PingReply)>();
                foreach ((IPAddress ip, PingReply reply) it in ScanResult)
                {
                    if (ipAdress.Contains(it.ip))
                        FiltredResult.Add(it);
                }
                return FiltredResult;
            }
            else return null;
        }
        public List<(IPAddress, PingReply)> FilterResults(IPStatus ipStatus, params IPAddress[] ipAdress)
        {
            if (ScanResult != null)
            {
                List<(IPAddress, PingReply)> FiltredResult = new List<(IPAddress, PingReply)>();
                foreach ((IPAddress ip, PingReply reply) it in ScanResult)
                {
                    if (it.reply.Status == ipStatus && ipAdress.Contains(it.ip))
                        FiltredResult.Add(it);
                }
                return FiltredResult;
            }
            else return null;
        }
        public List<(IPAddress, PingReply)> FilterResults(IPStatus[] ipStatus, params IPAddress[] ipAdress)
        {
            if (ScanResult != null)
            {
                List<(IPAddress, PingReply)> FiltredResult = new List<(IPAddress, PingReply)>();
                foreach ((IPAddress ip, PingReply reply) it in ScanResult)
                {
                    if (ipStatus.Contains(it.reply.Status) && ipAdress.Contains(it.ip))
                        FiltredResult.Add(it);
                }
                return FiltredResult;
            }
            else return null;
        }

        protected virtual void OnSentPingsCountUpdated(SentPingsCountUpdatedEventArgs e)
        {
            SentPingsCountUpdated?.Invoke(this, e);
        }
        protected virtual void OnCompletedPingsCountUpdated(CompletedPingsCountUpdatedEventArgs e)
        {
            CompletedPingsCountUpdated?.Invoke(this, e);
        }
        protected virtual void OnNewSuccessfullyPingReply(NewSuccessfullyPingReplyEventArgs e)
        {
            NewSuccessfullyPingReply?.Invoke(this, e);
        }
        protected virtual void OnStatusUpdated(ScanStatusUpdatedEventArgs e)
        {
            ScanStatusUpdated?.Invoke(this, e);
        }
        protected virtual void OnCompleted()
        {
            Completed?.Invoke(this);
            Locked = true;
        }

        public event SentPingsCountUpdatedEventHandler SentPingsCountUpdated;
        public event CompletedPingsCountUpdatedEventHandler CompletedPingsCountUpdated;
        public event NewSuccessfullyPingReplyEventHandler NewSuccessfullyPingReply;
        public event ScanStatusUpdatedEventHandler ScanStatusUpdated;
        public event CompletedEventHandler Completed;

        //INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SentPingsCountUpdatedEventArgs : EventArgs
    {
        public long ActualSentPingsCount;
        public long TotalNeededPings;
        public TimeSpan? ActualElapsedTime;
    }
    public class CompletedPingsCountUpdatedEventArgs : EventArgs
    {
        public long ActualCompletedPingsCount;
        public long TotalNeededPings;
        public TimeSpan? ActualElapsedTime;
    }
    public class NewSuccessfullyPingReplyEventArgs : EventArgs
    {
        public IPAddress Address;
        public PingReply Reply;
    }
    public class ScanStatusUpdatedEventArgs : EventArgs
    {
        public NetScanStatus ActualStatus;
    }

    public delegate void SentPingsCountUpdatedEventHandler(Object sender, SentPingsCountUpdatedEventArgs e);
    public delegate void CompletedPingsCountUpdatedEventHandler(Object sender, CompletedPingsCountUpdatedEventArgs e);
    public delegate void NewSuccessfullyPingReplyEventHandler(Object sender, NewSuccessfullyPingReplyEventArgs e);
    public delegate void ScanStatusUpdatedEventHandler(Object sender, ScanStatusUpdatedEventArgs e);
    public delegate void CompletedEventHandler(Object sender);
}
