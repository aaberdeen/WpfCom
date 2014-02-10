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
using System.Windows.Threading;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SplashWindow : Window
    {
        DispatcherTimer closeTime = new DispatcherTimer();


        public SplashWindow()
        {
            InitializeComponent();

            closeTime.Interval = new TimeSpan(0, 0, 4);

            closeTime.Tick += new EventHandler(closeTime_Tick);

            closeTime.Start();
    

           
        }

        void closeTime_Tick(object sender, EventArgs e)
        {
            this.Close();
        }

       
    }
}
