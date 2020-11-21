using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using NieroNetLib.Types;

namespace NieroNetLib.Tests
{
    [TestClass]
    public class LocalNetScanTest
    {
        [TestMethod]
        public void CreateTest()
        {
            LocalNetScan testObj = new LocalNetScan(IPAddress.Parse("127.0.0.1"), IPAddress.Parse("255.255.255.0"));
            if(testObj == null)
            {
                throw new Exception("Object creation return null!");
            }
        }

        [TestMethod]
        public void InitFieldsTest()
        {
            LocalNetScan testObj = new LocalNetScan(IPAddress.Parse("127.0.0.1"), IPAddress.Parse("255.255.255.0"));
            if(!(testObj.ScanResult == null &&
               testObj.Gateway != null &&
               testObj.NetMask != null &&
               testObj.Timeout >= 0 &&
               testObj.StartData == null &&
               testObj.FinishedData == null &&
               testObj.ScanStatus == NetScanStatus.Ready &&
               testObj.TotalIpsForScan == 0 &&
               testObj.SentPingsCount == 0 &&
               testObj.CompletedPingsCount == 0 &&
               testObj.ScanResult != null &&
               testObj.Locked == false))
            {
                throw new Exception("Fields initialization is not correct!");
            }
        }

        [TestMethod]
        public void ScaningTest()
        {
            LocalNetScan testObj = new LocalNetScan(IPAddress.Parse("192.168.0.1"), IPAddress.Parse("255.255.255.0"));
            testObj.StartScanning();
        }
    }
}
