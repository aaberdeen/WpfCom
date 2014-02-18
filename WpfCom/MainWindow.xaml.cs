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
        //public Thread _splashThread;      
        private errorLog _errorLog = new errorLog();
        private ConcurrentDictionary<string, EthernetConnection> _myEthConnList = new ConcurrentDictionary<string, EthernetConnection>();
        public Thread _connectionThread;
        private volatile bool _shouldStopConnectionThread;
        public AutoResetEvent ethernetConnectWaitHandle;
        
        ComSetup comSetup1 = new ComSetup();

        Message messageWindow1;
        DataBaseSetup dataBaseSetup1 = new DataBaseSetup();
        DispatcherTimer timerTree = new DispatcherTimer();
        MinersNamesForm minersNamesForm = new MinersNamesForm();
       // private static Lists allLists;
    
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

        int[] PortArray = new int[200];
        const int DLE = 0x10;
        const int STX = 0x2;


        /// <summary>
        /// Main
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            _errorLog.write("Hello");
            
   

            //_splashThread = new Thread(new ThreadStart(splashThread));
            //_splashThread.SetApartmentState(ApartmentState.STA);
            //_splashThread.Start();

            _connectionThread = new Thread(new ThreadStart(connectionThread));
            _connectionThread.Name = "_connectionThread";
            
            //_splashThread.Start();

           

            
            //subscribe4(comSetup1);

            timerTree.Interval = new TimeSpan(0, 0, 10);
            timerTree.Tick += new EventHandler(timerTree_Tick);
          //  allLists = new Lists();
            messageWindow1 = new Message(ref EthernetConnection.allLists);
            subscribe(comSetup1);
            subscribe2(messageWindow1);
           
            LoadConfig();
            ethernetConnectWaitHandle = new AutoResetEvent(false);
          //  messageWindow1 = new Message(ref allLists);
            //dataGridView1.AutoGenerateColumns = true;
        }

        private void connectionThread()
        {
            
           
            while (!_shouldStopConnectionThread)
            {
                ethernetConnectWaitHandle.WaitOne(); // blocks thread untill signall is recived 
                foreach (coodData conn in comSetup1.coordIpList)
                {
                    if (!_shouldStopConnectionThread)
                    {
                        if (!conn.connected)
                        {
                            int getIndex = comSetup1.coordIpList.IndexOf(conn);
                            //try reconnect
                            etherConnect(conn.IP, conn.localIP, conn.udpPort, conn.port, getIndex);
                        }
                    }

                }
            }
        }

        public void splashThread()
        {
            new SplashWindow().ShowDialog();
           // SplashWindow splash = new SplashWindow();
           // splash.ShowDialog();
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

            ethernetConnectWaitHandle.Set();

        }
        /// <summary>
        /// Event on click of Port Button on menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Port_Click(object sender, RoutedEventArgs e)
        {
            comSetup1.Show();
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

        private void ExitApplication()
        {
            _shouldStopConnectionThread = true;
            ethernetConnectWaitHandle.Set();
            _connectionThread.Abort();
                    
            
            foreach (KeyValuePair<string, EthernetConnection> ethCon in _myEthConnList)
            {
                ethCon.Value.EthernetAbort();
            }
            messageWindow1.Do_Work = false;
            messageWindow1.shouldStopSendMessageThread = true;
            messageWindow1.sendMessageWaitHandle.Set(); //kick this so the task can end
          
            messageWindow1.Close();

            Application.Current.Shutdown();
        }

        /// <summary>
        /// Attenpts an ethernet connection
        /// </summary>
        /// <param name="server"></param>
        /// <param name="port"></param>
        public void etherConnect(string server, string localIP, string udpPort, string port, int index)
        {
            if (Usefull.ValidIP(server))
            {
                try
                {   //check to see if it is already in the list if it is remove then add

                    if (_myEthConnList.ContainsKey(server))
                    {
                        EthernetConnection value1;

                        _myEthConnList.TryRemove(server, out value1);
                    }

                    bool trackEn = false;
                    if (Properties.Settings.Default.EnableTracking)
                    {
                        trackEn = true;
                    }

                    //EthernetConnection newConnect = new EthernetConnection(server, port, localIP, udpPort, ref comSetup1, ref minersNamesForm, ref dataBaseSetup1, trackEn, ref allLists, index, ref messageWindow1);
                    EthernetConnection newConnect = new EthernetConnection(server, port, localIP, udpPort, ref comSetup1, ref minersNamesForm, ref dataBaseSetup1, trackEn, index, ref messageWindow1);
                    _myEthConnList.TryAdd(server, newConnect);


                }
                catch (SocketException e)
                {
                    comSetup1.coordIpList[index].connected = false;
                    _errorLog.write(e, "MainWindow etherConnect");
                    
                }

            }
            else
            {
                comSetup1.label8.Content = string.Format("Not valid IP");
            }
        }


        public static void Log(string logMessage, TextWriter w)
        {
            //w.Write("\r\nLog Entry : ");
            //w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
            //    DateTime.Now.ToLongDateString());
            //w.WriteLine("  :");
            // w.WriteLine("  :{0}", logMessage);
            w.Write("{0}", logMessage);
            //w.WriteLine("-------------------------------");
        }



        /**********************************need to move this to ethernet ******************************************************/
        private void decAllTtlOflistToReturn()
        {
            //and dec ttl of all tags.
            for (int i = 0; i <= (EthernetConnection.allLists.listToReturn.Count - 1); i++)
            {
                int k = EthernetConnection.allLists.listToReturn[i].Children.Count - 1;      //dec all tags in list
                for (int j = 0; j <= k; j++)
                {
                    if (EthernetConnection.allLists.listToReturn[i].Children[j].Children[0].TTL != 0)
                    {
                        EthernetConnection.allLists.listToReturn[i].Children[j].Children[0].TTL--;
                    }

                    if (EthernetConnection.allLists.listToReturn[i].Children[j].Children[0].TTL <= 7)
                    {
                        if (EthernetConnection.allLists.listToReturn[i].Children[j].endPointType == "Key")
                        {
                            if (EthernetConnection.allLists.listToReturn[i].Children[j].readerAddress != "0000000000000000")
                            {
                                _errorLog.write(string.Format("ttl= {0}, reader= {1}, type= {2}", EthernetConnection.allLists.listToReturn[i].Children[j].Children[0].TTL, EthernetConnection.allLists.listToReturn[i].Children[j].Name, EthernetConnection.allLists.listToReturn[i].Children[j].endPointType));
                            }
                        }
                    }

                    if (EthernetConnection.allLists.listToReturn[i].Children[j].Children[0].TTL <= 1)
                    {
                        Node remove = EthernetConnection.allLists.listToReturn[i].Children[j];                       
                        EthernetConnection.allLists.listToReturn[i].Children.Remove(remove);
                         k--;
                    }
                }
            }

            for (int i = 0; i <= (EthernetConnection.allLists.allTagList.Count - 1); i++)
            {
                EthernetConnection.allLists.allTagList[i].TTL--;
                if (EthernetConnection.allLists.allTagList[i].TTL == 0)
                {

                    //  toRemove.Add(allLists.allTagList[i]);
                    EthernetConnection.allLists.allTagList.RemoveAt(i);
                }
            }
        }




        private void MenuItem_Start_Click(object sender, RoutedEventArgs e)
        {
            if ((_connectionThread.ThreadState == System.Threading.ThreadState.Unstarted) | (_connectionThread.ThreadState == System.Threading.ThreadState.Stopped))
            {
                _connectionThread.Start();

            }
            
            ethernetConnectWaitHandle.Set();
            //historyDataBaseSetup();
            comSetup1.SetComboBoxDefault();
            //  comSetup1.openComm();                     //Removed for etherent testing
           // dataGridView1.ItemsSource = allLists.allTagList;
           // dataGridView1.AutoGenerateColumns = true;
            //dataGridView1.DataContext = allLists.allTagList;
          
            //DataContext = allLists;
            DataContext = EthernetConnection.allLists;                             //static

 



            //treeView.ItemsSource = allLists.listToReturn;
            treeView.ItemsSource = EthernetConnection.allLists.listToReturn;       //static
            MenuStop.IsEnabled = true;
            MenuStart.IsEnabled = false;
            timerTree.Start();
           // int i = dataGridView1.Columns.Count;
            setUpGridViewOrder();


        }

        private void setUpGridViewOrder()
        {
            foreach (var col in dataGridView1.Columns)
            {


                switch (col.Header.ToString())
                {
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
                        col.DisplayIndex = Properties.Settings.Default.BrSequPersistInt;
                        col.Width = Properties.Settings.Default.BrSequPersistWidth;
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
                        col.DisplayIndex = Properties.Settings.Default.CH4gasIndex;
                        col.Width = Properties.Settings.Default.CH4gasWidth;
                        break;
                    case "COgas":
                        col.DisplayIndex = Properties.Settings.Default.COgasIndex;
                        col.Width = Properties.Settings.Default.COgasWidth;
                        break;
                    case "O2gas":
                        col.DisplayIndex = Properties.Settings.Default.O2gasIndex;
                        col.Width = Properties.Settings.Default.O2gasWidth;
                        break;
                    case "CO2gas":
                        col.DisplayIndex = Properties.Settings.Default.CO2gasIndex;
                        col.Width = Properties.Settings.Default.CO2gasWidth;
                        break;
                    case "failState":
                        col.DisplayIndex = Properties.Settings.Default.failStateIndex;
                        col.Width = Properties.Settings.Default.failStateWidth;
                        break;
                    case "u57":
                        col.DisplayIndex = Properties.Settings.Default.u57Index;
                        col.Width = Properties.Settings.Default.u57Width;
                        break;
                    case "u54":
                        col.DisplayIndex = Properties.Settings.Default.u54Index;
                        col.Width = Properties.Settings.Default.u54Width;
                        break;
                    case "u55":
                        col.DisplayIndex = Properties.Settings.Default.u55Index;
                        col.Width = Properties.Settings.Default.u55Width;
                        break;
                    case "u56":
                        col.DisplayIndex = Properties.Settings.Default.u56Index;
                        col.Width = Properties.Settings.Default.u56Width;
                        break;
                    case "u58":
                        col.DisplayIndex = Properties.Settings.Default.u58Index;
                        col.Width = Properties.Settings.Default.u58Width;
                        break;
                    case "switchState":
                        col.DisplayIndex = Properties.Settings.Default.switchStateIndex;
                        col.Width = Properties.Settings.Default.switchStateWidth;
                        break;
                    case "Image":
                        col.DisplayIndex = Properties.Settings.Default.ImageIndex;
                        col.Width = Properties.Settings.Default.ImageWidth;
                        break;
                    case "remoteLockout":
                        col.DisplayIndex = Properties.Settings.Default.remoteLockoutIndex;
                        col.Width = Properties.Settings.Default.remoteLockoutWidth;
                        break;
                    case "keyShort":
                        col.DisplayIndex = Properties.Settings.Default.keyShortIndex;
                        col.Width = Properties.Settings.Default.keyShortWidth;
                        break;
                    case "switchError":
                        col.DisplayIndex = Properties.Settings.Default.switchErrorIndex;
                        col.Width = Properties.Settings.Default.switchErrorWidth;
                        break;
                    case "RLO_Error":
                        col.DisplayIndex = Properties.Settings.Default.RLO_ErrorIndex;
                        col.Width = Properties.Settings.Default.RLO_ErrorWidth;
                        break;
                    case "dcVoltsState":
                        col.DisplayIndex = Properties.Settings.Default.dcVoltsStateIndex;
                        col.Width = Properties.Settings.Default.dcVoltsStateWidth;
                        break;
                    case "adcReadError1":
                        col.DisplayIndex = Properties.Settings.Default.adcReadError1Index;
                        col.Width = Properties.Settings.Default.adcReadError1Width;
                        break;
                    case "adcReadError2":
                        col.DisplayIndex = Properties.Settings.Default.adcReadError2Index;
                        col.Width = Properties.Settings.Default.adcReadError2Width;
                        break;
                }


            }
        }



        public void MenuItem_Stop_Click(object sender, RoutedEventArgs e)
        {
            ethernetConnectWaitHandle.Reset();
            foreach (KeyValuePair<string, EthernetConnection> ethCon in _myEthConnList)
            {
                ethCon.Value.EthernetAbort();                    
            }

            if (comSetup1.comport.IsOpen) comSetup1.comport.Close();

            MenuStart.IsEnabled = true;
            MenuStop.IsEnabled = false;
            timerTree.Stop();
            foreach (KeyValuePair<string, EthernetConnection> ethCon in _myEthConnList)
            {
                ethCon.Value.EthernetAbort();     
            }
            _myEthConnList.Clear();
            //allLists.workingTagQueue0.Clear();
            EthernetConnection.allLists.workingTagQueue0.Clear();

            // allLists.myTagReaderList.Clear();
        }

        private void MenuItem_DataBase_Click(object sender, RoutedEventArgs e)
        {
            dataBaseSetup1.Show();
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
            foreach (KeyValuePair<string, EthernetConnection> ethCon in _myEthConnList)
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


        private void treeView1_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var parent = sender as TreeView;

            if (parent.SelectedItem != null)
            {
                var children = parent.SelectedItem as TreeTag;  //TreeViewItem;
                string MAC = children.Name;

                messageWindow1.textBox1.Text = MAC.Insert(8, ":");
                messageWindow1.Show();
                StatusText.Text = "Message window opened";
            }
            else
            {
                StatusText.Text = "No item selected - Left click to select item";
            }
        }

        private void MenuItem_Message_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                messageWindow1.Show();
            }
            catch
            {
                messageWindow1 = new Message(ref EthernetConnection.allLists);
                subscribe2(messageWindow1);
                messageWindow1.Show();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ExitApplication();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            minersNamesForm.Show();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            string filename = null;
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Names.ddb";
            dlg.DefaultExt = ".ddb";
            dlg.Filter = "(.ddb)|*.ddb";
            //Show save dlg
            Nullable<bool> result = dlg.ShowDialog();
            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                filename = dlg.FileName;
            }
            if ((filename != "") & (filename != null))
            {
                FileClass.LoadMapXml(filename, ref comSetup1, ref minersNamesForm);
            }
        }

       

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            SaveConfig();

            saveGridViewOrder();

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

        private void SaveConfig()
        {
            string filename = null;
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "Names.ddb";
            dlg.DefaultExt = ".ddb";
            dlg.Filter = "(.ddb)|*.ddb";
            //Show save dlg
            Nullable<bool> result = dlg.ShowDialog();
            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                filename = dlg.FileName;
            }
            FileClass.saveMapXml(filename, ref comSetup1, ref minersNamesForm);
        }

      

        private void MenuClear_Click(object sender, RoutedEventArgs e)
        {
            EthernetConnection.allLists.allTagList.Clear();
        }



        private void DataGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

            int col = dataGridPullkey.CurrentCell.Column.DisplayIndex;
            string colName = dataGridPullkey.Columns[col].Header.ToString();

            if (colName == "remoteLockout")
            {
                               
                int row = dataGridPullkey.SelectedIndex;
                if (EthernetConnection.allLists.allTagList[row].remoteLockout == false)
                {
                    messageWindow1.addMessageToTxQueue(EthernetConnection.allLists.allTagList[row].zoneID, EthernetConnection.allLists.allTagList[row].unitID, 0x55, 0x01);
                }
                else
                {
                    messageWindow1.addMessageToTxQueue(EthernetConnection.allLists.allTagList[row].zoneID, EthernetConnection.allLists.allTagList[row].unitID, 0x55, 0x00);
                }
            }
        }

        private void MenuItem_Click_5(object sender, RoutedEventArgs e)
        {
            CoordSetup coordSetup = new CoordSetup();
            coordSetup.Show();
        }

        private void TabItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            using (StreamReader w = new StreamReader("debugLog.txt"))
            {
                string line;
                while ((line = w.ReadLine()) != null)
                {
                    consoleText.AppendText(line); // Write to console.
                }
                w.Close();
                w.Dispose();


            }

        }

        private void TabItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            updatConsoleText();
        }

        private void updatConsoleText()
        {
            try
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
            }
            catch
            { }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            consoleText.Clear();
            using (FileStream stream = new FileStream("debugLog.txt", FileMode.Create)) 
            using (TextWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("");
            }

        }

        private void consoleText_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            consoleText.Clear();
            using (FileStream stream = new FileStream("debugLog.txt", FileMode.Create))
            using (TextWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("");
            }
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {

        }

    }
}
