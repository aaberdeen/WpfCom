using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.IO.Ports;
using System.Net.Sockets;
using System.ComponentModel;


namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for ComSetup.xaml
    /// </summary>
    public partial class ComSetup : Window
    {
       // public BindingList<coodData> coordIpList = new BindingList<coodData>(); 

        //public int BaudRate { get; set; }
        //public int DataBits { get; set; }
        //public string PortName { get; set; }  
        public SerialPort comport = new SerialPort();
        public event StopHandler Stop;
       
        public delegate void SendDataHandler(byte[] message, int length);
        //public event EthernetConnect ConnectEvent;

        //public EventArgs e = null;
        public delegate void StopHandler();
        public delegate void EthernetConnect(string server, string port);

        /* NetworkStream that will be used */
        private static NetworkStream _myStream;
        /* TcpClient that will connect for us */
        private static TcpClient _myClient;
        /* Storage space */
        public static byte[] _myBuffer;
        /* Application running flag */
         private static bool _bActive = true;


            
        public ComSetup()
        {
            InitializeComponent();
            PopulateComboBox();
            SetComboBoxDefault();
            createCoordTable();

            CoodTable.ItemsSource = EthernetConnection.allLists.coordinators;


        }

     
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        public void SetComboBoxDefault()
        {
           comboBoxPort.SelectedIndex = comboBoxPort.Items.IndexOf(Properties.Settings.Default.PortName);
         //  textBox1.Text = Properties.Settings.Default.CoordServer;
         //  textBox2.Text = Properties.Settings.Default.CoordPort;
            comboBoxBaud.SelectedIndex = 11;
            comboBoxDataBits.SelectedIndex = 1;
            comboBoxParity.SelectedIndex = 2;
            comboBoxStop.SelectedIndex = 0;
        }

        private void PopulateComboBox()
        {
            comboBoxBaud.Items.Add("110");
            comboBoxBaud.Items.Add("300");
            comboBoxBaud.Items.Add("600");
            comboBoxBaud.Items.Add("1200");
            comboBoxBaud.Items.Add("2400");
            comboBoxBaud.Items.Add("4800");
            comboBoxBaud.Items.Add("9600");
            comboBoxBaud.Items.Add("14400");
            comboBoxBaud.Items.Add("19200");
            comboBoxBaud.Items.Add("38400");
            comboBoxBaud.Items.Add("57600");
            comboBoxBaud.Items.Add("115200");
            comboBoxBaud.Items.Add("230400");
            comboBoxBaud.Items.Add("460800");
            comboBoxBaud.Items.Add("921600");

            comboBoxDataBits.Items.Add("7");
            comboBoxDataBits.Items.Add("8");

            comboBoxParity.Items.Add("even");
            comboBoxParity.Items.Add("odd");
            comboBoxParity.Items.Add("none");

            comboBoxStop.Items.Add("1");
            comboBoxStop.Items.Add("2");

            foreach (string s in SerialPort.GetPortNames())
                comboBoxPort.Items.Add(s);
        }

        private void comboBoxPort_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            comboBoxPort.Items.Clear();
            foreach (string s in SerialPort.GetPortNames())
                comboBoxPort.Items.Add(s);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
                //// Set the port's settings
                //comport.BaudRate = int.Parse(comboBoxBaud.Text);
                //comport.DataBits = int.Parse(comboBoxDataBits.Text);
                //comport.StopBits = StopBits.One;
                //comport.Parity = Parity.None;
                //comport.PortName = comboBoxPort.Text;
           
            // Set the port's settings
            comport.BaudRate = int.Parse(comboBoxBaud.Text); // 115200;
            Properties.Settings.Default.BaudRate = comport.BaudRate;
            comport.DataBits = int.Parse(comboBoxDataBits.Text); //8
            Properties.Settings.Default.DataBits = comport.DataBits;
            comport.StopBits = StopBits.One;
            comport.Parity = Parity.None;
            comport.PortName = comboBoxPort.Text;
            Properties.Settings.Default.PortName = comport.PortName;
            Properties.Settings.Default.Save();

           // openComm();
            this.Hide();

        }

        public void openComm()
        {
            bool error = false;

            // If the port is open, close it.
            if (comport.IsOpen) comport.Close();
            else
            {
                comport.BaudRate = Properties.Settings.Default.BaudRate;
                comport.DataBits = Properties.Settings.Default.DataBits;
                comport.StopBits = StopBits.One;
                comport.Parity = Parity.None;
                comport.PortName = Properties.Settings.Default.PortName;
               

                try
                {
                    // Open the port
                    comport.Open();
                    if (comport.IsOpen)
                    {
                        stackPanelComms.IsEnabled = false;
                        buttonDisconnect.IsEnabled = true;

                    }
                }
                catch (UnauthorizedAccessException) { error = true; }
                //catch (IOException) { error = true; }
                catch (ArgumentException) { error = true; }

                if (error) MessageBox.Show(this, "Could not open the COM port.  Most likely it is already in use, has been removed, or is unavailable.", "COM Port Unavalible"); //, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                else
                {

                }
            }
        }

        public int get_comms()
        {
            // If the com port has been closed, message box, return -1;
            if (!comport.IsOpen)
            {
                MessageBox.Show("comport not open");
                return -2;
            }
            else
            {
                try
                {
                    int read = comport.ReadByte();
                    return read;      //returns -1 if end of stream has been read
                }
                catch
                {
                    return -1;
                }
            }

        }

        public void SendData(string data)
        {
            // if (CurrentDataMode == DataMode.Text)
            // {
            // Send the user's text straight out the port
            try
            {
                comport.Write(data);
            }
            catch (Exception)
            {
              //  Log(LogMsgType.Error, "Send problem: Is com port selected? " + "\n");
            }

           // txtSendData.SelectAll();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            comport.Close();
            stackPanelComms.IsEnabled = true;
            buttonDisconnect.IsEnabled = false;
           
            //event to stop main form
            
            Stop();
            
        }

        //private void button2_Click_1(object sender, RoutedEventArgs e)
        //{
        //  //  button2.IsEnabled = false;
            
        //    Properties.Settings.Default.CoordServer = coordIpList[0].IP;
        //    Properties.Settings.Default.CoordPort = coordIpList[0].port;
        //    Properties.Settings.Default.Save();

        //    ConnectEvent(coordIpList[0].IP, coordIpList[0].port);
            
        //}





        private void regSession()
        {
            //Console.WriteLine("Sending...");

            //richTextBox1.Invoke(new EventHandler(delegate
            //{
            //    richTextBox1.AppendText("Sending...");

            //}));



            /* Simple prompt */
            Console.Write("> ");
            /* Reading message/command from console */
            //                String myString = Console.ReadLine() + "\n";
            //send CIP frame
            //String myString = "test";

            byte[] data2 = { 0x65, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 
                                 0x00, 0x00 };

            /* Sending the data */
            //myStream.Write(Encoding.ASCII.GetBytes(myString.ToCharArray()), 0, myString.Length);
            _myStream.Write(data2, 0, data2.Length);
        }

        /* Thread responsible for "remote input" */
        public void ListenThread()
        {
            //Console.WriteLine("Listening...");

            //richTextBox1.Invoke(new EventHandler(delegate
            //{
            //    richTextBox1.AppendText("Listening...");

            //}));


            while (_bActive)
            {
                /* Reading data from socket (stores the length of data) */
                int lData = _myStream.Read(_myBuffer, 0, _myClient.ReceiveBufferSize);
                /* String conversion (to be displayed on console) */
                String myString = Encoding.ASCII.GetString(_myBuffer);

                //for (int i = 0; i < lData; i++)
                //{
                //    //myString = string.Format("{0:X}", myBuffer[i]);

                //    int data = myBuffer[i];
                //    SendDataEvent(data);
                //}

                //SendDataEvent(myBuffer, lData);
                

                /* Trimming data to needed length, 
                   because TcpClient buffer is 8kb long */
                /* and we don't need that load of data 
                   to be displayed at all times */
                /* (this could be done better for sure) */
                myString = myString.Substring(0, lData);

                
                /* Display message   */
                //Console.Write(myString);

                //richTextBox1.Invoke(new EventHandler(delegate
                //{
                //    richTextBox1.AppendText(myString);

                //}));


                }





                

            

        }

        /* Thread responsible for "local input" */
        private void SendThread()
        {
            while (_bActive)
            {       
                byte[] data2 = { 0x6f, 0x00, 0x18, 0x00, 0x00, 0x00, 
                                 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                                 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0xb2, 0x00, 0x08, 0x00, 0x0e, 0x03, 
                                 0x20, 0x90, 0x24, 0x01, 0x30, 0x01 };


                /* Sending the data */
                //myStream.Write(Encoding.ASCII.GetBytes(myString.ToCharArray()), 0, myString.Length);
                _myStream.Write(data2, 0, data2.Length);
                            
            }



        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            //Properties.Settings.Default.CoordServer = coordIpList[0].IP;
            //Properties.Settings.Default.CoordPort = coordIpList[0].port;
            //Properties.Settings.Default.Save();
            List<Coordinators> toRemove = new List<Coordinators>();
            foreach (Coordinators entry in EthernetConnection.allLists.coordinators)
            {
                
                if (Usefull.ValidIP(entry.IP) == false)
                {
                    toRemove.Add(entry);
                    //coordIpList.Remove(entry);
                }

           
            }

            foreach (Coordinators a in toRemove)
            {
                EthernetConnection.allLists.coordinators.Remove(a);
            }

            this.Hide();
        }



        public void createCoordTable()
        {
            
           // coordIpList.Add(new coodData(Properties.Settings.Default.CoordServer, Properties.Settings.Default.CoordPort, false,1));    

        }

        private void button4_Click(object sender, RoutedEventArgs e)
        {
           int getIndex = 0;

           foreach (Coordinators data in EthernetConnection.allLists.coordinators)
            {
                if (data.Index > getIndex)
                {
                    getIndex = data.Index;
                }
            }
            string udpPort = Convert.ToString(4444 + getIndex);
            EthernetConnection.allLists.coordinators.Add(new Coordinators("xxx.xxx.xxx.xxx", "40", false, (getIndex + 1), "xxx.xxx.xxx.xxx", udpPort));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Coordinators selected = (Coordinators)CoodTable.SelectedItem;
            int selectedIndex = CoodTable.SelectedIndex;

            if(selected!=null)
            {
                EthernetConnection.allLists.coordinators.Remove(selected);

            if (selectedIndex+1 > CoodTable.Items.Count)
            {
                CoodTable.SelectedIndex = selectedIndex-1;
            }
            else
            {
                
                CoodTable.SelectedIndex = selectedIndex;
                
            }
            }

            
        }




    }
    public class Coordinators : INotifyPropertyChanged
    {
        private int _Index;
        private string _IP;
        private string _localIP;
        private string _udpPort;
        private string _TCPport;
        private bool _connected;
        private int _reCons;
        private TcpClient _tcpClient;

        public event PropertyChangedEventHandler PropertyChanged;

        public Coordinators(string ipIn, string portIn, bool connectedIn,int index, string localIPin, string udpPortIn)
        {
            this.IP = ipIn;
            this.TCPport = portIn;
            this.connected = connectedIn;
            this.Index = index;
            this.localIP = localIPin;
            this.udpPort = udpPortIn;
        }


        

        public int Index
        {
            get { return _Index; }
            set
            {
                _Index = value;
                this.NotifyPropertyChanged("Index");
            }
        }

        public string IP
        {
            get { return _IP; }
            set
            {
                _IP = value;
                this.NotifyPropertyChanged("IP");
            }
        }

        public string localIP
        {
            get { return _localIP; }
            set
            {
                _localIP = value;
                this.NotifyPropertyChanged("localIP");
            }
        }

        public string udpPort
        {
            get { return _udpPort; }
            set
            {
                _udpPort = value;
                this.NotifyPropertyChanged("udpPort");
            }
        }

        public string TCPport
        {
            get { return _TCPport; }
            set
            {
                _TCPport = value;
                this.NotifyPropertyChanged("port");
            }
        }

        public bool connected
        {
            get { return _connected; }
            set
            {
                if (value == true)
                {
                    _reCons = _reCons + 1;
                    this.NotifyPropertyChanged("reCons");
                }
                _connected = value;
                this.NotifyPropertyChanged("connected");
            }
        }

        //public TcpClient tcpClient
        //{
        //    set
        //    {
        //        _tcpClient = value;
        //        this.NotifyPropertyChanged("tcpClient");
        //        this.NotifyPropertyChanged("tcpConnected");
        //    }
        //}

        //public bool tcpConnected
        //{
        //    //get 
        //    //{
        //    // //   return _tcpClient.Connected;
        //    //}
        //}

        public int reCons
        {
            get { return _reCons; }
            set
            {
                _reCons = value;
                this.NotifyPropertyChanged("reCons");
            }

        }



        private void NotifyPropertyChanged(string name)
        {


            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));


        }

    }
}
