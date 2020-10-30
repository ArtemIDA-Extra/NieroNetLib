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
        public NetScanStatus ScanStatus;
        public int TotalIpsForScan, SentPingsCount;
        public List<(IPAddress, PingReply)> ScanResult { get; private set; }

        LocalNetScan(IPAddress netIP, IPAddress netMask, int timeout = 2000)
        {
            ScanResult = null;
            NetIp = netIP;
            NetMask = netMask;
            Timeout = timeout;
            TotalIpsForScan = SentPingsCount = 0;
            ScanStatus = NetScanStatus.Ready;
        }

        public async Task StartScanning()
        {
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

            PingReply p;

            PingReply[] pingsReplies = await Task.WhenAll(asyncPingTasks);
            for (int i = 0; i < pingsReplies.Length; i++)
            {
                ScanResult.Add((ipAddressesForScan[i], pingsReplies[i]));
            }

            ScanStatus = NetScanStatus.Completed;
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
    }
}
