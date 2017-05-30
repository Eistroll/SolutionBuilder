using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SolutionBuilder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Model _Model = new Model();
        private MainViewModel _ViewModel = new MainViewModel();
        private BitmapImage ImageBuildSuccess;
        private BitmapImage ImageBuildFailure;
        public MainWindow()
        {
            InitializeComponent();
            ImageBuildFailure = new BitmapImage(new Uri(@"Images/img_delete_16.png",UriKind.Relative));
            ImageBuildSuccess = new BitmapImage(new Uri(@"Images/img_check_16.png",UriKind.Relative));
            
            _Model = Model.Load();
            _ViewModel = MainViewModel.Load();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = _ViewModel;
            _ViewModel.BindToModel( ref _Model);
        }
        private void RemoveSolution_OnClick(object sender, RoutedEventArgs e)
        {
            TabItem tab = (TabItem)tabs.SelectedContent;
            int index = tab.SelectedSolutionIndex;
            if (index >= 0 && index < tab.Solutions.Count) {
                var solutions = _Model.Scope2SolutionObjects[tab.Header];
                solutions.RemoveAt(index);
                tab.Solutions.RemoveAt(index);
            }
        }
        private void SaveCmd_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void SaveCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _ViewModel.Save();
            _Model.Save();
        }
        private void LoadCmd_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void LoadCmd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _ViewModel = MainViewModel.Load();
            _Model = Model.Load();
            this.DataContext = _ViewModel;
            _ViewModel.BindToModel(ref _Model);
        }
        private void cbPlatform_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {

        }
        private void mnuSettings_Click(object sender, RoutedEventArgs e)
        {
            Window settings = new SolutionBuilder.Settings();
            settings.DataContext = _ViewModel;
            settings.ShowDialog();
        }
        private void mnuNewTab_Click(object sender, RoutedEventArgs e)
        {
            String tabName = "New";
            _ViewModel.SettingsList.Add(new Setting() { Scope = tabName, Key = "BaseDir", Value = "" });
            TabItem tab = new TabItem() { Header = tabName };
            tab.BindToModel(ref _Model, ref _ViewModel);
            _ViewModel.Tabs.Add( tab );

        }
        private void btBuild_Click(object sender, RoutedEventArgs e)
        {
            FileInfo buildExe = new FileInfo(_ViewModel.GetSetting("BuildExe"));
            if (!buildExe.Exists)
                return;
            foreach ( var tab in _ViewModel.Tabs)
            {
                foreach (SolutionObjectView solution in tab.Solutions) {
                    if (solution.Selected) {
                        System.Diagnostics.Process process = new System.Diagnostics.Process();
                        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        startInfo.RedirectStandardOutput = true;
                        startInfo.UseShellExecute = false;
                        startInfo.CreateNoWindow = true;
                        startInfo.FileName = buildExe.ToString();
                        StringBuilder path = new StringBuilder(_ViewModel.GetSetting("BaseDir"));
                        path.Append("\\" + solution.Name);
                        startInfo.Arguments = tab.BaseOptions + " " + solution.Options + " " + path;
                        process.StartInfo = startInfo;
                        bool Success = process.Start();
                        while (!process.StandardOutput.EndOfStream) {
                            String line = process.StandardOutput.ReadLine() + Environment.NewLine;
                            solution.BuildLog += line;
                            textBox.AppendText(line);
                        }
                        process.WaitForExit();
                        int exitCode = process.ExitCode;
                        if (exitCode == 0)
                            solution.BuildState = ImageBuildSuccess;
                        else
                            solution.BuildState = ImageBuildFailure;
                    }
                }
            }
        }
    }
}
