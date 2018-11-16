using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SolutionBuilder.Converters
{
    [ValueConversion(typeof(string), typeof(string))]
    public class TailOfFilePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var path = (string)value;
            var split = path.Split(Path.DirectorySeparatorChar);
            string tail = "";
            int count = 0;
            foreach (string s in split.Reverse<string>())
            {
                if (count++ < 2)
                    tail = (tail.Length == 0 ? s : s + Path.DirectorySeparatorChar ) + tail;
            }
            return tail.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
