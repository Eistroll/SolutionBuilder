using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SolutionBuilder.View
{
    /// <summary>
    /// Interaction logic for StringQueryDialog.xaml
    /// </summary>
    public partial class StringQueryDialog : Window
    {
        public String QueryString { get { return QueryStringBox.Text; } set { QueryStringBox.Text = value; } }
        public String Text { get; set; }
        public String DialogTitle { get; set; }
        public StringQueryDialog(String text, String title = "StringQueryDialog")
        {
            InitializeComponent();
            DialogTitle = title;
            TextBlock.Text = text;
            DataContext = this;
        }
        private void OkButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
