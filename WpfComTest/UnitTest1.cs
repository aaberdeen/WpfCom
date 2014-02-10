using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfApplication1;

namespace WpfComTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestValidIP()
        {
            string server = "192.168.0.255";
            bool returnedValue = false;
           returnedValue =  Usefull.ValidIP(server);

           Assert.AreEqual(returnedValue, true);  //(returnedValue, true);
        }

        [TestMethod]
        public void TestNotValidIP()
        {
            string server = "192.168.0.300";
            bool returnedValue = false;
            returnedValue = Usefull.ValidIP(server);

            Assert.AreEqual(returnedValue, false);  //(returnedValue, true);








        }
    }
}
