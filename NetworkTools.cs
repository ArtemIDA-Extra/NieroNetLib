using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NieroNetLib
{
    public static class NetworkTools
    {
        public static List<IPAddress> GenerateIpList(string netIP = "0.0.0.0", string netMask = "255.255.255.0")
        {
            List<IPAddress> ipAddresses = new List<IPAddress>();
            string[] netMaskNodes_str = netMask.Split('.');
            string[] netIPNodes_str = netIP.Split('.');
            for (int Node_0 = Int32.Parse(netMaskNodes_str[0]); Node_0 <= 255; Node_0++)
            {
                for (int Node_1 = Int32.Parse(netMaskNodes_str[1]); Node_1 <= 255; Node_1++)
                {
                    for (int Node_2 = Int32.Parse(netMaskNodes_str[2]); Node_2 <= 255; Node_2++)
                    {
                        for (int Node_3 = Int32.Parse(netMaskNodes_str[3]); Node_3 <= 255; Node_3++)
                        {
                            IPAddress temp;
                            if (Int32.Parse(netMaskNodes_str[0]) == 255 && Int32.Parse(netMaskNodes_str[1]) == 255 && Int32.Parse(netMaskNodes_str[2]) == 255 && Int32.Parse(netMaskNodes_str[3]) == 255)
                            {
                                temp = IPAddress.Parse("0.0.0.0");
                                ipAddresses.Add(temp);
                            }
                            if (Int32.Parse(netMaskNodes_str[0]) == 255 && Int32.Parse(netMaskNodes_str[1]) == 255 && Int32.Parse(netMaskNodes_str[2]) == 255 && Int32.Parse(netMaskNodes_str[3]) != 255)
                            {
                                temp = IPAddress.Parse($"{netIPNodes_str[0]}.{netIPNodes_str[1]}.{netIPNodes_str[2]}.{Node_3}");
                                ipAddresses.Add(temp);
                            }
                            if (Int32.Parse(netMaskNodes_str[0]) == 255 && Int32.Parse(netMaskNodes_str[1]) == 255 && Int32.Parse(netMaskNodes_str[2]) != 255 && Int32.Parse(netMaskNodes_str[3]) != 255)
                            {
                                temp = IPAddress.Parse($"{netIPNodes_str[0]}.{netIPNodes_str[1]}.{Node_2}.{Node_3}");
                                ipAddresses.Add(temp);
                            }
                            if (Int32.Parse(netMaskNodes_str[0]) == 255 && Int32.Parse(netMaskNodes_str[1]) != 255 && Int32.Parse(netMaskNodes_str[2]) != 255 && Int32.Parse(netMaskNodes_str[3]) != 255)
                            {
                                temp = IPAddress.Parse($"{netIPNodes_str[0]}.{Node_1}.{Node_2}.{Node_3}");
                                ipAddresses.Add(temp);
                            }
                            if (Int32.Parse(netMaskNodes_str[0]) != 255 && Int32.Parse(netMaskNodes_str[1]) != 255 && Int32.Parse(netMaskNodes_str[2]) != 255 && Int32.Parse(netMaskNodes_str[3]) != 255)
                            {
                                temp = IPAddress.Parse($"{Node_0}.{Node_1}.{Node_2}.{Node_3}");
                                ipAddresses.Add(temp);
                            }
                        }
                    }
                }
            }
            return ipAddresses;
        }

        public static async Task<List<(IPAddress, PingReply)>> ScanLocaNetwork(string netIP = "192.168.0.1", string netMask = "255.255.255.0")
        {
            List<IPAddress> ipAddressesForScan = GenerateIpList(netIP, netMask);
            List<Task<PingReply>> asyncPingTasks = new List<Task<PingReply>>();
            List<(IPAddress, PingReply)> results = new List<(IPAddress, PingReply)>();

            foreach(IPAddress ip in ipAddressesForScan)
            {
                asyncPingTasks.Add(new Ping().SendPingAsync(ip, 2000));      // надо создавать новый Ping каждый раз
            }
            PingReply[] pingsReplies = await Task.WhenAll(asyncPingTasks);
            for(int i = 0; i < pingsReplies.Length; i++)
            {
                results.Add((ipAddressesForScan[i], pingsReplies[i]));
            }
            
            return results;
        }

        public static async Task<List<(IPAddress, PingReply)>> PingIps(List<IPAddress> ipAddresses)
        {
            List<Task<PingReply>> asyncPingTasks = new List<Task<PingReply>>();
            List<(IPAddress, PingReply)> results = new List<(IPAddress, PingReply)>();

            foreach (IPAddress ip in ipAddresses)
            {
                asyncPingTasks.Add(new Ping().SendPingAsync(ip, 2000));      //надо создавать новый Ping каждый раз
            }
            PingReply[] pingsReplies = await Task.WhenAll(asyncPingTasks);
            for (int i = 0; i < pingsReplies.Length; i++)
            {
                results.Add((ipAddresses[i], pingsReplies[i]));
            }

            return results;
        }

        public static void GetNetworkInterfaces()
        {
            IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();

            if (nics == null || nics.Length < 1)
            {
                throw new Exception ("Empty Network Interface list");
            }

        }

        public static List<IPAddress> GetLocalIPv4(NetworkInterfaceType _type)
        {
            List<IPAddress> ipAddrList = new List<IPAddress>();
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == _type && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ipAddrList.Add(ip.Address);
                        }
                    }
                }
            }
            return ipAddrList;
        }
    }

}
