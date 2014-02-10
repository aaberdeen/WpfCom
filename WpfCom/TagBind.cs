using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using ComPort;
using System.Drawing;
using System.IO;
using System.Collections.Specialized;

namespace WpfApplication1
{
    public partial class TagBind : INotifyPropertyChanged
    {
        private errorLog _errorLog = new errorLog();
        //public event SendDataHandler SendDataEvent;
        public delegate void SendDataHandler(byte[] message);

        //  public string TagAdd { get; set; }
        private int _TTL { get; set; }           // tag time to live
        private string _minersName;
        private string _endPointType;
        private int _PktLength;
        private int _PktSequence;
        private string _PktType;
        private int _PktEvent;
        private int _PktTemp;
        private int _Volt;
        private int _PktLqi;
        private int _BrSequ;
        private int _BrSequPersist;
        private int _BrCmd;
        private int _TOFping;
        private int _TOFtimeout;
        private int _TOFrefuse;
        private int _TOFsuccess;
        private int _TOFdistance;
        private int _RSSIdistance;
        private int _TOFerror;
        private string _TOFmac;
        private string _ReaderAdd;
        private string _TagAdd;
        private int _RxLQI;
        private float _CH4gas;
        private int _COgas;
        private float _O2gas;
        private float _CO2gas;
        private uint _u54;
        private uint _u55;
        private uint _u56;
        private uint _u57;
        private uint _u58;
        private uint _u59;
        private uint _u60;
        private uint _u61;
       
        
       


        public event PropertyChangedEventHandler PropertyChanged;



        public TagBind(ref Tag TagIn)
        {
            _PktLength = TagIn.PktLength;
            _PktSequence = TagIn.PktSequence;
            _PktEvent = TagIn.PktEvent;
            _PktTemp = TagIn.PktTemp;
            _Volt = TagIn.Volt;
            _PktLqi = TagIn.PktLqi;
            _BrSequ = TagIn.BrSequ;
            _BrCmd = TagIn.BrCmd;
            _TOFping = TagIn.TOFping;
            _TOFtimeout = TagIn.TOFtimeout;
            _TOFrefuse = TagIn.TOFrefuse;
            _TOFsuccess = TagIn.TOFsuccess;
            _TOFdistance = TagIn.TOFdistance;
            _RSSIdistance = TagIn.RSSIdistance;
            _TOFerror = TagIn.TOFerror;
            _TOFmac = TagIn.TOFmac;
            _ReaderAdd = TagIn.ReaderAdd;
            _RxLQI = TagIn.RxLQI;
            _TagAdd = TagIn.TagAdd;
            _TTL = TagIn.TTL;
            _minersName = TagIn.Name;
            endPointType = TagIn.endPointType;

        }

        public TagBind()
        {
            // TODO: Complete member initialization
        }

        public int TTL
        {
            get { return _TTL; }
            set
            {
                if (_TTL != value)
                {
                _TTL = value;
                this.NotifyPropertyChanged("TTL");
                }
            }
        }
        public int PktLength
        {
            get { return _PktLength; }
            set
            {
                if (_PktLength != value)
                {
                _PktLength = value;
                this.NotifyPropertyChanged("PktLength");
                }
            }
        }
        public string minersName
        {
            get { return _minersName; }
            set
            {
                if (_minersName != value)
                {
                _minersName = value;
                this.NotifyPropertyChanged("minersName");
                }
            }
        }
        public string endPointType
        {
            get { return _endPointType; }
            set
            {
                if (_endPointType != value)
                {
                    _endPointType = value;
                this.NotifyPropertyChanged("endPointType");
                }
            }
        }
        public int PktSequence
        {
            get { return _PktSequence; }
            set
            {
                if (_PktSequence != value)
                {
                _PktSequence = value;
                this.NotifyPropertyChanged("PktSequence");
                }
            }
        }
        public string PktType
        {
            get { return _PktType; }
            set
            {
                if (_PktType != value)
                {
                _PktType = value;
                this.NotifyPropertyChanged("PktType");
                }
            }
        }


        public string ReaderAdd
        {
            get { return _ReaderAdd; }
            set
            {
                if (_ReaderAdd != value)
                {
                    _ReaderAdd = value;
                    this.NotifyPropertyChanged("ReaderAdd");
                }

            }
        }

        public string TagAdd
        {
            get { return _TagAdd; }
            set
            {
                if (_TagAdd != value)
                {
                    _TagAdd = value;
                    this.NotifyPropertyChanged("TagAdd");
                }
            }
        }
        public int PktLqi
        {
            get { return _PktLqi; }
            set
            {
                if (_PktLqi != value)
                {
                    _PktLqi = value;
                    this.NotifyPropertyChanged("PktLqi");
                }
            }
        }
        public int PktEvent
        {
            get { return _PktEvent; }
            set
            {
                if (_PktEvent != value)
                {
                    _PktEvent = value;
                    this.NotifyPropertyChanged("PktEvent");
                }
            }
        }
        public int PktTemp
        {
            get { return _PktTemp; }
            set
            {
                if (_PktTemp != value)
                {
                    _PktTemp = value;
                    this.NotifyPropertyChanged("PktTemp");
                }
            }
        }
        public int iVolt
        {
            //get { return _Volt; }
            set
            {
                if (_Volt != value)
                {
                    _Volt = value;
                    this.NotifyPropertyChanged("iVolt");
                    this.NotifyPropertyChanged("Volt");
                }
            }
        }

        public float Volt
        {
            get
            {
                float toReturn = _Volt;
                return toReturn/1000;
            }
        }

        public int BrSequ
        {
            get { return _BrSequ; }
            set
            {
                
                _BrSequ = value;
                this.NotifyPropertyChanged("BrSequ");
                if (value != 0)
                {
                    _BrSequPersist = value;
                this.NotifyPropertyChanged("BrSequPersist");
                }
                
                
            }
        }
        public int BrSequPersist
        {
            get
            {
               return _BrSequPersist;
            }
        }
        public int BrCmd
        {
            get { return _BrCmd; }
            set
            {
                if (_BrCmd != value)
                {
                    _BrCmd = value;
                    this.NotifyPropertyChanged("BrCmd");
                }
            }
        }
        public int TOFping
        {
            get { return _TOFping; }
            set
            {
                if (_TOFping != value)
                {
                    _TOFping = value;
                    this.NotifyPropertyChanged("TOFping");
                }
            }
        }
        public int TOFtimeout
        {
            get { return _TOFtimeout; }
            set
            {
                if (_TOFtimeout != value)
                {
                    _TOFtimeout = value;
                    this.NotifyPropertyChanged("TOFtimeout");
                }
            }
        }
        public int TOFrefuse
        {
            get { return _TOFrefuse; }
            set
            {
                if (_TOFrefuse != value)
                {
                    _TOFrefuse = value;
                    this.NotifyPropertyChanged("TOFrefuse");
                }
            }
        }
        public int TOFsuccess
        {
            get { return _TOFsuccess; }
            set
            {
                if (_TOFsuccess != value)
                {
                    _TOFsuccess = value;
                    this.NotifyPropertyChanged("TOFsuccess");
                }
            }
        }
        public int TOFdistance
        {
            get { return _TOFdistance; }
            set
            {
                if (_TOFdistance != value)
                {
                    _TOFdistance = value;
                    this.NotifyPropertyChanged("TOFdistance");
                }
            }
        }
        public int RSSIdistance
        {
            get { return _RSSIdistance; }
            set
            {
                if (_RSSIdistance != value)
                {
                    _RSSIdistance = value;
                    this.NotifyPropertyChanged("RSSIdistance");
                }
            }
        }
        public int TOFerror
        {
            get { return _TOFerror; }
            set
            {
                if (_TOFerror != value)
                {
                    _TOFerror = value;
                    this.NotifyPropertyChanged("TOFerror");
                }
            }
        }
        public string TOFmac
        {
            get { return _TOFmac; }
            set
            {
                if (_TOFmac != value)
                {
                    _TOFmac = value;
                    this.NotifyPropertyChanged("TOFmac");
                }
            }
        }


        public int RxLQI
        {
            get { return _RxLQI; }
            set
            {
                if (_RxLQI != value)
                {
                    _RxLQI = value;
                    this.NotifyPropertyChanged("RxLQI");
                }
            }
        }

        public float CH4gas
        {
            get { return _CH4gas; }
            set
            {
                if (_CH4gas != value)
                {
                    _CH4gas = value;
                    this.NotifyPropertyChanged("CH4gas");
                }
            }
        }
        public int COgas
        {
            get { return _COgas; }
            set
            {
                if (_COgas != value)
                {
                    _COgas = value;
                    this.NotifyPropertyChanged("COgas");
                }
            }
        }
        public float O2gas
        {
            get { return _O2gas; }
            set
            {
                if (_O2gas != value)
                {
                    _O2gas = value;
                    this.NotifyPropertyChanged("O2gas");
                }
            }
        }
        public float CO2gas
        {
            get { return _CO2gas; }
            set
            {
                if (_CO2gas != value)
                {
                    _CO2gas = value;
                    this.NotifyPropertyChanged("CO2gas");
                }
            }
        }
        //for pullkey
        public uint u54
        {
            //  get { return _u54; }
            set
            {
                if (_u54 != value)
                {
                    _u54 = value;
                    this.NotifyPropertyChanged("u54");
                    this.NotifyPropertyChanged("zoneID");
                }
            }
        }
        public string zoneID
        {
            // get { return string.Format("{0:B}", _u54); } // _u61; }
            get { return Convert.ToString(_u54, 16); } //.PadLeft(8,'0');}

        }
        public uint u55
        {
            //   get { return _u55; }
            set
            {
                if (_u55 != value)
                {
                    _u55 = value;
                    this.NotifyPropertyChanged("u55");
                    this.NotifyPropertyChanged("unitID");
                }
            }
        }
        public string unitID
        {
            // get { return string.Format("{0:b}", _u55); } // _u61; }
            get { return Convert.ToString(_u55, 16); } //.PadLeft(8, '0'); }
        }
        public uint u56
        {
            // get { return _u56; }
            set
            {
                if (_u56 != value)
                {
                    _u56 = value;
                    this.NotifyPropertyChanged("u56");
                    this.NotifyPropertyChanged("failState");
                }
            }
        }
        public string failState
        {
            //get { return string.Format("{0:b}", _u56); } // _u61; }
            get { return Convert.ToString(_u56, 2).PadLeft(8, '0'); }
        }
        public uint u57
        {
            //  get { return _u57; }
            set
            {
                if (_u57 != value)
                {
                    _u57 = value;
                    this.NotifyPropertyChanged("u57");
                    this.NotifyPropertyChanged("s57");
                }
            }
        }
        public string s57
        {
            //get { return string.Format("{0:b}", _u57); } // _u61; }
            get { return Convert.ToString(_u57, 2).PadLeft(8, '0'); }

        }
        public uint u58
        {
            // get { return _u58; }
            set
            {
                if (_u58 != value)
                {
                    _u58 = value;

                    this.NotifyPropertyChanged("u58");
                    this.NotifyPropertyChanged("dcVoltsState");
                    this.NotifyPropertyChanged("switchState");
                    this.NotifyPropertyChanged("Image");
                    this.NotifyPropertyChanged("remoteLockout");
                    this.NotifyPropertyChanged("switchError");
                    this.NotifyPropertyChanged("RLO_Error");
                    this.NotifyPropertyChanged("keyShort");
                }
            }
        }
        public string switchState
        {
            //get { return string.Format("{0:b}", _u58); } // _u61; }



            get
            {
                string text = "";
                if (this._endPointType == "Key")
                {
                    if ((_u58 & 0x0f) == 0)
                    {
                        text += "Clear \n";
                    }
                    if ((_u58 & 0x01) > 0)
                    {
                        text += "FET LO \n";
                    }
                    if ((_u58 & 0x02) > 0)
                    {
                        text += "LH Slack \n";
                    }
                    if ((_u58 & 0x04) > 0)
                    {
                        text += "LockOut \n";
                    }
                    if ((_u58 & 0x08) > 0)
                    {
                        text += "RH Slack \n";
                    }
                }
                return text;

                // return Convert.ToString(_u58, 2).PadLeft(8, '0'); 

            }

        }

        public string Image
        {

            get
            {

                string text = "";
                if (this._endPointType == "Key")
                {
                    uint u58 = _u58 >> 1;

                    if (u58 == 0)
                    {

                        text = "key_good.png";
                        _errorLog.write(string.Format("Key Good, Zone= {0}, Unit= {1}", this.zoneID, this.unitID));
                    }

                    if (u58 == 1)
                    {
                        text = "key_LH.png";
                        _errorLog.write(string.Format("Key Slack Left, Zone= {0}, Unit= {1}", this.zoneID, this.unitID));
                    }
                    if (u58 == 2)
                    {
                        text = "key_LO.png";
                        _errorLog.write(string.Format("Key Lock Out, Zone= {0}, Unit= {1}", this.zoneID, this.unitID));
                    }
                    if (u58 == 3)
                    {
                        text = "key_LO_LH.png";
                        _errorLog.write(string.Format("Key Lock Out, Slack Left, Zone= {0}, Unit= {1}", this.zoneID, this.unitID));
                    }
                    if (u58 == 4)
                    {
                        text = "key_RH.png";
                        _errorLog.write(string.Format("Key Slack Right, Zone= {0}, Unit= {1}", this.zoneID, this.unitID));
                    }

                    if (u58 == 5)
                    {
                        text = "key_LH_RH.png";
                        _errorLog.write(string.Format("Key Slack Left, Slack Right, Zone= {0}, Unit= {1}", this.zoneID, this.unitID));
                    }
                    if (u58 == 6)
                    {
                        text = "key_LO_RH.png";
                        _errorLog.write(string.Format("Key Lock Out, Slack Right, Zone= {0}, Unit= {1}", this.zoneID, this.unitID));

                    }
                    if (u58 == 7)
                    {
                        text = "key_LO_LH_RH.png";
                        _errorLog.write(string.Format("Key Lock Out, Slack Left, Slack Right, Zone= {0}, Unit= {1}", this.zoneID, this.unitID));
                    }
                }
                    return text;

                    // return Convert.ToString(_u58, 2).PadLeft(8, '0'); 
                
         
            }

        }

        public bool remoteLockout
        {
            get
            {


                if ((_u58 & switchMask.LO_FET_Gate) == switchMask.LO_FET_Gate)
                {
                    if (this._endPointType == "Key")
                    {
                        _errorLog.write(string.Format("remote lockout enabled, Zone= {0}, Unit= {1}", this.zoneID, this.unitID));
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
         
         
        }



        public bool keyShort
        {
            get
            {
                if ((_u59 & voltMask.fetOpenAlt) == voltMask.fetOpenAlt) // the fet is open so we v3 will equal v4
                {
                    return false;
                }
                else
                {
                    if ((_u59 & voltMask.lineOpen) == voltMask.lineOpen)
                    {
                        return false; //the line is open so we can't tell if there is a key short
                    }
                    if ((_u59 & voltMask.switchesClosed) != voltMask.switchesClosed)
                    {
                        return false; //the switches are open so we can't tell if there is a key short
                    }
                    else
                    {
                        if ((_u59 & voltMask.keyshort) == voltMask.keyshort)
                        {
                            if (this._endPointType == "Key")
                            {
                                _errorLog.write(string.Format("key short Detected, Zone= {0}, Unit= {1}", this.zoneID, this.unitID));
                            }
                                return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }


        public bool switchError
        {    
            get
            {
                if ((_u59 & voltMask.lineOpen) == voltMask.lineOpen)
                {
                    return false; //the line is open so we can't tell if there is a switch error
                }
                else
                {
                    if ((_u59 & voltMask.switchesClosed) == 0)    //v1 != v2  a switch must be open 
                    {
                        if ((_u58 & switchMask.All_Switches_open) == 0)      //((_u58 >> 1) == 0)  // all switches closed
                        {
                            if (this._endPointType == "Key")
                            {
                                _errorLog.write(string.Format("switch error Detected, Zone= {0}, Unit= {1}", this.zoneID, this.unitID));
                            }        
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else  //switches must be closed
                    {
                        if ((_u58 & switchMask.All_Switches_open) > 0) // a switch muse be open
                        {
                            if (this._endPointType == "Key")
                            {
                                _errorLog.write(string.Format("switch error Detected, Zone= {0}, Unit= {1}", this.zoneID, this.unitID));
                            }
                                return true;

                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

        }
        
        //volt Masks
        private struct voltMask
        {
            public static byte lineOpen = 0x80;
            public static byte keyshort = 0x40;
            public static byte fetOpenAlt = 0x20;
            public static byte switchOpenAlt = 0x10;
            public static byte fetClosed = 0x02;
            public static byte switchesClosed = 0x01;
        }
        //switch masks
        private struct switchMask
        {
            public static byte All_switches_FET_open = 0x0f;
            public static byte All_Switches_open = 0xE;
            public static byte LH_SLACK = 0x08;
            public static byte LOCK_OUT = 0x04;
            public static byte RH_SLACK = 0x02;
            public static byte LO_FET_Gate = 0x01;
        }
        
        public bool RLO_Error
        {
            get
            {
                if ((_u59 & voltMask.lineOpen) == voltMask.lineOpen)
                {
                    return false; //the line is open so we can't tell if there is a switch error
                }
                else
                {

                    if ((_u59 & voltMask.fetClosed) == 0) // v2 != v3 fet must be open
                    {
                        if ((_u58 & switchMask.LO_FET_Gate) != switchMask.LO_FET_Gate) // RLO off
                        {
                            if (this._endPointType == "Key")
                            {
                                _errorLog.write(string.Format("remote lockout error, Zone= {0}, Unit= {1}", this.zoneID, this.unitID));
                            }
                            return true;
                            
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else //fet must be closed
                    {
                        if ((_u58 & switchMask.LO_FET_Gate) == switchMask.LO_FET_Gate) //RLO on
                        {
                            if (this._endPointType == "Key")
                            {
                                _errorLog.write(string.Format("remote lockout error, Zone= {0}, Unit= {1}", this.zoneID, this.unitID));
                            }
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

            }

        }
        
        public uint u59
        {
            // get { return _u59; }
            set
            {
                if (_u59 != value)
                {
                    _u59 = value;
                    this.NotifyPropertyChanged("u59");
                    this.NotifyPropertyChanged("dcVoltsState");
                    this.NotifyPropertyChanged("switchState");
                    this.NotifyPropertyChanged("Image");
                    this.NotifyPropertyChanged("remoteLockout");
                    this.NotifyPropertyChanged("switchError");
                    this.NotifyPropertyChanged("RLO_Error");
                    this.NotifyPropertyChanged("keyShort");
                }
            }
        }
        public string dcVoltsState
        {
            //get { return string.Format("{0:b}", _u59); } // _u61; }
            get { return Convert.ToString(_u59, 2).PadLeft(8, '0'); }

        }
        public uint u60
        {
            // get { return _u60; }
            set
            {
                if (_u60 != value)
                {
                    _u60 = value;
                    this.NotifyPropertyChanged("u60");
                    this.NotifyPropertyChanged("adcReadError1");
                }
            }
        }
        public string adcReadError1
        {
            //get { return string.Format("{0:b}", _u60); } // _u61; }
            get { return Convert.ToString(_u60, 2).PadLeft(8, '0'); }

        }
        public uint u61
        {
            // get { return _u61; }
            set
            {
                if (_u61 != value)
                {
                    _u61 = value;
                    this.NotifyPropertyChanged("u61");
                    this.NotifyPropertyChanged("adcReadError2");
                }
            }
        }
        public string adcReadError2
        {
            //get { return string.Format("{0:b}",_u61); } // _u61; }
            get { return Convert.ToString(_u61, 2).PadLeft(8, '0'); }

        }


        private void NotifyPropertyChanged(string name)
        {


            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));


        }

     

    }
}
