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
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using SolutionBuilder.View;

namespace SolutionBuilder
{
    [DataContract]
    public class BuildTabItem : INotifyPropertyChanged
    {
        #region Public Properties

        public string TabName
        {
            get => _TabName;
            set
            {
                if (_TabName != value)
                {
                    var oldTabName = _TabName;
                    _TabName = value;
                    if (_Model != null && _Model.Scope2SolutionObjects.ContainsKey(oldTabName))
                    {
                        var oldData = _Model.Scope2SolutionObjects[oldTabName];
                        _Model.Scope2SolutionObjects.Remove(oldTabName);
                        _Model.Scope2SolutionObjects[_TabName] = oldData;
                    }

                    NotifyPropertyChanged("TabName");
                }
            }
        }

        [DataMember] public string SelectedPath { get; set; }

        [DataMember]
        public string BaseDir
        {
            get => _BaseDir;
            set
            {
                if (!value.Equals(_BaseDir))
                {
                    _BaseDir = value;
                    UpdateAvailableSolutions();
                }
            }
        }

        [DataMember] public string BuildExe { get; set; }

        [DataMember] public string BaseOptions { get; set; }

        [DataMember] public StringCollection CheckedSolutions { get; set; }

        public string SelectedConfiguration
        {
            get => _SelectedConfiguration;
            set
            {
                _SelectedConfiguration = value;
                for (var i = 0; i < Solutions.Count; ++i)
                {
                    Solutions[i].Options = _Model.Scope2SolutionObjects[TabName][i].Options[_SelectedConfiguration];
                    if (_Model.Scope2SolutionObjects[TabName][i].PostBuildSteps
                        .TryGetValue(_SelectedConfiguration, out var step))
                        Solutions[i].PostBuildStep = step;
                }
            }
        }

        [DataMember] public bool DoBuild { get; set; }

        //////////////////////////////////////////////////////////////////////////
        //Not serialized Data members
        //////////////////////////////////////////////////////////////////////////
        public StringCollection Configurations { get; set; }

        public int SelectedSolutionIndex
        {
            get => _SelectedSolutionIndex;
            set
            {
                _SelectedSolutionIndex = value;
                RemoveSolutionCmd.RaiseCanExecuteChanged();
                if (_SelectedSolutionIndex != -1)
                    _ViewModel.UpdateLog(Solutions[_SelectedSolutionIndex]);
            }
        }

        public ObservableCollection<SolutionObjectView> Solutions { get; set; }

        public ObservableCollection<string> AllSolutionsInBaseDir
        {
            get => _AllSolutionsInBaseDir ?? (_AllSolutionsInBaseDir = new ObservableCollection<string>());
            set => _AllSolutionsInBaseDir = value;
        }

        public State BuildState
        {
            get => _BuildState;
            set
            {
                if (_BuildState != value)
                {
                    _BuildState = value;
                    NotifyPropertyChanged("BuildState");
                }
            }
        }

        public bool ProgressVisible
        {
            get => _ProgressVisible;
            set
            {
                if (_ProgressVisible != value)
                {
                    _ProgressVisible = value;
                    NotifyPropertyChanged("ProgressVisible");
                }
            }
        }

        public int ProgressMin
        {
            get => _ProgressMin;
            set
            {
                if (_ProgressMin != value)
                {
                    _ProgressMin = value;
                    NotifyPropertyChanged("ProgressMin");
                }
            }
        }

        public int ProgressMax
        {
            get => _ProgressMax;
            set
            {
                if (_ProgressMax != value)
                {
                    _ProgressMax = value;
                    NotifyPropertyChanged("ProgressMax");
                }
            }
        }

        public int ProgressCurrent
        {
            get => _ProgressCurrent;
            set
            {
                if (_ProgressCurrent != value)
                {
                    _ProgressCurrent = value;
                    NotifyPropertyChanged("ProgressCurrent");
                }
            }
        }

        public IEnumerable<SolutionObjectView> SelectedSolutionViews
        {
            get { return Solutions.Where(o => o.IsSelected); }
        }

        public ICommand OpenSettingsCmd
        {
            get { return _OpenSettingsCmd ?? (_OpenSettingsCmd = new CommandHandler(param => OpenSettings())); }
        }

        public ICommand BuildCmd
        {
            get { return _BuildCmd ?? (_BuildCmd = new CommandHandler(param => BuildCheckedSolutions())); }
        }

        public ICommand CancelCmd
        {
            get { return _CancelCmd ?? (_CancelCmd = new CommandHandler(param => CancelBuild())); }
        }

        public ICommand AddSolutionCmd
        {
            get { return _AddSolutionCmd ?? (_AddSolutionCmd = new CommandHandler(param => AddSolution())); }
        }

        public CommandHandler RemoveSolutionCmd
        {
            get
            {
                return _RemoveSolutionCmd ?? (_RemoveSolutionCmd = new CommandHandler(param => RemoveSolution(param),
                    param => RemoveSolution_CanExecute(param)));
            }
        }

        public CommandHandler BuildSolutionCmd
        {
            get
            {
                return _BuildSolutionCmd ?? (_BuildSolutionCmd =
                    new CommandHandler(param => BuildSelectedSolutions(param),
                        param => RemoveSolution_CanExecute(param)));
            }
        }

        public CommandHandler OpenSolutionCmd
        {
            get
            {
                return _OpenSolutionCmd ?? (_OpenSolutionCmd = new CommandHandler(param => OpenSolution(param),
                    param => RemoveSolution_CanExecute(param)));
            }
        }

        public CommandHandler CopySolutionsToCmd
        {
            get
            {
                return _CopySolutionsToCmd ?? (_CopySolutionsToCmd = new CommandHandler(param => CopySolutionsTo(param),
                    param => RemoveSolution_CanExecute(param)));
            }
        }

        #endregion

        #region Public Methods

        public void UpdateAvailableSolutions()
        {
            AllSolutionsInBaseDir.Clear();
            if (string.IsNullOrEmpty(BaseDir))
                return;
            var baseDirInfo = new DirectoryInfo(BaseDir);
            if (baseDirInfo.Exists)
            {
                var solutionPaths = Directory.GetFiles(BaseDir, @"*.sln", SearchOption.AllDirectories);
                foreach (var path in solutionPaths)
                {
                    var newPath = path.Replace(BaseDir, "");
                    AllSolutionsInBaseDir.Add(newPath);
                }

                // Trigger notification so that Combobox selects the stored solution
                foreach (var solution in Solutions) solution.NotifyAllPropertiesChanged();
            }
        }

        public void BindToModel(ref Model Model, ref MainViewModel ViewModel)
        {
            _Model = Model;
            _ViewModel = ViewModel;
            UpdateAvailableSolutions();

            UpdateFromModel(ref Model);
        }

        public void AddSolution()
        {
            var solution = new SolutionObject();
            if (!_Model.Scope2SolutionObjects.ContainsKey(TabName))
                _Model.Scope2SolutionObjects[TabName] = new ObservableCollection<SolutionObject>();
            _Model.Scope2SolutionObjects[TabName].Add(solution);
            var solutionView = new SolutionObjectView(ref solution, SelectedConfiguration);
            solutionView.PropertyChanged += SolutionView_PropertyChanged;
            ExplicitAddInProgress = true;
            Solutions.Add(solutionView);
            ExplicitAddInProgress = false;
        }

        public bool RemoveSolution_CanExecute(object parameter)
        {
            return SelectedSolutionIndex != -1;
        }

        public void RemoveSolution(object parameter)
        {
            var indexListToRemove = new List<int>();
            foreach (var solutionView in SelectedSolutionViews)
                if (solutionView != null)
                {
                    _Model.Scope2SolutionObjects[TabName].Remove(solutionView.SolutionObject);
                    indexListToRemove.Add(Solutions.IndexOf(solutionView));
                }

            indexListToRemove.Reverse();
            ExplicitRemoveInProgress = true;
            foreach (var index in indexListToRemove)
                Solutions.RemoveAt(index);
            ExplicitRemoveInProgress = false;
            SelectedSolutionIndex = Solutions.Count - 1;
        }

        public bool BuildSolution_CanExecute(object parameter)
        {
            return SelectedSolutionIndex != -1;
        }

        public void DoUpdateProgress(int min, int max, int current, string text, bool failure)
        {
            ProgressMin = min;
            ProgressMax = max;
            ProgressCurrent = current;
            var progress = current == 0 ? 0.001 : (double)current / (max - min);
            _ViewModel.ProgressValue = Math.Min(1.0, progress);
            _ViewModel.ProgressDesc = text;
            if (current == min && !failure)
            {
                _ViewModel.ProgressBuildState = State.None;
                _ViewModel.ProgressState = min + 1 == max
                    ? TaskbarItemProgressState.Indeterminate
                    : TaskbarItemProgressState.Normal;
            }

            if (failure && _ViewModel.ProgressState != TaskbarItemProgressState.Error)
                _ViewModel.ProgressState = TaskbarItemProgressState.Error;
            if (current >= min && current < max)
            {
                ProgressVisible = true;
            }
            else if (current == max)
            {
                ProgressVisible = false;
                _ViewModel.ProgressBuildState = State.Success;
                _ViewModel.ProgressState = failure ? TaskbarItemProgressState.Error : TaskbarItemProgressState.None;
                _ViewModel.ProgressType = "";
                _ViewModel.ProgressDesc = "";
            }
        }

        public void BuildSelectedSolutions(object parameter)
        {
            var solutions = new Collection<SolutionObjectView>();
            foreach (var solutionView in SelectedSolutionViews)
                solutions.Add(solutionView);
            BuildSolutions(solutions, "Building selected solutions");
        }

        public void CancelBuild()
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            if (mainWindow == null)
                return;
            mainWindow.executor.Cancel();
        }

        public void BuildSolutions(ICollection<SolutionObjectView> solutionsToBuild, string progressText)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            if (mainWindow == null)
                return;
            mainWindow.ClearLog();
            BuildState = State.None;
            ProgressVisible = true;
            var buildExecution = new Builder
            {
                BaseDir = BaseDir,
                BaseOptions = BaseOptions,
                BuildExe = new FileInfo(string.IsNullOrEmpty(BuildExe)
                    ? _ViewModel.GetSetting(Setting.Executables.BuildExe.ToString())
                    : BuildExe),
                solutions = solutionsToBuild,
                AddToLog = mainWindow.AddToLog,
                UpdateProgress = DoUpdateProgress
            };
            _ViewModel.ProgressType = progressText;
            var task = mainWindow.executor.Execute(action =>
            {
                buildExecution.Build(action, ref mainWindow.executor.CurrentProcessId);
            });
        }

        public void BuildCheckedSolutions()
        {
            ICollection<SolutionObjectView> solutionsToBuild = new Collection<SolutionObjectView>();
            foreach (var solution in Solutions)
            {
                solution.BuildState = State.None;
                if (solution.Checked) solutionsToBuild.Add(solution);
            }

            BuildSolutions(solutionsToBuild, "Building checked solutions");
        }

        public bool OpenSolution_CanExecute(object parameter)
        {
            return SelectedSolutionIndex != -1;
        }

        public void OpenSolution(object parameter)
        {
            var solution = Solutions[SelectedSolutionIndex];
            var path = new StringBuilder(BaseDir);
            path.Append("\\" + solution.Name);
            Process.Start(path.ToString());
        }

        public bool CopySolutionsTo_CanExecute(object parameter)
        {
            return _ViewModel.Tabs.Count > 1 && SelectedSolutionIndex != -1;
        }

        public void CopySolutionsTo(object parameter)
        {
            var tabNames = new StringCollection();
            foreach (var tab in _ViewModel.Tabs)
                if (tab.TabName != TabName)
                    tabNames.Add(tab.TabName);
            var dialog = new ComboBoxQueryDialog
            {
                Owner = Application.Current.MainWindow,
                DialogTitle = "Copy solutions to...",
                ComboBoxLabel = "Build tab",
                Entries = tabNames,
                SelectedEntry = tabNames[0]
            };
            if (dialog.ShowDialog() == true)
            {
                var tabName = dialog.SelectedEntry;
                var tab = _ViewModel.Tabs.First(x => x.TabName == tabName);
                foreach (var solutionView in SelectedSolutionViews)
                    if (solutionView != null)
                    {
                        if (!_Model.Scope2SolutionObjects.ContainsKey(tabName))
                            _Model.Scope2SolutionObjects[tabName] = new ObservableCollection<SolutionObject>();
                        _Model.Scope2SolutionObjects[tabName].Add(solutionView.SolutionObject);
                        var newSolutionView = solutionView.Clone() as SolutionObjectView;
                        newSolutionView.IsSelected = false;
                        if (!tab.Solutions.Contains(newSolutionView))
                            tab.Solutions.Add(newSolutionView);
                    }

                tab.SelectedSolutionIndex = -1;
            }
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Private Fields

        private ICommand _AddSolutionCmd;
        private ObservableCollection<string> _AllSolutionsInBaseDir;
        private string _BaseDir;
        private ICommand _BuildCmd;
        private CommandHandler _BuildSolutionCmd;
        private State _BuildState;
        private ICommand _CancelCmd;
        private CommandHandler _CopySolutionsToCmd;
        private Model _Model;
        private ICommand _OpenSettingsCmd;
        private CommandHandler _OpenSolutionCmd;
        private int _ProgressCurrent;
        private int _ProgressMax;
        private int _ProgressMin;
        private bool _ProgressVisible;
        private CommandHandler _RemoveSolutionCmd;

        [DataMember] private string _SelectedConfiguration;

        [DataMember] private int _SelectedSolutionIndex;

        private MainViewModel _ViewModel;
        private bool ExplicitAddInProgress;
        private bool ExplicitRemoveInProgress;

        #endregion

        #region Ctor

        public BuildTabItem()
        {
            OnCreated();
        }

        #endregion

        #region Private Methods

        [DataMember(Name = "Header")] private string _TabName { get; set; }

        private void OnCreated()
        {
            AllSolutionsInBaseDir = new ObservableCollection<string>();
            CheckedSolutions = new StringCollection();
            Solutions = new ObservableCollection<SolutionObjectView>();
            Configurations = new StringCollection { "Release", "Debug" };
            _SelectedConfiguration = "Debug";
            SelectedSolutionIndex = -1;
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext c)
        {
            OnCreated();
        }

        private void UpdateFromModel(ref Model Model)
        {
            Solutions.Clear();
            if (Model.Scope2SolutionObjects.Count == 0 || !Model.Scope2SolutionObjects.ContainsKey(TabName))
                return;
            foreach (var solution in Model.Scope2SolutionObjects[TabName])
            {
                var tmp = solution;
                var solutionView = new SolutionObjectView(ref tmp, SelectedConfiguration);
                if (CheckedSolutions != null && CheckedSolutions.Contains(tmp.Name))
                    solutionView.Checked = true;
                solutionView.PropertyChanged += SolutionView_PropertyChanged;
                Solutions.Add(solutionView);
            }

            Solutions.CollectionChanged += Solutions_CollectionChanged;
        }

        private void Solutions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ExplicitAddInProgress || ExplicitRemoveInProgress)
                return;
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var index = e.NewStartingIndex;
                foreach (SolutionObjectView solution in e.NewItems)
                    _Model.Scope2SolutionObjects[TabName].Insert(index++, solution.SolutionObject);
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
                foreach (SolutionObjectView solution in e.OldItems)
                    _Model.Scope2SolutionObjects[TabName].Remove(solution.SolutionObject);
        }

        private void SolutionView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Options")
            {
                var solView = (SolutionObjectView)sender;
                if (solView == null)
                    return;
                if (solView.SolutionObject != null)
                    solView.SolutionObject.Options[SelectedConfiguration] = solView.Options;
            }

            if (e.PropertyName == "PostBuildStep")
            {
                var solView = (SolutionObjectView)sender;
                if (solView == null)
                    return;
                if (solView.SolutionObject != null)
                    solView.SolutionObject.PostBuildSteps[SelectedConfiguration] = solView.PostBuildStep;
            }

            if (e.PropertyName == "Checked")
            {
                var solutionView = (SolutionObjectView)sender;
                if (solutionView == null)
                    return;
                if (!solutionView.Checked)
                {
                    CheckedSolutions.Remove(solutionView.Name);
                }
                else
                {
                    if (CheckedSolutions == null)
                        CheckedSolutions = new StringCollection();
                    CheckedSolutions.Add(solutionView.Name);
                }
            }
        }

        private void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void OpenSettings()
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            if (mainWindow == null)
                return;
            Window settings = new BuildTabSettings { DataContext = this, Owner = mainWindow };
            settings.ShowDialog();
        }

        #endregion
    }
}