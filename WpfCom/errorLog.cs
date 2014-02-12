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
            try
            {
                StreamWriter w = File.AppendText("debugLog.txt");
                using (w)
                {
                    w.WriteLine(DateTime.Now);
                    w.WriteLine("{0}", errorCode);
                    w.WriteLine("{0}", e.ToString());
                }
            }
            catch
            { }

        }
        public void write(string errorCode)
        {
            try
            {

                StreamWriter w = File.AppendText("debugLog.txt");
                using (w)
                {
                    w.WriteLine(string.Format("{0} - {1}", DateTime.Now, errorCode));
                }
            }
            catch
            {
            }

        }

    }
}
