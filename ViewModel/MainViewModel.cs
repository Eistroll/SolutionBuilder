using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using System.Xml;
using SolutionBuilder.View;
using SolutionBuilder.ViewModel;

namespace SolutionBuilder
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Public Properties

        public BindingList<DistributionItem> DistributionList
        {
            get => _DistributionList;
            set
            {
                _DistributionList = value;
                foreach (var distributionItem in _DistributionList)
                {
                    distributionItem.ApplyToAll += DistributionItem_ApplyToAll;
                    distributionItem.ApplyToSelected += DistributionItem_ApplyToSelected;
                }
            }
        }

        public int SelectedDistributionIndex { get; set; }
        public StringCollection Platforms { get; set; }
        public StringCollection Configurations { get; set; }

        public int SelectedSettingIndex
        {
            get => _SelectedSettingIndex;
            set
            {
                _SelectedSettingIndex = value;
                RemoveSettingCmd.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<Setting> SettingsList { get; set; }
        public ObservableCollection<BuildTabItem> Tabs { get; set; }
        public int SelectedTabIndex { get; set; }

        [IgnoreDataMemberAttribute]
        public ObservableCollection<string> AllSolutionsForSelectedTab
        {
            get
            {
                if (SelectedTabIndex >= 0 && SelectedTabIndex < Tabs.Count)
                    return Tabs[SelectedTabIndex].AllSolutionsInBaseDir;
                return new ObservableCollection<string>();
            }
        }

        [IgnoreDataMemberAttribute]
        public string Log
        {
            get => _Log;
            set
            {
                if (value != _Log)
                {
                    _Log = value;
                    NotifyPropertyChanged("Log");
                }
            }
        }

        public TaskbarItemProgressState ProgressState
        {
            get => _ProgressState;
            set
            {
                if (value != _ProgressState)
                {
                    _ProgressState = value;
                    NotifyPropertyChanged("ProgressState");
                    NotifyPropertyChanged("ProgressIsIndeterminate");
                }
            }
        }

        [IgnoreDataMemberAttribute]
        public string ProgressType
        {
            get => _ProgressType;
            set
            {
                if (value != _ProgressType)
                {
                    _ProgressType = value;
                    NotifyPropertyChanged("ProgressType");
                }
            }
        }

        [IgnoreDataMemberAttribute]
        public string ProgressDesc
        {
            get => _ProgressDesc;
            set
            {
                if (value != _ProgressDesc)
                {
                    _ProgressDesc = value;
                    NotifyPropertyChanged("ProgressDesc");
                }
            }
        }

        [IgnoreDataMemberAttribute]
        public double ProgressValue
        {
            get => _ProgressValue;
            set
            {
                if (value != _ProgressValue)
                {
                    _ProgressValue = value;
                    NotifyPropertyChanged("ProgressValue");
                }
            }
        }

        [IgnoreDataMemberAttribute]
        public bool ProgressIsIndeterminate => _ProgressState == TaskbarItemProgressState.Indeterminate;

        [IgnoreDataMemberAttribute]
        public State ProgressBuildState
        {
            get => _ProgressBuildState;
            set
            {
                if (value != _ProgressBuildState)
                {
                    _ProgressBuildState = value;
                    NotifyPropertyChanged("ProgressBuildState");
                }
            }
        }

        [IgnoreDataMemberAttribute]
        public string CompleteLog
        {
            get
            {
                var log = new StringBuilder();
                foreach (var tab in Tabs)
                foreach (var solution in tab.Solutions)
                    log.AppendLine(solution.BuildLog);
                return log.ToString();
            }
        }

        [IgnoreDataMemberAttribute] public Dictionary<string, string> DistributionSourceMap { get; set; }

        [IgnoreDataMemberAttribute] public Dictionary<string, string> DistributionTargetMap { get; set; }

        [IgnoreDataMemberAttribute] public ObservableCollection<string> Executables { get; set; }
        [IgnoreDataMemberAttribute] public int SelectedDistributionItems { get; set; }

        public ICommand AddSettingCmd
        {
            get { return _AddSettingCmd ?? (_AddSettingCmd = new CommandHandler(param => AddSetting())); }
        }

        public CommandHandler RemoveSettingCmd
        {
            get
            {
                return _RemoveSettingCmd ?? (_RemoveSettingCmd = new CommandHandler(param => RemoveSetting(param),
                    param => RemoveSetting_CanExecute(param)));
            }
        }

        #endregion

        #region Public Methods

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var toCompareWith = obj as MainViewModel;
            if (toCompareWith == null)
                return false;
            return true;
        }

        public void Init()
        {
            SettingsList.CollectionChanged += SettingsChangedMethod;
            foreach (var item in SettingsList)
            {
                item.PropertyChanged += Setting_PropertyChanged;
                if (item.Scope == Setting.Scopes.DistributionExe.ToString())
                    if (!Executables.Contains(item.Value))
                        Executables.Add(item.Value);
            }

            DistributionList.ListChanged += DistributionListChangedMethod;
            UpdateDistributionSourceMap();
            UpdateDistributionTargetMap();
        }

        public string GetSetting(string key, string scope)
        {
            foreach (var setting in SettingsList)
                if (setting.Scope == scope && setting.Key == key)
                    return setting.Value;
            return "";
        }

        public string GetSetting(string key, Setting.Scopes scope = Setting.Scopes.Base)
        {
            foreach (var setting in SettingsList)
                if (setting.Scope == scope.ToString() && setting.Key == key)
                    return setting.Value;
            return "";
        }

        public void Save()
        {
            var file = new FileInfo("DataViewModel.xml");
            var serializer = new DataContractSerializer(typeof(MainViewModel));
            var writer = new FileStream(file.Name, FileMode.Create);
            if (!writer.CanWrite)
                return;
            serializer.WriteObject(writer, this);
            writer.Close();
        }

        public static MainViewModel Load()
        {
            var file = new FileInfo("DataViewModel.xml");
            if (!file.Exists)
                return new MainViewModel();
            try
            {
                var stream = new FileStream(file.Name, FileMode.Open);
                var reader = XmlDictionaryReader.CreateTextReader(stream, new XmlDictionaryReaderQuotas());
                var serializer = new DataContractSerializer(typeof(MainViewModel));
                var model = (MainViewModel) serializer.ReadObject(reader, true);
                reader.Close();
                model.Init();
                return model;
            }
            catch (Exception)
            {
                return new MainViewModel();
            }
        }

        public void BindToModel(ref Model Model)
        {
            Model.PropertyChanged += Model_PropertyChanged;
            _Model = Model;
            var me = this;
            foreach (var tab in Tabs) tab.BindToModel(ref _Model, ref me);
        }

        public void UpdateLog(SolutionObjectView solution)
        {
            var logBuilder = new StringBuilder();
            logBuilder.Append(solution.BuildLog);
            Log = logBuilder.ToString();
        }

        public void AddSetting()
        {
            var scopes = new StringCollection();
            scopes.Add(Setting.Scopes.Base.ToString());
            scopes.Add(Setting.Scopes.DistributionSource.ToString());
            scopes.Add(Setting.Scopes.DistributionTarget.ToString());
            scopes.Add(Setting.Scopes.DistributionExe.ToString());
            foreach (var tab in Tabs)
                scopes.Add(tab.TabName);
            var dialog = new SettingCreationDialog
                {Owner = Application.Current.MainWindow, Scopes = scopes}; // TODO #GUI access SettingsDialog
            if (dialog.ShowDialog() == true)
            {
                var mainWindow = (MainWindow) Application.Current.MainWindow;
                if (mainWindow != null)
                    SettingsList.Add(new Setting {Scope = dialog.Scope, Key = dialog.Key, Value = dialog.Value});
            }
        }

        public bool RemoveSetting_CanExecute(object parameter)
        {
            return SelectedSettingIndex != -1;
        }

        public void RemoveSetting(object parameter)
        {
            SettingsList.RemoveAt(SelectedSettingIndex);
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Private Fields

        private ICommand _AddSettingCmd;
        private BindingList<DistributionItem> _DistributionList;
        private string _Log;

        private Model _Model;
        private State _ProgressBuildState;
        private string _ProgressDesc;
        private TaskbarItemProgressState _ProgressState;
        private string _ProgressType;
        private double _ProgressValue;
        private CommandHandler _RemoveSettingCmd;
        private int _SelectedSettingIndex = -1;

        #endregion

        #region Ctor

        // Constructor
        public MainViewModel()
        {
            DistributionList = new BindingList<DistributionItem>();
            Configurations = new StringCollection {"Release", "Debug"};
            Platforms = new StringCollection {"x86", "x64"};
            Executables = new ObservableCollection<string>();
            DistributionSourceMap = new Dictionary<string, string>();
            DistributionTargetMap = new Dictionary<string, string>();
            Tabs = new ObservableCollection<BuildTabItem>();
            var me = this;
            SettingsList = new ObservableCollection<Setting>
            {
                new Setting
                {
                    Scope = Setting.Scopes.Base.ToString(), Key = Setting.Executables.BuildExe.ToString(),
                    Value = @"C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild.exe"
                },
                new Setting
                {
                    Scope = Setting.Scopes.Base.ToString(), Key = Setting.Executables.CopyExe.ToString(),
                    Value = @"C:\Windows\System32\Robocopy.exe"
                }
            };
            SelectedSettingIndex = -1;
            ProgressState = TaskbarItemProgressState.Normal;
            ProgressValue = 0;
            Init();
        }

        #endregion

        #region Private Methods

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        private void DistributionListChangedMethod(object sender, ListChangedEventArgs e)
        {
            switch (e.ListChangedType)
            {
                case ListChangedType.Reset:
                    break;
                case ListChangedType.ItemAdded:
                    if (sender is IList<DistributionItem> list)
                    {
                        var item = list.ElementAt(e.NewIndex);
                        item.ApplyToAll += DistributionItem_ApplyToAll;
                        item.ApplyToSelected += DistributionItem_ApplyToSelected;
                    }

                    break;
                case ListChangedType.ItemDeleted:
                    // todo
                    break;
                case ListChangedType.ItemChanged:
                case ListChangedType.ItemMoved:
                case ListChangedType.PropertyDescriptorAdded:
                case ListChangedType.PropertyDescriptorDeleted:
                case ListChangedType.PropertyDescriptorChanged:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DistributionItem_ApplyToAll(object sender, DistributionItem.ApplyToAllEventArgs e)
        {
            if (sender is DistributionItem itemModified)
                foreach (var distributionItem in DistributionList)
                    if (distributionItem != itemModified)
                        distributionItem[e.PropertyName] = itemModified[e.PropertyName];
        }

        private void DistributionItem_ApplyToSelected(object sender, DistributionItem.ApplyToSelectedEventArgs e)
        {
            if (sender is DistributionItem itemModified && e.SelectedItems != null)
                foreach (var item in e.SelectedItems)
                    if (item is DistributionItem distributionItem && distributionItem != itemModified)
                        distributionItem[e.PropertyName] = itemModified[e.PropertyName];
        }

        private void SettingsChangedMethod(object sender, NotifyCollectionChangedEventArgs e)
        {
            //different kind of changes that may have occurred in collection
            if (e.Action == NotifyCollectionChangedAction.Add)
                foreach (Setting item in e.NewItems)
                {
                    item.PropertyChanged += Setting_PropertyChanged;
                    if (item.Scope == Setting.Scopes.DistributionSource.ToString() && item.Key != null)
                    {
                        DistributionSourceMap[item.Key] = item.Value;
                        NotifyPropertyChanged("DistributionSourceMap");
                    }

                    if (item.Scope == Setting.Scopes.DistributionTarget.ToString() && item.Key != null)
                    {
                        var distributionItem = new DistributionItem {Folder = item.Key};
                        DistributionList.Add(distributionItem);
                        DistributionTargetMap[item.Key] = item.Value;
                        NotifyPropertyChanged("DistributionTargetMap");
                    }

                    if (item.Scope == Setting.Scopes.DistributionExe.ToString() && item.Key != null)
                    {
                        if (!Executables.Contains(item.Value)) Executables.Add(item.Value);
                        var distribution = DistributionList.FirstOrDefault(x => x.Folder == item.Key);
                        if (distribution != null) distribution.Executable = item.Value;
                    }
                }

            if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (Setting item in e.OldItems)
                    item.PropertyChanged -= Setting_PropertyChanged;
                foreach (Setting item in e.NewItems)
                    item.PropertyChanged += Setting_PropertyChanged;
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
                foreach (Setting item in e.OldItems)
                {
                    item.PropertyChanged -= Setting_PropertyChanged;
                    if (item.Scope == Setting.Scopes.DistributionSource.ToString() && item.Key != null)
                        DistributionSourceMap.Remove(item.Key);
                    if (item.Scope == Setting.Scopes.DistributionTarget.ToString() && item.Key != null)
                        DistributionTargetMap.Remove(item.Key);
                    if (item.Scope == Setting.Scopes.DistributionExe.ToString() && item.Key != null)
                    {
                        var setting = SettingsList.First(x => x.Scope == item.Scope && x.Value == item.Value);
                        if (setting == null && Executables.Contains(item.Value)) Executables.Remove(item.Value);
                        var distribution = DistributionList.First(x => x.Folder == item.Key);
                        if (distribution != null) distribution.Executable = "";
                    }
                }

            if (e.Action == NotifyCollectionChangedAction.Move)
            {
                //your code
            }
        }

        private void UpdateDistributionSourceMap()
        {
            foreach (var set in SettingsList)
                if (set.Scope == Setting.Scopes.DistributionSource.ToString())
                    DistributionSourceMap[set.Key] = set.Value;
        }

        private void UpdateDistributionTargetMap()
        {
            foreach (var set in SettingsList)
                if (set.Scope == Setting.Scopes.DistributionTarget.ToString())
                    DistributionTargetMap[set.Key] = set.Value;
        }

        private void Setting_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var setting = (Setting) sender;
            foreach (var Tab in Tabs)
                if (Tab.TabName == setting.Scope)
                {
                    Tab.UpdateAvailableSolutions();
                    NotifyPropertyChanged("AllSolutionsForSelectedTab");
                }

            if (setting.Scope == Setting.Scopes.DistributionSource.ToString())
            {
                DistributionSourceMap[setting.Key] = setting.Value;
                NotifyPropertyChanged("DistributionSourceMap");
            }

            if (setting.Scope == Setting.Scopes.DistributionTarget.ToString())
            {
                DistributionTargetMap[setting.Key] = setting.Value;
                NotifyPropertyChanged("DistributionTargetMap");
            }
        }

        private void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            foreach (var tab in Tabs)
                if (tab.BaseDir == null)
                {
                    tab.BaseDir = GetSetting("BaseDir", tab.TabName);
                    SettingsList.Remove(new Setting {Scope = tab.TabName, Key = "BaseDir"});
                }
        }

        #endregion
    }
}