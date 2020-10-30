using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using NieroNetLib.Types;
using System.Linq;

namespace NieroNetLib
{
    class LocalNetScan
    {
        public IPAddress NetMask { get; private set; }
        public IPAddress NetIp { get; private set; }
        public int Timeout { get; private set; }
        public NetScanStatus ScanStatus
        {
            private set
            {
                ScanStatus = value;
                ScanStatusUpdatedEventArgs e = new ScanStatusUpdatedEventArgs
                {
                    ActualStatus = ScanStatus
                };
                OnStatusUpdate(e);
            }
            get { return ScanStatus; }
        }
        private TimeSpan? completeElapsedTime;
        public TimeSpan? ElapsedTime
        {
            private set { }
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
        public int SentPingsCount
        {
            private set
            {
                SentPingsCount = value;
                PingsCountUpdatedEventArgs e = new PingsCountUpdatedEventArgs
                {
                    ActualElapsedTime = ElapsedTime,
                    ActualPingsCount = SentPingsCount,
                    TotalNeededPings = TotalIpsForScan
                };
                OnPingsCountUpdate(e);
            }
            get { return SentPingsCount; }
        }
        public bool Locked { get; private set; }
        public List<(IPAddress, PingReply)> ScanResult { get; private set; }

        LocalNetScan(IPAddress netIP, IPAddress netMask, int timeout = 2000)
        {
            ScanResult = null;
            NetIp = netIP;
            NetMask = netMask;
            Timeout = timeout;
            completeElapsedTime = null;
            ElapsedTime = null;
            StartData = FinishedData = null;
            TotalIpsForScan = SentPingsCount = 0;
            ScanStatus = NetScanStatus.Ready;
            Locked = false;
        }

        public async Task StartScanning()
        {
            if (!Locked)
            {
                StartData = DateTime.Now;
                ScanStatus = NetScanStatus.ScanStarted;

                List<IPAddress> ipAddressesForScan = NetworkTools.GenerateIpList(NetIp, NetMask);
                List<Task<PingReply>> asyncPingTasks = new List<Task<PingReply>>();

                TotalIpsForScan = ipAddressesForScan.Count;
                ScanStatus = NetScanStatus.SendingPings;

                foreach (IPAddress ip in ipAddressesForScan)
                {
                    asyncPingTasks.Add(new Ping().SendPingAsync(ip, Timeout));      // надо создавать новый Ping каждый раз
                    SentPingsCount++;
                }

                ScanStatus = NetScanStatus.CompilingPingResponse;

                PingReply[] pingsReplies = await Task.WhenAll(asyncPingTasks);
                for (int i = 0; i < pingsReplies.Length; i++)
                {
                    ScanResult.Add((ipAddressesForScan[i], pingsReplies[i]));
                }

                FinishedData = DateTime.Now;
                completeElapsedTime = StartData - FinishedData;
                ScanStatus = NetScanStatus.Completed;
                Locked = true;
            }
            else throw new Exception("This scan object has already been scanned and is locked for re-scanning. You can find out the results of the previous scan.");
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

        protected virtual void OnPingsCountUpdate(PingsCountUpdatedEventArgs e)
        {
            PingsCountUpdated?.Invoke(this, e);
        }
        protected virtual void OnStatusUpdate(ScanStatusUpdatedEventArgs e)
        {
            ScanStatusUpdated?.Invoke(this, e);
        }

        public event PingsCountUpdatedEventHandler PingsCountUpdated;
        public event ScanStatusUpdatedEventHandler ScanStatusUpdated;
    }

    public class PingsCountUpdatedEventArgs : EventArgs
    {
        public int ActualPingsCount;
        public int TotalNeededPings;
        public TimeSpan? ActualElapsedTime;
    }
    public class ScanStatusUpdatedEventArgs : EventArgs
    {
        public NetScanStatus ActualStatus;
    }

    public delegate void PingsCountUpdatedEventHandler(Object sender, PingsCountUpdatedEventArgs e);
    public delegate void ScanStatusUpdatedEventHandler(Object sender, ScanStatusUpdatedEventArgs e);
}
