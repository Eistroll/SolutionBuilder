using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using SolutionBuilder.ViewModel;
using System.Collections.Generic;
using System.Windows.Input;
using SolutionBuilder.View;
using System.Linq;
using System.Windows;
using System.Windows.Shell;

namespace SolutionBuilder
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public BindingList<DistributionItem> DistributionList { get; set; }
        public StringCollection Platforms { get; set; }
        public StringCollection Configurations { get; set; }
        private int _SelectedSettingIndex = -1;
        public int SelectedSettingIndex
        {
            get { return _SelectedSettingIndex; }
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
                else
                    return new ObservableCollection<string>();
            }
        }
        private String _Log;
        [IgnoreDataMemberAttribute]
        public String Log
        {
            get { return _Log; }
            set { if (value != _Log) { _Log = value; NotifyPropertyChanged("Log"); } }
        }
        private TaskbarItemProgressState _ProgressState;
        public TaskbarItemProgressState ProgressState
        {
            get { return _ProgressState; }
            set { if (value != _ProgressState) { _ProgressState = value; NotifyPropertyChanged("ProgressState");  NotifyPropertyChanged("ProgressIsIndeterminate");} }
        }
        private String _ProgressType;
        [IgnoreDataMemberAttribute]
        public String ProgressType
        {
            get { return _ProgressType; }
            set { if (value != _ProgressType) { _ProgressType = value; NotifyPropertyChanged("ProgressType"); } }
        }
        private String _ProgressDesc;
        [IgnoreDataMemberAttribute]
        public String ProgressDesc
        {
            get { return _ProgressDesc; }
            set { if (value != _ProgressDesc) { _ProgressDesc = value; NotifyPropertyChanged("ProgressDesc"); } }
        }
        private double _ProgressValue;
        [IgnoreDataMemberAttribute]
        public double ProgressValue
        {
            get { return _ProgressValue; }
            set { if (value != _ProgressValue) { _ProgressValue = value; NotifyPropertyChanged("ProgressValue"); } }
        }
        [IgnoreDataMemberAttribute]
        public bool ProgressIsIndeterminate
        {
            get { return _ProgressState == System.Windows.Shell.TaskbarItemProgressState.Indeterminate; }
        }
        private View.State _ProgressBuildState;
        [IgnoreDataMemberAttribute]
        public View.State ProgressBuildState
        {
            get { return _ProgressBuildState; }
            set { if (value != _ProgressBuildState) { _ProgressBuildState = value; NotifyPropertyChanged("ProgressBuildState"); } }
        }
        [IgnoreDataMemberAttribute]
        public String CompleteLog
        {
            get
            {
                StringBuilder log = new StringBuilder();
                foreach (var tab in Tabs)
                {
                    foreach (SolutionObjectView solution in tab.Solutions)
                    {
                        log.AppendLine(solution.BuildLog);
                    }
                }
                return log.ToString();
            }
        }
        [IgnoreDataMemberAttribute]
        public Dictionary<string, string> DistributionSourceMap { get; set; }
        [IgnoreDataMemberAttribute]
        public Dictionary<string, string> DistributionTargetMap { get; set; }
        [IgnoreDataMemberAttribute]
        public ObservableCollection<string> Executables { get; set; }

        private Model _Model;
        // Constructor
        public MainViewModel()
        {
            DistributionList = new BindingList<DistributionItem>();
            Configurations = new StringCollection() { "Release", "Debug" };
            Platforms = new StringCollection() { "x86", "x64" };
            Executables = new ObservableCollection<string>();
            DistributionSourceMap = new Dictionary<string, string>();
            DistributionTargetMap = new Dictionary<string, string>();
            Tabs = new ObservableCollection<BuildTabItem>();
            var me = this;
            SettingsList = new ObservableCollection<Setting>
            {
                new Setting { Scope = Setting.Scopes.Base.ToString(), Key = Setting.Executables.BuildExe.ToString(), Value = @"C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild.exe" },
                new Setting { Scope = Setting.Scopes.Base.ToString(), Key = Setting.Executables.CopyExe.ToString(), Value= @"C:\Windows\System32\Robocopy.exe" },
            };
            SelectedSettingIndex = -1;
            ProgressState = TaskbarItemProgressState.Normal;
            ProgressValue = 0;
            Init();
        }
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
            SettingsList.CollectionChanged += new NotifyCollectionChangedEventHandler(SettingsChangedMethod);
            foreach (Setting item in SettingsList)
            {
                item.PropertyChanged += Setting_PropertyChanged;
                if(item.Scope == Setting.Scopes.DistributionExe.ToString())
                    if (!Executables.Contains(item.Value))
                        Executables.Add(item.Value);
            }

            DistributionList.ListChanged += new ListChangedEventHandler(DistributionListChangedMethod);
            UpdateDistributionSourceMap();
            UpdateDistributionTargetMap();
        }
        public String GetSetting(String key, string scope)
        {
            foreach (Setting setting in SettingsList)
            {
                if (setting.Scope == scope && setting.Key == key)
                {
                    return setting.Value;
                }
            }
            return "";
        }
        public String GetSetting(String key, Setting.Scopes scope = Setting.Scopes.Base)
        {
            foreach (Setting setting in SettingsList)
            {
                if (setting.Scope == scope.ToString() && setting.Key == key)
                {
                    return setting.Value;
                }
            }
            return "";
        }
        public void Save()
        {
            FileInfo file = new FileInfo("DataViewModel.xml");
            DataContractSerializer serializer = new DataContractSerializer(typeof(MainViewModel));
            FileStream writer = new FileStream(file.Name, FileMode.Create);
            if (!writer.CanWrite)
                return;
            serializer.WriteObject(writer, this);
            writer.Close();
        }
        public static MainViewModel Load()
        {
            FileInfo file = new FileInfo("DataViewModel.xml");
            if (!file.Exists)
                return new MainViewModel();
            try
            {
                FileStream stream = new FileStream(file.Name, FileMode.Open);
                XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(stream, new XmlDictionaryReaderQuotas());
                DataContractSerializer serializer = new DataContractSerializer(typeof(MainViewModel));
                MainViewModel model = (MainViewModel)serializer.ReadObject(reader, true);
                reader.Close();
                model.Init();
                return model;
            }
            catch (System.Exception)
            {
                return new MainViewModel();
            }
        }

        public void BindToModel(ref Model Model)
        {
            Model.PropertyChanged += new PropertyChangedEventHandler(Model_PropertyChanged);
            _Model = Model;
            var me = this;
            foreach (var tab in Tabs)
            {
                tab.BindToModel(ref _Model, ref me);
            }
        }
        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }
        public void UpdateLog(SolutionObjectView solution)
        {
            StringBuilder logBuilder = new StringBuilder();
            logBuilder.Append(solution.BuildLog);
            Log = logBuilder.ToString();
        }
        private void DistributionListChangedMethod(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType==ListChangedType.ItemChanged)
            {
                if (e.PropertyDescriptor.Name == "ApplyToAllProperty")
                {
                    DistributionItem itemModified = DistributionList[e.NewIndex];
                    foreach ( DistributionItem item in DistributionList)
                    {
                        item[itemModified.ApplyToAllProperty] = itemModified[itemModified.ApplyToAllProperty];
                    }
                }
            }
        }
        private void SettingsChangedMethod(object sender, NotifyCollectionChangedEventArgs e)
        {
            //different kind of changes that may have occurred in collection
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
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
                        DistributionList.Add(new DistributionItem() { Folder = item.Key });
                        DistributionTargetMap[item.Key] = item.Value;
                        NotifyPropertyChanged("DistributionTargetMap");
                    }
                    if (item.Scope == Setting.Scopes.DistributionExe.ToString() && item.Key != null)
                    {
                        if ( !Executables.Contains(item.Value) )
                        {
                            Executables.Add(item.Value);
                        }
                        var distribution = DistributionList.First(x => x.Folder == item.Key);
                        if ( distribution != null )
                        {
                            distribution.Executable = item.Value;
                        }
                    }
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
            {
                foreach (Setting item in e.OldItems)
                {
                    item.PropertyChanged -= Setting_PropertyChanged;
                    if (item.Scope == Setting.Scopes.DistributionSource.ToString() && item.Key != null)
                        DistributionSourceMap.Remove(item.Key);
                    if (item.Scope == Setting.Scopes.DistributionTarget.ToString() && item.Key != null)
                        DistributionTargetMap.Remove(item.Key);
                    if (item.Scope == Setting.Scopes.DistributionExe.ToString() && item.Key != null)
                    {
                        var setting = SettingsList.First(x => (x.Scope == item.Scope && x.Value == item.Value));
                        if ( setting == null && Executables.Contains( item.Value ) )
                        {
                            Executables.Remove(item.Value);
                        }
                        var distribution = DistributionList.First(x => x.Folder == item.Key);
                        if (distribution != null)
                        {
                            distribution.Executable = "";
                        }
                    }
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Move)
            {
                //your code
            }
        }

        private void UpdateDistributionSourceMap()
        {
            foreach (Setting set in SettingsList)
            {
                if (set.Scope == Setting.Scopes.DistributionSource.ToString())
                {
                    DistributionSourceMap[set.Key] = set.Value;
                }
            }
        }

        private void UpdateDistributionTargetMap()
        {
            foreach (Setting set in SettingsList)
            {
                if (set.Scope == Setting.Scopes.DistributionTarget.ToString())
                {
                    DistributionTargetMap[set.Key] = set.Value;
                }
            }
        }

        private void Setting_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Setting setting = (Setting)sender;
            foreach (var Tab in Tabs)
            {
                if (Tab.TabName == setting.Scope)
                {
                    Tab.UpdateAvailableSolutions();
                    NotifyPropertyChanged("AllSolutionsForSelectedTab");
                }
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
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private ICommand _AddSettingCmd;
        public ICommand AddSettingCmd
        {
            get { return _AddSettingCmd ?? (_AddSettingCmd = new CommandHandler(param => AddSetting())); }
        }
        public void AddSetting()
        {
            StringCollection scopes = new StringCollection();
            scopes.Add(Setting.Scopes.Base.ToString());
            scopes.Add(Setting.Scopes.DistributionSource.ToString());
            scopes.Add(Setting.Scopes.DistributionTarget.ToString());
            scopes.Add(Setting.Scopes.DistributionExe.ToString());
            foreach (var tab in Tabs)
                scopes.Add(tab.TabName);
            var dialog = new SettingCreationDialog() { Owner = Application.Current.MainWindow, Scopes = scopes }; // TODO #GUI access SettingsDialog
            if (dialog.ShowDialog() == true)
            {
                View.MainWindow mainWindow = (View.MainWindow)System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                    SettingsList.Add(new Setting() { Scope = dialog.Scope, Key = dialog.Key, Value = dialog.Value });
            }
        }
        private CommandHandler _RemoveSettingCmd;
        public CommandHandler RemoveSettingCmd
        {
            get { return _RemoveSettingCmd ?? (_RemoveSettingCmd = new CommandHandler(param => RemoveSetting(param), param => RemoveSetting_CanExecute(param))); }
        }
        public bool RemoveSetting_CanExecute(object parameter)
        {
            return SelectedSettingIndex != -1;
        }
        public void RemoveSetting(object parameter)
        {
            SettingsList.RemoveAt(SelectedSettingIndex);
        }
        [OnDeserialized()]
        private void OnDeserializedMethod(StreamingContext context)
        {
            foreach (var tab in Tabs)
            {
                if (tab.BaseDir == null)
                {
                    tab.BaseDir = GetSetting("BaseDir", tab.TabName);
                    SettingsList.Remove(new Setting() { Scope=tab.TabName, Key="BaseDir" });
                }
            }
        }
    }
}
