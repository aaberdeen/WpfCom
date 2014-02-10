using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WiPANFactory
{
    public partial class WiPANmessages
    {
        public enum messageType : byte
        {
            none, // 0x0
            SYSTEM_CONFIG, //0x1
            TAG_CONFIG,     //0x2
            UDP_CONFIG      //0x3

        }

        public enum dataType
        {
          
            SYSTEM_SET_MAC_CFG = 0x2, 
            SYSTEM_SET_STATIC_IP_ADDR_CFG = 0x3,      
            SYSTEM_SET_STATIC_IP_ENABLE_CFG = 0x4,     
            SYSTEM_SET_SUBNET_MASK_CFG = 0x5,
            SYSTEM_SET_GATEWAY_CFG = 0x6,
            SYSTEM_SET_PASSWORD_CFG = 0x7,
 
            SYSTEM_SET_TIME_CFG = 0xb,
            SYSTEM_SET_DATE_CFG = 0xc,
            
            SYSTEM_RESET = 0x80,
            SYSTEM_REQUEST_INVENTORY = 0X81,  // returns 12 bytes
            SYSTEM_WATCHDOG = 0X0A,

        }

        public enum dataTypeUDP
        {
            START_UDP = 0x1,
            STOP_UDP = 0x2,
        }

    }


    class WipanCmd
    {
        /// <summary>
        /// Constricts data[] message to send reset to coordinator
        /// </summary>
        /// <returns>byte array to send over tcp connection</returns>
        
        public static byte getTypeLength(WiPANmessages.dataType type)
        {
            switch(type)
            {
                case WiPANmessages.dataType.SYSTEM_SET_MAC_CFG:
                  return  0x6;
                case WiPANmessages.dataType.SYSTEM_SET_STATIC_IP_ADDR_CFG:
                  return 0x4;
                case WiPANmessages.dataType.SYSTEM_SET_STATIC_IP_ENABLE_CFG:
                  return 0x1;
                case WiPANmessages.dataType.SYSTEM_SET_SUBNET_MASK_CFG:
                  return 0x4;
                case WiPANmessages.dataType.SYSTEM_SET_GATEWAY_CFG:
                  return 0x4;
                case WiPANmessages.dataType.SYSTEM_SET_PASSWORD_CFG:
                  return 0x4;
                case WiPANmessages.dataType.SYSTEM_SET_TIME_CFG:
                  return 0x4;// not tested
                case WiPANmessages.dataType.SYSTEM_SET_DATE_CFG:
                  return 0x4;// not tested
                case WiPANmessages.dataType.SYSTEM_RESET:
                  return 0x0;
                case WiPANmessages.dataType.SYSTEM_REQUEST_INVENTORY:
                  return 0x0;
                case WiPANmessages.dataType.SYSTEM_WATCHDOG:
                  return 0x0;
  
                default: return 0;
 
            }

        }

        public static byte getTypeLength(WiPANmessages.dataTypeUDP type)
        {
            switch (type)
            {                
                case WiPANmessages.dataTypeUDP.START_UDP:
                    return 0x7;
                case WiPANmessages.dataTypeUDP.STOP_UDP:
                    return 0x7;

                default: return 0;

            }

        }



        public static byte[] UDPstart(string SessionNo, string ipAddress, string port)
        {
            byte session = Convert.ToByte(SessionNo);
            
            //Format IP address************
            byte[] IpAddress = new byte[getTypeLength(WiPANmessages.dataTypeUDP.START_UDP)];
            string[] IPsplit = ipAddress.Split('.');
            for (int i = 0; i < 4; i++)
            {
                //string TEST = string.Format("{0:X}", textBox5.Text.Substring((i * 2), 2));
                IpAddress[i] = Convert.ToByte(IPsplit[i], 10);
            }
            //****************************
            //Format Port****************
            byte[] Port = new byte[2];
            if (port != "")
            {
                int portInt = Convert.ToInt32(port);
                Port[0] = (byte)(portInt & 0xff);            //LSB
                Port[1] = (byte)((portInt & 0xff00) >> 8);   //MSB
            }

            //***************************
                   
            byte[] data2 = {    (byte)WiPANmessages.messageType.UDP_CONFIG,
                                (byte)WiPANmessages.dataTypeUDP.START_UDP,
                                getTypeLength(WiPANmessages.dataTypeUDP.START_UDP),
                                session,
                                IpAddress[3], IpAddress[2], IpAddress[1], IpAddress[0],
                                Port[0], Port[1]
                             };

            return data2;
        }

        public static byte[] UDPstop(string SessionNo, string ipAddress, string port)
        {
            byte session = Convert.ToByte(SessionNo);

            //Format IP address************
            byte[] IpAddress = new byte[getTypeLength(WiPANmessages.dataTypeUDP.STOP_UDP)];
            string[] IPsplit = ipAddress.Split('.');
            for (int i = 0; i < 4; i++)
            {
                //string TEST = string.Format("{0:X}", textBox5.Text.Substring((i * 2), 2));
                IpAddress[i] = Convert.ToByte(IPsplit[i], 10);
            }
            //****************************
            //Format Port****************
            byte[] Port = new byte[2];
            if (port != "")
            {
                int portInt = Convert.ToInt32(port);
                Port[0] = (byte)(portInt & 0xff);            //LSB
                Port[1] = (byte)((portInt & 0xff00) >> 8);   //MSB
            }

            //***************************

            byte[] data2 = {    (byte)WiPANmessages.messageType.UDP_CONFIG,
                                (byte)WiPANmessages.dataTypeUDP.STOP_UDP,
                                getTypeLength(WiPANmessages.dataTypeUDP.STOP_UDP),
                                session,
                                IpAddress[3], IpAddress[2], IpAddress[1], IpAddress[0],
                                Port[0], Port[1]
                             };

            return data2;
        }

        public static byte[] resetBoard()
        {
            byte[] data2 = { (byte)WiPANmessages.messageType.SYSTEM_CONFIG, 
                               (byte)WiPANmessages.dataType.SYSTEM_RESET, 
                               getTypeLength(WiPANmessages.dataType.SYSTEM_RESET)};
            return data2;
        }
        /// <summary>
        /// Constricts data[] message to send IP address to coordinator
        /// </summary>
        /// <param name="ipAddress">ip address to be sent</param>
        /// <returns>byte array to send over tcp connection</returns>
        public static byte[] sendIP(string ipAddress)
        {
            byte[] IpAddress = new byte[getTypeLength(WiPANmessages.dataType.SYSTEM_SET_STATIC_IP_ADDR_CFG)];

            string[] IPsplit = ipAddress.Split('.');


            for (int i = 0; i < 4; i++)
            {
                //string TEST = string.Format("{0:X}", textBox5.Text.Substring((i * 2), 2));
                IpAddress[i] = Convert.ToByte(IPsplit[i], 10);
            }


           

            byte[] data2 = { (byte)WiPANmessages.messageType.SYSTEM_CONFIG, 
                             (byte)WiPANmessages.dataType.SYSTEM_SET_STATIC_IP_ADDR_CFG, 
                             getTypeLength(WiPANmessages.dataType.SYSTEM_SET_STATIC_IP_ADDR_CFG), 
                             IpAddress[3], IpAddress[2], IpAddress[1], IpAddress[0] };

        
        
            return data2;
        }
        /// <summary>
        /// Constricts data[] message to send MAC address to coordinator
        /// </summary>
        /// <param name="macAddress">MAC to be sent</param>
        /// <returns>byte array to send over tcp connection</returns>
        public static byte[] sendMAC(string macAddress)
        {
            byte[] MacAddress = new byte[getTypeLength(WiPANmessages.dataType.SYSTEM_SET_MAC_CFG)];

            for (int i = 0; i < 6; i++)
            {
                string TEST = string.Format("{0:X}", macAddress.Substring((i * 2), 2));
                MacAddress[i] = Convert.ToByte(TEST, 16); // byte.Parse(string.Format("{0:X}", TEST));
            }

            byte[] data2 = { (byte)WiPANmessages.messageType.SYSTEM_CONFIG, 
                             (byte)WiPANmessages.dataType.SYSTEM_SET_MAC_CFG, 
                             getTypeLength(WiPANmessages.dataType.SYSTEM_SET_MAC_CFG), 
                             MacAddress[0], MacAddress[1], MacAddress[2], MacAddress[3], MacAddress[4], MacAddress[5] };
            return data2;
        }
        /// <summary>
        /// Constricts data[] message to send DHCP bit to coordinator
        /// </summary>
        /// <param name="DHCP">DHCP bit val to be sent</param>
        /// <returns>byte array to send over tcp connection</returns>
        public static byte[] sendDHCP(string DHCP)
        {
            byte dhcp = Convert.ToByte(DHCP, 16);

            byte[] data2 = { (byte)WiPANmessages.messageType.SYSTEM_CONFIG, 
                             (byte)WiPANmessages.dataType.SYSTEM_SET_STATIC_IP_ENABLE_CFG, 
                             getTypeLength(WiPANmessages.dataType.SYSTEM_SET_STATIC_IP_ENABLE_CFG), 
                             dhcp};
            return data2;
        }
        /// <summary>
        /// Constricts data[] message to send subnet mask to coordinator
        /// </summary>
        /// <param name="subnetMask">mask to be sent</param>
        /// <returns>byte array to send over tcp connection</returns>
        public static byte[] sendSubnetMask(string subnetMask)
        {
            byte[] subMask = new byte[getTypeLength(WiPANmessages.dataType.SYSTEM_SET_SUBNET_MASK_CFG)];

            string[] IPsplit = subnetMask.Split('.');

            for (int i = 0; i < 4; i++)
            {
                //string TEST = string.Format("{0:X}", textBox5.Text.Substring((i * 2), 2));
                subMask[i] = Convert.ToByte(IPsplit[i], 10);
            }


            byte[] data2 = { (byte)WiPANmessages.messageType.SYSTEM_CONFIG, 
                             (byte)WiPANmessages.dataType.SYSTEM_SET_SUBNET_MASK_CFG,
                             getTypeLength(WiPANmessages.dataType.SYSTEM_SET_SUBNET_MASK_CFG), 
                             subMask[3], subMask[2], subMask[1], subMask[0] };
            return data2;
        }
        /// <summary>
        /// Constricts data[] message to send gateway address to coordinator
        /// </summary>
        /// <param name="gatewayAddress">gateway to be sent</param>
        /// <returns>byte array to send over tcp connection</returns>
        public static byte[] sendGateway(string gatewayAddress)
        {
            byte[] gateway = new byte[getTypeLength(WiPANmessages.dataType.SYSTEM_SET_GATEWAY_CFG)];

            string[] IPsplit = gatewayAddress.Split('.');

            for (int i = 0; i < 4; i++)
            {
                //string TEST = string.Format("{0:X}", textBox5.Text.Substring((i * 2), 2));
                gateway[i] = Convert.ToByte(IPsplit[i], 10);
            }


            byte[] data2 = { (byte)WiPANmessages.messageType.SYSTEM_CONFIG, 
                             (byte)WiPANmessages.dataType.SYSTEM_SET_GATEWAY_CFG,
                             getTypeLength(WiPANmessages.dataType.SYSTEM_SET_GATEWAY_CFG), 
                             gateway[3], gateway[2], gateway[1], gateway[0] };
            return data2;

        }
        /// <summary>
        /// Constricts data[] message to send password to coordinator
        /// </summary>
        /// <param name="password">password to send</param>
        /// <returns>byte array to send over tcp connection</returns>
        public static byte[] sendPassword(string password)
        {
            byte[] byteOut = new byte[getTypeLength(WiPANmessages.dataType.SYSTEM_SET_PASSWORD_CFG)];


            string[] passwordArray = new string[4] { password.Substring(0, 2), 
                                                password.Substring(2, 2), 
                                                password.Substring(4, 2), 
                                                password.Substring(6, 2) 
                                              };
            for (int i = 0; i < 4; i++)
            {
                //string TEST = string.Format("{0:X}", textBox5.Text.Substring((i * 2), 2));
                byteOut[i] = Convert.ToByte(passwordArray[i], 10);
            }


            byte[] data2 = { (byte)WiPANmessages.messageType.SYSTEM_CONFIG, 
                             (byte)WiPANmessages.dataType.SYSTEM_SET_PASSWORD_CFG,
                             getTypeLength(WiPANmessages.dataType.SYSTEM_SET_PASSWORD_CFG), 
                             byteOut[3], byteOut[2], byteOut[1], byteOut[0] };
            return data2;
        }
        /// <summary>
        /// Constricts data[] message to send inventory request to coordinator
        /// </summary>
        /// <returns>byte array to send over tcp connection</returns>
        public static byte[] getINV()
        {
            byte[] data2 = { (byte)WiPANmessages.messageType.SYSTEM_CONFIG, 
                             (byte)WiPANmessages.dataType.SYSTEM_REQUEST_INVENTORY, 
                             getTypeLength(WiPANmessages.dataType.SYSTEM_REQUEST_INVENTORY), 
                             };
            return data2;
        }
    }
}
