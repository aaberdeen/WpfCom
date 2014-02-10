using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Linq.Expressions;

namespace WpfApplication1
{
    public class MySortableBindingList<T> : BindingList<T>
    {
        protected override bool SupportsSortingCore
        {
            get
            {
                return true;
            }
        }

        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            var modifier = direction == ListSortDirection.Ascending ? 1 : -1;
            if (prop.PropertyType.GetInterface("IComparable") != null)
            {
                var items = Items.ToList();
                items.Sort(new Comparison<T>((a, b) =>
                {
                    var aVal = prop.GetValue(a) as IComparable;
                    var bVal = prop.GetValue(b) as IComparable;
                    return aVal.CompareTo(bVal) * modifier;
                }));
                Items.Clear();
                foreach (var i in items)
                    Items.Add(i);
            }
        }
    }
}
