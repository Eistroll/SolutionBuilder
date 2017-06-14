#define TRACE
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
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
using System.Diagnostics;
using System.Collections.ObjectModel;
using SolutionBuilder.ViewModel;

namespace SolutionBuilder.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Model _Model = new Model();
        private MainViewModel _ViewModel = new MainViewModel();
        public MainViewModel ViewModel { get { return _ViewModel; } set { _ViewModel = value; } }
        private ImageSource ImageBuildSuccess;
        private ImageSource ImageBuildFailure;
        static internal ImageSource GetImageSourceFromResource(string imageName)
        {
            Uri oUri = new Uri("pack://application:,,,/" + Assembly.GetExecutingAssembly().GetName().Name + ";component/" + imageName, UriKind.RelativeOrAbsolute);
            return BitmapFrame.Create(oUri);
        }
        public MainWindow()
        {
            InitializeComponent();
            ImageBuildFailure = GetImageSourceFromResource("Images/img_delete_16.png");
            ImageBuildSuccess = GetImageSourceFromResource("Images/img_check_16.png");

            _Model = Model.Load();
            _ViewModel = MainViewModel.Load();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = _ViewModel;
            _ViewModel.BindToModel( ref _Model);
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _ViewModel.Save();
            _Model.Save();
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
        private void Platform_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {

        }
        private void MnuExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void MnuSettings_Click(object sender, RoutedEventArgs e)
        {
            Window settings = new Settings() { DataContext = _ViewModel };
            settings.ShowDialog();
        }
        private void MnuNewTab_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new StringQueryDialog("Enter Tab name:");
            if ( dialog.ShowDialog() == true )
            {
                String tabName = dialog.QueryString;
                _ViewModel.SettingsList.Add(new Setting() { Scope = tabName, Key = "BaseDir", Value = "" });
                TabItem tab = new TabItem() { Header = tabName };
                tab.BindToModel(ref _Model, ref _ViewModel);
                _ViewModel.Tabs.Add(tab);
            }
        }
        private void MnuCopyTab_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new StringQueryDialog("Enter Tab name:");
            if (dialog.ShowDialog() == true) {
                String tabName = dialog.QueryString;
                TabItem originalTab = tabs.SelectedItem as TabItem;
                String originalBaseDir = _ViewModel.GetSetting("BaseDir", originalTab.Header);
                String newBaseDir = originalBaseDir.Replace(originalTab.Header, tabName);
                _ViewModel.SettingsList.Add(new Setting() { Scope = tabName, Key = "BaseDir", Value = newBaseDir });
                TabItem tab = new TabItem() { Header = tabName };
                var clonedList = _Model.Scope2SolutionObjects[originalTab.Header].Select(obj => (SolutionObject) obj.Clone()).ToList();
                _Model.Scope2SolutionObjects[tabName] = new System.Collections.ObjectModel.ObservableCollection<SolutionObject>(clonedList);
                tab.BindToModel(ref _Model, ref _ViewModel);
                _ViewModel.Tabs.Add(tab);
            }
        }
        private void MnuRemoveTab_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure?", "Delete Tab Confirmation", System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes) { 
                TabItem selectedTab = tabs.SelectedItem as TabItem;
                _ViewModel.Tabs.Remove(selectedTab);
            }
        }
        private void AddToLog( string text )
        {
            if (!textBoxLog.CheckAccess())
            {
                textBoxLog.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action<string>(AddToLog), text);
            }
            else 
            {
                textBoxLog.Text += text;
                textBoxLog.ScrollToEnd();
            }
        }
        private void BuildOutputHandler(object sender, DataReceivedEventArgs e, SolutionObjectView solution)
        {
            string line = e.Data + Environment.NewLine;
            solution.BuildLog += line;
        }

        public bool BuildSolutions(TabItem tab, FileInfo buildExe, ObservableCollection<SolutionObjectView> solutions = null)
        {
            bool buildFailure = false;
            bool ignoreSelection = true;
            if (solutions == null) {
                solutions = tab.Solutions;
                ignoreSelection = false;
            }
            foreach (SolutionObjectView solution in solutions) {
                if (ignoreSelection || solution.Selected) {
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo()
                    { WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                    startInfo.FileName = buildExe.ToString();
                    StringBuilder path = new StringBuilder(_ViewModel.GetSetting("BaseDir", tab.Header));
                    path.Append("\\" + solution.Name);
                    startInfo.Arguments = tab.BaseOptions + " " + solution.Options + " " + path;
                    process.StartInfo = startInfo;
                    // Set event handler
                    process.OutputDataReceived += (s, eventargs) => BuildOutputHandler(s, eventargs, solution);
                    bool Success = process.Start();
                    process.BeginOutputReadLine();
                    process.WaitForExit();
                    int exitCode = process.ExitCode;
                    if (exitCode == 0) {
                        buildFailure = false;
                        solution.BuildState = ImageBuildSuccess;
                    }
                    else {
                        buildFailure = true;
                        solution.BuildState = ImageBuildFailure;
                    }

                    AddToLog(path + (buildFailure ? " failed" : " successful") + Environment.NewLine);
                    solution.SuccessFlag = exitCode == 0 ? true : false;
                }
            }
            return buildFailure;
        }

        private void BuildAll_Click(object sender, RoutedEventArgs e)
        {
            FileInfo buildExe = new FileInfo(_ViewModel.GetSetting("BuildExe"));
            if (!buildExe.Exists)
                return;
            this.textBoxLog.Clear();
            foreach (var tab in _ViewModel.Tabs)
            {
                if (!tab.DoBuild)
                    continue;
                tab.BuildState = null;
                foreach (SolutionObjectView solution in tab.Solutions)
                {
                    solution.BuildState = null;
                }
                Task.Factory.StartNew(() =>
                {
                    bool buildFailure = BuildSolutions(tab, buildExe);
                    tab.BuildState = buildFailure ? ImageBuildFailure : ImageBuildSuccess;
                });
            }
        }
        private void ExecuteAll_Click(object sender, RoutedEventArgs e)
        {
            FileInfo copyExe = new FileInfo(_ViewModel.GetSetting("CopyExe"));
            //if (!copyExe.Exists)
            //    return;
            this.textBoxLog.Clear();
            foreach (var distribution in _ViewModel.DistributionList)
            {
                if (!distribution.Selected)
                    continue;
                if(distribution.Copy)
                {
                    Copy(copyExe.ToString(), distribution);
                }
                if(distribution.Start)
                { }
            }
        }
        private void KillAll_Click(object sender, RoutedEventArgs e)
        {
        }
        private void ExecuteDistribution_Click(object sender, RoutedEventArgs e)
        {
        }
        private void KillDistribution_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Copy(string copyExe, DistributionItem distribution)
        {
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo()
                { WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden, RedirectStandardOutput = false, UseShellExecute = false, CreateNoWindow = true };
                startInfo.FileName = @copyExe;
                string source = cbDistributionSource.SelectedValue as string;
                string targetSetting = _ViewModel.GetSetting(distribution.Folder, MainViewModel.DISTRIBUTION_TARGET);
                string Platform = cbDistributionPlatforms.SelectedValue as string;
                string target = targetSetting.Replace(@"{Platform}",Platform);
                target = target.Replace(@"{Name}", distribution.Folder);
                source = source.Replace(@"{Platform}", Platform);
                string options = "/MIR";
                startInfo.Arguments = $"{source} {target} {options}";
                process.StartInfo = startInfo;
                //process.OutputDataReceived += (s, eventargs) => BuildOutputHandler(s, eventargs, solution);
                AddToLog($"Start: Copy {source} to {target}" + Environment.NewLine);
                bool started = process.Start();
                //process.BeginOutputReadLine();
                process.WaitForExit();
                int exitCode = process.ExitCode;
                switch(exitCode) {
                    case 0: 
                    case 1:
                    case 2:
                    case 3: AddToLog($"Finished with code {exitCode} (Success): Copy {source} to {target}" + Environment.NewLine); break;
                    case 4:
                    case 5:
                    case 6:
                    case 7: AddToLog($"Finished with code {exitCode} (Warning): Copy {source} to {target}" + Environment.NewLine); break;
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                    case 12:
                    case 13:
                    case 14:
                    case 15: AddToLog($"Finished with code {exitCode} (Error): Copy {source} to {target}" + Environment.NewLine); break;
                    case 16: AddToLog($"Finished with code {exitCode}: did not run" + Environment.NewLine); break;
                }
            }
            catch (System.Exception ex)
            {
                AddToLog(ex.Message + Environment.NewLine);
            }
        }
    }
}
