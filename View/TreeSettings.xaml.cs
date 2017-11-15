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
        public TreeSettings()
        {
            InitializeComponent();

            List<ViewModel.TreeSettings> settings = new List<ViewModel.TreeSettings>();

            ViewModel.TreeSettings baseSettings = new ViewModel.TreeSettings() { Name = "Base" };
            baseSettings.Members.Add(new TreeSetting() { Key = "BuildExe", Value = "MSBuild.exe" });
            baseSettings.Members.Add(new TreeSetting() { Key = "BaseDir", Value = "C:" });
            settings.Add(baseSettings);

            ViewModel.TreeSettings distributionSettings = new ViewModel.TreeSettings() { Name = "Distribution" };
            distributionSettings.Members.Add(new TreeSetting() { Key = "CopyExe", Value = "Robocopy.exe" });
            distributionSettings.Members.Add(new TreeSetting() { Key = "Executable", Value = "WinGuard.exe" });
            settings.Add(distributionSettings);

            trvFamilies.ItemsSource = settings;
        }
    }
}
