using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using WiPANFactory;
using System.IO;

namespace WpfApplication1
{
    public partial class CoordSetup : Form
    {

        /* NetworkStream that will be used */
        private static NetworkStream _myStream;
        /* TcpClient that will connect for us */
        private static TcpClient _myClient;
        /* Storage space */
        private static byte[] _myBuffer;
        public CoordSetup()
        {
            InitializeComponent();
        }

        private void CoordSetup_Load(object sender, EventArgs e)
        {

        }

        private void button_Connect_Click(object sender, EventArgs e)
        {

           

            String strServer = textBox_IP.Text;  //Console.ReadLine();
            String strPort = textBox_Port.Text;  //Console.ReadLine();

            /* Connecting to server (will crash if address/name is incorrect) */

            try
            {

                if (comboBox_product.Text != "Flex Display")
                {
                    textBox_Inv.Text = "";

                    _myClient = new TcpClient(strServer, Int32.Parse(strPort));


                    textBox3.Text = "connected"; //Console.WriteLine("Connected...");
                    /* Store the NetworkStream */
                    _myStream = _myClient.GetStream();
                    /* Create data buffer */
                    _myBuffer = new byte[_myClient.ReceiveBufferSize];

                    getINV();
                }
              //  checkMACstock();

             //   string checkResult = checkDBforUID();
                //if (checkResult == null)
                //{
                //    getNewMac();
                //}
                //else
                //{
                //    textBox_MAC.Text = checkResult.ToUpper();
                //}

                textBox_MacArp.Text = arpIP(textBox_IP.Text).ToUpper();

                if (textBox_MAC.Text != textBox_MacArp.Text)
                {
                    textBox_MacArp.BackColor = Color.Red;
                }
                else
                {
                    textBox_MacArp.BackColor = Color.White;
                }


            }
            catch (System.Net.Sockets.SocketException se)
            {
                richTextBox1.AppendText(string.Format("\nconnection failed {0}", se.ToString()));
                richTextBox1.AppendText("\n");
            }
            catch(Exception exception)
            {
                richTextBox1.AppendText(string.Format("\nno inv returned {0}", exception.ToString()));
                richTextBox1.AppendText("\n");
            }



        }
        public void getINV()
        {
            byte[] data2 = WipanCmd.getINV();

            /* Sending the data */
            _myStream.Write(data2, 0, data2.Length);
            richTextBox1.AppendText("\nget INV sent");

            //wait for reply with time out
            _myStream.ReadTimeout = 1000;
            int lData = _myStream.Read(_myBuffer, 0, _myClient.ReceiveBufferSize);
            textBox_Inv.Text = string.Format("{0:x}{1:x}{2:x}{3:x}{4:x}{5:x}{6:x}{7:x}{8:x}{9:x}{10:x}{11:x}", _myBuffer[0], _myBuffer[1], _myBuffer[2], _myBuffer[3], _myBuffer[4], _myBuffer[5], _myBuffer[6], _myBuffer[7], _myBuffer[8], _myBuffer[9], _myBuffer[10], _myBuffer[11]);

        }
        private string arpIP(string IP)
        {
            return GetMac.GetMacAddress(IP);
        }

        private void button_Disconnect_Click(object sender, EventArgs e)
        {
            _myStream.Dispose();
            _myClient.Close();


            textBox3.Text = "disconnected";
        }

        private void UDPstartButton_Click(object sender, EventArgs e)
        {
            byte[] data2 = WipanCmd.UDPstart(textBoxUDPSess.Text,textBoxUDPaddress.Text, textBoxUDPport.Text);
            if (_myClient != null)
            {
                if (_myClient.Connected)
                {
                    try
                    {
                        _myStream.Write(data2, 0, data2.Length);
                        richTextBox1.AppendText(string.Format("\nUDP Start Sent:{0:X2},{1:X2},{2:X2},{3:X2},{4:X2},{5:X2},{6:X2},{7:X2},{8:X2},{9:X2}", data2[0], data2[1], data2[2], data2[3], data2[4], data2[5], data2[6], data2[7], data2[8], data2[9]));
                    }
                    catch(Exception ex)
                    {
                        richTextBox1.AppendText(ex.ToString());

                    }
                }
                else
                {
                    richTextBox1.AppendText("\nnot Connected");
                }
            }
            else
            {
                richTextBox1.AppendText("\nnot Connected");
            }
        }

        private void UDPstopButton_Click(object sender, EventArgs e)
        {
            byte[] data2 = WipanCmd.UDPstop(textBoxUDPSess.Text, textBoxUDPaddress.Text, textBoxUDPport.Text);
            if (_myClient != null)
            {
                if (_myClient.Connected)
                {
                    try
                    {
                        _myStream.Write(data2, 0, data2.Length);
                        richTextBox1.AppendText(string.Format("\nUDP Stop Sent:{0:X2},{1:X2},{2:X2},{3:X2},{4:X2},{5:X2},{6:X2},{7:X2},{8:X2},{9:X2}", data2[0], data2[1], data2[2], data2[3], data2[4], data2[5], data2[6], data2[7], data2[8], data2[9]));
                    }
                    catch (Exception ex)
                    {
                        richTextBox1.AppendText(ex.ToString());
                    }
                }
                else
                {
                    richTextBox1.AppendText("\nnot Connected");
                }
            }
            else
            {
                richTextBox1.AppendText("\nnot Connected");
            }
        }

        private static void errorLog(SocketException e, string errorCode)
        {
            using (StreamWriter w = File.AppendText("debugLog.txt"))
            {
                w.WriteLine(DateTime.Now);
                w.WriteLine("{0}", errorCode);
                w.WriteLine("{0}", e.ToString());
            }
        }

        private void textBoxUDPaddress_TextChanged(object sender, EventArgs e)
        {

        }

        private void button_sendMac_Click(object sender, EventArgs e)
        {

        }

     
    }
}
