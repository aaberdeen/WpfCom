using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using ComPort;
using System.Linq;
using SimpleUdpReciever;
using System.Net;
using System.Diagnostics;
using WiPANFactory;
using System.IO;

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
        private DBConnect _dbConnect;
        private MinersNamesForm _MNform;
        private DataBaseSetup _DBsetup;
        /* NetworkStream that will be used */
        private NetworkStream _tcpStream;
        /* TcpClient that will connect for us */
        private TcpClient _tcpClient;
        /* Storage space */
        private byte[] _TCPrxBuffer;
        private byte[] _TCPtxBuffer;
        private int _txBufferPosition;
        private byte _InFrameFlag = 0;
        private byte _DLEflag = 0;
        private int _PortArrayCount = 0;
        private int[] _PortArray = new int[200];
        private int _PortArrayMax = 200;
        const int DLE = 0x10;
        const int STX = 0x2;
        private int _PktLengthInt = 0;
        private Tag _WorkingTag = new Tag();
       // DBConnect dbConnectRef;
        private Lists _allLists;
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
        private string _remoteIP;
        private string _TCPport;
        private string _localIP;
        private string _udpPort;
       

        public EthernetConnection(string remoteIP, string TCPport, string localIP, string udpPort, ref ComSetup com, ref MinersNamesForm MNform, ref DataBaseSetup DBsetup, bool DBcon, ref Lists allLists, int index, ref Message messageWindow1)
        {
            _com = com;
            _MNform = MNform;
            _DBsetup = DBsetup;
           // dbConnectRef = DBcon;
            _allLists = allLists;
            _index = index;
            _messageWindow1 = messageWindow1;
            _localIP = localIP;
            _udpPort = udpPort;
            _ethernetSendWaitHandle = new AutoResetEvent(false);
            _remoteIP = remoteIP;
            _TCPport = TCPport;

            if (DBcon)
            {
                trackingDataBaseSetup();
            }

            try
            {
                if (TCPinit())                        //(tcpClient.Connected)
                {
                    _com.coordIpList[_index].connected = true;
                    // this is in the try because we don't want to start the treads without the TCP connecton
                    ThreadInit();
                    UDPInit();
                }
            }
            catch(Exception e)
            {
                _com.coordIpList[_index].connected = false;
                _errorLog.write(e, "EthernetConnection Init");
            }
        }

        /// <summary>
        /// Initalise TCP connection and then send a message to set up the UDP at the far end
        /// </summary>
        /// <returns></returns>
        private bool TCPinit()
        {
            _tcpClient = new TcpClient(_remoteIP, Int32.Parse(_TCPport));
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
            _formFrameThread = new Thread(new ThreadStart(formFrameThread));
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
            if (IPAddress.TryParse(_remoteIP, out udpAddress))
            {
                remoteSender.Address = udpAddress;
                remoteSender.Port = remotePort;
                _udpCallbackRun = true;
                _udPclient = new UdpClient(Convert.ToInt32(_udpPort));
                UdpState state = new UdpState(_udPclient, remoteSender);
                _udPclient.BeginReceive(new AsyncCallback(UdpDataReceived), state);  // this is blocking 
                _errorLog.write(string.Format("udp setup done {0}:{1}", udpAddress, _udpPort));
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
                string UDPaddress = _localIP;  //"10.1.0.97"; // my pc for now
                byte[] startFrame = WipanCmd.UDPstart(UDPSess, UDPaddress, _udpPort);
                byte[] stopframe = WipanCmd.UDPstop(UDPSess, UDPaddress, _udpPort);

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
        /// takes bytes from _ethernetReciveQueue and makes them into frames
        /// </summary>
        private void formFrameThread()
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

        private void extractDataThread()
        {
            while (!_shouldStopTidExtractData)
            {
                tidExtractDataWaitHandle.WaitOne();
                lock (_allLists.workingTagQueue0)
                {
                    if (_allLists.workingTagQueue0.Count > 0)
                    {
                        Tag temp = _allLists.workingTagQueue0.Dequeue();
                        if (temp != null)
                        {
                            ExtractData(temp);
                        }
                    }
                }
            }
        }

       

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
                        _errorLog.write(e, "EthernetConnection TCPconnectionLoss");
                    }

                    lock (_ethernetReciveQueue)
                    {
                       // ethernetReciveQueue.Enqueue(myByte);  // *********************  removed for DEBUG ****************************************************************
                    }


                    //if (ethernetReciveQueue.Count != 0)
                    //{
                    //    mainThreadWaitHandle.Set();
                    //}


                    ////DEBUG**************************************************
                    //string myString = string.Format("{0:X2}", myByte);
                    //using (StreamWriter w = File.AppendText("c:\\log.txt"))
                    //{
                    //    Log(myString, w);
                    //}
                    ////****************************************************

                }
                else
                {
                    //*** need back in****comSetup1.label8.Content = string.Format("connection fail");
                    // comSetup1.button2.IsEnabled = true;
                    MessageBox.Show("error connection lost");
                }


            }
        }

        public void TCPSend()
        {
            _ethernetSendWaitHandle.Set();
        }

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
                        
                        // ethernetPort.myStream.WriteByte( (byte)(ethernetTransmitQueue.Dequeue()) );

                        //Works with breakpoint here probably becaus it gives it time for the queue to fill up
                        lock (_ethernetTransmitQueue)
                        {
                            this._TCPtxBuffer[this._txBufferPosition] = (byte)(_ethernetTransmitQueue.Dequeue());
                        }

                        //for (int delay = 0; delay < 99999; delay++) //this delay is a frig
                        //{

                        //}

                        if (this._TCPtxBuffer[this._txBufferPosition] == 0)
                        {
                        }

                        this._txBufferPosition++;

                        if (this._TCPtxBuffer[(this._txBufferPosition - 1)] == 0) // this was when I was looking for a null before sending
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
                        
                        // ethernetPort.myStream.Write(ethernetPort.txBuffer, 0, ethernetPort.txBuffer[3] + 3);
                    }
                    catch (SocketException e)
                    {
                        if (e.NativeErrorCode.Equals(10035))
                           // MessageBox.Show(string.Format("Still Connected, but the Send would block"));
                            _errorLog.write(e, "Ethernet keepAlive write, Still Connected, but the Send would block");
                        else
                        {
                           // MessageBox.Show(string.Format("Disconnected: error code {0}!", e.NativeErrorCode));
                            _errorLog.write(e, "Ethernet keepAlive write, Disconnected");

                        }

                    }
                    catch
                    {
                        //MessageBox.Show("dead");
                        handleTCPconnectionLoss();
                    }





                }
            }

        }



        private bool processRawData(bool Do_Work, byte com_byte)
        {
            if (_InFrameFlag == 0)
            {
                if (DleStxCheck(com_byte))
                {
                    _PortArrayCount = 1;
                    _InFrameFlag = 1;
                }
            }
            if (_InFrameFlag == 1) //Put bytes into array
            {
                fillPortArray(com_byte);

                if ((_PortArrayCount == _PktLengthInt) & (_PortArrayCount > 3))                                             //Pull stuff out of array
                {
                    uint stripCount = DleStrip();
                    const int checkSumLength = 4;
                    const int dleStxLength = 2;
                    uint ckSumCalc = calcAdler32(3, (uint)_PktLengthInt - checkSumLength - dleStxLength - stripCount);  //Minus checksum from length 
                    lock (_PortArray)
                    {
                        _WorkingTag.UpdateTag(_PortArray, ckSumCalc);
                    }
                    if (_WorkingTag.CheckSum == _WorkingTag.calculatedCheckSum)
                    {
                        _allLists.workingTagQueue0.Enqueue(_WorkingTag);
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
                            lock (_allLists.rxMessageQueue)
                            {
                                _allLists.rxMessageQueue.Enqueue(_WorkingTag);

                            }
                            _messageWindow1.rxMessageWaitHandle.Set();
                        }
                    }
                    else
                    {
                        _checkSumFails++;
                        Debug.WriteLine("checksum fail count = {0}", _checkSumFails);
                    }

                    _InFrameFlag = 0;
                    _PortArrayCount = 0;

                    for (int i = 0; i < _PortArrayMax; i++)
                    {
                        _PortArray[i] = 0;                          //Clear Down Array
                    }
                }

                if (_PortArrayCount < _PortArrayMax - 1)
                {
                    _PortArrayCount++;
                }
                else
                { //PortArrayCount Error
                    _InFrameFlag = 0;
                    _PortArrayCount = 0;
                }

            }

            return Do_Work;
        }

        private void fillPortArray(byte com_byte)
        {
            _PortArray[_PortArrayCount] = com_byte;
            if (_PortArrayCount == 2)                                                         //Pull out packet length once we have it
            {
                _PktLengthInt = _PortArray[2] + 2;   //Add 2 because my length includes DLE and STX bytes but DLE byte is always 0 not x10
            }
            if (_PktLengthInt > _PortArrayMax - 1)   //ERROR THE LENGTH IS WAY TO BIG!!!
            {
                _InFrameFlag = 0;
                _PortArrayCount = 0;
                _PktLengthInt = 0;
                for (int i = 0; i < _PortArrayMax; i++)
                {
                    _PortArray[i] = 1;                          //Clear Down Array
                }
            }
        }

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
        private void ExtractData(Tag tag)
        {



            //First extract all the data into tag Object


            string[] returnString = _MNform.addMacToMinersNames(tag.TagAdd);

            tag.Name = returnString[0];
            tag.endPointType = returnString[1];

            if (tag.BrSequ != 0)
            {
                _allLists.brSequReciveQueue.Enqueue(tag.BrSequ);
            }

            //List<tag> myTagList = new List<tag>();

            // historyDataBaseAddNew();           //add new data to DB. Adds everything

            // string TagReaderAddTemp = tag.TagAdd + tag.ReaderAdd; // TagAddTemp + ReaderAddTemp;
            // need to make list of readers and the tags that they have

            //if (tag.calculatedCheckSum == tag.CheckSum)
            //{

                if (_DBsetup.trackingEnabled)
                {

                    _dbConnect.trackingDBaseUpDate(tag);          //Adds new tag to DB OR updates tags


                }

                // Update List of readers and there tags
                _allLists.upDateMyReaderList(tag);  // used for treeview
               
                // Create a separate list of all the tagAdd+readerAdd in system
                // Search for uneque tagAdd+routerAdd 
                
               // TagReader searchResultTR = allListsRef.myTagReaderList.Find(TRtest => TRtest.TagReaderAdd == tag.TagAdd + tag.ReaderAdd);

                
///NOTE TO SELF this is the way to search the binding list !!!   
///Need to change code to use as it will simplify add and remove
///
                //IEnumerable<TagBind> found = allListsRef.allTagList.TakeWhile(item => (item.TagAdd + item.ReaderAdd) == (tag.TagAdd + tag.ReaderAdd));
                //TagBind test = null;
                //foreach (TagBind check in found)
                //{
                //     test = check;
                    
                //}

                int findIndex = _allLists.allTagList.ToList().FindIndex(item => (item.TagAdd + item.ReaderAdd) == (tag.TagAdd + tag.ReaderAdd));
///**********************************************************

                //if (searchResultTR == null)
                if(findIndex == -1)
                {//add to tagReaderList and add to allTagList 

                  //  TagReader NewTagReader = new TagReader { TagReaderAdd = tag.TagAdd + tag.ReaderAdd };
                  //  allListsRef.myTagReaderList.Add(NewTagReader);
                    upDateDataGridView(tag); // adds to allTagList
                 //   int indexTR = allListsRef.myTagReaderList.IndexOf(NewTagReader); // get an index to update from myTagReaderList
                    int indexTR = _allLists.allTagList.ToList().FindIndex(item => (item.TagAdd + item.ReaderAdd) == (tag.TagAdd + tag.ReaderAdd));

                    if (indexTR != -1)
                    {
                        updateBingingList(indexTR, tag);
                    }
                    else
                    {
                        
                    }
                }
                else
                {// update values in allTagList
                   // int indexTR = allListsRef.myTagReaderList.IndexOf(searchResultTR); // get an index to update from myTagReaderList

                    // dataGridView1.Invoke(new EventHandler(delegate  //need to invoke datagridview1 because allTagList is linked to it
                    // {

                   
                        updateBingingList(findIndex, tag);
                
                    

                    //  }));

                }
            //}
            //else
            //{
            //    checkSumFails++;
            //    Debug.WriteLine("checksum fail count = {0}", checkSumFails);
                
            //}

        }

        private void updateBingingList(int indexTR, Tag tag)
        {
            _allLists.allTagList[indexTR].PktLength = tag.PktLength;
            _allLists.allTagList[indexTR].PktSequence = tag.PktSequence;
            _allLists.allTagList[indexTR].PktType = tag.PktType;
            _allLists.allTagList[indexTR].PktEvent = tag.PktEvent;
            _allLists.allTagList[indexTR].PktTemp = tag.PktTemp;
            _allLists.allTagList[indexTR].iVolt = tag.Volt;
            _allLists.allTagList[indexTR].PktLqi = tag.PktLqi;
            _allLists.allTagList[indexTR].BrSequ = tag.BrSequ;
            _allLists.allTagList[indexTR].BrCmd = tag.BrCmd;
            _allLists.allTagList[indexTR].TOFping = tag.TOFping;
            _allLists.allTagList[indexTR].TOFtimeout = tag.TOFtimeout;
            _allLists.allTagList[indexTR].TOFrefuse = tag.TOFrefuse;
            _allLists.allTagList[indexTR].TOFsuccess = tag.TOFsuccess;
            _allLists.allTagList[indexTR].TOFdistance = tag.TOFdistance;
            _allLists.allTagList[indexTR].RSSIdistance = tag.RSSIdistance;
            _allLists.allTagList[indexTR].TOFerror = tag.TOFerror;
            _allLists.allTagList[indexTR].TOFmac = tag.TOFmac;
            _allLists.allTagList[indexTR].ReaderAdd = tag.ReaderAdd;
            _allLists.allTagList[indexTR].RxLQI = tag.RxLQI;
            _allLists.allTagList[indexTR].TagAdd = tag.TagAdd;
            _allLists.allTagList[indexTR].CH4gas = tag.CH4gas;
            _allLists.allTagList[indexTR].COgas = tag.COgas;
            _allLists.allTagList[indexTR].O2gas = tag.O2gas;
            _allLists.allTagList[indexTR].CO2gas = tag.CO2gas;
            _allLists.allTagList[indexTR].minersName = tag.Name;
            _allLists.allTagList[indexTR].endPointType = tag.endPointType;
            _allLists.allTagList[indexTR].TTL = 10;

            //for pullkey
            _allLists.allTagList[indexTR].u54 = tag.u54;
            _allLists.allTagList[indexTR].u55 = tag.u55;
            _allLists.allTagList[indexTR].u56 = tag.u56;
            _allLists.allTagList[indexTR].u57 = tag.u57;
            _allLists.allTagList[indexTR].u58 = tag.u58;
            _allLists.allTagList[indexTR].u59 = tag.u59;
            _allLists.allTagList[indexTR].u60 = tag.u60;
            _allLists.allTagList[indexTR].u61 = tag.u61;
        }

        public void upDateDataGridView(Tag tag)
        {

            //if (this.dataGridView1.InvokeRequired)
            //{
            //    this.BeginInvoke(new MethodInvoker(() => upDateDataGridView()));
            //}
            //else
            //{
            System.Windows.Application.Current.Dispatcher.Invoke(
            System.Windows.Threading.DispatcherPriority.Normal,
            (Action)delegate()
            {
                // Your Action Code
                _allLists.allTagList.Add(new TagBind(ref tag));  // PortArray));
            });




            //}

        }
        private void trackingDataBaseSetup()
        {
            if (Properties.Settings.Default.EnableTracking)
            {
                _dbConnect = new DBConnect(Properties.Settings.Default.Server,
                                            Properties.Settings.Default.Port,
                                            Properties.Settings.Default.Database,
                                            Properties.Settings.Default.UID,
                                            Properties.Settings.Default.Password); //initialises new db connection
            }

        }

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
        private void handleTCPconnectionLoss()
        {
           // comSetup1.button2.IsEnabled = true;
            //comSetup1.coordIpList[0].connected = "false";

            try
            {
                _com.coordIpList[_index].connected = false;
            }
            catch
            {

            }
            this._shouldStopListen = true;
            this._shouldStopSend = true;
            _udpCallbackRun = false;
            _udPclient.Close();
            
           // this.tidListen.Abort();
           // this.tidSend.Abort();


        }

        public void TCPAbort()
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
        
        public void RequestListenStop()
        {
            this._shouldStopListen = true;
            this._shouldStopTidExtractData = true;
        }
        public void RequestSendStop()
        {
            this._shouldStopSend = true;
        }
        public void RequestSendMain()
        {
            this._shouldStopMain = true;
        }



       
    }
}
