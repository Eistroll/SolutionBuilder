using SolutionBuilder.ViewModel;
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
    /// Interaktionslogik für TreeSettings.xaml
    /// </summary>
    public partial class TreeSettings : Window
    {
        public TreeSettings( MainViewModel viewModel )
        {
            InitializeComponent();

            trvSettings.ItemsSource = viewModel.TreeSettingsList;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
        private ICommand _AddSettingCmd;
        public ICommand AddSettingCmd
        {
            get { return _AddSettingCmd ?? (_AddSettingCmd = new CommandHandler(param => AddSetting())); }
        }
        public void AddSetting()
        {
        }
            private void NewDistributionSource_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new StringQueryDialog("Enter Source name:") { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                String name = dialog.QueryString;
                View.MainWindow mainWindow = (View.MainWindow)System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                    mainWindow.ViewModel.DistributionSourceMap.Add(name, "");
            }
        }
        private void NewDistributionTarget_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new StringQueryDialog("Enter Target name:") { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                String name = dialog.QueryString;
                View.MainWindow mainWindow = (View.MainWindow)System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                    mainWindow.ViewModel.DistributionTargetMap.Add(name, "");
            }
        }
        private void OkButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
