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
        public BindingList<NamesBind> minerNames = new BindingList<NamesBind>();
 
        public MinersNamesForm()
        {
            InitializeComponent();
        
            //test data#################
            //minerNames.Add(new NamesBind("00158D0000122C35", "Andy Aberdeen"));
            //minerNames.Add(new NamesBind("00158D000011B247", "Paul Briggs"));
            //minerNames.Add(new NamesBind("00158D0000122C35", "Doug Etches"));  
            //#######################


            namesGrd.ItemsSource = minerNames;



        }

        private void LoadTagsMacs()
        {
           

        }
        /// <summary>
        /// Adds a MAC to the list of Miners names. 
        /// If the MAC as already there returns minersName and endPointType in a string array
        /// </summary>
        /// <param name="macToAdd"></param>
        /// <returns>name of miner, endPointType</returns>
        public string[] addMacToMinersNames(string macToAdd)
        {
            //bool search;
            //NamesBind result;
            //foreach (var n in minerNames)
            //{
            //    if (n.Key == macToAdd)
            //    {
            //        search = true;
                    
            //    }
            //}

            var test = minerNames.ToList().FirstOrDefault(item => item.MAC == macToAdd);
            string[] returnStrings = new string[2];

            //string a = search.minerName;

            if (test !=null)
            {
                

                // if it is in the list send back the name
                returnStrings[0] = test.minerName;
                returnStrings[1] = test.endPointType.ToString();
                
                return returnStrings;
              
            }
            else
            {
            System.Windows.Application.Current.Dispatcher.Invoke(
                                System.Windows.Threading.DispatcherPriority.Normal,


                                (Action)delegate()
                                {

                                    //add(macToAdd, "", "");
                                    add(macToAdd, "", NamesBind.endPointTypes.Man);
                                });
            return returnStrings;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            
            this.Hide();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

        //minerNames.Add(new NamesBind("", "", ""));
        //add("", "", "");
        add("", "", NamesBind.endPointTypes.Man);
        }

        //private void add(string macToAdd, string nameToAdd, string typeToAdd)
        //{
        //    minerNames.Add(new NamesBind(macToAdd, nameToAdd, typeToAdd));
        //}

        private void add(string macToAdd, string nameToAdd, NamesBind.endPointTypes typeToAdd)
        {
            minerNames.Add(new NamesBind(macToAdd, nameToAdd, typeToAdd));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LoadTagsMacs();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.Hide();
        }
    }
    public class NamesBind : INotifyPropertyChanged
    {
        private string _MAC;
        private string _minerName;
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

        public NamesBind(string mac, string name, string endType)
        {
            this.MAC = mac;
            this.minerName = name;
            this.endPointType = this.getEndPointType(endType);

        }

        public NamesBind(string mac, string name, endPointTypes endType)
        {
            this.MAC = mac;
            this.minerName = name;
            this.endPointType = endType;

        }

        public string MAC
        {
            get { return _MAC; }
            set
            {
                _MAC = value;
                this.NotifyPropertyChanged("MAC");
            }
        }

        public string minerName
        {
            get { return _minerName; }
            set
            {
                _minerName = value;
                this.NotifyPropertyChanged("minerName");
            }
        }
        //public string endPointType
        //{
        //    get 
        //    { 
        //        switch(_endPointType)
        //        {
        //        case 1: return "Man"; 
        //        case 2: return "Gas";
        //        default: return null;
        //        }
            
        //    }
            
            
        //    set
        //    {
        //        switch (value)
        //        {
        //            case "Man": 
        //                _endPointType = 1;
        //                this.NotifyPropertyChanged("endPointType");
        //                break;
        //            case "Gas": 
        //                _endPointType = 2;
        //                this.NotifyPropertyChanged("endPointType");
        //                break;
        //            default: 
        //                _endPointType = 1;
        //                this.NotifyPropertyChanged("endPointType");
        //                break;
        //        }

        //       // this.NotifyPropertyChanged("endPointType");
        //    }
        //}

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
