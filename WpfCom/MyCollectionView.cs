using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Windows.Data;
using System.ComponentModel;

namespace WpfApplication1
{
    class MyCollectionView : ListCollectionView
    {
        public MyCollectionView(IList sourceCollection): base(sourceCollection)
        {
            foreach (var item in sourceCollection)
            {
                if (item is INotifyPropertyChanged)
                {
                    ((INotifyPropertyChanged)item).PropertyChanged +=
                                                      (s, e) => Refresh();
                }
            }
        }
    }
}
