using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    /// Interaction logic for ComboBoxQueryDialog.xaml
    /// </summary>
    public partial class ComboBoxQueryDialog : Window
    {
        public StringCollection Entries { get; set; }
        public string SelectedEntry { get; set; }
        public string ComboBoxLabel { get; set; }
        public string DialogTitle { get; set; }
        public ComboBoxQueryDialog()
        {
            InitializeComponent();
            DataContext = this;
        }
        private void OkButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
