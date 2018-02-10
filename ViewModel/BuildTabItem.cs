using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace SolutionBuilder
{
    [DataContract]
    public class BuildTabItem : INotifyPropertyChanged
    {
        [DataMember]
        public string Header { get; set; }
        [DataMember]
        public String SelectedPath { get; set; }
        [DataMember]
        public String BaseOptions { get; set; }
        [DataMember]
        public StringCollection CheckedSolutions { get; set; }
        [DataMember]
        public StringCollection Platforms { get; set; }
        [DataMember]
        private String _SelectedPlatform;
        public String SelectedPlatform
        {
            get { return _SelectedPlatform; }
            set
            {
                _SelectedPlatform = value;
                for (int i = 0; i < Solutions.Count; ++i)
                {
                    Solutions[i].Options = _Model.Scope2SolutionObjects[Header][i].Options[_SelectedPlatform];
                }
            }
        }
        [DataMember]
        private int _SelectedSolutionIndex;
        [DataMember]
        public bool DoBuild { get; set; }
        //////////////////////////////////////////////////////////////////////////
        //Not serialized Data members
        //////////////////////////////////////////////////////////////////////////
        public int SelectedSolutionIndex
        {
            get { return _SelectedSolutionIndex; }
            set
            {
                _SelectedSolutionIndex = value;
                RemoveSolutionCmd.RaiseCanExecuteChanged();
                if (_SelectedSolutionIndex != -1)
                    _ViewModel.UpdateLog(Solutions[_SelectedSolutionIndex]);
            }
        }
        private ObservableCollection<SolutionObjectView> _Solutions;
        public ObservableCollection<SolutionObjectView> Solutions
        {
            get { return _Solutions ?? (Solutions = new ObservableCollection<SolutionObjectView>()); }
            set { _Solutions = value; }
        }
        private ObservableCollection<string> _AllSolutionsInBaseDir;
        public ObservableCollection<string> AllSolutionsInBaseDir
        {
            get { return _AllSolutionsInBaseDir ?? (_AllSolutionsInBaseDir = new ObservableCollection<string>()); }
            set { _AllSolutionsInBaseDir = value; }
        }
        private View.State _BuildState;
        public View.State BuildState
        {
            get { return _BuildState; }
            set { if (_BuildState != value) { _BuildState = value; NotifyPropertyChanged("BuildState"); } }
        }
        private MainViewModel _ViewModel;
        private Model _Model;
        private bool ExplicitAddInProgress = false;
        private bool ExplicitRemoveInProgress = false;
        private bool _ProgressVisible = false;
        public bool ProgressVisible
        {
            get { return _ProgressVisible; }
            set { if (_ProgressVisible != value) { _ProgressVisible = value; NotifyPropertyChanged("ProgressVisible"); } }
        }
        private int _ProgressMin = 0;
        public int ProgressMin
        {
            get { return _ProgressMin; }
            set { if (_ProgressMin != value) { _ProgressMin = value; NotifyPropertyChanged("ProgressMin"); } }
        }
        private int _ProgressMax = 0;
        public int ProgressMax
        {
            get { return _ProgressMax; }
            set { if (_ProgressMax != value) { _ProgressMax = value; NotifyPropertyChanged("ProgressMax"); } }
        }
        private int _ProgressCurrent = 0;
        public int ProgressCurrent
        {
            get { return _ProgressCurrent; }
            set { if (_ProgressCurrent != value) { _ProgressCurrent = value; NotifyPropertyChanged("ProgressCurrent"); } }
        }
        public BuildTabItem()
        {
            AllSolutionsInBaseDir = new ObservableCollection<string>();
            CheckedSolutions = new StringCollection();
            Solutions = new ObservableCollection<SolutionObjectView>();
            Platforms = new StringCollection() { "Release", "Debug" };
            _SelectedPlatform = "Debug";
            SelectedSolutionIndex = -1;
        }
        public IEnumerable<SolutionObjectView> SelectedSolutionViews
        {
            get { return _Solutions.Where(o => o.IsSelected); }
        }
        public void UpdateAvailableSolutions()
        {
            AllSolutionsInBaseDir.Clear();
            String BaseDir = _ViewModel.GetSetting("BaseDir", Header);
            if (BaseDir.Length == 0)
                return;
            System.IO.DirectoryInfo BaseDirInfo = new System.IO.DirectoryInfo(BaseDir);
            if (BaseDirInfo.Exists)
            {
                var solutionPaths = Directory.GetFiles(BaseDir, @"*.sln", SearchOption.AllDirectories);
                foreach (var path in solutionPaths)
                {
                    String newPath = path.Replace(BaseDir, "");
                    AllSolutionsInBaseDir.Add(newPath);
                }
            }
        }
        public void BindToModel(ref Model Model, ref MainViewModel ViewModel)
        {
            _Model = Model;
            _ViewModel = ViewModel;
            UpdateAvailableSolutions();

            UpdateFromModel(ref Model);
        }
        private void UpdateFromModel(ref Model Model)
        {
            Solutions.Clear();
            if (Model.Scope2SolutionObjects.Count == 0 || !Model.Scope2SolutionObjects.ContainsKey(Header))
                return;
            foreach (SolutionObject solution in Model.Scope2SolutionObjects[Header])
            {
                SolutionObject tmp = solution;
                SolutionObjectView solutionView = new SolutionObjectView(ref tmp, SelectedPlatform);
                if (CheckedSolutions != null && CheckedSolutions.Contains(tmp.Name))
                    solutionView.Checked = true;
                solutionView.PropertyChanged += new PropertyChangedEventHandler(SolutionView_PropertyChanged);
                Solutions.Add(solutionView);
            }
            Solutions.CollectionChanged += new NotifyCollectionChangedEventHandler(Solutions_CollectionChanged);
       }
        private void Solutions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ExplicitAddInProgress || ExplicitRemoveInProgress)
                return;
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                int index = e.NewStartingIndex;
                foreach (SolutionObjectView solution in e.NewItems)
                {
                    _Model.Scope2SolutionObjects[Header].Insert(index++, solution.SolutionObject);
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (SolutionObjectView solution in e.OldItems)
                {
                    _Model.Scope2SolutionObjects[Header].Remove(solution.SolutionObject);
                }
            }
        }
        private void SolutionView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Options")
            {
                SolutionObjectView solView = (SolutionObjectView)sender;
                if (solView == null)
                    return;
                if (solView.SolutionObject != null)
                {
                    solView.SolutionObject.Options[SelectedPlatform] = solView.Options;
                }
            }
            if (e.PropertyName == "Checked")
            {
                SolutionObjectView solutionView = (SolutionObjectView)sender;
                if (solutionView == null)
                    return;
                if (!solutionView.Checked)
                    CheckedSolutions.Remove(solutionView.Name);
                else
                {
                    if (CheckedSolutions == null)
                        CheckedSolutions = new StringCollection();
                    CheckedSolutions.Add(solutionView.Name);
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private ICommand _AddSolutionCmd;
        public ICommand AddSolutionCmd
        {
            get { return _AddSolutionCmd ?? (_AddSolutionCmd = new CommandHandler(param => AddSolution())); }
        }
        public void AddSolution()
        {
            SolutionObject solution = new SolutionObject();
            if (!_Model.Scope2SolutionObjects.ContainsKey(Header))
            {
                _Model.Scope2SolutionObjects[Header] = new ObservableCollection<SolutionObject>();
            }
            _Model.Scope2SolutionObjects[Header].Add(solution);
            SolutionObjectView solutionView = new SolutionObjectView(ref solution, SelectedPlatform);
            solutionView.PropertyChanged += new PropertyChangedEventHandler(SolutionView_PropertyChanged);
            ExplicitAddInProgress = true;
            Solutions.Add(solutionView);
            ExplicitAddInProgress = false;
        }
        private CommandHandler _RemoveSolutionCmd;
        public CommandHandler RemoveSolutionCmd
        {
            get { return _RemoveSolutionCmd ?? (_RemoveSolutionCmd = new CommandHandler(param => RemoveSolution(param), param => RemoveSolution_CanExecute(param))); }
        }
        public bool RemoveSolution_CanExecute(object parameter)
        {
            return SelectedSolutionIndex != -1;
        }
        public void RemoveSolution(object parameter)
        {
            List<int> indexListToRemove = new List<int>();
            foreach (var solutionView in SelectedSolutionViews)
            {
                if (solutionView != null)
                {
                    _Model.Scope2SolutionObjects[Header].Remove(solutionView.SolutionObject);
                    indexListToRemove.Add(Solutions.IndexOf(solutionView));
                }
            }
            indexListToRemove.Reverse();
            ExplicitRemoveInProgress = true;
            foreach (var index in indexListToRemove)
                Solutions.RemoveAt(index);
            ExplicitRemoveInProgress = false;
            SelectedSolutionIndex = Solutions.Count - 1;
        }
        private CommandHandler _BuildSolutionCmd;
        public CommandHandler BuildSolutionCmd
        {
            get { return _BuildSolutionCmd ?? (_BuildSolutionCmd = new CommandHandler(param => BuildSolution(param), param => RemoveSolution_CanExecute(param))); }
        }
        public bool BuildSolution_CanExecute(object parameter)
        {
            return SelectedSolutionIndex != -1;
        }
        private ICommand _CancelBuildCmd;
        public ICommand CancelBuildCmd
        {
            get { return _CancelBuildCmd ?? (_CancelBuildCmd = new CommandHandler(param => CancelBuild())); }
        }
        public void CancelBuild()
        {
            View.MainWindow mainWindow = (View.MainWindow)System.Windows.Application.Current.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.executor.Cancel();
            }
        }
        public void UpdateProgress(int min, int max, int current, string text, bool failure)
        {
            ProgressMin = min;
            ProgressMax = max;
            ProgressCurrent = current;
            _ViewModel.ProgressValue = (double)current / (max - min);
            _ViewModel.ProgressDesc = text;
            if ( current == min && !failure )
            {
                _ViewModel.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            }
            if ( failure && _ViewModel.ProgressState != System.Windows.Shell.TaskbarItemProgressState.Error)
            {
                _ViewModel.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
            }
            if (current >= min && current < max)
                ProgressVisible = true;
            else if (current == max)
            {
                ProgressVisible = false;
                _ViewModel.ProgressType = "";
                _ViewModel.ProgressDesc = "";
            }
        }
        public void BuildSolution(object parameter)
        {
            SolutionObjectView solution = Solutions[SelectedSolutionIndex];
            View.MainWindow mainWindow = (View.MainWindow)System.Windows.Application.Current.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.ClearLog();
                //(mainWindow.FindResource("showMe") as Storyboard).Begin(mainWindow.);
                solution.BuildState = View.State.None;
                Builder buildExecution = new Builder()
                {
                    BaseDir = _ViewModel.GetSetting("BaseDir", Header),
                    BaseOptions = BaseOptions,
                    BuildExe = new FileInfo(_ViewModel.GetSettingByScope(Setting.Executables.BuildExe.ToString())),
                    solutions = new ObservableCollection<SolutionObjectView>() { solution },
                    AddToLog = mainWindow.AddToLog,
                    UpdateProgress = UpdateProgress
                };
                _ViewModel.ProgressType = "Building single solution";
                var task = mainWindow.executor.Execute( action =>
                {
                    buildExecution.Build(action);
                });
                //_ViewModel.ProgressType = "";
            }
        }
        public void BuildCheckedSolutions()
        {
            View.MainWindow mainWindow = (View.MainWindow)System.Windows.Application.Current.MainWindow;
            if (mainWindow == null)
                return;
            BuildState = View.State.None;
            ProgressVisible = true;
            ObservableCollection<SolutionObjectView> solutionsToBuild = new ObservableCollection<SolutionObjectView>();
            foreach (SolutionObjectView solution in Solutions)
            {
                solution.BuildState = View.State.None;
                if (solution.Checked)
                {
                    solutionsToBuild.Add(solution);
                }
            }
            Builder buildExecution = new Builder()
            {
                BaseDir = _ViewModel.GetSetting("BaseDir", Header),
                BaseOptions = BaseOptions,
                BuildExe = new FileInfo(_ViewModel.GetSettingByScope(Setting.Executables.BuildExe.ToString())),
                solutions = solutionsToBuild,
                AddToLog = mainWindow.AddToLog,
                UpdateProgress = UpdateProgress
            };
            _ViewModel.ProgressType = "Building checked solutions";
            var task = mainWindow.executor.Execute(action =>
            {
                buildExecution.Build(action);
            });
        }
        private CommandHandler _OpenSolutionCmd;
        public CommandHandler OpenSolutionCmd
        {
            get { return _OpenSolutionCmd ?? (_OpenSolutionCmd = new CommandHandler(param => OpenSolution(param), param => RemoveSolution_CanExecute(param))); }
        }
        public bool OpenSolution_CanExecute(object parameter)
        {
            return SelectedSolutionIndex != -1;
        }
        public void OpenSolution(object parameter)
        {
            SolutionObjectView solution = Solutions[SelectedSolutionIndex];
            StringBuilder path = new StringBuilder(_ViewModel.GetSetting("BaseDir", Header));
            path.Append("\\" + solution.Name);
            Process.Start(path.ToString());
        }
        private CommandHandler _CopySolutionsToCmd;
        public CommandHandler CopySolutionsToCmd
        {
            get { return _CopySolutionsToCmd ?? (_CopySolutionsToCmd = new CommandHandler(param => CopySolutionsTo(param), param => RemoveSolution_CanExecute(param))); }
        }
        public bool CopySolutionsTo_CanExecute(object parameter)
        {
            return _ViewModel.Tabs.Count > 1 && SelectedSolutionIndex != -1;
        }
        public void CopySolutionsTo(object parameter)
        {
            StringCollection tabNames = new StringCollection();
            foreach (var tab in _ViewModel.Tabs)
                if (tab.Header != Header)
                    tabNames.Add(tab.Header);
            var dialog = new View.ComboBoxQueryDialog() { Owner = Application.Current.MainWindow, DialogTitle = "Copy solutions to...", ComboBoxLabel = "Build tab", Entries = tabNames, SelectedEntry = tabNames[0] };
            if (dialog.ShowDialog() == true)
            {
                String tabName = dialog.SelectedEntry;
                BuildTabItem tab = _ViewModel.Tabs.First(x => x.Header == tabName);
                foreach (var solutionView in SelectedSolutionViews)
                {
                    if (solutionView != null)
                    {
                        if (!_Model.Scope2SolutionObjects.ContainsKey(tabName))
                            _Model.Scope2SolutionObjects[tabName] = new ObservableCollection<SolutionObject>();
                        _Model.Scope2SolutionObjects[tabName].Add(solutionView.SolutionObject);
                        SolutionObjectView newSolutionView = solutionView.Clone() as SolutionObjectView;
                        newSolutionView.IsSelected = false;
                        if (!tab.Solutions.Contains(newSolutionView))
                            tab.Solutions.Add(newSolutionView);
                    }
                }
                tab.SelectedSolutionIndex = -1;
            }
        }
    }
}
