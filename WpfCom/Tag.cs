using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace WpfApplication1
{

    public class Tag
    {

        private errorLog _errorLog = new errorLog();
        public string TagAdd { get; set; }
        public int TTL { get; set; }           // tag time to live

        public string Name;
        public string endPointType;
        public int PktLength;
        public int PktSequence;
        public string PktType;
        public int PktEvent;
        public int PktTemp;
        public int Volt;
        public int PktLqi;
        public int BrSequ;
        public int BrCmd;
        public int TOFping;
        public int TOFtimeout;
        public int TOFrefuse;
        public int TOFsuccess;
        public int TOFdistance;
        public int RSSIdistance;
        public int TOFerror;
        public string TOFmac;
        public string ReaderAdd;
        public int RxLQI;
        public float CH4gas;
        public int COgas;
        public float O2gas;
        public float CO2gas;
        public uint CheckSum;
        public uint u54;
        public uint u55;
        public uint u56;
        public uint u57;
        public uint u58;
        public uint u59;
        public uint u60;
        public uint u61;
        public uint calculatedCheckSum;


        public void UpdateTag(int[] rxArray, uint calculatedCheckSumIn)
        {
            UInt16 u16data = 2;

            this.PktLength = rxArray[2]; //2
            this.PktSequence = rxArray[3]; //3
            this.PktType = string.Format("{0:X}",rxArray[4]);
            //4 Type               
           //5 Sleep time
           //6 Sleep time
            //7 Sleep time
            //8 Sleep time
            this.PktEvent = (rxArray[10] << 8) + rxArray[9];
            //11 buttons
            this.PktTemp = rxArray[12];
            this.Volt = (rxArray[14] << 8) + rxArray[13];
            u16data++;//15 tag ping sequ count
            this.TagAdd = string.Format("{0:X}{1:X}{2:X}{3:X}{4:X}{5:X}{6:X}{7:X}",
                                        rxArray[23].ToString("X2"), rxArray[22].ToString("X2"), rxArray[21].ToString("X2"), rxArray[20].ToString("X2"),
                                        rxArray[19].ToString("X2"), rxArray[18].ToString("X2"), rxArray[17].ToString("X2"), rxArray[16].ToString("X2")
                                        );
            //WorkingTag.PktLength = (PortArray[2] << 8) + PortArray[1]; //Then use it to index allTag List

            this.ReaderAdd = string.Format("{0:X}{1:X}{2:X}{3:X}{4:X}{5:X}{6:X}{7:X}",
                                          rxArray[31].ToString("X2"), rxArray[30].ToString("X2"), rxArray[29].ToString("X2"), rxArray[28].ToString("X2"),
                                          rxArray[27].ToString("X2"), rxArray[26].ToString("X2"), rxArray[25].ToString("X2"), rxArray[24].ToString("X2")
                                          );
            this.PktLqi = rxArray[32];
            //Broadcast
            this.BrSequ = rxArray[33];
            this.BrCmd = rxArray[34];
            //TOF
            this.TOFping = (rxArray[36] << 8) + rxArray[35];
            this.TOFtimeout = rxArray[37];
            this.TOFrefuse = rxArray[38];
            this.TOFsuccess = rxArray[39];
            this.TOFdistance = ((rxArray[43] << 24) + (rxArray[42] << 16) + (rxArray[41] << 8) + (rxArray[40]));

            this.TOFerror = rxArray[44];
            this.TOFmac = string.Format("{0:X}{1:X}{2:X}{3:X}{4:X}{5:X}{6:X}{7:X}",
                                             rxArray[52].ToString("X2"), rxArray[51].ToString("X2"), rxArray[50].ToString("X2"), rxArray[49].ToString("X2"),
                                             rxArray[48].ToString("X2"), rxArray[47].ToString("X2"), rxArray[46].ToString("X2"), rxArray[45].ToString("X2"));

            this.RxLQI = rxArray[53];

            //Gas Sensors correct way
            //this.CH4gas = (float)((rxArray[54] << 8) + (rxArray[55])) / 100;  // %
            //this.COgas = (rxArray[56] << 8) + rxArray[57];                    //ppm
            //this.O2gas = (float)((rxArray[58] << 8) + rxArray[59]) / 10;      // %
            //this.CO2gas = (float)((rxArray[60] << 8) + rxArray[61]) / 10;      // %

            //Gas Sensors fix to hide error in embeded code co and co2 swap 
            this.CH4gas = (float)((rxArray[54] << 8) + (rxArray[55])) / 100;  // %
            this.COgas = (rxArray[60] << 8) + rxArray[61];                    //ppm
            this.O2gas = (float)((rxArray[58] << 8) + rxArray[59]) / 10;      // %
            this.CO2gas = (float)((rxArray[56] << 8) + rxArray[57]) / 10;      // %
            this.CheckSum = (uint)((rxArray[65]<<24) + (rxArray[64]<<16) + (rxArray[63]<<8) + rxArray[62]);
            this.TTL = 10;

            //add tag name

            //this.Name = "";

            //for pullkey readings
            this.u54 = (uint)rxArray[54];
            this.u55 = (uint)rxArray[55];
            this.u56 = (uint)rxArray[56];
            this.u57 = (uint)rxArray[57];
            this.u58 = (uint)rxArray[58];
            this.u59 = (uint)rxArray[59];
            this.u60 = (uint)rxArray[60];
            this.u61 = (uint)rxArray[61];

            this.calculatedCheckSum = calculatedCheckSumIn;

            if (CheckSum != calculatedCheckSum)
            {
               _errorLog.write("tag check sum fail ");
            }

            
        }

        
        public void UpdateTag(ref Tag tag)
        {
            UInt16 u16data = 2;

            this.PktLength = tag.PktLength; //2
            this.PktSequence = tag.PktSequence; //3
            this.PktType = tag.PktType;
            //4 Type               
            //5 Sleep time
            //6 Sleep time
            //7 Sleep time
            //8 Sleep time
            this.PktEvent = tag.PktEvent;
            //11 buttons
            this.PktTemp = tag.PktTemp;
            this.Volt = tag.Volt;
            u16data++;//15 tag ping sequ count
            this.TagAdd = tag.TagAdd;
            //WorkingTag.PktLength = (PortArray[2] << 8) + PortArray[1]; //Then use it to index allTag List

            this.ReaderAdd = tag.ReaderAdd;
            this.PktLqi = tag.PktLqi;
            //Broadcast
            this.BrSequ = tag.BrSequ;
            this.BrCmd = tag.BrCmd;
            //TOF
            this.TOFping = tag.TOFping;
                ;
            this.TOFtimeout = tag.TOFtimeout;
            this.TOFrefuse = tag.TOFrefuse;
            this.TOFsuccess = tag.TOFsuccess;
            this.TOFdistance = tag.TOFdistance;

            this.TOFerror = tag.TOFerror;
            this.TOFmac = tag.TOFmac;

            this.RxLQI = tag.RxLQI;

            //Gas Sensors correct way
            //this.CH4gas = (float)((rxArray[54] << 8) + (rxArray[55])) / 100;  // %
            //this.COgas = (rxArray[56] << 8) + rxArray[57];                    //ppm
            //this.O2gas = (float)((rxArray[58] << 8) + rxArray[59]) / 10;      // %
            //this.CO2gas = (float)((rxArray[60] << 8) + rxArray[61]) / 10;      // %

            //Gas Sensors fix to hide error in embeded code co and co2 swap 
            this.CH4gas = tag.CH4gas;  // %
            this.COgas = tag.COgas;                    //ppm
            this.O2gas = tag.O2gas;      // %
            this.CO2gas = tag.CO2gas;      // %
            this.CheckSum = tag.CheckSum;
            this.TTL = tag.TTL;

            //add tag name

            //this.Name = "";

            //for pullkey readings
            this.u54 = tag.u54;
            this.u55 = tag.u55;
            this.u56 = tag.u56;
            this.u57 = tag.u57;
            this.u58 = tag.u58;
            this.u59 = tag.u59;
            this.u60 = tag.u60;
            this.u61 = tag.u61;


        }


    }


}
