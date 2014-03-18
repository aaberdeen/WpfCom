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
using System.ComponentModel;


namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MinersNamesForm : Window
    { 
        public MinersNamesForm()
        {
            InitializeComponent();
            namesGrd.ItemsSource = EthernetConnection.allLists.endPoints;
        }
        /// <summary>
        /// Adds a MAC to the list of Miners names. 
        /// If the MAC as already there returns minersName and endPointType in a string array
        /// </summary>
        /// <param name="mac"></param>
        /// <returns>name of miner, endPointType</returns>
        public string[] addMacToEndpointList(string mac)
        {
            var test = EthernetConnection.allLists.endPoints.ToList().FirstOrDefault(item => item.endpointMAC == mac);
            string[] returnStrings = new string[2];
             if (test !=null)
            {
                // if it is in the list send back the name
                returnStrings[0] = test.endpointName;
                returnStrings[1] = test.endPointType.ToString();
                test.timeSeen = ""; 
                return returnStrings;
            }
            else
            {
            System.Windows.Application.Current.Dispatcher.Invoke(
                                System.Windows.Threading.DispatcherPriority.Normal,
                                (Action)delegate()
                                {
                                    add(mac, "", EndPoints.endPointTypes.Man);
                                });
            return returnStrings;
            }
        }

        private void closeButtonClick(object sender, RoutedEventArgs e)
        {   
            this.Hide();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
        private void addNewButtonClick(object sender, RoutedEventArgs e)
        {
        add("", "", EndPoints.endPointTypes.Man);
        }
        private void add(string macToAdd, string nameToAdd, EndPoints.endPointTypes typeToAdd)
        {
            EthernetConnection.allLists.endPoints.Add(new EndPoints(macToAdd, nameToAdd, typeToAdd));
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            this.Hide();
        }
    }

    public class EndPoints : INotifyPropertyChanged
    {
        private string _endpointMAC;
        private string _endpointName;
        private int _endpointNumber;
        private DateTime _timeSeen;
        //private short _endPointType;
        public enum endPointTypes
        {
            Man,
            Gas,
            Key,
            Router,
            Coord
        };
        private endPointTypes _endPointType;
        public event PropertyChangedEventHandler PropertyChanged;
        public EndPoints(string mac, string name, string endType)
        {
            this.endpointMAC = mac;
            this.endpointName = name;
            this.endPointType = this.getEndPointType(endType);
        }
        public EndPoints(string mac, string name, endPointTypes endType)
        {
            this.endpointMAC = mac;
            this.endpointName = name;
            this.endPointType = endType;
        }
        public string endpointMAC
        {
            get { return _endpointMAC; }
            set
            {
                _endpointMAC = value;
                this.NotifyPropertyChanged("endpointMAC");
            }
        }
        public string endpointName
        {
            get { return _endpointName; }
            set
            {
                _endpointName = value;
                this.NotifyPropertyChanged("endpointName");
            }
        }
        public endPointTypes endPointType
        {
            get
            {
                { return _endPointType; }
            }
            set
            {
                _endPointType = value;
                this.NotifyPropertyChanged("endPointType");
            }
        }
        public string timeSeen
        {
            get
            {
                return string.Format("{0:H:mm:ss dd/MM/yy}", _timeSeen); 
                
            }
            set
            {
                _timeSeen = DateTime.Now;
            }
        }

        public endPointTypes getEndPointType(string endPointTypeString)
        {
            switch (endPointTypeString)
            {
                case "Man": return endPointTypes.Man;
                case "Gas": return endPointTypes.Gas;
                case "Key": return endPointTypes.Key;
                case "Router": return endPointTypes.Router;
                case "Coord": return endPointTypes.Coord;
                default: return endPointTypes.Man;

            }
        }
        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

    }
}
