using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using NieroNetLib.Types;
using System.Linq;
using System.Threading;

namespace NieroNetLib
{
    public class LocalNetScan
    {
        public IPAddress NetMask { get; private set; }
        public IPAddress Gateway { get; private set; }
        public int Timeout { get; private set; }
        private NetScanStatus p_ScanStatus;
        public NetScanStatus ScanStatus
        {
            private set
            {
                p_ScanStatus = value;
                ScanStatusUpdatedEventArgs e = new ScanStatusUpdatedEventArgs()
                {
                    ActualStatus = ScanStatus
                };
                OnStatusUpdated(e);
            }
            get { return p_ScanStatus; }
        }
        private TimeSpan? completeElapsedTime;
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
                    return completeElapsedTime;
                }
            }
        }
        public DateTime? StartData { get; private set; }
        public DateTime? FinishedData { get; private set; }
        public int TotalIpsForScan { get; private set; }
        private int p_SentPingsCount;
        private int p_CompletedPingsCount;
        public int SentPingsCount
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
                OnSentPingsCountUpdated(e);
            }
            get { return p_SentPingsCount; }
        }
        public int CompletedPingsCount
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
                OnCompletedPingsCountUpdated(e);
            }
            get { return p_CompletedPingsCount; }
        }
        public bool Locked { get; private set; }
        public List<(IPAddress, PingReply)> ScanResult { get; private set; }

        public LocalNetScan(IPAddress gateway, IPAddress netMask, int timeout = 100)
        {
            ScanResult = null;
            Gateway = gateway;
            NetMask = netMask;
            if (timeout >= 0)
                Timeout = timeout;
            else
                throw new Exception("Timeout cannot be lower than 0");
            completeElapsedTime = null;
            StartData = FinishedData = null;
            ScanStatus = NetScanStatus.Ready;
            TotalIpsForScan = SentPingsCount = CompletedPingsCount = 0;
            ScanResult = new List<(IPAddress, PingReply)>();
            Locked = false;
        }

        public async Task StartScanning()
        {
            if (!Locked)
            {
                StartData = DateTime.Now;
                ScanStatus = NetScanStatus.ScanStarted;

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
                completeElapsedTime = FinishedData - StartData;
                ScanStatus = NetScanStatus.Completed;
                Locked = true;
            }
            else
            {
                throw new Exception("Object is locked!");
            }
        }

        private void PingCompleted(object sender, PingCompletedEventArgs e)
        {
            CompletedPingsCount++;
            if(e.Reply.Status == IPStatus.Success)
            {
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

        public event SentPingsCountUpdatedEventHandler SentPingsCountUpdated;
        public event CompletedPingsCountUpdatedEventHandler CompletedPingsCountUpdated;
        public event NewSuccessfullyPingReplyEventHandler NewSuccessfullyPingReply;
        public event ScanStatusUpdatedEventHandler ScanStatusUpdated;
    }

    public class SentPingsCountUpdatedEventArgs : EventArgs
    {
        public int ActualSentPingsCount;
        public int TotalNeededPings;
        public TimeSpan? ActualElapsedTime;
    }
    public class CompletedPingsCountUpdatedEventArgs : EventArgs
    {
        public int ActualCompletedPingsCount;
        public int TotalNeededPings;
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
}
