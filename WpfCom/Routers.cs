using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComPort;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Timers;

namespace WpfApplication1
{
    public class Node :INotifyPropertyChanged
    {
        private errorLog _errorLog = new errorLog();
        private string _name = "";
        private string _endPointType = "";
        BindingList<Node> _Children = null;
        private int _TTL = 5;            
        private int _PktLength;
        private int _PktSequence;
        //private int _PktEvent;
        //private int _PktTemp;
        //private int _Volt;
        //private int _PktLqi;
        private int _BrSequ;
        //private int _BrCmd;
        //private int _TOFping;
        //private int _TOFtimeout;
        //private int _TOFrefuse;
        //private int _TOFsuccess;
        //private int _TOFdistance;
        //private int _RSSIdistance;
        //private int _TOFerror;
        //private string _TOFmac;
        private string _readerAddress;
        //private int _RxLQI;
        //private static Timer _TickTimer= new Timer(10000);
        private DateTime _timeSeen;
        public event PropertyChangedEventHandler PropertyChanged;


        #region Properties
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }
        public string endPointType
        {
            get
            {
                return _endPointType;
            }
            set
            {
                _endPointType = value;
            }
        }

        public string readerAddress
        {
            get
            {
                return _readerAddress;
            }
            set
            {
                _readerAddress = value;
            }
        }

        public int TTL
        {
            get
            {
                return _TTL;
            }
            set
            {
                _TTL = value;
                this.NotifyPropertyChanged("TTL");
            }
        }
        public int PktLength
        {
            get
            {
                return _PktLength;
            }
            set
            {
                _PktLength = value;
                this.NotifyPropertyChanged("PktLength");
            }
        }

        public int PktSequence
        {
            get
            {
                return _PktSequence;
            }
            set
            {
                _PktSequence = value;
                this.NotifyPropertyChanged("PktSequence");
            }
        }

        public int BrSequ
        {
            get
            {
                return _BrSequ;
            }
            set
            {
                _BrSequ = value;
                this.NotifyPropertyChanged("BrSequ");
            }

        }


        public BindingList<Node> Children    //test for tree view
        {
            get
            {

                if (_Children == null) _Children = new BindingList<Node>();
                return _Children;
            }
            set { _Children = value; }

        }

        #endregion
        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }   
    }


    public class TreeReader : Node
    {

    }
    
    public class TreeTag : Node
    {

    }
    public class TreeValue : Node
    {
        public string Display { get; set; }
    }
      
    class Routers                       
    {
        public string routerMac { get; set; }
        public string routerName { get; set; }
        public int Xpos { get; set; }
        public int Ypos { get; set; }
        public int Zpos { get; set; }
        public int ZoneID { get; set; }
        public bool Online { get; set; }

        public List<Tag> myTagList { get; set; }        //list of tags 
        
        public void AddNewTag(ref Tag tag)
        {
            myTagList.Add(new Tag
            {
                PktEvent = tag.PktEvent,
                PktLength = tag.PktLength,
                PktTemp = tag.PktTemp,
                Volt = tag.Volt,
                TagAdd = tag.TagAdd,
                ReaderAdd =  tag.ReaderAdd,
                PktLqi = tag.PktLqi,
                BrSequ = tag.BrSequ,
                BrCmd = tag.BrCmd,
                TOFping = tag.TOFping,
                TOFtimeout = tag.TOFtimeout,
                TOFrefuse = tag.TOFrefuse,
                TOFsuccess = tag.TOFsuccess,
                TOFdistance = tag.TOFdistance,  
                RSSIdistance = tag.RSSIdistance, 
                TOFerror = tag.TOFerror,
                TOFmac = tag.TOFmac,
                RxLQI = tag.RxLQI,


            });

        }

        /// <summary>
        /// Method Remove Tag from Router List
        /// </summary>
        /// <param name="TagItem"></param>

        public void RemoveTag(Tag TagItem)
        {
            myTagList.Remove(TagItem);
        }
     

        /// <summary>
        /// Method to find tag in router list
        /// </summary>
        /// <param name="TagAddTemp"></param>
        /// <returns></returns>

        public Tag FindTag(string TagAddTemp)
        {
            Tag searchResult = myTagList.ToList().Find(Ttest => Ttest.TagAdd == TagAddTemp);
            return searchResult;
        }

        /// <summary>
        /// Method to find All tags in router list
        /// </summary>
        /// <returns></returns>

        public List<Tag> FindAllTags()
        {
            List<Tag> searchResult = myTagList.ToList().FindAll(Ttest => Ttest.TagAdd != "");
            return searchResult;
        }

        /// <summary>
        /// Method to get number of tags in router list
        /// </summary>
        /// <returns></returns>

        public int NumberOfTags()
        {
            return myTagList.Count;
        }


    }        
}
