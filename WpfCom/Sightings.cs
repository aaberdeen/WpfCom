using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfApplication1
{
    public partial class Sightings
    {
        private int _endpointId;
        private int _RouterId1;
        private int _RouterId2;
        private int _RouterId3;
        private int _Signal1;
        private int _Signal2;
        private int _Signal3;
        private DateTime _TimeStamp;
        private int _Xpos;
        private int _Ypos;
        private int _Zpos;
        private int _CH4;
        private int _CO;
        private int _O2;
        private int _Volts;

        public int endPointId
        {
            get { return _endpointId; }
            set { _endpointId = value; }
        }

        public int RouterId1
        {
            get { return _RouterId1; }
            set { _RouterId1 = value; }
        }
        public int RouterId2
        {
            get { return _RouterId2; }
            set { _RouterId2 = value; }
        }
        public int RouterId3
        {
            get { return _RouterId3; }
            set { _RouterId3 = value; }
        }
        public int Signal1
        {
            get { return _Signal1; }
            set { _Signal1 = value; }
        }
        public int Signal2
        {
            get { return _Signal2; }
            set { _Signal2 = value; }
        }
        public int Signal3
        {
            get { return _Signal3; }
            set { _Signal3 = value; }
        }

        public DateTime TimeStamp
        {
            get { return _TimeStamp; }
            set { _TimeStamp = value; }
        }
        public int Xpos
        {
            get { return _Xpos; }
            set { _Xpos = value; }
        }
        public int Ypos
        {
            get { return _Ypos; }
            set { _Ypos = value; }
        }
        public int Zpos
        {
            get { return _Zpos; }
            set { _Zpos = value; }
        }
        public int CH4
        {
            get { return _CH4; }
            set { _CH4 = value; }
        }
        public int CO
        {
            get { return _CO; }
            set { _CO = value; }
        }
        public int O2
        {
            get { return _O2; }
            set { _O2 = value; }
        }
        public int Volts
        {
            get { return _Volts; }
            set { _Volts = value; }
        }
    }
}
