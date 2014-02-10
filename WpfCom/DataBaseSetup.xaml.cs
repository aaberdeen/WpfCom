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
using ComPort;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for DataBaseSetup.xaml
    /// </summary>
    public partial class DataBaseSetup : Window
    {
        public bool trackingEnabled;
        public bool historyEnabled;
        public string server;
        public string port;
        public string database;
        public string uid;
        public string password;

        public DataBaseSetup()
        {
            InitializeComponent();
            textBoxServer.Text = Properties.Settings.Default.Server;
            textBoxPort.Text = Properties.Settings.Default.Port;
            textBoxDataBase.Text = Properties.Settings.Default.Database;
            textBoxUID.Text = Properties.Settings.Default.UID;
            textBoxPassword.Text = Properties.Settings.Default.Password;
            checkBoxTracking.IsChecked = Properties.Settings.Default.EnableTracking;

        }
      
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void checkBoxTracking_Checked(object sender, RoutedEventArgs e)
        {
            trackingEnabled = true;
        }

        private void checkBoxHistory_Checked(object sender, RoutedEventArgs e)
        {
            historyEnabled = true;
        }

        private void checkBoxTracking_Unchecked(object sender, RoutedEventArgs e)
        {
            trackingEnabled = false;
        }

        private void checkBoxHistory_Unchecked(object sender, RoutedEventArgs e)
        {
            historyEnabled = false;
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            server = textBoxServer.Text;
            Properties.Settings.Default.Server = server;
            port = textBoxPort.Text;
            Properties.Settings.Default.Port = port;
            database = textBoxDataBase.Text;
            Properties.Settings.Default.Database = database;
            uid = textBoxUID.Text;
            Properties.Settings.Default.UID = uid;


            if (textBoxPassword.Text == "")
            {
                password = null;
            }
            else
            {
                password = textBoxPassword.Text;
            }

            Properties.Settings.Default.Password = password;
            Properties.Settings.Default.EnableTracking = trackingEnabled;

            Properties.Settings.Default.Save();
            this.Hide();

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            DBConnect tempConnect;
            string password = null;
            
            if (textBoxPassword.Text == "")
            {
                password = null;
            }
            else
            {
                password = textBoxPassword.Text;
            }

            tempConnect = new DBConnect(textBoxServer.Text,
                                        textBoxPort.Text,
                                           "",
                                           textBoxUID.Text,
                                           password); //initialises new db connection

            tempConnect.exicuteScript("wpanSchema.sql");

            tempConnect = new DBConnect(textBoxServer.Text,
                                        textBoxPort.Text,
                                           "wpandb",
                                           textBoxUID.Text,
                                           password); //initialises new db connection
            tempConnect.exicuteScript("wpanTable.sql");
        }
    }
}
