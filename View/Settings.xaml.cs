using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvSettings.ItemsSource);
            PropertyGroupDescription groupDescription = new PropertyGroupDescription(nameof(Setting.Scope));
            view?.GroupDescriptions.Add(groupDescription);
            view?.SortDescriptions.Add(new SortDescription {PropertyName = nameof(Setting.Scope), Direction = ListSortDirection.Ascending});
            view?.SortDescriptions.Add(new SortDescription {PropertyName = nameof(Setting.Key), Direction = ListSortDirection.Ascending});
        }
        private void NewDistributionSource_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new StringQueryDialog("Enter Source name:") { Owner = this };
            if (dialog.ShowDialog() == true) {
                String name = dialog.QueryString;
                View.MainWindow mainWindow = (View.MainWindow)System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                    mainWindow.ViewModel.DistributionSourceMap.Add(name, "");
            }
        }
        private void NewDistributionTarget_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new StringQueryDialog("Enter Target name:") { Owner = this };
            if (dialog.ShowDialog() == true) {
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
