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
        public static List<IPAddress> GenerateIpList(IPAddress gateway, IPAddress netMask)
        {
            List<IPAddress> ipAddresses = new List<IPAddress>();
            string[] netMaskNodes_str = netMask.ToString().Split('.');
            string[] gatewayNodes_str = gateway.ToString().Split('.');

            for (int Node_0 = 0; Node_0 <= 255 - Int32.Parse(netMaskNodes_str[0]); Node_0++)
            {
                for (int Node_1 = 0; Node_1 <= 255 - Int32.Parse(netMaskNodes_str[1]); Node_1++)
                {
                    for (int Node_2 = 0; Node_2 <= 255 - Int32.Parse(netMaskNodes_str[2]); Node_2++)
                    {
                        for (int Node_3 = 0; Node_3 <= 255 - Int32.Parse(netMaskNodes_str[3]); Node_3++)
                        {
                            IPAddress tempAddress;
                            string tempNode0, tempNode1, tempNode2, tempNode3;

                            if (Int32.Parse(netMaskNodes_str[0]) == 255) tempNode0 = gatewayNodes_str[0];
                            else tempNode0 = (Node_0).ToString();
                            if (Int32.Parse(netMaskNodes_str[1]) == 255) tempNode1 = gatewayNodes_str[1];
                            else tempNode1 = (Node_1).ToString();
                            if (Int32.Parse(netMaskNodes_str[2]) == 255) tempNode2 = gatewayNodes_str[2];
                            else tempNode2 = (Node_2).ToString();
                            if (Int32.Parse(netMaskNodes_str[3]) == 255) tempNode3 = gatewayNodes_str[3];
                            else tempNode3 = (Node_3).ToString();

                            tempAddress = IPAddress.Parse($"{tempNode0}.{tempNode1}.{tempNode2}.{tempNode3}");
                            ipAddresses.Add(tempAddress);
                        }
                    }
                }
            }
            return ipAddresses;
        }

        public static long CalculateNumberOfIPs(IPAddress netMask)
        {
            if (netMask == IPAddress.Parse("255.255.255.255")) return 0;
            long NumberOfIPs = 1;
            string[] netMaskNodes_str = netMask.ToString().Split('.');

            NumberOfIPs *= (256 - Int32.Parse(netMaskNodes_str[3])); 
            NumberOfIPs *= (256 - Int32.Parse(netMaskNodes_str[2]));
            NumberOfIPs *= (256 - Int32.Parse(netMaskNodes_str[1])); 
            NumberOfIPs *= (256 - Int32.Parse(netMaskNodes_str[0]));
            return NumberOfIPs;
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

        public static List<NetworkInterface> GetNetworkInterfaces(OperationalStatus status, params NetworkInterfaceType[] interfaceTypes)
        {
            List<NetworkInterface> allAviableInterfaces, selectedInterfaces = new List<NetworkInterface>();

            allAviableInterfaces = NetworkInterface.GetAllNetworkInterfaces().ToList();

            if (interfaceTypes.Length != 0)
            {
                foreach (NetworkInterface netInt in allAviableInterfaces)
                {
                    if (interfaceTypes.Contains(netInt.NetworkInterfaceType) && netInt.OperationalStatus == status)
                    {
                        selectedInterfaces.Add(netInt);
                    }
                }
                return selectedInterfaces;
            }
            else
            {
                foreach(NetworkInterface netInt in allAviableInterfaces)
                {
                    if (netInt.OperationalStatus == status)
                    {
                        selectedInterfaces.Add(netInt);
                    }
                }
                return selectedInterfaces;
            }
        }

        public static List<IPAddress> GetLocalIPv4(OperationalStatus status, params NetworkInterfaceType[] interfaceTypes)
        {
            List<IPAddress> IPAddresList = new List<IPAddress>();
            foreach (NetworkInterface inter in GetNetworkInterfaces(status))
            {
                if (interfaceTypes.Contains(inter.NetworkInterfaceType))
                {
                    foreach (UnicastIPAddressInformation ip in inter.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            IPAddresList.Add(ip.Address);
                        }
                    }
                }
            }
            return IPAddresList;
        }

        public static List<IPAddress> GetNetworkInterfacesGateways(OperationalStatus status, params NetworkInterfaceType[] interfaceTypes)
        {
            List<NetworkInterface> allAviableInterfaces = NetworkInterface.GetAllNetworkInterfaces().ToList();
            List<IPAddress> resultGateways = new List<IPAddress>();
            if (interfaceTypes.Length != 0)
            {
                foreach (NetworkInterface netInt in allAviableInterfaces)
                {
                    if (interfaceTypes.Contains(netInt.NetworkInterfaceType) && netInt.OperationalStatus == status)
                    {
                        foreach(GatewayIPAddressInformation gatewayInfo in netInt.GetIPProperties().GatewayAddresses)
                        {
                            if (!resultGateways.Contains(gatewayInfo.Address))
                            {
                                if (gatewayInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                                    resultGateways.Add(gatewayInfo.Address);
                            }
                        }
                    }
                }
                return resultGateways;
            }
            else
            {
                foreach (NetworkInterface netInt in allAviableInterfaces)
                {
                    if (netInt.OperationalStatus == status)
                    {
                        foreach (GatewayIPAddressInformation gatewayInfo in netInt.GetIPProperties().GatewayAddresses)
                        {
                            if (!resultGateways.Contains(gatewayInfo.Address))
                            {
                                if (gatewayInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                                    resultGateways.Add(gatewayInfo.Address);
                            }
                        }
                    }
                }
                return resultGateways;
            }
        }

        //Can upgrade to search mac address of ip (|arp -a| win command)
        public static List<(IPAddress, string)> GetDnsNamesOfIps(List<IPAddress> IPAddresses)
        {
            List<(IPAddress IP, string Name)> resultList = new List<(IPAddress, string)>();

            foreach (IPAddress ip in IPAddresses)
            {
                (IPAddress IP, string Name) SelectedIpInfo;
                SelectedIpInfo.IP = ip;

                try
                {
                    SelectedIpInfo.Name = Dns.GetHostEntry(ip).HostName.Split('.')[0];
                }
                catch (Exception ex)
                {
                    SelectedIpInfo.Name = string.Empty;
                }

                resultList.Add(SelectedIpInfo);
            }
            return resultList;
        }

        public static string GetMyDeviceName()
        {
            return Dns.GetHostName();
        }

        public static string BytesConvert(double bytes)
        {
            if (bytes >= Math.Pow(1024, 4))
            {
                return $"{Math.Round(bytes / Math.Pow(1024, 4), 1)} Tb";
            }
            if (bytes >= Math.Pow(1024, 3))
            {
                return $"{Math.Round(bytes / Math.Pow(1024, 3), 1)} Gb";
            }
            if (bytes >= Math.Pow(1024, 2))
            {
                return $"{Math.Round(bytes / Math.Pow(1024, 2), 1)} Mb";
            }
            if (bytes >= 1024)
            {
                return $"{Math.Round(bytes / 1024, 1)} Kb";
            }
            return $"{Math.Round(bytes, 0)} b";
        }
    }

}
