using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WpfApplication1
{
    class errorLog
    {
        public void write(Exception e, string errorCode)
        {
            using (StreamWriter w = File.AppendText("debugLog.txt"))
            {
                w.WriteLine(DateTime.Now);
                w.WriteLine("{0}", errorCode);
                w.WriteLine("{0}", e.ToString());
            }
        }
        public void write(string errorCode)
        {
            using (StreamWriter w = File.AppendText("debugLog.txt"))
            {
                w.WriteLine(string.Format("{0} - {1}" ,DateTime.Now, errorCode));
            }
        }
      
    }
}
