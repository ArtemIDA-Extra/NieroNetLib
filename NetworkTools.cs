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
        public static List<IPAddress> GenerateIpList(IPAddress netIP, IPAddress netMask)
        {
            List<IPAddress> ipAddresses = new List<IPAddress>();
            int NodeTemplate_0 = 255, NodeTemplate_1 = 255, NodeTemplate_2 = 255, NodeTemplate_3 = 255;
            string[] netMaskNodes_str = netMask.ToString().Split('.');
            string[] netIPNodes_str = netIP.ToString().Split('.');

            if (Int32.Parse(netMaskNodes_str[0]) != 255) NodeTemplate_0 = Int32.Parse(netIPNodes_str[0]);
            if (Int32.Parse(netMaskNodes_str[1]) != 255) NodeTemplate_1 = Int32.Parse(netIPNodes_str[1]);
            if (Int32.Parse(netMaskNodes_str[2]) != 255) NodeTemplate_2 = Int32.Parse(netIPNodes_str[2]);
            if (Int32.Parse(netMaskNodes_str[3]) != 255) NodeTemplate_3 = Int32.Parse(netIPNodes_str[3]);

            for (int Node_0 = NodeTemplate_0; Node_0 <= 255; Node_0++)
            {
                for (int Node_1 = NodeTemplate_1; Node_1 <= 255; Node_1++)
                {
                    for (int Node_2 = NodeTemplate_2; Node_2 <= 255; Node_2++)
                    {
                        for (int Node_3 = NodeTemplate_3; Node_3 <= 255; Node_3++)
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
