using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Collections.Concurrent;
using System.Windows.Data;



namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 


    public partial class MainWindow : Window
    {
       // private Thread _splashThread;      
        private errorLog _errorLog = new errorLog();
       // private static ConcurrentDictionary<string, EthernetConnection> _myEthConnList = new ConcurrentDictionary<string, EthernetConnection>();
        private Thread _connectionThread;
        private volatile bool _shouldStopConnectionThread;
        private AutoResetEvent _ethernetConnectWaitHandle;
        private static ComSetup _comSetup1 = new ComSetup();
        private static Message _messageWindow1;
        private DataBaseSetup _dataBaseSetup1 = new DataBaseSetup();
        private DispatcherTimer _timerTree = new DispatcherTimer();
        private static MinersNamesForm _minersNamesForm = new MinersNamesForm();

        private struct tcpMessageType
        {
            public static int SYSTEM_CONFIG = 1;
            public static int SYSTEM_WATCHDOG = 1;
            public static int SYSTEM_SET_TIME_CFG = 2;
            public static int SYSTEM_SET_DATE_CFG = 3;

            public static int TAG_CONFIG = 2;
            public static int TAG_BROADCAST_PACKET = 1;
            public static int TAG_UNICAST_PACKET = 2;

            public static int RADIO_NETWORK_CONFIG = 3;
        }

        
        /// <summary>
        /// Main
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            _errorLog.write("Welome");
            //_splashThread = new Thread(new ThreadStart(splashThread));
            //_splashThread.SetApartmentState(ApartmentState.STA);
            //_splashThread.Name = "_splashThread";
            //_splashThread.IsBackground = true;
            _ethernetConnectWaitHandle = new AutoResetEvent(false);
            _connectionThread = new Thread(new ThreadStart(connectionThread));
            _connectionThread.Name = "_connectionThread";
            //_splashThread.IsBackground = true;
            //_splashThread.Start();
            //subscribe4(comSetup1);
            _timerTree.Interval = new TimeSpan(0, 0, 10);
            _timerTree.Tick += new EventHandler(timerTree_Tick);
             _messageWindow1 = new Message(ref EthernetConnection.allLists);
            subscribe(_comSetup1);
            //subscribe2(_messageWindow1);
            LoadConfig();
            
            //messageWindow1 = new Message(ref allLists);
            //dataGridView1.AutoGenerateColumns = true;
        }

        /// <summary>
        /// Tread to connect to all ethernet ports.
        /// When the waithandle is kicked it will check all possible connections and connect if connected already
        /// </summary>
        private void connectionThread()
        {        
            while (!_shouldStopConnectionThread)
            {
                _ethernetConnectWaitHandle.WaitOne(); // blocks thread untill signall is recived 
                foreach (Coordinators conn in EthernetConnection.allLists.coordinators)
                {
                    if (!_shouldStopConnectionThread)
                    {
                        if (!conn.connected)
                        {
                            int getIndex = EthernetConnection.allLists.coordinators.IndexOf(conn);
                            //try reconnect
                           // etherConnect(conn.IP, conn.localIP, conn.udpPort, conn.port, getIndex);
                            etherConnect(conn, getIndex);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Tread to show splash screen
        /// </summary>
        public void splashThread()
        {
            new SplashWindow().ShowDialog();
        }
        
        /// <summary>
        /// Timer used to decrement the time to live of tags in tree view
        /// Also used to check TCP connection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void timerTree_Tick(object sender, EventArgs e)
        {              
            decAllTtlOflistToReturn(); // moved to Ethernetconnection class
            updatConsoleText();
            if (_connectionThread.ThreadState == System.Threading.ThreadState.Unstarted)
            {
                _connectionThread.Start();
            }
            _ethernetConnectWaitHandle.Set();
        }
        /// <summary>
        /// Event on click of Port Button on menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Port_Click(object sender, RoutedEventArgs e)
        {
            _comSetup1.Show();
        }
        /// <summary>
        /// Event on click of exit button on menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            ExitApplication();
        }
        /// <summary>
        /// closes all threds then application is exited
        /// </summary>
        private void ExitApplication()
        {
            saveGridViewOrder();
            _shouldStopConnectionThread = true;
            _ethernetConnectWaitHandle.Set();
            _connectionThread.Abort();
            foreach (KeyValuePair<string, EthernetConnection> ethCon in EthernetConnection._myEthConnList)
            {
                ethCon.Value.EthernetAbort();
            }
            _messageWindow1.Do_Work = false;
            _messageWindow1.shouldStopSendMessageThread = true;
            _messageWindow1.sendMessageWaitHandle.Set(); //kick this so the task can end
            _messageWindow1.Close();
            Application.Current.Shutdown();
        }

        ///// <summary>
        ///// Attenpts an ethernet connection
        ///// </summary>
        ///// <param name="server"></param>
        ///// <param name="port"></param>
        //public void etherConnect(string server, string localIP, string udpPort, string port, int index)
        //{
        //    if (Usefull.ValidIP(server))
        //    {
        //        bool trackEn = false;
        //        if (Properties.Settings.Default.EnableTracking)
        //        {
        //            trackEn = true;
        //        }

        //        try
        //        {   //check to see if it is already in the list if it is remove then add

        //            if (EthernetConnection._myEthConnList.ContainsKey(server))
        //            {
        //                if (EthernetConnection._myEthConnList[server]._tcpClient != null)
        //                {
        //                    if (!EthernetConnection._myEthConnList[server]._tcpClient.Connected)
        //                    {
        //                        EthernetConnection value1;
        //                        EthernetConnection._myEthConnList.TryRemove(server, out value1);
                               
        //                        EthernetConnection newConnect = new EthernetConnection(server, port, localIP, udpPort, ref _comSetup1, ref _minersNamesForm, ref _dataBaseSetup1, trackEn, index, ref _messageWindow1);
        //                        EthernetConnection._myEthConnList.TryAdd(server, newConnect);
        //                    }
        //                    else
        //                    {
        //                        _comSetup1.coordIpList[index].connected = true;
        //                    }
        //                }
        //                else //no client so cant be connected
        //                {
        //                    EthernetConnection newConnect = new EthernetConnection(server, port, localIP, udpPort, ref _comSetup1, ref _minersNamesForm, ref _dataBaseSetup1, trackEn, index, ref _messageWindow1);
        //                    EthernetConnection._myEthConnList.TryAdd(server, newConnect);
        //                }
        //            }
        //            else //not in the list
        //            {
        //                EthernetConnection newConnect = new EthernetConnection(server, port, localIP, udpPort, ref _comSetup1, ref _minersNamesForm, ref _dataBaseSetup1, trackEn, index, ref _messageWindow1);
        //                EthernetConnection._myEthConnList.TryAdd(server, newConnect);
        //            }
        //        }
        //        catch (SocketException e)
        //        {
        //            _comSetup1.coordIpList[index].connected = false;
        //            _errorLog.write(e, "MainWindow etherConnect");
        //        }
        //    }
        //    else
        //    {
        //        _comSetup1.label8.Content = string.Format("Not valid IP");
        //    }
        //}

        /// <summary>
        /// Attenpts an ethernet connection
        /// </summary>
        /// <param name="server"></param>
        /// <param name="port"></param>
        public void etherConnect(Coordinators coord, int index)
        {
            if (Usefull.ValidIP(coord.IP))
            {
                bool trackEn = false;
                if (Properties.Settings.Default.EnableTracking)
                {
                    trackEn = true;
                }

                try
                {   //check to see if it is already in the list if it is remove then add

                    if (EthernetConnection._myEthConnList.ContainsKey(coord.IP))
                    {
                        if (EthernetConnection._myEthConnList[coord.IP]._tcpClient != null)
                        {
                            if (!EthernetConnection._myEthConnList[coord.IP]._tcpClient.Connected)
                            {
                                EthernetConnection value1;
                                EthernetConnection._myEthConnList.TryRemove(coord.IP, out value1);

                                EthernetConnection newConnect = new EthernetConnection(coord, ref _comSetup1, ref _minersNamesForm, ref _dataBaseSetup1, trackEn, index, ref _messageWindow1);
                                EthernetConnection._myEthConnList.TryAdd(coord.IP, newConnect);
                            }
                            else
                            {
                                EthernetConnection.allLists.coordinators[index].connected = true;
                            }
                        }
                        else //no client so cant be connected
                        {
                            EthernetConnection newConnect = new EthernetConnection(coord, ref _comSetup1, ref _minersNamesForm, ref _dataBaseSetup1, trackEn, index, ref _messageWindow1);
                            EthernetConnection._myEthConnList.TryAdd(coord.IP, newConnect);
                        }
                    }
                    else //not in the list
                    {
                        EthernetConnection newConnect = new EthernetConnection(coord, ref _comSetup1, ref _minersNamesForm, ref _dataBaseSetup1, trackEn, index, ref _messageWindow1);
                        EthernetConnection._myEthConnList.TryAdd(coord.IP, newConnect);
                    }
                }
                catch (SocketException e)
                {
                    EthernetConnection.allLists.coordinators[index].connected = false;
                    _errorLog.write(e, "MainWindow etherConnect");
                }
            }
            else
            {
                _comSetup1.label8.Content = string.Format("Not valid IP");
            }
        }


        /// <summary>
        /// decrements the TTL of each tag
        /// </summary>
        private void decAllTtlOflistToReturn()
        {
            //for (int i = 0; i <= (EthernetConnection.allLists.allTagList.Count - 1); i++)
            for(int i = (EthernetConnection.allLists.allTagList.Count - 1); i>=0; i--) 
            {
                //if (EthernetConnection.allLists.allTagList[i].Volt <= 2.4)
                //{
                //    _errorLog.write(string.Format("Low Volt= {0}, tag= {1}, type= {2}", EthernetConnection.allLists.allTagList[i].Volt, EthernetConnection.allLists.allTagList[i].TagAdd, EthernetConnection.allLists.allTagList[i].endPointType));
                //}

                if (EthernetConnection.allLists.allTagList[i].TTL <= 7)
                {
                    if (EthernetConnection.allLists.allTagList[i].PktEvent == 16384 || EthernetConnection.allLists.allTagList[i].PktEvent == 32768)
                    {//network message 
                    }
                    else
                    {
                        if (EthernetConnection.allLists.allTagList[i].endPointType == "Key")
                        {
                            if (EthernetConnection.allLists.allTagList[i].ReaderAdd != "0000000000000000")
                            {
                                _errorLog.write(string.Format("ttl= {0}, reader= {1}, type= {2}, tag={3}", EthernetConnection.allLists.allTagList[i].TTL,
                                                                                                           EthernetConnection.allLists.allTagList[i].ReaderAdd,
                                                                                                           EthernetConnection.allLists.allTagList[i].endPointType,
                                                                                                           EthernetConnection.allLists.allTagList[i].TagAdd));
                            }
                        }
                    }
                }
                if (EthernetConnection.allLists.allTagList[i].TTL != 0)
                {
                    EthernetConnection.allLists.allTagList[i].TTL--;
                }
                else
                {
                    EthernetConnection.allLists.allTagList.RemoveAt(i);
                }
            }
       }
        /// <summary>
        /// Start buton click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Start_Click(object sender, RoutedEventArgs e)
        {
            if ((_connectionThread.ThreadState == System.Threading.ThreadState.Unstarted) | (_connectionThread.ThreadState == System.Threading.ThreadState.Stopped))
            {
                try
                {
                    _connectionThread.Start();
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            _ethernetConnectWaitHandle.Set();
            //historyDataBaseSetup();
            _comSetup1.SetComboBoxDefault();
            //comSetup1.openComm();                     //Removed for etherent testing
            //dataGridView1.ItemsSource = allLists.allTagList;
            //dataGridView1.AutoGenerateColumns = true;
            //dataGridView1.DataContext = allLists.allTagList;
            //DataContext = allLists;
            DataContext = EthernetConnection.allLists;                             //static
            MenuStop.IsEnabled = true;
            MenuStart.IsEnabled = false;
            _timerTree.Start();
            setUpGridViewOrder();
        }
        /// <summary>
        /// Loads user config for all Tab Positions and Widths
        /// </summary>
        private void setUpGridViewOrder()
        {
            foreach (var col in dataGridView1.Columns)
            {
                switch (col.Header.ToString())
                {
                    case "lowVolt":
                        col.Visibility = System.Windows.Visibility.Hidden;
                        break;
                    case "TTL":
                        col.DisplayIndex = Properties.Settings.Default.TTLwidth;
                        col.Width = Properties.Settings.Default.TTLindex;
                        break;

                    case "PktLength":
                        col.DisplayIndex = Properties.Settings.Default.PktLengthindex;
                        col.Width = Properties.Settings.Default.PktLengthWidth;
                        break;

                    case "minersName":
                        col.DisplayIndex = Properties.Settings.Default.minersNameindex;
                        col.Width = Properties.Settings.Default.minersNameWidth;
                        break;

                    case "endPointType":
                        col.DisplayIndex = Properties.Settings.Default.endPointTypeIndex;
                        col.Width = Properties.Settings.Default.endPointTypeWidth;
                        break;

                    case "PktSequence":
                        col.DisplayIndex = Properties.Settings.Default.PktSequenceindex;
                        col.Width = Properties.Settings.Default.PktSequenceWidth;
                        break;
                    case "PktType":
                        col.DisplayIndex = Properties.Settings.Default.PktTypeindex;
                        col.Width = Properties.Settings.Default.PktTypeWidth;
                        break;
                    case "ReaderAdd":
                        col.DisplayIndex = Properties.Settings.Default.ReaderAddIndex;
                        col.Width = Properties.Settings.Default.ReaderAddWidth;
                        break;
                    case "TagAdd":
                        col.DisplayIndex = Properties.Settings.Default.tagAddIndex;
                        col.Width = Properties.Settings.Default.tagAddWidth;
                        break;
                    case "PktLqi":
                        col.DisplayIndex = Properties.Settings.Default.PktLqiIndex;
                        col.Width = Properties.Settings.Default.PktLqiWidth;
                        break;
                    case "PktEvent":
                        col.DisplayIndex = Properties.Settings.Default.PktEventindex;
                        col.Width = Properties.Settings.Default.PktEventWidth;
                        break;
                    case "PktTemp":
                        col.DisplayIndex = Properties.Settings.Default.PktTempindex;
                        col.Width = Properties.Settings.Default.PktTempWidth;
                        break;
                    case "Volt":
                        col.DisplayIndex = Properties.Settings.Default.Voltindex;
                        col.Width = Properties.Settings.Default.VoltWidth;
                        break;
                    case "BrSequ":
                        col.DisplayIndex = Properties.Settings.Default.BrSequIndex;
                        col.Width = Properties.Settings.Default.BrSequWidth;
                        break;
                    case "BrSequPersist":
                        col.Visibility = System.Windows.Visibility.Hidden;
                        //col.DisplayIndex = Properties.Settings.Default.BrSequPersistInt;
                        //col.Width = Properties.Settings.Default.BrSequPersistWidth;
                        break;
                    case "BrCmd":
                        col.DisplayIndex = Properties.Settings.Default.BrCmdIndex;
                        col.Width = Properties.Settings.Default.BrCmdWidth;
                        break;
                    case "TOFping":
                        col.DisplayIndex = Properties.Settings.Default.TOFpingIndex;
                        col.Width = Properties.Settings.Default.TOFpingWidth;
                        break;
                    case "TOFtimeout":
                        col.DisplayIndex = Properties.Settings.Default.TOFtimeoutIndex;
                        col.Width = Properties.Settings.Default.TOFtimeoutWidth;
                        break;
                    case "TOFrefuse":
                        col.DisplayIndex = Properties.Settings.Default.TOFrefuseIndex;
                        col.Width = Properties.Settings.Default.TOFrefuseWidth;
                        break;
                    case "TOFSuccess":
                        col.DisplayIndex = Properties.Settings.Default.TOFsuccessIndex;
                        col.Width = Properties.Settings.Default.TOFsuccessWidth;
                        break;
                    case "TOFdistance":
                        col.DisplayIndex = Properties.Settings.Default.TOFdistanceIndex;
                        col.Width = Properties.Settings.Default.TOFdistanceWidth;
                        break;
                    case "RSSIdistance":
                        col.DisplayIndex = Properties.Settings.Default.RSSIdistanceIndex;
                        col.Width = Properties.Settings.Default.RSSIdistanceWidth;
                        break;
                    case "TOFerror":
                        col.DisplayIndex = Properties.Settings.Default.TOFerrorIndex;
                        col.Width = Properties.Settings.Default.TOFerrorWidth;
                        break;
                    case "TOFmac":
                        col.DisplayIndex = Properties.Settings.Default.TOFmacIndex;
                        col.Width = Properties.Settings.Default.TOFmacWidth;
                        break;

                    case "unitID":
                        col.DisplayIndex = Properties.Settings.Default.unitIDIndex;
                        col.Width = Properties.Settings.Default.unitIDWidth;
                        break;
                    case "zoneID":
                        col.DisplayIndex = Properties.Settings.Default.zoneIDIndex;
                        col.Width = Properties.Settings.Default.zoneIDWidth;
                        break;
                    case "RxLQI":
                        col.DisplayIndex = Properties.Settings.Default.RxLQIInxex;
                        col.Width = Properties.Settings.Default.RxLQIWidth;
                        break;
                    case "CH4gas":
                        col.Visibility = System.Windows.Visibility.Hidden;
                        //col.DisplayIndex = Properties.Settings.Default.CH4gasIndex;
                        //col.Width = Properties.Settings.Default.CH4gasWidth;
                        break;
                    case "COgas":
                        col.Visibility = System.Windows.Visibility.Hidden;
                        //col.DisplayIndex = Properties.Settings.Default.COgasIndex;
                        //col.Width = Properties.Settings.Default.COgasWidth;
                        break;
                    case "O2gas":
                        col.Visibility = System.Windows.Visibility.Hidden;
                        //col.DisplayIndex = Properties.Settings.Default.O2gasIndex;
                        //col.Width = Properties.Settings.Default.O2gasWidth;
                        break;
                    case "CO2gas":
                        col.Visibility = System.Windows.Visibility.Hidden;
                        //col.DisplayIndex = Properties.Settings.Default.CO2gasIndex;
                        //col.Width = Properties.Settings.Default.CO2gasWidth;
                        break;
                    case "failState":
                        col.Visibility = System.Windows.Visibility.Hidden;
                        //col.DisplayIndex = Properties.Settings.Default.failStateIndex;
                        //col.Width = Properties.Settings.Default.failStateWidth;
                        break;
                    case "u57":
                        col.Visibility = System.Windows.Visibility.Hidden;
                        //col.DisplayIndex = Properties.Settings.Default.u57Index;
                        //col.Width = Properties.Settings.Default.u57Width;
                        break;
                    case "u54":
                        col.Visibility = System.Windows.Visibility.Hidden;
                        //col.DisplayIndex = Properties.Settings.Default.u54Index;
                        //col.Width = Properties.Settings.Default.u54Width;
                        break;
                    case "u55":
                        col.Visibility = System.Windows.Visibility.Hidden;
                        //col.DisplayIndex = Properties.Settings.Default.u55Index;
                        //col.Width = Properties.Settings.Default.u55Width;
                        break;
                    case "u56":
                        col.Visibility = System.Windows.Visibility.Hidden;
                        //col.DisplayIndex = Properties.Settings.Default.u56Index;
                        //col.Width = Properties.Settings.Default.u56Width;
                        break;
                    case "u58":
                        col.Visibility = System.Windows.Visibility.Hidden;
                        //col.DisplayIndex = Properties.Settings.Default.u58Index;
                        //col.Width = Properties.Settings.Default.u58Width;
                        break;
                    case "switchState":
                        col.Visibility = System.Windows.Visibility.Hidden;
                        //col.DisplayIndex = Properties.Settings.Default.switchStateIndex;
                        //col.Width = Properties.Settings.Default.switchStateWidth;
                        break;
                    case "Image":
                        //Visibility = System.Windows.Visibility.Hidden;
                        col.DisplayIndex = Properties.Settings.Default.ImageIndex;
                        col.Width = Properties.Settings.Default.ImageWidth;
                        break;
                    case "remoteLockout":
                        col.Visibility = System.Windows.Visibility.Hidden;
                        ////col.DisplayIndex = Properties.Settings.Default.remoteLockoutIndex;
                        ////col.Width = Properties.Settings.Default.remoteLockoutWidth;
                        break;
                    case "keyShort":
                        col.Visibility = System.Windows.Visibility.Hidden;
                        //col.DisplayIndex = Properties.Settings.Default.keyShortIndex;
                        //col.Width = Properties.Settings.Default.keyShortWidth;
                        break;
                    case "switchError":
                        col.Visibility = System.Windows.Visibility.Hidden;
                        //col.DisplayIndex = Properties.Settings.Default.switchErrorIndex;
                        //col.Width = Properties.Settings.Default.switchErrorWidth;
                        break;
                    case "RLO_Error":
                        col.Visibility = System.Windows.Visibility.Hidden;
                        //col.DisplayIndex = Properties.Settings.Default.RLO_ErrorIndex;
                        //col.Width = Properties.Settings.Default.RLO_ErrorWidth;
                        break;
                    case "dcVoltsState":
                        col.Visibility = System.Windows.Visibility.Hidden;
                        //col.DisplayIndex = Properties.Settings.Default.dcVoltsStateIndex;
                        //col.Width = Properties.Settings.Default.dcVoltsStateWidth;
                        break;
                    case "adcReadError1":
                        col.Visibility = System.Windows.Visibility.Hidden;
                        //col.DisplayIndex = Properties.Settings.Default.adcReadError1Index;
                        //col.Width = Properties.Settings.Default.adcReadError1Width;
                        break;
                    case "adcReadError2":
                        col.Visibility = System.Windows.Visibility.Hidden;
                        //col.DisplayIndex = Properties.Settings.Default.adcReadError2Index;
                        //col.Width = Properties.Settings.Default.adcReadError2Width;
                        break;
                    case "s57":
                        col.Visibility = System.Windows.Visibility.Hidden;
                       
                        break;
                }


            }
        }
        /// <summary>
        /// Stop Button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MenuItem_Stop_Click(object sender, RoutedEventArgs e)
        {
            _ethernetConnectWaitHandle.Reset();
            foreach (KeyValuePair<string, EthernetConnection> ethCon in EthernetConnection._myEthConnList)
            {
                ethCon.Value.EthernetAbort();                    
            }
            if (_comSetup1.comport.IsOpen) _comSetup1.comport.Close();
            MenuStart.IsEnabled = true;
            MenuStop.IsEnabled = false;
            _timerTree.Stop();
            foreach (KeyValuePair<string, EthernetConnection> ethCon in EthernetConnection._myEthConnList)
            {
                ethCon.Value.EthernetAbort();     
            }
            EthernetConnection._myEthConnList.Clear();
            //allLists.workingTagQueue0.Clear();
            EthernetConnection.allLists.workingTagQueue0.Clear();
            // allLists.myTagReaderList.Clear();
        }
        /// <summary>
        /// Database button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_DataBase_Click(object sender, RoutedEventArgs e)
        {
            _dataBaseSetup1.Show();
        }

        public void subscribe(ComSetup c)
        {
            c.Stop += new ComSetup.StopHandler(c_Stop);
        }

        public void subscribe2(Message o)
        {
            o.SendDataEvent += new Message.SendDataHandler(o_SendDataEvent);
        }

        void o_SendDataEvent(byte[] message)
        {
            foreach (KeyValuePair<string, EthernetConnection> ethCon in EthernetConnection._myEthConnList)
            {
                int[] frame = new int[3 + message.Length];
                int i = 0;
                frame[i++] = tcpMessageType.TAG_CONFIG;
                frame[i++] = tcpMessageType.TAG_BROADCAST_PACKET;
                // ethernetTransmitQueue.Enqueue(tcpSequ);
                frame[i++] = message.Length;
                for (int j = 0; j < message.Length; j++)
                {
                    frame[i++] = (Convert.ToInt32(message[j]));
                }
                ethCon.Value.TCPSend(frame);
            }
        }

        void c_Stop()
        {
            MenuItem_Stop_Click(this, null);
        }

        /// <summary>
        /// Button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Message_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _messageWindow1.Show();
            }
            catch
            {
                _messageWindow1 = new Message(ref EthernetConnection.allLists);
                subscribe2(_messageWindow1);
                _messageWindow1.Show();
            }
        }
        /// <summary>
        /// Red cross click to exit event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            ExitApplication();
        }
        /// <summary>
        /// Button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NamesForm_Click(object sender, RoutedEventArgs e)
        {
            _minersNamesForm.Show();
        }
        private void LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            LoadConfig();
        }

        /// <summary>
        /// Displays Load config dialog
        /// </summary>
        private void LoadConfig()
        {
            string filename = null;
            System.Windows.Forms.OpenFileDialog dlg = new  System.Windows.Forms.OpenFileDialog(); // Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Names.ddb";
            dlg.DefaultExt = ".ddb";
            dlg.Filter = "(.ddb)|*.ddb";
            dlg.Title = "Open WiPAN config, Davis Derby Ltd";
            System.Windows.Forms.DialogResult result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                filename = dlg.FileName;
            }
            if ((filename != "") & (filename != null))
            {
                FileClass.LoadMapXml(filename);
            }
        }
        /// <summary>
        /// Save config click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();
        }

        private void saveGridViewOrder()
        {
            foreach (var col in dataGridView1.Columns)
            {

                int width = (int)(float.Parse(col.ActualWidth.ToString()));

                switch (col.Header.ToString())
                {

                    case "TTL":
                        Properties.Settings.Default.TTLwidth = col.DisplayIndex;

                        Properties.Settings.Default.TTLindex = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;

                    case "PktLength":
                        Properties.Settings.Default.PktLengthindex = col.DisplayIndex;
                        Properties.Settings.Default.PktLengthWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;

                    case "minersName":
                        Properties.Settings.Default.minersNameindex = col.DisplayIndex;
                        Properties.Settings.Default.minersNameWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;

                    case "endPointType":
                        Properties.Settings.Default.endPointTypeIndex = col.DisplayIndex;
                        Properties.Settings.Default.endPointTypeWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;

                    case "PktSequence":
                        Properties.Settings.Default.PktSequenceindex = col.DisplayIndex;
                        Properties.Settings.Default.PktSequenceWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "PktType":
                        Properties.Settings.Default.PktTypeindex = col.DisplayIndex;
                        Properties.Settings.Default.PktTypeWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "ReaderAdd":
                        Properties.Settings.Default.ReaderAddIndex = col.DisplayIndex;
                        Properties.Settings.Default.ReaderAddWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "TagAdd":
                        Properties.Settings.Default.tagAddIndex = col.DisplayIndex;
                        Properties.Settings.Default.tagAddWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "PktLqi":
                        Properties.Settings.Default.PktLqiIndex = col.DisplayIndex;
                        Properties.Settings.Default.PktLqiWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "PktEvent":
                        Properties.Settings.Default.PktEventindex = col.DisplayIndex;
                        Properties.Settings.Default.PktEventWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "PktTemp":
                        Properties.Settings.Default.PktTempindex = col.DisplayIndex;
                        Properties.Settings.Default.PktTempWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "Volt":
                        Properties.Settings.Default.Voltindex = col.DisplayIndex;
                        Properties.Settings.Default.VoltWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "BrSequ":
                        Properties.Settings.Default.BrSequIndex = col.DisplayIndex;
                        Properties.Settings.Default.BrSequWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "BrSequPersist":
                        Properties.Settings.Default.BrSequPersistInt = col.DisplayIndex;
                        Properties.Settings.Default.BrSequPersistWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "BrCmd":
                        Properties.Settings.Default.BrCmdIndex = col.DisplayIndex;
                        Properties.Settings.Default.BrCmdWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "TOFping":
                        Properties.Settings.Default.TOFpingIndex = col.DisplayIndex;
                        Properties.Settings.Default.TOFpingWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "TOFtimeout":
                        Properties.Settings.Default.TOFtimeoutIndex = col.DisplayIndex;
                        Properties.Settings.Default.TOFtimeoutWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "TOFrefuse":
                        Properties.Settings.Default.TOFrefuseIndex = col.DisplayIndex;
                        Properties.Settings.Default.TOFrefuseWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "TOFSuccess":
                        Properties.Settings.Default.TOFsuccessIndex = col.DisplayIndex;
                        Properties.Settings.Default.TOFsuccessWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "TOFdistance":
                        Properties.Settings.Default.TOFdistanceIndex = col.DisplayIndex;
                        col.Width = Properties.Settings.Default.TOFdistanceWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "RSSIdistance":
                        Properties.Settings.Default.RSSIdistanceIndex = col.DisplayIndex;
                        Properties.Settings.Default.RSSIdistanceWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "TOFerror":
                        Properties.Settings.Default.TOFerrorIndex = col.DisplayIndex;
                        Properties.Settings.Default.TOFerrorWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "TOFmac":
                        Properties.Settings.Default.TOFmacIndex = col.DisplayIndex;
                        Properties.Settings.Default.TOFmacWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;

                    case "unitID":
                        Properties.Settings.Default.unitIDIndex = col.DisplayIndex;
                        Properties.Settings.Default.unitIDWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "zoneID":
                        Properties.Settings.Default.zoneIDIndex = col.DisplayIndex;
                        Properties.Settings.Default.zoneIDWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "RxLQI":
                        Properties.Settings.Default.RxLQIInxex = col.DisplayIndex;
                        Properties.Settings.Default.RxLQIWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "CH4gas":
                        Properties.Settings.Default.CH4gasIndex = col.DisplayIndex;
                        Properties.Settings.Default.CH4gasWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "COgas":
                        Properties.Settings.Default.COgasIndex = col.DisplayIndex;
                        Properties.Settings.Default.COgasWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "O2gas":
                        Properties.Settings.Default.O2gasIndex = col.DisplayIndex;
                        Properties.Settings.Default.O2gasWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "CO2gas":
                        Properties.Settings.Default.CO2gasIndex = col.DisplayIndex;
                        Properties.Settings.Default.CO2gasWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "failState":
                        Properties.Settings.Default.failStateIndex = col.DisplayIndex;
                        Properties.Settings.Default.failStateWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "u57":
                        Properties.Settings.Default.u57Index = col.DisplayIndex;
                        Properties.Settings.Default.u57Width = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "u54":
                        Properties.Settings.Default.u54Index = col.DisplayIndex;
                        Properties.Settings.Default.u54Width = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "u55":
                        Properties.Settings.Default.u55Index = col.DisplayIndex;
                        Properties.Settings.Default.u55Width = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "u56":
                        Properties.Settings.Default.u56Index = col.DisplayIndex;
                        Properties.Settings.Default.u56Width = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "u58":
                        Properties.Settings.Default.u58Index = col.DisplayIndex;
                        Properties.Settings.Default.u58Width = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "switchState":
                        Properties.Settings.Default.switchStateIndex = col.DisplayIndex;
                        Properties.Settings.Default.switchStateWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "Image":
                        Properties.Settings.Default.ImageIndex = col.DisplayIndex;
                        Properties.Settings.Default.ImageWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "remoteLockout":
                        Properties.Settings.Default.remoteLockoutIndex = col.DisplayIndex;
                        Properties.Settings.Default.remoteLockoutWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "keyShort":
                        Properties.Settings.Default.keyShortIndex = col.DisplayIndex;
                        Properties.Settings.Default.keyShortWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "switchError":
                        Properties.Settings.Default.switchErrorIndex = col.DisplayIndex;
                        Properties.Settings.Default.switchErrorWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "RLO_Error":
                        Properties.Settings.Default.RLO_ErrorIndex = col.DisplayIndex;
                        Properties.Settings.Default.RLO_ErrorWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "dcVoltsState":
                        Properties.Settings.Default.dcVoltsStateIndex = col.DisplayIndex;
                        Properties.Settings.Default.dcVoltsStateWidth = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "adcReadError1":
                        Properties.Settings.Default.adcReadError1Index = col.DisplayIndex;
                        Properties.Settings.Default.adcReadError1Width = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                    case "adcReadError2":
                        Properties.Settings.Default.adcReadError2Index = col.DisplayIndex;
                        Properties.Settings.Default.adcReadError2Width = (int)(float.Parse(col.ActualWidth.ToString()));
                        break;
                }
                Properties.Settings.Default.Save();

            }
        }

        /// <summary>
        /// Displays save config dialog
        /// </summary>
        private void SaveConfig()
        {
            string filename = null;
            System.Windows.Forms.SaveFileDialog dlg = new System.Windows.Forms.SaveFileDialog();
            dlg.FileName = "Names.ddb";
            dlg.DefaultExt = ".ddb";
            dlg.Filter = "(.ddb)|*.ddb";
            dlg.Title = "Save WiPAN config, Davis Derby Ltd";
            System.Windows.Forms.DialogResult result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                filename = dlg.FileName;
            }
            FileClass.saveMapXml(filename);
        }
        /// <summary>
        /// Clear button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuClear_Click(object sender, RoutedEventArgs e)
        {
            EthernetConnection.allLists.allTagList.Clear();
        }
        /// <summary>
        /// checks for button clicks events in data grid cells
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DataGrid temp = sender as DataGrid;
            if (temp.CurrentCell.Column != null)
            {
                int col = temp.CurrentCell.Column.DisplayIndex;
                string colName = temp.CurrentCell.Column.Header.ToString();
                if (colName == "remoteLockout")
                {
                    int row = dataGridPullkey.SelectedIndex;
                    if (EthernetConnection.allLists.allTagList[row].remoteLockout == false)
                    {
                        _messageWindow1.addMessageToTxQueue(EthernetConnection.allLists.allTagList[row].zoneID, EthernetConnection.allLists.allTagList[row].unitID, 0x55, 0x01);
                    }
                    else
                    {
                        _messageWindow1.addMessageToTxQueue(EthernetConnection.allLists.allTagList[row].zoneID, EthernetConnection.allLists.allTagList[row].unitID, 0x55, 0x00);
                    }
                }
                if (colName == "minersName")
                {
                    _minersNamesForm.Show();
                }
            }
        }
        /// <summary>
        /// Coord Setup click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void coordSetup_Click(object sender, RoutedEventArgs e)
        {
            CoordSetup coordSetup = new CoordSetup();
            coordSetup.Show();
        }

        /// <summary>
        /// TabItem click event
        /// Makes changes based on which tab is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TabItem temp = sender as TabItem;
            if (temp.Header.ToString() == "Console")
            {
                MenuClearLog.Visibility = System.Windows.Visibility.Visible;
                updatConsoleText();
            }
            else
            {
                MenuClearLog.Visibility = System.Windows.Visibility.Hidden;
            }
            
        }

        /// <summary>
        /// Reloads debugLog to console text box
        /// </summary>
        private void updatConsoleText()
        {
            try
            {
                if ((File.GetAttributes("debugLog.txt") & FileAttributes.Archive) == FileAttributes.Archive)
                {
                    consoleText.Clear();
                    using (StreamReader w = new StreamReader("debugLog.txt"))
                    {
                        string line;
                        while ((line = w.ReadLine()) != null)
                        {
                            consoleText.AppendText("\n");
                            consoleText.AppendText(line); // Write to console.
                        }
                        w.Close();
                        w.Dispose();


                    }
                    File.SetAttributes("debugLog.txt", File.GetAttributes("debugLog.txt") & ~FileAttributes.Archive);
                }
                else 
                {
                }

               
            }
            catch
            { }

        }
         


        private void MenuClearLog_Click(object sender, RoutedEventArgs e)
        {
            consoleText.Clear();
            using (FileStream stream = new FileStream("debugLog.txt", FileMode.Create))
            using (TextWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("");
            }
        }

       

    }
}
