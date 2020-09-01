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
    static class NetworkTools
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

        public static async Task<List<PingReply>> ScanLocaNetwork(string netIP = "192.168.0.1", string netMask = "255.255.255.0")
        {
            List<IPAddress> ipAddressesForScan = GenerateIpList(netIP, netMask);

            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();

            options.Ttl = 128;                                                        // Время жизни пакета ICMP (измеряется в прыгах) 
            options.DontFragment = true;                                              // Не разбивать пакет на порции (может понадобится для проверки макс. пропускной способности)
            string data = "For the glory of mankind! With love, 2B, 9S, A2, YoRHa!";  // Ну я могу же оставить пару пасхалок :3
            int timeout = 120;                                                        // Фиг знает, в чем оно измеряется, мсдн не говорит

            var tasks = ipAddressesForScan.Select(ip => pingSender.SendPingAsync(ip,2000));
            var result = await Task.WhenAll(tasks);

            return result.ToList();
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
    }

}
