using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SolutionBuilder.Converters
{
    [ValueConversion(typeof(View.State), typeof(ImageSource))]
    public class ContentToImageSourceConverter : IValueConverter
    {
        static internal ImageSource GetImageSourceFromResource(string imageName)
        {
            Uri oUri = new Uri("pack://application:,,,/" + Assembly.GetExecutingAssembly().GetName().Name + ";component/" + imageName, UriKind.RelativeOrAbsolute);
            return BitmapFrame.Create(oUri);
        }
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return GetImageSourceFromResource("Images/cmd_text_new_20.png");
            else
                return GetImageSourceFromResource("Images/img_pen_16.png");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
