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
        ConcurrentDictionary<string, EthernetConnection> myEthConnList = new ConcurrentDictionary<string, EthernetConnection>();
        public Thread _connectionThread;
        private volatile bool _shouldStopConnectionThread;
        public AutoResetEvent ethernetConnectWaitHandle;
        
        ComSetup comSetup1 = new ComSetup();

        Message messageWindow1;
        DataBaseSetup dataBaseSetup1 = new DataBaseSetup();
        DispatcherTimer timerTree = new DispatcherTimer();
        MinersNamesForm minersNamesForm = new MinersNamesForm();
        Lists allLists;
    
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
            allLists = new Lists();
            messageWindow1 = new Message(ref allLists);
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
                    if (!conn.connected)
                    {
                        int getIndex = comSetup1.coordIpList.IndexOf(conn);
                        //try reconnect
                        etherConnect(conn.IP, conn.localIP,conn.udpPort, conn.port, getIndex);
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
            //_tickTimerThread.Start();  cant just start it as it may not have ended so need to do tick in thread. Or somthing like that



            // this should be done in separate thread to stop screen locking up when can't connect
            decAllTtlOflistToReturn();
            updatConsoleText();
            foreach (KeyValuePair<string, EthernetConnection> ethCon in myEthConnList)
            {
                ethCon.Value.ethernetSendWaitHandle.Set(); //kicks send thread to send a packet to check link

            }

            //foreach (coodData conn in comSetup1.coordIpList)
            //{
            //    if (!conn.connected)
            //    {
            //        int getIndex = comSetup1.coordIpList.IndexOf(conn);
            //        //try reconnect
            //        etherConnect(conn.IP, conn.port, getIndex);
            //    }

            //}
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
            foreach (KeyValuePair<string, EthernetConnection> ethCon in myEthConnList)
            {
                try
                {
                    _connectionThread.Abort();
                    ethernetConnectWaitHandle.Set();
                    
                }
                catch
                {
                }

                //if (ethCon.Value.tidListen != null)
                //{
                //    ethCon.Value.tidListen.Abort();
                //}
                //if (ethCon.Value.tidSend != null)
                //{
                //    ethCon.Value.tidSend.Abort();
                //}

                try
                {
                    ethCon.Value._TCPListenThread.Abort();
                }
                catch
                {
                }
                try
                {
                    ethCon.Value._TCPSendThread.Abort();
                }
                catch
                {
                }
                try
                {
                    ethCon.Value.tidExtractData.Abort();
                }
                catch
                {
                }


                ethCon.Value.RequestSendMain();
            }
            //mainThread.Abort();
            messageWindow1.Do_Work = false;


            Thread.Sleep(1000);

            foreach (KeyValuePair<string, EthernetConnection> ethCon in myEthConnList)
            {
                //if (ethCon.Value.myClient != null)
                //{
                //    ethCon.Value.myClient.Close();
                //}

                try
                {
                    ethCon.Value.tcpClient.Close();
                    
                    ethCon.Value.udpCallbackRun = false;
                    ethCon.Value.udPclient.Close();
                }
                catch
                {
                }
            }
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

                    if (myEthConnList.ContainsKey(server))
                    {
                        EthernetConnection value1;

                        myEthConnList.TryRemove(server, out value1);
                    }

                    bool trackEn = false;
                    if (Properties.Settings.Default.EnableTracking)
                    {
                        trackEn = true;
                    }

                    EthernetConnection newConnect = new EthernetConnection(server, port, localIP, udpPort, ref comSetup1, ref minersNamesForm, ref dataBaseSetup1, trackEn, ref allLists, index, ref messageWindow1);

                    myEthConnList.TryAdd(server, newConnect);


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




        private void decAllTtlOflistToReturn()
        {
            //and dec ttl of all tags.
            for (int i = 0; i <= (allLists.listToReturn.Count - 1); i++)
            {
                int k = allLists.listToReturn[i].Children.Count - 1;      //dec all tags in list
                for (int j = 0; j <= k; j++)
                {
                    if (allLists.listToReturn[i].Children[j].Children[0].TTL != 0)
                    {
                        allLists.listToReturn[i].Children[j].Children[0].TTL--;
                    }

                    if (allLists.listToReturn[i].Children[j].Children[0].TTL <= 7)
                    {
                        if (allLists.listToReturn[i].Children[j].endPointType == "Key")
                        {
                            _errorLog.write(string.Format("ttl= {0}, reader= {1}, type= {2}", allLists.listToReturn[i].Children[j].Children[0].TTL, allLists.listToReturn[i].Children[j].Name, allLists.listToReturn[i].Children[j].endPointType));
                        }
                    }
                    
                    
                    
                    
                    
                    if (allLists.listToReturn[i].Children[j].Children[0].TTL <= 1)
                    {
                        Node remove = allLists.listToReturn[i].Children[j];

                        System.Windows.Application.Current.Dispatcher.Invoke(
                        System.Windows.Threading.DispatcherPriority.Normal,
                        (Action)delegate()
                        {

                            allLists.listToReturn[i].Children.Remove(remove);
                            k--;
                        });

                    }
                }
            }

           // List<TagBind> toRemove = new List<TagBind>();
            
            for(int i =0; i<=(allLists.allTagList.Count-1); i++)
            {
                allLists.allTagList[i].TTL--;
                if (allLists.allTagList[i].TTL == 0)
                {
                    
                  //  toRemove.Add(allLists.allTagList[i]);
                    allLists.allTagList.RemoveAt(i);
                }
            }

         //   foreach (TagBind tag in toRemove)
         //   {

              //  allLists.allTagList.Remove(tag);
                
               // TagReader searchResultTR = allLists.myTagReaderList.Find(TRtest => TRtest.TagReaderAdd == tag.TagAdd + tag.ReaderAdd);
               // allLists.myTagReaderList.Remove(searchResultTR);

        //    }
            
            

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
            DataContext = allLists;
            treeView.ItemsSource = allLists.listToReturn;
            MenuStop.IsEnabled = true;
            MenuStart.IsEnabled = false;
            timerTree.Start();

        }



        public void MenuItem_Stop_Click(object sender, RoutedEventArgs e)
        {
            ethernetConnectWaitHandle.Reset();
            foreach (KeyValuePair<string, EthernetConnection> ethCon in myEthConnList)
            {
               
                try
                {
                    ethCon.Value._TCPListenThread.Abort();
                }
                catch{}
                try
                {
                    ethCon.Value._TCPSendThread.Abort();
                }
                catch
                {
                }
                ethCon.Value.RequestSendMain();

                try
                {
                    ethCon.Value.tcpClient.Close();
                    
                    ethCon.Value.udpCallbackRun = false;
                    ethCon.Value.udPclient.Close();
                }
                catch
                {
                }

            }

            if (comSetup1.comport.IsOpen) comSetup1.comport.Close();

            MenuStart.IsEnabled = true;
            MenuStop.IsEnabled = false;
            timerTree.Stop();
            foreach (KeyValuePair<string, EthernetConnection> ethCon in myEthConnList)
            {

                try
                {
                    /* Stop listening thread */
                    ethCon.Value._TCPListenThread.Abort();
                }
                catch{}

                try
                {
                    /* Stop sending thread */
                    ethCon.Value._TCPSendThread.Abort();
                }
                catch{}
                try
                {
                    ethCon.Value.tidExtractData.Abort();
                }
                catch
                {
                }

                //  mainThread.Abort();
               
            }
            myEthConnList.Clear();
            allLists.workingTagQueue0.Clear();
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
            foreach (KeyValuePair<string, EthernetConnection> ethCon in myEthConnList)
            {
                lock (ethCon.Value.ethernetTransmitQueue) //  myEthConnList[0].ethernetTransmitQueue)
                {
                    ethCon.Value.ethernetTransmitQueue.Enqueue(tcpMessageType.TAG_CONFIG);
                    ethCon.Value.ethernetTransmitQueue.Enqueue(tcpMessageType.TAG_BROADCAST_PACKET);
                    // ethernetTransmitQueue.Enqueue(tcpSequ);
                    ethCon.Value.ethernetTransmitQueue.Enqueue(message.Length);
                                       
                    for (int i = 0; i < message.Length; i++)
                    {
                        ethCon.Value.ethernetTransmitQueue.Enqueue(Convert.ToInt32(message[i]));
                                              
                        if (Convert.ToInt32(message[i]) == 0)
                        {
                        }

                    }
                }
                ethCon.Value.ethernetSendWaitHandle.Set();
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
                // var chName = children as DBConnect.TagBind;
                //string MAC = children.Header.ToString();
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
                messageWindow1 = new Message(ref allLists);
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
            allLists.allTagList.Clear();
        }



        private void DataGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

            int col = dataGridPullkey.CurrentCell.Column.DisplayIndex;
            string colName = dataGridPullkey.Columns[col].Header.ToString();

            if (colName == "remoteLockout")
            {
                               
                int row = dataGridPullkey.SelectedIndex;
                if (allLists.allTagList[row].remoteLockout == false)
                {
                    messageWindow1.addMessageToTxQueue(allLists.allTagList[row].zoneID, allLists.allTagList[row].unitID, 0x55, 0x01);
                }
                else
                {
                    messageWindow1.addMessageToTxQueue(allLists.allTagList[row].zoneID, allLists.allTagList[row].unitID, 0x55, 0x00);
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

    }
}
