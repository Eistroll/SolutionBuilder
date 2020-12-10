using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace SolutionBuilder.Converters
{
    class SelectedDistributionItemsConverter : IMultiValueConverter
    {
        public struct Selection
        {
            public string Name;
            public IList SelectedItems;
        }
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2)
            {
                var selection = new Selection {Name = values[0] as string};
                if (values[1] is ListView listView)
                    selection.SelectedItems = listView.SelectedItems;
                return selection;
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
