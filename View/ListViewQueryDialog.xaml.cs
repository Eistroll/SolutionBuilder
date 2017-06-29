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
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace SolutionBuilder.View
{
    public class SettingsPair : INotifyPropertyChanged
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public SettingsPair(string _Key, string _Value)
        {
            Key = _Key;
            Value = _Value;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
    /// <summary>
    /// Interaction logic for ListViewQueryDialog.xaml
    /// </summary>
    public partial class ListViewQueryDialog : Window , INotifyPropertyChanged
    {
        public string DialogTitle { get; set; }
        public string HeaderKey { get; set; }
        public string HeaderValue { get; set; }
        public ObservableCollection<SettingsPair> Entries { get; set; }
        public ListViewQueryDialog(string title)
        {
            InitializeComponent();
            Entries = new ObservableCollection<SettingsPair>();
            DialogTitle = title;
            HeaderKey = "Key";
            HeaderValue = "Value";

            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;
        }
        private void OkButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
