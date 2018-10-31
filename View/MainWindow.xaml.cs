#define TRACE
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SolutionBuilder.ViewModel;
using System.Threading;
using System.Collections.ObjectModel;

namespace SolutionBuilder.View
{
    public enum State
    {
        None = 0,
        Success,
        Failure
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        private Model _Model = new Model();
        private MainViewModel _ViewModel = new MainViewModel();
        public Executor executor;

        public MainViewModel ViewModel { get { return _ViewModel; } set { _ViewModel = value; } }
        public MainWindow()
        {
            InitializeComponent();
            var userPrefs = new UserPreferences();
            this.Height = userPrefs.WindowHeight;
            this.Width = userPrefs.WindowWidth;
            this.Top = userPrefs.WindowTop;
            this.Left = userPrefs.WindowLeft;
            this.WindowState = userPrefs.WindowState;

            _Model = Model.Load();
            _ViewModel = MainViewModel.Load();
            executor = new Executor(_ViewModel);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = _ViewModel;
            _ViewModel.BindToModel(ref _Model);
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            var userPrefs = new UserPreferences();
            userPrefs.WindowHeight = this.Height;
            userPrefs.WindowWidth = this.Width;
            userPrefs.WindowTop = this.Top;
            userPrefs.WindowLeft = this.Left;
            userPrefs.WindowState = this.WindowState;
            userPrefs.Save();

            _ViewModel.Save();
            _Model.Save();
        }
        public void ClearLog()
        {
            textBoxLog.Clear();
        }
        public void AddToLog(string text)
        {
            if (!textBoxLog.CheckAccess())
            {
                textBoxLog.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action<string>(AddToLog), text);
            }
            else
            {
                _ViewModel.Log += text;
                textBoxLog.ScrollToEnd();
            }
        }
        private void RemoveSolution_OnClick(object sender, RoutedEventArgs e)
        {
            BuildTabItem tab = (BuildTabItem)tabs.SelectedContent;
            int index = tab.SelectedSolutionIndex;
            if (index >= 0 && index < tab.Solutions.Count)
            {
                var solutions = _Model.Scope2SolutionObjects[tab.TabName];
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
        private void Platform_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void MnuExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void MnuSettings_Click(object sender, RoutedEventArgs e)
        {
            Window settings = new Settings() { DataContext = _ViewModel, Owner = this };
            settings.ShowDialog();
        }
        private void MnuNewTab_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ListViewQueryDialog("New Tab") { Owner = this };
            dialog.Entries.Add(new SettingsPair("Name", ""));
            dialog.Entries.Add(new SettingsPair("Base dir", ""));
            if (dialog.ShowDialog() == true)
            {
                String tabName = dialog.Entries.FirstOrDefault(x => x.Key == "Name").Value;
                if (tabName == null && tabName.Length <= 0)
                    return;
                BuildTabItem tab = new BuildTabItem() { TabName = tabName };
                tab.BindToModel(ref _Model, ref _ViewModel);
                _ViewModel.Tabs.Add(tab);
            }
        }
        private void MnuCopyTab_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new StringQueryDialog("Enter Tab name:") { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                String tabName = dialog.QueryString;
                BuildTabItem originalTab = tabs.SelectedItem as BuildTabItem;
                String originalBaseDir = originalTab.BaseDir;
                String newBaseDir = originalBaseDir.Replace(originalTab.TabName, tabName);
                BuildTabItem tab = new BuildTabItem() { TabName = tabName, BaseDir = newBaseDir, BaseOptions = originalTab.BaseOptions };
                var clonedList = _Model.Scope2SolutionObjects[originalTab.TabName].Select(obj => (SolutionObject)obj.Clone()).ToList();
                _Model.Scope2SolutionObjects[tabName] = new System.Collections.ObjectModel.ObservableCollection<SolutionObject>(clonedList);
                tab.BindToModel(ref _Model, ref _ViewModel);
                _ViewModel.Tabs.Add(tab);
            }
        }
        private void MnuRemoveTab_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure?", "Delete Tab Confirmation", System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                BuildTabItem selectedTab = tabs.SelectedItem as BuildTabItem;
                _ViewModel.Tabs.Remove(selectedTab);
            }
        }
        private void BuildAll_Click(object sender, RoutedEventArgs e)
        {
            FileInfo buildExe = new FileInfo(_ViewModel.GetSetting(Setting.Executables.BuildExe.ToString()));
            if (!buildExe.Exists)
                return;
            ClearLog();
            foreach (var tab in _ViewModel.Tabs)
            {
                if (!tab.DoBuild)
                    continue;
                tab.BuildCheckedSolutions();
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            executor.Cancel();
        }
        private void RefreshLog_Click(object sender, RoutedEventArgs e)
        {
            RefreshLog();
        }
        public void RefreshLog()
        {
            BuildTabItem tab = (BuildTabItem)tabs.SelectedContent;
            if ( tab.SelectedSolutionIndex != -1 )
            {
                _ViewModel.UpdateLog(tab.Solutions[tab.SelectedSolutionIndex]);
            }
        }
        private bool ExecuteDistribution(DistributionItem distribution, bool Copy, bool Start, Executor executor, FileInfo copyExe)
        {
            if (!Copy && !Start)
                return false;

            Task task = null;
            string target = _ViewModel.GetSetting(distribution.Folder, Setting.Scopes.DistributionTarget);
            if (target.Count() == 0)
            {
                AddToLog($"No folder defined for DistributionTarget {distribution.Folder}\n");
                return false;
            }
            if (Copy)
            {
                string source = _ViewModel.DistributionSourceMap[distribution.Source];
                target = target.Replace(@"{Platform}", distribution.Platform);
                target = target.Replace(@"{Configuration}", distribution.Configuration);
                target = target.Replace(@"{Name}", distribution.Folder);
                source = source.Replace(@"{Platform}", distribution.Platform);
                source = source.Replace(@"{Configuration}", distribution.Configuration);
                AddToLog($"Copy\n{source} -> {target}" + Environment.NewLine);
                InvalidateVisual();
                Distributor distributeExecution = new Distributor()
                {
                    copyExe = copyExe.ToString(),
                    source = source,
                    target = target,
                    AddToLog = AddToLog
                };
                task = executor.Execute(action =>
                {
                    distributeExecution.Copy(action);
                });
            }
            if (Start)
            {
                if (task != null)
                    Task.WaitAll(task);
                target = target.Replace(@"{Platform}", distribution.Platform);
                target = target.Replace(@"{Configuration}", distribution.Configuration);
                target = target.Replace(@"{Name}", distribution.Folder);
                string exe = distribution.Executable;
                if (exe.Count() == 0)
                {
                    AddToLog($"No file defined for DistributionExe {distribution.Folder}\n");
                    return false;
                }
                FileInfo exePath = new FileInfo(target + Path.DirectorySeparatorChar + exe);
                if (!exePath.Exists)
                {
                    AddToLog($"Executable for starting does not exist: {exePath.ToString()}");
                    return false;
                }
                AddToLog($"Execute {exePath}" + Environment.NewLine);
                task = Task.Factory.StartNew(() =>
                 {
                     System.Diagnostics.Process process = new System.Diagnostics.Process();
                     process.StartInfo.FileName = exePath.ToString();
                     process.Start();
                     distribution.Proc = process;
                 });
            }
            return true;
        }

        private void CopySelected_Click(object sender, RoutedEventArgs e)
        {
            ClearLog();
            FileInfo copyExe = new FileInfo(_ViewModel.GetSetting(Setting.Executables.CopyExe.ToString()));
            if (!copyExe.Exists)
            {
                AddToLog("Executable for copying is not defined!");
                return;
            }
            foreach (var distribution in _ViewModel.DistributionList)
            {
                if(distribution.Checked)
                {
                    ExecuteDistribution(distribution, true, false, executor, copyExe);
                }
            }
        }
        private void StartSelected_Click(object sender, RoutedEventArgs e)
        {
            ClearLog();
            FileInfo copyExe = new FileInfo(_ViewModel.GetSetting(Setting.Executables.CopyExe.ToString()));
            if (!copyExe.Exists)
            {
                AddToLog("Executable for copying is not defined!");
                return;
            }
            foreach (var distribution in _ViewModel.DistributionList)
            {
                if (distribution.Checked)
                {
                    ExecuteDistribution(distribution, false, true, executor, copyExe);
                }
            }
        }
        private void KillSelected_Click(object sender, RoutedEventArgs e)
        {
            foreach (var distribution in _ViewModel.DistributionList)
            {
                KillProcss(distribution);
            }
        }
        private void ExecuteDistribution_Click(object sender, RoutedEventArgs e)
        {
            Button executeButton = (Button)sender;
            DistributionItem distribution = executeButton.DataContext as DistributionItem;
            ClearLog();
            var nameExe = _ViewModel.GetSetting(Setting.Executables.CopyExe.ToString());
            if (nameExe.Length == 0)
            {
                AddToLog("Executable for copying is not defined!");
                return;
            }
            FileInfo copyExe = new FileInfo(nameExe);
            if (!copyExe.Exists)
            {
                AddToLog("Executable for copying does not exists!");
                return;
            }
            ExecuteDistribution(distribution, distribution.Copy, distribution.Start, executor, copyExe);
        }
        private void StartDistribution_Click(object sender, RoutedEventArgs e)
        {
            Button executeButton = (Button)sender;
            DistributionItem distribution = executeButton.DataContext as DistributionItem;
            ClearLog();
            var nameExe = _ViewModel.GetSetting(Setting.Executables.CopyExe.ToString());
            if (nameExe.Length == 0)
            {
                AddToLog("Executable for copying is not defined!");
                return;
            }
            FileInfo copyExe = new FileInfo(nameExe);
            if (!copyExe.Exists)
            {
                AddToLog("Executable for copying does not exists!");
                return;
            }
            ExecuteDistribution(distribution, false, true, executor, copyExe);
        }
        private void CopyDistribution_Click(object sender, RoutedEventArgs e)
        {
            Button executeButton = (Button)sender;
            DistributionItem distribution = executeButton.DataContext as DistributionItem;
            ClearLog();
            var nameExe = _ViewModel.GetSetting(Setting.Executables.CopyExe.ToString());
            if (nameExe.Length == 0)
            {
                AddToLog("Executable for copying is not defined!");
                return;
            }
            FileInfo copyExe = new FileInfo(nameExe);
            if (!copyExe.Exists)
            {
                AddToLog("Executable for copying does not exists!");
                return;
            }
            ExecuteDistribution(distribution, true, false, executor, copyExe);
        }

        private void KillProcss(DistributionItem distribution)
        {
            if (distribution.Proc != null && !distribution.Proc.HasExited)
            {
                AddToLog($"Kill process {distribution.Proc.ProcessName} ({distribution.Proc.Id})" + Environment.NewLine);
                distribution.Proc.Kill();
            }
            else
            {
                AddToLog("Process already has exited!" + Environment.NewLine);
            }
        }

        private void KillDistribution_Click(object sender, RoutedEventArgs e)
        {
            Button killButton = (Button)sender;
            DistributionItem distribution = killButton.DataContext as DistributionItem;
            KillProcss(distribution);
        }

        private void OpenPostBuildStep_Click(object sender, RoutedEventArgs e)
        {
            Button openButton = (Button)sender;
            SolutionObjectView solutionObject = openButton.DataContext as SolutionObjectView;
            var dialog = new StringQueryDialog("Enter command:", "Postbuild step") { Owner = this };
            dialog.Width = Width*0.8;
            dialog.QueryString = solutionObject.PostBuildStep;
            if (dialog.ShowDialog() == true)
            {
                solutionObject.PostBuildStep = dialog.QueryString;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                executor.Dispose();
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
