using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using NieroNetLib;

namespace NieroNetLib.Types
{
    public class BasicInterfaceInfo
    {
        public NetworkInterface Interface { get; private set; }
        public NetworkInterfaceType Type { get; private set; }

        public string Name { get; private set; }
        public PhysicalAddress MacAddress { get; private set; }
        public long Speed { get; private set; }
        public string ActualSpeed { get; private set; }
        public double ActualSpeedInBytes { get; private set; }
        public IPAddress IPv4 { get; private set; }
        public IPAddress IPv6 { get; private set; }

        private System.Timers.Timer SpeedUpdateTimer = new System.Timers.Timer { Interval = 500, AutoReset = true };
        private object lockObj = new object();

        public BasicInterfaceInfo(NetworkInterface networkInterface)
        {
            Interface = networkInterface;
            Type = Interface.NetworkInterfaceType;

            Speed = Interface.Speed;
            SpeedUpdateTimer.Elapsed += CalculateActualSpeed;
            SpeedUpdateTimer.Enabled = true;

            Name = Interface.Name;
            MacAddress = Interface.GetPhysicalAddress();

            foreach(UnicastIPAddressInformation ip in Interface.GetIPProperties().UnicastAddresses)
            {
                if (Interface.Supports(NetworkInterfaceComponent.IPv4))
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        IPv4 = ip.Address;
                    }
                }
                else
                {
                    IPv4 = null;
                }
                if (Interface.Supports(NetworkInterfaceComponent.IPv6))
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        IPv6 = ip.Address;
                    }
                }
                else
                {
                    IPv6 = null;
                }
            }
        }

        private void CalculateActualSpeed(object sender, ElapsedEventArgs args)
        {
            ThreadPool.QueueUserWorkItem(callback =>
            {
                long beginValue = Interface.GetIPv4Statistics().BytesReceived;
                DateTime beginTime = DateTime.Now;

                Thread.Sleep(500);

                long endValue = Interface.GetIPv4Statistics().BytesReceived;
                DateTime endTime = DateTime.Now;

                long recievedBytes = endValue - beginValue;
                double totalSeconds = (endTime - beginTime).TotalSeconds;

                lock (lockObj)
                {
                    ActualSpeedInBytes = Math.Round((double)(recievedBytes / totalSeconds), 1);
                    ActualSpeed = NetworkTools.BytesConvert((double)(recievedBytes / totalSeconds)) + "/s";
                }
            });
        }
    }
}
