using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO.Ports;
using System.Windows.Threading;
//using ComPort;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Threading;
using System.Collections;



namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Message : Window
    {

       
        public event SendDataHandler SendDataEvent;
        public delegate void SendDataHandler(byte[] message);
       // public Queue<Tag> rxMessageQueue = new Queue<Tag>();
        //public Queue<txMessage> txMessageQueue = new Queue<txMessage>();
       // DispatcherTimer timerMessage = new DispatcherTimer();
       private BindingList<RxMessageBind> _rxMessageList = new BindingList<RxMessageBind>();
       // public BackgroundWorker backgroundWorkerMessage = new BackgroundWorker();
        
         public AutoResetEvent rxMessageWaitHandle;
        public AutoResetEvent sendMessageWaitHandle;
        private Thread _rxMessages;
        private Thread _txMessage;
       
        
       
        public bool Do_Work;
        private Lists _allLists;
        public volatile bool shouldStopSendMessageThread;



        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }


        //public Message(ref Lists allLists)
        public Message(ref Lists allLists)
        {
            InitializeComponent();
            
            dataGridRxMessages.AutoGenerateColumns = true;
            dataGridRxMessages.ItemsSource = _rxMessageList;
            rxMessageWaitHandle = new AutoResetEvent(false);
            _allLists = allLists;

        
          //  timerMessage.Interval = new TimeSpan(0, 0, 1);
           
          //  timerMessage.Tick += new EventHandler(checkMessages);
          //  timerMessage.Start();

           // backgroundWorkerMessage.DoWork += BackGroundWorker_DoWork;
           //// backgroundWorkerMessage.RunWorkerCompleted += BackGroundWorker_RunWorkComplete;
           // backgroundWorkerMessage.WorkerSupportsCancellation = true;
           // backgroundWorkerMessage.RunWorkerAsync();
            sendMessageWaitHandle = new AutoResetEvent(false);
             
            _rxMessages = new Thread(new ThreadStart(getMessageThread));
           _rxMessages.Name = "rxMessages";

    
           _txMessage = new Thread(new ThreadStart(sendMessageThread));
  
            _txMessage.Name = "txMessage";

            _rxMessages.Start();
            _txMessage.Start();

          //  for (int i = 1; i <= 255; i++)
          //  {
          //      ComboItems.Add(string.Format("{0:x}", i));
          //  }

          //  zoneCombo.ItemsSource = ComboItems;
          //  unitCombo.ItemsSource = ComboItems;
            
            
        }



        public List<string> ComboItems = new List<string>();
 

        public void getMessageThread()   // ***********THREAD*****************
        {
            Tag message;
            int debugCount = _allLists.rxMessageQueue.Count;
            //  BindingList<RxMessageBind> rxMessageList = new BindingList<RxMessageBind>();
            //  dataGridRxMessages.ItemsSource = rxMessageList;

            Do_Work = true;

            while (Do_Work)
            {
                rxMessageWaitHandle.WaitOne(1000,true);
                //rxMessageWaitHandle.WaitOne();

                while (_allLists.rxMessageQueue.Count != 0)
                {
                    lock (_allLists.rxMessageQueue)
                    {
                        message = _allLists.rxMessageQueue.Dequeue();
                        TagMessageEvent(message);
                    }
                }


            }
            _rxMessages.Abort();

        }


        //private void checkMessages(object sender, EventArgs e)
        //{
        //    DBConnect.Tag2 message;
        //    int debugCount = rxMessageQueue.Count;
                
        //    lock (rxMessageQueue)
        //        {
        //            while (rxMessageQueue.Count != 0)
        //            {
                    
        //                    message = rxMessageQueue.Dequeue();

                   
        //                if (message.BrCmd == 0)
        //                {
        //                }

        //                TagMessageEvent(message);
        //            }
        //        }

        //}

        private void button1_Click(object sender, RoutedEventArgs e) //UniCast Button
        {


            //string sequ = "";
            //sequ += String.Format("{0:x2}", (uint)System.Convert.ToUInt32(textBox3.Text.ToString()));
            //string message = "U -s " + sequ.ToUpper() + " -f 7E -t " + textBox1.Text + " -m " + textBox2.Text + '\0';
            ////sendData(message);
           
            ////SendDataEvent(message); commented out when we went to Cobs

            //incrementSequence();
        }

        private void button2_Click(object sender, RoutedEventArgs e) //Broadcas Button
        {
            //string message = constructMessage();

            //SendData(message);
            // endDataEvent(constructMessage("FF",textBox2.Text)); cmmented out wen we went to cobs

            //incrementSequence();
        }

        private void incrementSequence()
        {
            int sequInt = Convert.ToInt16(textBox3.Text) + 1;
            if (sequInt == 255) { sequInt = 1; }
            textBox3.Text = Convert.ToString(sequInt);
        }


        private enum Jennic
        {
            TXOPTION_ACKREQU = 0X01,
            TXOPTION_BDCAST = 0X04,
           // TXOPTION_SILENT = 0X08
        }


        private string constructMessage(string flag, string mess)
        {
            string sequ=null;
            
            sequ += String.Format("{0:x2}", (uint)System.Convert.ToUInt32(textBox3.Text.ToString()));

            sequ = sequ.ToUpper();
            flag = flag.ToUpper();

            string message = "B -s " + sequ + " -f " + flag + "-m " + mess + "\0";
            return message;
        }

        // before parsing removed from coord and router
        //private string constructMessage(int flag1, int flag2)
        //{
        //    string sequ = null;

        //    sequ += String.Format("{0:x2}", (uint)System.Convert.ToUInt32(textBox3.Text.ToString()));

        //    sequ = sequ.ToUpper();
        //   // flag = flag.ToUpper();

        //    //string smess1 = Char.ConvertFromUtf32(mess1);   //String.Format("{0:X2}", mess1);
        //   // string smess2 = Char.ConvertFromUtf32(mess2); //String.Format("{0:X2}", mess2);
        //    //string sflag1 = Char.ConvertFromUtf32(flag1); //String.Format("{0:X2}", flag);
        //   // string sflag2 = Char.ConvertFromUtf32(flag2); //String.Format("{0:X2}", flag);
            
        //    //string sflag2 = String.Format("{0:X2}", flag2);
        //    string sflag2 = Char.ConvertFromUtf32(flag2);

        //   //old message format
        //    // string message = "B -s " + sequ + " -f " + sflag1 + sflag2 + " -m " + smess1 + " " + smess2 + "\0";

        //    //string sflag1 = Char.ConvertFromUtf32(0x01);
        //    string sflag1 = String.Format("{0:x2}", 0x01);
                       
        //    string MAC = textBox1.Text;
        //    MAC = MAC.Insert(8, ":");
        //    //textBox1.Text = MAC.Insert(8, ":");
        //    //string message = "U -s " + sequ + " -f " + sflag1 + " -t " + MAC + " -m " + "-h " +"0"+ sflag2 + "\0";
        //    string message = "U -s " + sequ + " -f " + "0" + sflag2 + " -j " + "0" + "1" + " -t " + MAC + " -m " + "0" + sflag2 +"\0";
            
        //    return message;
        //}

        //private string constructMessage(byte flag)
        
        public static byte[] constructMessage(byte flag, string MAC, byte u8Sequence)
        {
           

           // string MAC = textBox1.Text;
           
            //char cType = 'U';
            
            byte u8HwFlags = flag;
            byte u8JenNetFlags =0x01;
            UInt64 u64DstMacAdd = System.Convert.ToUInt64(MAC,16);
            byte u8MsgLen = 0x2;
            char[] cMessage = new char[u8MsgLen];
            cMessage[0] = '0';
            cMessage[1] = '1';



            byte[] bMac = BitConverter.GetBytes(u64DstMacAdd);
            char[] cMac = new char[8];
            
            for (int i = 0; i < 8; i++)
            {
                cMac[i] = (char)bMac[7-i];
            }


 
            byte[] bMessage = new byte[] { 0x55, //U
                                           u8Sequence, 
                                           u8HwFlags, 
                                           u8JenNetFlags, 
                                           bMac[7], bMac[6], bMac[5], bMac[4], bMac[3], bMac[2], bMac[1], bMac[0], 
                                           u8MsgLen,
                                           0x30, 0x31
                                           };

            byte[] cobsMessage = COBSCodec.encode(bMessage);

            //add /n to end of byte array
            byte[] cobsMessageAndNull = new byte[cobsMessage.Length + 1];
            cobsMessage.CopyTo(cobsMessageAndNull, 0);
            cobsMessageAndNull[cobsMessageAndNull.Length-1] = 0;



            return cobsMessageAndNull;
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
               // SendDataEvent(constructMessage("FF", textBox2.Text));

               // incrementSequence();
            }
        }


        public void TagMessageEvent(Tag message)
        {

            //int indexTR = rxMessageList.IndexOf(searchResultTR); // get an index to update from myTagReaderList


            System.Windows.Application.Current.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)delegate()
                {
                    _rxMessageList.Add(new RxMessageBind(message));
                });
            
            
            
            
            
            //string decodeMessage ="";

            ////throw new NotImplementedException();
            //switch (message.BrCmd)
            //{
            //    case 1:
            //        decodeMessage = "Ack";
            //        break;
            //    case 2:
            //        decodeMessage = "Nack";
            //        break;
            //    case 3:
            //        decodeMessage = "Nack-Ack";
            //        break;
            //    case 4:
            //        decodeMessage = "Working";
            //        break;
            //    case 5:
            //        decodeMessage = "Moving";
            //        break;
            //    case 6:
            //        decodeMessage = "Sleeping";
            //        break;
            //    case 7:
            //        decodeMessage = "Striking";
            //        break;
            //    case 8:
            //        decodeMessage = "Dumping";
            //        break;
            //    case 9:
            //        decodeMessage = "Troosi";
            //        break;
            //    case 10:
            //        decodeMessage = "End";
            //        break;
            //}

            //textRxMessage.Text += decodeMessage + " " + message.TagAdd + "\n";


        }

        class RxMessageBind : INotifyPropertyChanged
        {

            private DateTime _timestamp;
            private string _message;
            private int _PktSequence;
            private int _PktEvent;
            private int _PktTemp;
            private int _BrSequ;
            private int _BrCmd;
            private string _TagAdd;
            private CheckBox _Del;
            
            


            public event PropertyChangedEventHandler PropertyChanged;



            public RxMessageBind(Tag TagIn)
            {//Button delButton = new Button();


               // CheckBox delCheck = new CheckBox();
                //delCheck.Content = "del";

                _message = getMessage(TagIn.BrCmd);

                _PktSequence = TagIn.PktSequence;
                _PktEvent = TagIn.PktEvent;
                _PktTemp = TagIn.PktTemp;
                _BrSequ = TagIn.BrSequ;
                _BrCmd = TagIn.BrCmd;
               _TagAdd = TagIn.TagAdd;
               _Del = delCheck;


                //    TTL = 10,

            }


            public CheckBox delCheck
            {
                get { return _Del;}
                //set
                //{
                //    _Del = value;
                //    this.NotifyPropertyChanged("delCheck");
                //}
            }



            public DateTime timeStamp
            {
                get { return DateTime.Now; }
                set { }
            }
            
            public string message
            {
                get { return _message; }
                set
                {
                    _message = value;
                    _timestamp = DateTime.Now;
                    this.NotifyPropertyChanged("message");
                    this.NotifyPropertyChanged("timeStamp");
                }
            }

            public int PktSequence
            {
                get { return _PktSequence; }
                set
                {
                    _PktSequence = value;
                    this.NotifyPropertyChanged("PktSequence");
                }
            }
            public int PktEvent
            {
                get { return _PktEvent; }
                set
                {
                    _PktEvent = value;
                    this.NotifyPropertyChanged("PktEvent");
                }
            }
            public int PktTemp
            {
                get { return _PktTemp; }
                set
                {
                    _PktTemp = value;
                    this.NotifyPropertyChanged("PktTemp");
                }
            }
            public int BrSequ
            {
                get { return _BrSequ; }
                set
                {
                    _BrSequ = value;
                    this.NotifyPropertyChanged("BrSequ");
                }
            }
            public int BrCmd
            {
                get { return _BrCmd; }
                set
                {
                    _BrCmd = value;
                    this.NotifyPropertyChanged("BrCmd");
                }
            }
           

            public string TagAdd
            {
                get { return _TagAdd; }
                set
                {
                    _TagAdd = value;
                    this.NotifyPropertyChanged("TagAdd");
                }
            }

          


            public string getMessage(int BrCmd)
            {
                
                    switch (BrCmd)
                    {
                        case 0:
                            return "BCmd 0";
                        case 1:
                            return "Ack";
                            
                        case 2:
                            return "Nack";
                            
                        case 3:
                            return "Nack-Ack";
                            
                        case 4:
                            return "Working";
                            
                        case 5:
                            return "Moving";
                            
                        case 6:
                            return "Sleeping";
                            
                        case 7:
                            return "Striking";
                            
                        case 8:
                            return "Dumping";
                            
                        case 9:
                            return "Troosi";
                          
                        case 10:
                            return "End";

                        case 0xef:
                            return "Router Left Network";

                        case 0xf0:
                            return "Router Joined Network";
                        default:
                            return "?";
                            

                    } }

            private void NotifyPropertyChanged(string name)
            {


                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(name));


            }

        }

        private void dataGridRxMessages_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void dataGridRxMessages_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var cell = sender as DataGridColumn;


           // var col = cell.Column;

            //            int noSelected = cell.SelectedCells.Count;

          //  var selectedCells = cell;
        }

        

        private void sendMessageThread() //************THREAD********************
        {
            try
            {
                while (!shouldStopSendMessageThread)
                {
                    sendMessageWaitHandle.WaitOne(); // blocks thread untill signall is recived 
                    while (_allLists.txMessageQueue.Count != 0)
                    {
                        txMessage toSend = _allLists.txMessageQueue.Dequeue();

                        foreach (var key in _allLists.allTagList.ToList())
                        {
                            if (key.zoneID == toSend.ZoneID)
                            {
                                if (key.unitID == toSend.UnitID)
                                {
                                    //byte u8Sequence = (byte)System.Convert.ToByte(textBox3.Text.ToString());
                                    SendDataEvent(constructMessage(toSend.flag, key.TagAdd, toSend.u8Sequence));

                                    //while (ackBack(toSend.u8Sequence) == false)
                                    //{
                                    //    SendDataEvent(constructMessage(toSend.flag, key.TagAdd, toSend.u8Sequence));
                                    //    MessageBox.Show("Warning:no ack back");


                                    //}

                                    if (ackBack(toSend.u8Sequence) == false)
                                    {
                                        MessageBox.Show("Warning:no ack back");
                                    }
                                    else
                                    {
                                    }


                                    // incrementSequence();


                                }
                            }
                        }
                    }

                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }


        public void addMessageToTxQueue(string zone, string unit, byte sequ, byte flag)
        {
            txMessage messageToQueue = new txMessage();
            messageToQueue.ZoneID = zone;
            messageToQueue.UnitID = unit;
            messageToQueue.u8Sequence = sequ;
            messageToQueue.flag = flag;
            
            _allLists.txMessageQueue.Enqueue(messageToQueue);

            sendMessageWaitHandle.Set();
            incrementSequence();
        }

        private void LockOutButton_Click(object sender, RoutedEventArgs e)
        {   
            addMessageToTxQueue(zoneCombo.Text, unitCombo.Text, (byte)System.Convert.ToByte(textBox3.Text.ToString()), 0x01);
        }

        private bool ackBack(int txSequ)
        {
            int rxBrSequ = 0;
            if (_allLists.brSequReciveQueue.Count != 0)
            {
                rxBrSequ = _allLists.brSequReciveQueue.Dequeue();
            }

            Thread.Sleep(100);
            //  while (rxBrSequ != txSequ)
            //  {
            int i = 0;
            for (i = 0; i < 100; i++)
            {
                if (rxBrSequ != 0)
                {
                    _allLists.brSequReciveQueue.Enqueue(rxBrSequ); // not this one put it back
                }
                if (_allLists.brSequReciveQueue.Count != 0)
                {
                    rxBrSequ = _allLists.brSequReciveQueue.Dequeue();
                    if (rxBrSequ == txSequ)
                    {
                        return true;
                    }

                }
                Thread.Sleep(100);
            }
            return false;
            // }
        }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            addMessageToTxQueue(zoneCombo.Text, unitCombo.Text, (byte)System.Convert.ToByte(textBox3.Text.ToString()), 0x00);
        }

        private void LockOutAllButton_Click(object sender, RoutedEventArgs e)
        { 
           
            if (_allLists != null)
            {
                foreach (var key in _allLists.allTagList)
                {
                    if (key.endPointType == "Key")
                    {
                        //txMessage messageToQueue = new txMessage();

                        //messageToQueue.ZoneID = key.zoneID;
                        //messageToQueue.UnitID = key.unitID;
                        //messageToQueue.u8Sequence = (byte)System.Convert.ToByte(textBox3.Text.ToString());
                        //messageToQueue.flag = 0x01;

                        addMessageToTxQueue(key.zoneID, key.unitID, (byte)System.Convert.ToByte(textBox3.Text.ToString()), 0x01);

                        //allListsRef.txMessageQueue.Enqueue(messageToQueue);
                        //sendMessageWaitHandle.Set();
                        //incrementSequence();
                    }
                }     
            }   
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_allLists != null)
            {
                foreach (var key in _allLists.allTagList)
                {
                    if (key.endPointType == "Key")
                    {
                        //txMessage messageToQueue = new txMessage();

                        //messageToQueue.ZoneID = key.zoneID;
                        //messageToQueue.UnitID = key.unitID;
                        //messageToQueue.u8Sequence = (byte)System.Convert.ToByte(textBox3.Text.ToString());
                        //messageToQueue.flag = 0x00;

                        addMessageToTxQueue(key.zoneID, key.unitID, (byte)System.Convert.ToByte(textBox3.Text.ToString()), 0x00);

                        //allListsRef.txMessageQueue.Enqueue(messageToQueue);
                        //sendMessageWaitHandle.Set();
                        //incrementSequence();
                    }
                }
            } 
        }

        private void zoneCombo_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            foreach (var keys in _allLists.allTagList)
            {
               // ComboItems.Add(string.Format("{0:x2}", keys.zoneID));

            }
        }
    }

    public class txMessage
    {
       public string ZoneID;
        public string UnitID;
       public byte u8Sequence;
       public byte flag;

           
    }

}
