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
        public Thread _TCPSendThread;
        public Thread _TCPListenThread;
        public Queue<int> ethernetReciveQueue = new Queue<int>();
        public Queue<int> ethernetTransmitQueue = new Queue<int>();
       // public Queue<int> brSequReciveQueue = new Queue<int>();
        public AutoResetEvent ethernetSendWaitHandle;
        private volatile bool _shouldStopListen;
        private volatile bool _shouldStopSend;
        private volatile bool _shouldStopMain;
        private ComSetup _com;
        DBConnect dbConnect;
        private MinersNamesForm _MNform;
        private DataBaseSetup _DBsetup;
        /* NetworkStream that will be used */
        public NetworkStream tcpStream;
        /* TcpClient that will connect for us */
        public TcpClient tcpClient;
        /* Storage space */
        public byte[] TCPrxBuffer;
        public byte[] TCPtxBuffer;
        public int txBufferPosition;
        private byte InFrameFlag = 0;
        private byte AAflag = 0;
        private int PortArrayCount = 0;
        private int[] PortArray = new int[200];
        private int PortArrayMax = 200;
        const int DLE = 0x10;
        const int STX = 0x2;
        private int PktLengthInt = 0;
        Tag WorkingTag = new Tag();
       // DBConnect dbConnectRef;
        private Lists _allLists;
        public Thread mainThread;
        private int _index;
        private int checkSumFails;
        private Message _messageWindow1;
        public Thread tidExtractData;
        bool _shouldStopTidExtractData;
        public AutoResetEvent tidExtractDataWaitHandle= new AutoResetEvent(false);
       // public Thread tidExtractData1;
        public AutoResetEvent tidExtractData1WaitHandle= new AutoResetEvent(false);
        public UdpClient udPclient;
        public bool udpCallbackRun;
        private string _localIP;
        private string _udpPort;
       

        public EthernetConnection(string server, string TCPport, string localIP, string udpPort, ref ComSetup com, ref MinersNamesForm MNform, ref DataBaseSetup DBsetup, bool DBcon, ref Lists allLists, int index, ref Message messageWindow1)
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
            



            ethernetSendWaitHandle = new AutoResetEvent(false);

            if (DBcon)
            {
                trackingDataBaseSetup();
            }

            try // TCP setup
            {
                //TCPinit(server, port);


                if (TCPinit(server, TCPport))                        //(tcpClient.Connected)
                {
                    _com.coordIpList[_index].connected = true;
                }


                


                // this is in the try because we don't want to start the treads without the TCP connecton
              ThreadInit();






            }
            catch(Exception e)
            {
                _com.coordIpList[_index].connected = false;
                _errorLog.write(e, "EthernetConnection Init");
            }

                // Create UDP client ******************************************
                int localPort = 4444;
                IPEndPoint remoteSender = new IPEndPoint(IPAddress.Any, 0);
                IPAddress udpAddress;
                int tempInt;
                string value = "10.1.0.44";
                int remotePort = Convert.ToInt32(_udpPort);  // 0 listens to all ports rich uses 1000;
                if (IPAddress.TryParse(server, out udpAddress))
                {
                    remoteSender.Address = udpAddress;
                    remoteSender.Port = remotePort;
                }
                else if (int.TryParse(value, out tempInt) && tempInt == 0)
                    remoteSender.Address = IPAddress.Any;
                udpCallbackRun = true;
             
                udPclient = new UdpClient(localPort);
                UdpState state = new UdpState(udPclient, remoteSender);
               
                udPclient.BeginReceive(new AsyncCallback(DataReceived), state);  // this is blocking 
                
                
                _errorLog.write("udp setup done");
           
     
            //UDP ********************************************************


        }

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
            tidExtractData = new Thread(new ThreadStart(extractDataThread));
            tidExtractData.Name = "tidExtractData";
            tidExtractData.IsBackground = true;
            // tidExtractData1 = new Thread(new ThreadStart(extractDataThread1));
            /*main thread*/
            mainThread = new Thread(new ThreadStart(mainThread1));
            mainThread.Name = "mainThread";
            mainThread.IsBackground = true;

            _TCPListenThread.Start();
            _TCPSendThread.Start();
            mainThread.Start();
            tidExtractData.Start();
            //tidExtractData1.Start();
        }

        private bool TCPinit(string server, string port)
        {
            tcpClient = new TcpClient(server, Int32.Parse(port));
            /* Store the NetworkStream */
            tcpStream = tcpClient.GetStream();
            /* Create data buffer */
            TCPrxBuffer = new byte[tcpClient.ReceiveBufferSize];
            TCPtxBuffer = new byte[tcpClient.SendBufferSize];

            if (tcpClient.Connected)
            {
                string UDPSess = "0";
                string UDPaddress = _localIP;  //"10.1.0.97"; // my pc for now

               // string UDPport =  "4444"; 
            

                byte[] startFrame = WipanCmd.UDPstart(UDPSess, UDPaddress, _udpPort);
                byte[] stopframe = WipanCmd.UDPstop(UDPSess, UDPaddress, _udpPort);
                if (tcpClient != null)
                {
                    if (tcpClient.Connected)
                    {
                        try
                        {
                            //stop 3 times to clear all UDP transmits
                            tcpStream.Write(stopframe, 0, stopframe.Length);
                            Thread.Sleep(100);
                            tcpStream.Write(stopframe, 0, stopframe.Length);
                            Thread.Sleep(100);
                            tcpStream.Write(stopframe, 0, stopframe.Length);
                            Thread.Sleep(100);
                            //start udp
                            tcpStream.Write(startFrame, 0, startFrame.Length);
                            Debug.Write(string.Format("\nUDP Start Sent:{0:X2},{1:X2},{2:X2},{3:X2},{4:X2},{5:X2},{6:X2},{7:X2},{8:X2},{9:X2}", startFrame[0], startFrame[1], startFrame[2], startFrame[3], startFrame[4], startFrame[5], startFrame[6], startFrame[7], startFrame[8], startFrame[9]));
                        }
                        catch (Exception ex)
                        {
                            Debug.Write(ex.ToString());
                            _errorLog.write(ex, "ethernetConnect tcpStream.Write");
                        }
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

        private void extractDataThread()
        {
            while (!_shouldStopTidExtractData)
            {
                tidExtractDataWaitHandle.WaitOne();
                lock (_allLists.workingTagQueue0)
                {
                    if (_allLists.workingTagQueue0.Count > 0)
                    {
                        //for (int i = 0; i < allListsRef.workingTagQueue.Count; i++)
                        //{
                        //    ThreadPool.QueueUserWorkItem(ThreadPoolCallback, i);
                        //}
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

                if (this.tcpClient.Connected)
                {
                    //try - so we can see if the connection has been dropped or is half open
                    //in the send thread if we occasionaly send empty packets then this will check the stream state.
                    //if it's gone down we will catch it here in the listening thread. From tests takes about 20 seconds.

                    int myByte = -1;
                    try
                    {
                      //  this.myStream.ReadTimeout = 1000;
                        myByte = this.tcpStream.ReadByte();
                    }
                    catch (Exception e)
                    {
                        // MessageBox.Show("connection problem");
                        handleTCPconnectionLoss();
                        _errorLog.write(e, "EthernetConnection TCPconnectionLoss");
                    }

                    lock (ethernetReciveQueue)
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
        private void TCPSendThread()
        {

            while (!_shouldStopSend)
            {
                while (this.tcpClient.Connected)   //(ethernetPort.bActive)
                {
                    ethernetSendWaitHandle.WaitOne(); // blocks thread untill signall is recived 
                    
                    int queueTempLength = ethernetTransmitQueue.Count;
                    while (ethernetTransmitQueue.Count != 0)
                    {
                        
                        // ethernetPort.myStream.WriteByte( (byte)(ethernetTransmitQueue.Dequeue()) );

                        //Works with breakpoint here probably becaus it gives it time for the queue to fill up
                        lock (ethernetTransmitQueue)
                        {
                            this.TCPtxBuffer[this.txBufferPosition] = (byte)(ethernetTransmitQueue.Dequeue());
                        }

                        //for (int delay = 0; delay < 99999; delay++) //this delay is a frig
                        //{

                        //}

                        if (this.TCPtxBuffer[this.txBufferPosition] == 0)
                        {
                        }

                        this.txBufferPosition++;

                        if (this.TCPtxBuffer[(this.txBufferPosition - 1)] == 0) // this was when I was looking for a null before sending
                       // if (this.txBufferPosition == queueTempLength) // now I am using the lengh of the queue, this could be a problem if there is 2 messages in the queue
                        {

                            try
                            {
                                this.tcpStream.Write(this.TCPtxBuffer, 0, this.TCPtxBuffer[2] + 3);
                                
                            }
                            catch (SocketException e)
                            {
                                _errorLog.write(e, "EthernetConnection tcpStream.Write");
                            }


                            int txSequ = this.TCPtxBuffer[5];

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
                            this.txBufferPosition = 0;

                            // clear down txbuffer

                            int length = this.TCPtxBuffer[3] + 3;

                            for (int j = 0; j <= length; j++)
                            {
                                this.TCPtxBuffer[j] = 0;
                            }

                        }

                    }

                    //code to check if connection is still up
                    try
                    {
                        byte[] keepAlive = new byte[1] { 0x0 }; 
                        this.tcpStream.Write(keepAlive, 0, 1); //length of one and value of 0x0 sent to check that connection is still up if not = exeption.NOTE A length of 0 dose not rase the exeption! 
                        
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



        private void mainThread1()
        {
            while (!_shouldStopMain)
            {
               // System.Threading.Thread.Sleep(1);
                // int com_byte = comSetup1.get_comms();  //for comport connection

                bool Do_Work = true;

            
                    if (tcpClient.Connected)  // if ethernet is Rx is running
                    {
                        // mainThreadWaitHandle.WaitOne();

                        if (ethernetReciveQueue.Count != 0)
                        {
                            int com_byte;

                            lock (ethernetReciveQueue)
                            {
                                com_byte = ethernetReciveQueue.Dequeue();
                            }



                            //richTextBox1.Invoke(new EventHandler(delegate
                            //        {

                            //            richTextBox1.Text += string.Format("{0:x}", com_byte.ToString("x2"));
                            //            List<Reader> searchResultR = myReaderList.FindAll(Rtest => Rtest.ReaderAdd != "");

                            //        }));



                            Do_Work = processRawData(Do_Work, com_byte); 
                        }
                    }

            }

        }
        private bool processRawData(bool Do_Work, int com_byte)
        {
            uint ckSumCalc;
            const int checkSumLength = 4;
            const int dleStxLength = 2;
            int queueToggle = 0;

            if (com_byte == -1)
            {
                // reached end of buffer
                //So we can stop the background worker for now start again on buffer RX
                //backgroundWorker1.CancelAsync();  //not supported
                Do_Work = false;
            }
            if (com_byte == -2)
            {
                Do_Work = false;
            }
            else
            //if (com_byte != -1)
            {
                if (queueToggle == 0)
                { queueToggle = 1; }
                else
                { queueToggle = 0; }


                if (InFrameFlag == 0)
                {
                    if (com_byte == DLE)                      //Possible SoF
                    {
                        AAflag = 1;
                    }

                    if (AAflag == 1 & com_byte == STX)          //SoF Indication
                    {
                        PortArrayCount = 1;
                        AAflag = 0;
                        InFrameFlag = 1;
                    }
                    if (AAflag == 1 & com_byte == DLE)
                    {
                        InFrameFlag = 0;                        //Keep hunting for SoF this is a stuffed byte DLE DLE
                        PortArrayCount = 0;
                    }

                }
                if (InFrameFlag == 1)
                {
                    



                    //Put bytes into array

                    PortArray[PortArrayCount] = com_byte;



                    if (PortArrayCount == 2)                                                         //Pull out packet length once we have it
                    {
                        // PktLengthInt = ((PortArray[2] << 8) + PortArray[1]) + 7;                   
                        PktLengthInt = PortArray[2] + 2;   //Add 2 because my length includes DLE and STX bytes but DLE byte is always 0 not x10
                    }


                    if (PktLengthInt > PortArrayMax - 1) //ERROR THE LENGTH IS WAY TO BIG!!!
                    {
                        InFrameFlag = 0;
                        PortArrayCount = 0;
                        PktLengthInt = 0;

                        for (int i = 0; i < PortArrayMax; i++)
                        {
                            PortArray[i] = 1;                          //Clear Down Array
                        }
                    }


                    if ((PortArrayCount == PktLengthInt) & (PortArrayCount > 3))                                             //Pull stuff out of array
                    {




                        // InFrameFlag = 0;

                        // AAStrip();                                                                  //Remove AA Padding
                        uint stripCount = DleStrip();

                        ckSumCalc = calcAdler32(3, (uint)PktLengthInt - checkSumLength - dleStxLength - stripCount);  //Minus checksum from length 

                        lock (PortArray)
                        {

                            //ExtractData(ref PktSequence, ref TagAdd, ref ReaderAdd, ref TOFmac);                      //Put data into class lists
                            WorkingTag.UpdateTag(PortArray, ckSumCalc);
                        }
                            

                        // so now we are goint to put this in a tag queue so we can have a tread pool to handle lots of data
                        //now lets toggle between two queues so we can use two threads to service

                        //if (queueToggle == 0)
                        //{
                            _allLists.workingTagQueue0.Enqueue(WorkingTag);
                            tidExtractDataWaitHandle.Set();
                        //}
                        //if (queueToggle == 1)
                        //{
                        //    allListsRef.workingTagQueue1.Enqueue(WorkingTag);
                        //    tidExtractData1WaitHandle.Set();
                        //}
                        

                    //    ExtractData(WorkingTag);
                        // ckSum = WorkingTag.CheckSum;

                        //Recive message need to add back in
                        if (WorkingTag.BrCmd > 0)
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

                        if (WorkingTag.PktType != "0")
                        {
                        }

                        if (WorkingTag.ReaderAdd == "0000000000000000") // router has joined the network
                        {

                            if (WorkingTag.PktEvent == 0x8000) // Left Network
                            {
                                WorkingTag.BrCmd = 0xEF;     //these BrCmds I have made up just to send messages in this app
                            }
                            if (WorkingTag.PktEvent == 0x4000) // Joined Network
                            {
                                WorkingTag.BrCmd = 0xf0;    //these BrCmds I have made up just to send messages in this app
                            }


                            lock (_allLists.rxMessageQueue)
                            {
                                _allLists.rxMessageQueue.Enqueue(WorkingTag);
                                
                            }
                            _messageWindow1.rxMessageWaitHandle.Set();
                        }



                        

                        InFrameFlag = 0;
                        PortArrayCount = 0;

                        for (int i = 0; i < PortArrayMax; i++)
                        {
                            PortArray[i] = 0;                          //Clear Down Array
                        }

                        //DEBUG***************************************************

                        //try
                        //{

                        //    using (StreamWriter w = File.AppendText("c:\\log.txt"))
                        //    {
                        //        Log("\n", w);
                        //    }
                        //}
                        //catch
                        //{
                        //}

                        //*******************************************************




                    }

                    if (PortArrayCount < PortArrayMax - 1)
                    {
                        PortArrayCount++;
                    }
                    else
                    { //PortArrayCount Error
                        InFrameFlag = 0;
                        PortArrayCount = 0;
                    }

                }
            }
            return Do_Work;
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

            if (tag.calculatedCheckSum == tag.CheckSum)
            {

                if (_DBsetup.trackingEnabled)
                {

                    dbConnect.trackingDBaseUpDate(tag);          //Adds new tag to DB OR updates tags


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
            }
            else
            {
                checkSumFails++;
                Debug.WriteLine("checksum fail count = {0}", checkSumFails);
                
            }

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
                dbConnect = new DBConnect(Properties.Settings.Default.Server,
                                            Properties.Settings.Default.Port,
                                            Properties.Settings.Default.Database,
                                            Properties.Settings.Default.UID,
                                            Properties.Settings.Default.Password); //initialises new db connection
            }

        }

        private uint DleStrip()
        {
            uint count = 0;


            for (int i = 1; i <= (PortArrayCount - 1); i++)
            {
                if (PortArray[i] == DLE)
                {
                    if (PortArray[i - 1] == DLE)   //so double DLE
                    {
                        // shift array left form this point
                        for (int j = i; j <= PortArrayCount - 1; j++)
                        {
                            PortArray[j] = PortArray[j + 1];
                            
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
                    a = (a + (uint)PortArray[index]) % MOD_ADLER;
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
            udpCallbackRun = false;
            udPclient.Close();
            
           // this.tidListen.Abort();
           // this.tidSend.Abort();


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

        private void DataReceived(IAsyncResult ar)
        {
            if (udpCallbackRun)
            {
                UdpClient c = (UdpClient)((UdpState)ar.AsyncState).c;
                IPEndPoint wantedIpEndPoint = (IPEndPoint)((UdpState)(ar.AsyncState)).e;
                IPEndPoint receivedIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                Byte[] receiveBytes = c.EndReceive(ar, ref receivedIpEndPoint);


                // Check sender
                bool isRightHost = (wantedIpEndPoint.Address.Equals(receivedIpEndPoint.Address)) || wantedIpEndPoint.Address.Equals(IPAddress.Any);
             //   bool isRightPort = (wantedIpEndPoint.Port == receivedIpEndPoint.Port) || wantedIpEndPoint.Port == 0;
                if (isRightHost)// && isRightPort)
                {
                    string receivedText = "";
                    int lData = receiveBytes.Length;
                    for (int i = 0; i < lData; i++)
                    {//int rxByte = Convert.ToInt32(receiveBytes[i]);
                        if (lData > 1)
                        {
                            lock (ethernetReciveQueue)
                            {
                                ethernetReciveQueue.Enqueue(receiveBytes[i]);
                            }
                            if (receiveBytes[i] == 0x10)
                            {
                                if ((i + 1) < lData)
                                {
                                    if (receiveBytes[i + 1] == 0x02)
                                    {
                                        receivedText += "\n";
                                    }
                                }
                                else
                                {
                                }
                            }
                        }
                        receivedText += string.Format("{0:X2}", receiveBytes[i]);

                    }

                    Debug.Write(receivedText);
                }

                // Restart listening for udp data packages
                c.BeginReceive(new AsyncCallback(DataReceived), ar.AsyncState);

            }
            else
            {
                udPclient.Close();
            }
        }

       
    }
}
