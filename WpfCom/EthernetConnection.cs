using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
//using ComPort;
using System.Linq;
using SimpleUdpReciever;
using System.Net;
using System.Diagnostics;
using WiPANFactory;
using System.IO;
using System.Windows.Threading;
using System.Collections.Concurrent;


namespace WpfApplication1
{
    class EthernetConnection
    {
        private errorLog _errorLog = new errorLog();
        private Thread _TCPSendThread;
        private Thread _TCPListenThread;
        private Queue<byte> _ethernetReciveQueue = new Queue<byte>();
        private Queue<int> _ethernetTransmitQueue = new Queue<int>();
        private AutoResetEvent _ethernetSendWaitHandle;
        private volatile bool _shouldStopListen;
        private volatile bool _shouldStopSend;
        private volatile bool _shouldStopMain;
        private ComSetup _com;
       // private DBConnect _dbConnect;
        private MinersNamesForm _MNform;
        private DataBaseSetup _DBsetup;
        /* NetworkStream that will be used */
        private NetworkStream _tcpStream;
        /* TcpClient that will connect for us */
        public TcpClient _tcpClient;
        /* Storage space */
        private byte[] _TCPrxBuffer;
        private byte[] _TCPtxBuffer;
        private int _txBufferPosition;
        private bool _InFrame = false;
        private byte _DLEflag = 0;
        private int _PortArrayCount = 0;
        private int[] _PortArray = new int[200];
        private int _PortArrayMax = 200;
        const int DLE = 0x10;
        const int STX = 0x2;
        private int _PktLengthInt = 0;
        private Tag _WorkingTag = new Tag();
       // DBConnect dbConnectRef;
        //private Lists _allLists;
        private Thread _formFrameThread;
        private AutoResetEvent tidFormFrameWaitHandle = new AutoResetEvent(false);
        private int _index;
        private int _checkSumFails;
        private Message _messageWindow1;
        private Thread _tidExtractData;
        private bool _shouldStopTidExtractData;
        private AutoResetEvent tidExtractDataWaitHandle= new AutoResetEvent(false);
       // public Thread tidExtractData1;
       // public AutoResetEvent tidExtractData1WaitHandle= new AutoResetEvent(false);
        private UdpClient _udPclient;
        private bool _udpCallbackRun;
       const int checkSumLength = 4;
        const int dleStxLength = 2;
        public static Lists allLists = new Lists();
        private static DispatcherTimer _etherTickTimer = new DispatcherTimer();
        public static ConcurrentDictionary<string, EthernetConnection> _myEthConnList = new ConcurrentDictionary<string, EthernetConnection>();
        private Coordinators _coordData;

         public EthernetConnection(Coordinators coordData, ref ComSetup com, ref MinersNamesForm MNform, ref DataBaseSetup DBsetup, bool DBcon, int index, ref Message messageWindow1)
        {
            _com = com;
            _MNform = MNform;
            _DBsetup = DBsetup;
           // dbConnectRef = DBcon;
            _index = index;
            _messageWindow1 = messageWindow1;
            _coordData = coordData;
            _ethernetSendWaitHandle = new AutoResetEvent(false);
            _etherTickTimer.Interval = new TimeSpan(0, 0, 10);
            _etherTickTimer.Tick += new EventHandler(etherTickTimer);
            _etherTickTimer.Start();
            
            if (DBcon)
            {
                trackingDataBaseSetup();
            }

            try
            {
                if (TCPinit())                        //(tcpClient.Connected)
                {
                    allLists.coordinators[_index].connected = true;
                    // this is in the try because we don't want to start the treads without the TCP connecton
                    ThreadInit();
                    UDPInit();
                }
            }
            catch(Exception e)
            {
                EthernetConnection.allLists.coordinators[_index].connected = false;
                _errorLog.write(e, "EthernetConnection Init");
            }
        }

        private void etherTickTimer(object sender, EventArgs e)
        {
            TCPSend(); //kicks send thread to send a packet to check link
        }
      
        /// <summary>
        /// Initalise TCP connection and then send a message to set up the UDP at the far end
        /// </summary>
        /// <returns></returns>
        private bool TCPinit()
        {
            _tcpClient = new TcpClient(_coordData.IP, Int32.Parse(_coordData.TCPport));
            /* Store the NetworkStream */
            _tcpStream = _tcpClient.GetStream();
            /* Create data buffer */
            _TCPrxBuffer = new byte[_tcpClient.ReceiveBufferSize];
            _TCPtxBuffer = new byte[_tcpClient.SendBufferSize];

            if (_tcpClient.Connected)
            {
                if (_tcpClient != null)
                {
                    if (_tcpClient.Connected)
                    {
                        SendUDPSetupMessage();
                    }
                    else
                    {
                        Debug.Write("\nnot Connected");
                    }
                }
                else
                {
                    Debug.Write("\nnot Connected");
                }
                return true; 
            }
            else
            {
                return false;
            }
        }
  
        /// <summary>
        /// Initalise and start 4 treads
        /// </summary>
        private void ThreadInit()
        {
            /* Vital: Create listening thread and assign it to ListenThread() */
            _TCPListenThread = new Thread(new ThreadStart(TCPListenThread));
            _TCPListenThread.Name = "_TCPListenThread";
            _TCPListenThread.IsBackground = true;
            /* Vital: Create sending thread and assign it to SendThread() */
            _TCPSendThread = new Thread(new ThreadStart(TCPSendThread));
            _TCPSendThread.Name = "_TCPSendThread";
            _TCPSendThread.IsBackground = true;
            /*extract data thread*/
            _tidExtractData = new Thread(new ThreadStart(extractDataThread));
            _tidExtractData.Name = "tidExtractData";
            _tidExtractData.IsBackground = true;
            // tidExtractData1 = new Thread(new ThreadStart(extractDataThread1));
            /*main thread*/
            _formFrameThread = new Thread(new ThreadStart(makeFrameThread));
            _formFrameThread.Name = "formFrameThread";
            _formFrameThread.IsBackground = true;

            _TCPListenThread.Start();
            _TCPSendThread.Start();
            _formFrameThread.Start();
            _tidExtractData.Start();
            //tidExtractData1.Start();
        }

        /// <summary>
        /// Initalise UDP and set up RX call back
        /// </summary>
        private void UDPInit()
        {
            IPEndPoint remoteSender = new IPEndPoint(IPAddress.Any, 0);
            IPAddress udpAddress;
            int remotePort = 1000;  // 0 listens to all ports rich uses 1000;
            if (IPAddress.TryParse(_coordData.IP, out udpAddress))
            {
                remoteSender.Address = udpAddress;
                remoteSender.Port = remotePort;
                _udpCallbackRun = true;
                _udPclient = new UdpClient(Convert.ToInt32(_coordData.udpPort));
                UdpState state = new UdpState(_udPclient, remoteSender);
                _udPclient.BeginReceive(new AsyncCallback(UdpDataReceived), state);  // this is blocking 
                _errorLog.write(string.Format("udp setup done {0}:{1}", udpAddress, _coordData.udpPort));
            }
            else
            {
                _errorLog.write("udp Address parse fail");
            }
        }

        /// <summary>
        /// Sends the UDP setup messages over TCP
        /// </summary>
        private void SendUDPSetupMessage()
        {
            try
            {
                string UDPSess = "0";
                string UDPaddress = _coordData.localIP;  //"10.1.0.97"; // my pc for now
                byte[] startFrame = WipanCmd.UDPstart(UDPSess, UDPaddress, _coordData.udpPort);
                byte[] stopframe = WipanCmd.UDPstop(UDPSess, UDPaddress, _coordData.udpPort);

                //stop 3 times to clear all UDP transmits
                _tcpStream.Write(stopframe, 0, stopframe.Length);
                Thread.Sleep(100);
                _tcpStream.Write(stopframe, 0, stopframe.Length);
                Thread.Sleep(100);
                _tcpStream.Write(stopframe, 0, stopframe.Length);
                Thread.Sleep(100);
                //start udp
                _tcpStream.Write(startFrame, 0, startFrame.Length);
                Debug.Write(string.Format("\nUDP Start Sent:{0:X2},{1:X2},{2:X2},{3:X2},{4:X2},{5:X2},{6:X2},{7:X2},{8:X2},{9:X2}", startFrame[0], startFrame[1], startFrame[2], startFrame[3], startFrame[4], startFrame[5], startFrame[6], startFrame[7], startFrame[8], startFrame[9]));
            }
            catch (Exception ex)
            {
                Debug.Write(ex.ToString());
                _errorLog.write(ex, "ethernetConnect tcpStream.Write");
            }
        }
        /// <summary>
        /// call back for RX UDP data
        /// </summary>
        /// <param name="ar"></param>
        private void UdpDataReceived(IAsyncResult ar)
        {
            if (_udpCallbackRun)
            {
                UdpClient c = (UdpClient)((UdpState)ar.AsyncState).c;
                IPEndPoint wantedIpEndPoint = (IPEndPoint)((UdpState)(ar.AsyncState)).e;
                IPEndPoint receivedIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                Byte[] receiveBytes = c.EndReceive(ar, ref receivedIpEndPoint);
                // Check sender
                bool isRightHost = (wantedIpEndPoint.Address.Equals(receivedIpEndPoint.Address)) || wantedIpEndPoint.Address.Equals(IPAddress.Any);
                bool isRightPort = (wantedIpEndPoint.Port == receivedIpEndPoint.Port) || wantedIpEndPoint.Port == 0;
                if (isRightHost && isRightPort)
                {
                    //string receivedText = "";
                    int lData = receiveBytes.Length;
                    lock (_ethernetReciveQueue)
                    {
                        for (int i = 0; i < lData; i++)
                        {
                            if (lData > 1)
                            {

                                _ethernetReciveQueue.Enqueue(receiveBytes[i]);

                                //if (receiveBytes[i] == 0x10)
                                //{
                                //    if ((i + 1) < lData)
                                //    {
                                //        if (receiveBytes[i + 1] == 0x02)
                                //        {
                                //            receivedText += "\n";
                                //        }
                                //    }
                                //    else
                                //    {
                                //    }
                                //}
                            }
                            //receivedText += string.Format("{0:X2}", receiveBytes[i]);
                        }
                        //Debug.Write(receivedText);
                    }
                    tidFormFrameWaitHandle.Set();
                }
                c.BeginReceive(new AsyncCallback(UdpDataReceived), ar.AsyncState); // Restart listening for udp data packages
            }
            else
            {
                _udPclient.Close();
            }
        }

        /// <summary>
        /// takes serial bytes from _ethernetReciveQueue and makes them into frames.
        /// </summary>
        private void makeFrameThread()
        {
            while (!_shouldStopMain)
            {
                // System.Threading.Thread.Sleep(1);
                // int com_byte = comSetup1.get_comms();  //for comport connection
                tidFormFrameWaitHandle.WaitOne(); // blocks thread untill signall is recived 
                int queueCount = _ethernetReciveQueue.Count;
                bool Do_Work = true;
                if (_tcpClient.Connected)  // if ethernet is Rx is running
                {
                    while (queueCount != 0)
                    {
                        byte com_byte;
                        lock (_ethernetReciveQueue)
                        {
                            com_byte = _ethernetReciveQueue.Dequeue();
                        }
                        Do_Work = processRawData(Do_Work, com_byte);
                        queueCount--;
                    }

                }
            }
        }

        /// <summary>
        /// takes frames from queue and puts into queues for display and loging
        /// </summary>
        private void extractDataThread()
        {
            while (!_shouldStopTidExtractData)
            {
                tidExtractDataWaitHandle.WaitOne();
                lock (allLists.workingTagQueue0)
                {
                    if (allLists.workingTagQueue0.Count > 0)
                    {
                        Tag temp = allLists.workingTagQueue0.Dequeue();
                        if (temp != null)
                        {
                            ExtractData(temp);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// thread to listen to TCP connection
        /// </summary>
        public void TCPListenThread()
        {
            while (!_shouldStopListen)//(ethernetPort.bActive)
            {
                if (this._tcpClient.Connected)
                {
                    //try - so we can see if the connection has been dropped or is half open
                    //in the send thread if we occasionaly send empty packets then this will check the stream state.
                    //if it's gone down we will catch it here in the listening thread. From tests takes about 20 seconds.

                    int myByte = -1;
                    try
                    {
                      //  this.myStream.ReadTimeout = 1000;
                        myByte = this._tcpStream.ReadByte();
                    }
                    catch (Exception e)
                    {
                        // MessageBox.Show("connection problem");
                        handleTCPconnectionLoss();

                        _errorLog.write("TCP connection on listen thread");
                        //_errorLog.write(e, "EthernetConnection TCPconnectionLoss");
                    }
                    lock (_ethernetReciveQueue)
                    {
                       // ethernetReciveQueue.Enqueue(myByte);  // *********************  removed for DEBUG ****************************************************************
                    }
                    //if (ethernetReciveQueue.Count != 0)
                    //{
                    //    mainThreadWaitHandle.Set();
                    //}

                }
                else
                {
                    //*** need back in****comSetup1.label8.Content = string.Format("connection fail");
                    // comSetup1.button2.IsEnabled = true;
                    MessageBox.Show("error connection lost");
                }
            }
        }

        /// <summary>
        /// Kicks send tread to run with no data
        /// </summary>
        public void TCPSend()
        {
            _ethernetSendWaitHandle.Set();
        }
        /// <summary>
        /// Queues items up on TCP send and kicks send tread to run
        /// </summary>
        /// <param name="items"></param>
        public void TCPSend(int[] items)
        {
            lock (_ethernetTransmitQueue)
            {
                foreach (int item in items)
                {
                    _ethernetTransmitQueue.Enqueue(item);
                }
            }
            _ethernetSendWaitHandle.Set();
        }

        /// <summary>
        /// Thread to send TCP messages
        /// </summary>
        private void TCPSendThread()
        {
            while (!_shouldStopSend)
            {
                while (this._tcpClient.Connected)   //(ethernetPort.bActive)
                {
                    _ethernetSendWaitHandle.WaitOne(); // blocks thread untill signall is recived 
                    
                    int queueTempLength = _ethernetTransmitQueue.Count;
                    while (_ethernetTransmitQueue.Count != 0)
                    {
                        lock (_ethernetTransmitQueue)
                        {
                            this._TCPtxBuffer[this._txBufferPosition] = (byte)(_ethernetTransmitQueue.Dequeue());
                        }
                        if (this._TCPtxBuffer[this._txBufferPosition] == 0)
                        {
                        }
                        this._txBufferPosition++;

                        if (this._TCPtxBuffer[(this._txBufferPosition - 1)] == 0) // this was when I was look for a null before sending
                       // if (this.txBufferPosition == queueTempLength) // now I am using the lengh of the queue, this could be a problem if there is 2 messages in the queue
                        {
                            try
                            {
                                this._tcpStream.Write(this._TCPtxBuffer, 0, this._TCPtxBuffer[2] + 3);       
                            }
                            catch (SocketException e)
                            {
                                _errorLog.write(e, "EthernetConnection tcpStream.Write");
                            }
                            int txSequ = this._TCPtxBuffer[5];

                            //if (ackBack(txSequ) == false)
                            //{
                            //    MessageBox.Show("Note:no ack back Retry sent");
                            //    try
                            //    {
                            //        this.myStream.Write(this.txBuffer, 0, this.txBuffer[2] + 3);

                            //    }
                            //    catch (SocketException e)
                            //    {

                            //    }
                            //}
                            this._txBufferPosition = 0;
                            // clear down txbuffer
                            int length = this._TCPtxBuffer[3] + 3;
                            for (int j = 0; j <= length; j++)
                            {
                                this._TCPtxBuffer[j] = 0;
                            }
                        }
                    }
                    //code to check if connection is still up
                    try
                    {
                        byte[] keepAlive = new byte[1] { 0x0 }; 
                        this._tcpStream.Write(keepAlive, 0, 1); //length of one and value of 0x0 sent to check that connection is still up if not = exeption.NOTE A length of 0 dose not rase the exeption!  
                    }
                    catch (SocketException e)
                    {
                        if (e.NativeErrorCode.Equals(10035))
                            _errorLog.write("Ethernet keepAlive write, Still Connected, but the Send would block");
                        else
                        {
                            //_errorLog.write(e, "Ethernet keepAlive write, Disconnected");
                            _errorLog.write("TCP connection on send thread : SocketException ");
                        }
                    }
                    catch
                    {
                        handleTCPconnectionLoss();
                        _errorLog.write("TCP connection on send thread : all exeptions");
                    }
                }
            }

        }



        private bool processRawData(bool Do_Work, byte com_byte)
        {
            if (!_InFrame)
            {
                if (DleStxCheck(com_byte))
                {
                    _PortArrayCount = 1;
                    _InFrame = true;
                }
            }
            if (_InFrame) //Put bytes into array
            {
                bool frameEnd = fillPortArray(com_byte);
                if (frameEnd)                                             
                {
                    uint stripCount = DleStrip();
                    uint ckSumCalc = calcAdler32(3, (uint)_PktLengthInt - checkSumLength - dleStxLength - stripCount);  //Minus checksum from length 
                    lock (_PortArray)
                    {
                        _WorkingTag.UpdateTag(_PortArray, ckSumCalc);
                    }
                    if (_WorkingTag.CheckSum == _WorkingTag.calculatedCheckSum)
                    {
                        allLists.workingTagQueue0.Enqueue(_WorkingTag);
                        tidExtractDataWaitHandle.Set();

                        if (_WorkingTag.BrCmd > 0)
                        {

                            //    messageWindow1.rxMessageWaitHandle.Set();

                            //    lock (messageWindow1.rxMessageQueue)
                            //    {
                            //        messageWindow1.rxMessageQueue.Enqueue(WorkingTag);
                            //        Tag debug = messageWindow1.rxMessageQueue.Peek();
                            //        if (debug.BrCmd == 0)
                            //        {
                            //        }

                            //    }

                            //    //need to create an event
                            //    //messageWindow1.textRxMessage.Text = "hello";

                        }

                        if (_WorkingTag.PktType != "0")
                        {
                        }

                        if (_WorkingTag.ReaderAdd == "0000000000000000") // router has joined the network
                        {

                            if (_WorkingTag.PktEvent == 0x8000) // Left Network
                            {
                                _WorkingTag.BrCmd = 0xEF;     //these BrCmds I have made up just to send messages in this app
                            }
                            if (_WorkingTag.PktEvent == 0x4000) // Joined Network
                            {
                                _WorkingTag.BrCmd = 0xf0;    //these BrCmds I have made up just to send messages in this app
                            }
                            lock (allLists.rxMessageQueue)
                            {
                                allLists.rxMessageQueue.Enqueue(_WorkingTag);

                            }
                            _messageWindow1.rxMessageWaitHandle.Set();
                        }
                    }
                    else
                    {
                        _checkSumFails++;
                        Debug.WriteLine("checksum fail count = {0}", _checkSumFails);
                    }

                    _InFrame = false;
                    _PortArrayCount = 0;

                    for (int i = 0; i < _PortArrayMax; i++)
                    {
                        _PortArray[i] = 0;                          //Clear Down Array
                    }
                } //if frame end

                if (_PortArrayCount < _PortArrayMax - 1)
                {
                    _PortArrayCount++;
                }
                else
                { //PortArrayCount Error
                    _InFrame = false;
                    _PortArrayCount = 0;
                }

            }

            return Do_Work;
        }
        /// <summary>
        /// puts bytes into byte array true when _PortArrayCount equals _PktLengthInt 
        /// </summary>
        /// <param name="com_byte"></param>
        /// <returns>true when _PortArrayCount equals _PktLengthInt </returns>
        private bool fillPortArray(byte com_byte)
        {
            _PortArray[_PortArrayCount] = com_byte;
            if (_PortArrayCount == 2)                                                         //Pull out packet length once we have it
            {
                _PktLengthInt = _PortArray[2] + 2;   //Add 2 because my length includes DLE and STX bytes but DLE byte is always 0 not x10
            }
            if (_PktLengthInt > _PortArrayMax - 1)   //ERROR THE LENGTH IS WAY TO BIG!!!
            {
                _InFrame = false;
                _PortArrayCount = 0;
                _PktLengthInt = 0;
                for (int i = 0; i < _PortArrayMax; i++)
                {
                    _PortArray[i] = 1;                          //Clear Down Array
                }
            }

            if((_PortArrayCount == _PktLengthInt) & (_PortArrayCount > 3))
            {return true;}
            else
            {return false;}
        }

        /// <summary>
        /// Checks for start of framw DLE STX
        /// </summary>
        /// <param name="com_byte"></param>
        /// <returns></returns>
        private bool DleStxCheck(byte com_byte)
        {
            if (com_byte == DLE)                      //Possible SoF
            {
                _DLEflag = 1;
                return false;
            }

            if (_DLEflag == 1 & com_byte == STX)          //SoF Indication
            {
                _DLEflag = 0;
                return true;
            }
            return false;
        }
        /// <summary>
        /// puts tag data into lists for diaplay
        /// </summary>
        /// <param name="tag"></param>
        private void ExtractData(Tag tag)
        {
            string[] endPoint = _MNform.addMacToEndpointList(tag.TagAdd);
            tag.Name = endPoint[0];
            tag.endPointType = endPoint[1];
            if (tag.BrSequ != 0)
            {
                allLists.brSequReciveQueue.Enqueue(tag.BrSequ);
            }
            //List<tag> myTagList = new List<tag>();
            // historyDataBaseAddNew();           //add new data to DB. Adds everything
            // string TagReaderAddTemp = tag.TagAdd + tag.ReaderAdd; // TagAddTemp + ReaderAddTemp;
            // need to make list of readers and the tags that they have
            if (_DBsetup.trackingEnabled)
            {
                //_dbConnect.trackingDBaseUpDate(tag);          //Adds new tag to DB OR updates tags
            }
          //  allLists.upDateMyReaderList(tag);  //Update List of readers and there tags :FOR TREEVIEW
           // int tagIndex = allLists.allTagList.ToList().FindIndex(item => (item.TagAdd + item.ReaderAdd) == (tag.TagAdd + tag.ReaderAdd)); // Search for uneque tagAdd+routerAdd 
            int tagIndex = -1;
            int i = 0;                                                                                                                   //This give duplicate entrys occasionaly so switching to fer each
            var s1 = Stopwatch.StartNew();
            foreach (TagBind tb in allLists.allTagList)
            {
                if (tb.TagAdd == tag.TagAdd)
                {
                    if (tb.ReaderAdd == tag.ReaderAdd)
                    {
                        tagIndex = i;
                        break;
                        //_errorLog.write(string.Format("Duplicate entry  List.tag {0}, list.reader {1}, new.tag {2}, new.reader {3}", tb.TagAdd, tb.ReaderAdd, tag.TagAdd, tag.ReaderAdd));
                    }
                }
                i++;
            }
            s1.Stop();
            Debug.WriteLine(((double)(s1.Elapsed.TotalMilliseconds)).ToString("0.0000 ms"));

            if (tagIndex == -1) // not in list
            {
                int listSize = allLists.allTagList.Count();
                upDateAllTagList(tag); // this in turn updates the gridView
                if (checkListHasGrown(listSize))
                {
                    int indexTR = allLists.allTagList.ToList().FindIndex(item => (item.TagAdd + item.ReaderAdd) == (tag.TagAdd + tag.ReaderAdd));
                    if (indexTR != -1)
                    {
                        allLists.updateBingingList(indexTR, tag);
                    }
                    else
                    {
                        _errorLog.write("not in binding list");
                    }
                }
                else { _errorLog.write("not added to list: Break!"); }
            }
            else
            {
                allLists.updateBingingList(tagIndex, tag);
            }
        }

        
        private bool checkListHasGrown(int listSize)
        {
            int escape = 0;
            while (listSize == allLists.allTagList.Count()) // wait for it to be added to the list. Have to wait because it's on WPF another thread
            {
                Thread.Sleep(10);
                escape++;
                if (escape == 10)
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// Invoke update to allTagList
        /// </summary>
        /// <param name="tag"></param>
        public void upDateAllTagList(Tag tag)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(
            System.Windows.Threading.DispatcherPriority.Normal,
            (Action)delegate()
            {
                // Your Action Code
                allLists.allTagList.Add(new TagBind(ref tag));  // PortArray));
            });
        }
        /// <summary>
        /// Tracking databse setup
        /// </summary>
        private void trackingDataBaseSetup()
        {
            if (Properties.Settings.Default.EnableTracking)
            {
                //_dbConnect = new DBConnect(Properties.Settings.Default.Server,
                //                            Properties.Settings.Default.Port,
                //                            Properties.Settings.Default.Database,
                //                            Properties.Settings.Default.UID,
                //                            Properties.Settings.Default.Password); //initialises new db connection
            }

        }

        /// <summary>
        /// Strips double DLE from frame
        /// </summary>
        /// <returns></returns>
        private uint DleStrip()
        {
            uint count = 0;
            for (int i = 1; i <= (_PortArrayCount - 1); i++)
            {
                if (_PortArray[i] == DLE)
                {
                    if (_PortArray[i - 1] == DLE)   //so double DLE
                    {
                        // shift array left form this point
                        for (int j = i; j <= _PortArrayCount - 1; j++)
                        {
                            _PortArray[j] = _PortArray[j + 1];
                            
                        }
                    count++;
                    }
                }
            }
            return count;
        }
        /// <summary>
        /// Calculate Adler Checksum
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private uint calcAdler32(int startPoint, uint length)
        {
            uint ckSumCalc;
            //adler32 check sum
            {
                uint a = 1, b = 0;
                int index;
                const uint MOD_ADLER = 65521;

                /* Process each byte of the data in order */
                for (index = startPoint; index < (startPoint + length); ++index)
                {
                    a = (a + (uint)_PortArray[index]) % MOD_ADLER;
                    b = (b + a) % MOD_ADLER;
                }

                ckSumCalc = (b << 16) | a;   // needs work !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            }
            return ckSumCalc;
        }
        
       /// <summary>
       /// handles TCP conection loss. Stops UDP and TCP
       /// </summary>
        private void handleTCPconnectionLoss()
        {
            try
            {
                allLists.coordinators[_index].connected = false;
            }
            catch
            {

            }
            this._shouldStopListen = true;
            this._shouldStopSend = true;
            _udpCallbackRun = false;
            if (_udPclient != null)
            {
                _udPclient.Close();
            }
            try
            {
                //System.Media.SoundPlayer player = new System.Media.SoundPlayer(Properties.Resources.Warning_Siren);
                //player.Play();
            }
            catch (Exception e)
            {
                _errorLog.write(e.ToString());
            }
        }

        /// <summary>
        /// abourts running threads and callbacks for ethernet connections
        /// </summary>
        public void EthernetAbort()
        {
            try
            {
                _udpCallbackRun = false;
                _udPclient.Close();
            }
            catch { }
            
            try
                {
                    _TCPListenThread.Abort();
                }
                catch{}
                try
                {
                    _TCPSendThread.Abort();
                }
                catch{}
                try
                {
                    _tidExtractData.Abort();
                }
                catch{}
                this._shouldStopMain = true;
                try
                {
                    _tcpClient.Close();
                }
                catch { }
  
        }
               
    }
}
