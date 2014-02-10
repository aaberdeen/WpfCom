using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WiPANFactory
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    class GetMac
    {

        public static string GetMacAddress(string strAddress)
        {
            var inetAddr = inet_addr(strAddress);

            uint addressLen = 16;
            var macAddress = new byte[addressLen];

            if (SendARP(inetAddr, 0, macAddress, ref addressLen) == 0)
            {
                var sb = new StringBuilder();

                for (var index = 0; index < addressLen; index++)
                {
                    if (index > 0)
                    {
                        sb.Append("");
                    }

                    sb.Append(
                        string.Format(
                            "{0:X}",
                            macAddress[index]).PadLeft(2, '0'));
                }

                return sb.ToString();
            }

            return "no result";
            //throw new Exception("SendARP call failed.");
        }

        [DllImport("Ws2_32.dll", CharSet = CharSet.Ansi)]
        private static extern uint inet_addr(string address);

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern int SendARP(
            uint destinationIp,
            uint sourceIp,
            byte[] macAddress,
            ref uint addressLen);

    }
}
