﻿using SolutionBuilder.View;
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
        [DataMember(Name = "Header")]
        private string _TabName { get; set; }
        public string TabName
        {
            get { return _TabName; }
            set
            {
                if (_TabName != value)
                {
                    string oldTabName = _TabName;
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
        [DataMember]
        public String SelectedPath { get; set; }

        [DataMember]
        public String BaseDir
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

        [DataMember]
        public String BuildExe { get; set; }
        [DataMember]
        public String BaseOptions { get; set; }
        [DataMember]
        public StringCollection CheckedSolutions { get; set; }
        [DataMember]
        private String _SelectedConfiguration;
        public String SelectedConfiguration
        {
            get { return _SelectedConfiguration; }
            set
            {
                _SelectedConfiguration = value;
                for (int i = 0; i < Solutions.Count; ++i)
                {
                    Solutions[i].Options = _Model.Scope2SolutionObjects[TabName][i].Options[_SelectedConfiguration];
                    Solutions[i].PostBuildStep = _Model.Scope2SolutionObjects[TabName][i].PostBuildSteps[_SelectedConfiguration];
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
        public StringCollection Configurations { get; set; }
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
            OnCreated();
        }
        private void OnCreated()
        {
            AllSolutionsInBaseDir = new ObservableCollection<string>();
            CheckedSolutions = new StringCollection();
            Solutions = new ObservableCollection<SolutionObjectView>();
            Configurations = new StringCollection() { "Release", "Debug" };
            _SelectedConfiguration = "Debug";
            SelectedSolutionIndex = -1;
        }
        [OnDeserializing]
        private void OnDeserializing(StreamingContext c)
        {
            OnCreated();
        }
        public IEnumerable<SolutionObjectView> SelectedSolutionViews
        {
            get { return _Solutions.Where(o => o.IsSelected); }
        }
        public void UpdateAvailableSolutions()
        {
            AllSolutionsInBaseDir.Clear();
            if (string.IsNullOrEmpty(BaseDir))
                return;
            System.IO.DirectoryInfo baseDirInfo = new System.IO.DirectoryInfo(BaseDir);
            if (baseDirInfo.Exists)
            {
                var solutionPaths = Directory.GetFiles(BaseDir, @"*.sln", SearchOption.AllDirectories);
                foreach (var path in solutionPaths)
                {
                    String newPath = path.Replace(BaseDir, "");
                    AllSolutionsInBaseDir.Add(newPath);
                }
                // Trigger notification so that Combobox selects the stored solution
                foreach (var solution in Solutions)
                {
                    solution.NotifyAllPropertiesChanged();
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
            if (Model.Scope2SolutionObjects.Count == 0 || !Model.Scope2SolutionObjects.ContainsKey(TabName))
                return;
            foreach (SolutionObject solution in Model.Scope2SolutionObjects[TabName])
            {
                SolutionObject tmp = solution;
                SolutionObjectView solutionView = new SolutionObjectView(ref tmp, SelectedConfiguration);
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
                    _Model.Scope2SolutionObjects[TabName].Insert(index++, solution.SolutionObject);
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (SolutionObjectView solution in e.OldItems)
                {
                    _Model.Scope2SolutionObjects[TabName].Remove(solution.SolutionObject);
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
                    solView.SolutionObject.Options[SelectedConfiguration] = solView.Options;
                }
            }
            if (e.PropertyName == "PostBuildStep")
            {
                SolutionObjectView solView = (SolutionObjectView)sender;
                if (solView == null)
                    return;
                if (solView.SolutionObject != null)
                {
                    solView.SolutionObject.PostBuildSteps[SelectedConfiguration] = solView.PostBuildStep;
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
        private ICommand _OpenSettingsCmd;
        public ICommand OpenSettingsCmd
        {
            get { return _OpenSettingsCmd ?? (_OpenSettingsCmd = new CommandHandler(param => OpenSettings())); }
        }
        private ICommand _BuildCmd;
        public ICommand BuildCmd
        {
            get { return _BuildCmd ?? (_BuildCmd = new CommandHandler(param => BuildCheckedSolutions())); }
        }
        private ICommand _CancelCmd;
        public ICommand CancelCmd
        {
            get { return _CancelCmd ?? (_CancelCmd = new CommandHandler(param => CancelBuild())); }
        }
        private ICommand _AddSolutionCmd;
        public ICommand AddSolutionCmd
        {
            get { return _AddSolutionCmd ?? (_AddSolutionCmd = new CommandHandler(param => AddSolution())); }
        }
        public void AddSolution()
        {
            SolutionObject solution = new SolutionObject();
            if (!_Model.Scope2SolutionObjects.ContainsKey(TabName))
            {
                _Model.Scope2SolutionObjects[TabName] = new ObservableCollection<SolutionObject>();
            }
            _Model.Scope2SolutionObjects[TabName].Add(solution);
            SolutionObjectView solutionView = new SolutionObjectView(ref solution, SelectedConfiguration);
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
                    _Model.Scope2SolutionObjects[TabName].Remove(solutionView.SolutionObject);
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
            get { return _BuildSolutionCmd ?? (_BuildSolutionCmd = new CommandHandler(param => BuildSelectedSolutions(param), param => RemoveSolution_CanExecute(param))); }
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
            double progress = current == 0 ? 0.001 : (double)current / (max - min);
            _ViewModel.ProgressValue = Math.Min(1.0, progress);
            _ViewModel.ProgressDesc = text;
            if (current == min && !failure)
            {
                _ViewModel.ProgressBuildState = View.State.None;
                _ViewModel.ProgressState = (min + 1 == max) ? System.Windows.Shell.TaskbarItemProgressState.Indeterminate : System.Windows.Shell.TaskbarItemProgressState.Normal;
            }
            if (failure && _ViewModel.ProgressState != System.Windows.Shell.TaskbarItemProgressState.Error)
            {
                _ViewModel.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
            }
            if (current >= min && current < max)
                ProgressVisible = true;
            else if (current == max)
            {
                ProgressVisible = false;
                _ViewModel.ProgressBuildState = View.State.Success;
                _ViewModel.ProgressState = failure ? System.Windows.Shell.TaskbarItemProgressState.Error : System.Windows.Shell.TaskbarItemProgressState.None;
                _ViewModel.ProgressType = "";
                _ViewModel.ProgressDesc = "";
            }
        }
        public void BuildSelectedSolutions(object parameter)
        {
            Collection<SolutionObjectView> solutions = new Collection<SolutionObjectView>();
            foreach (var solutionView in SelectedSolutionViews)
                solutions.Add(solutionView);
            BuildSolutions(solutions, "Building selected solutions");
        }
        private void OpenSettings()
        {
            View.MainWindow mainWindow = (View.MainWindow)System.Windows.Application.Current.MainWindow;
            if (mainWindow == null)
                return;
            System.Windows.Window settings = new BuildTabSettings() { DataContext = this, Owner = mainWindow };
            settings.ShowDialog();
        }
        public void CancelBuild()
        {
            View.MainWindow mainWindow = (View.MainWindow)System.Windows.Application.Current.MainWindow;
            if (mainWindow == null)
                return;
            mainWindow.executor.Cancel();
        }
        public void BuildSolutions(ICollection<SolutionObjectView> solutionsToBuild, string progressText)
        {
            View.MainWindow mainWindow = (View.MainWindow)System.Windows.Application.Current.MainWindow;
            if (mainWindow == null)
                return;
            mainWindow.ClearLog();
            BuildState = View.State.None;
            ProgressVisible = true;
            Builder buildExecution = new Builder()
            {
                BaseDir = BaseDir,
                BaseOptions = BaseOptions,
                BuildExe = new FileInfo(string.IsNullOrEmpty(BuildExe) ? _ViewModel.GetSetting(Setting.Executables.BuildExe.ToString()) : BuildExe),
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
            foreach (SolutionObjectView solution in Solutions)
            {
                solution.BuildState = View.State.None;
                if (solution.Checked)
                {
                    solutionsToBuild.Add(solution);
                }
            }
            BuildSolutions(solutionsToBuild, "Building checked solutions");
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
            StringBuilder path = new StringBuilder(BaseDir);
            path.Append("\\" + solution.Name);
            Process.Start(path.ToString());
        }
        private CommandHandler _CopySolutionsToCmd;
        private string _BaseDir;

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
                if (tab.TabName != TabName)
                    tabNames.Add(tab.TabName);
            var dialog = new View.ComboBoxQueryDialog() { Owner = Application.Current.MainWindow, DialogTitle = "Copy solutions to...", ComboBoxLabel = "Build tab", Entries = tabNames, SelectedEntry = tabNames[0] };
            if (dialog.ShowDialog() == true)
            {
                String tabName = dialog.SelectedEntry;
                BuildTabItem tab = _ViewModel.Tabs.First(x => x.TabName == tabName);
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
