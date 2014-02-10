﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComPort;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace WpfApplication1
{
    
    
    public class Node :INotifyPropertyChanged
    {
        private errorLog _errorLog = new errorLog();
        private string _name = "";
        private string _endPointType = "";
        BindingList<Node> _Children = null;
        private uint _TTL = 5;
             
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
        //private string _ReaderAdd;
        //private int _RxLQI;


        public event PropertyChangedEventHandler PropertyChanged;

    
        
        
        
        public Node()
        {
        }
        
        public Node(string name, BindingList<Node> Children)
        {

            _name = name;
            _Children = Children;
        }



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
        public uint TTL
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
      
    class Reader                       
    {
        public string ReaderAdd { get; set; }

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
        /// Method to decrement TTL in Router List
        /// </summary>
        /// <param name="TagAddTemp"></param>

        public void DecTTL(string TagAddTemp)
        {
            Tag result = FindTag(TagAddTemp);
            if (result.TTL != 0)
            {
                result.TTL--;
            }



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