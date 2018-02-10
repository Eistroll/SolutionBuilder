using SolutionBuilder.View;
using SolutionBuilder.ViewModel;
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

namespace SolutionBuilder
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public BindingList<DistributionItem> DistributionList { get; set; }
        public StringCollection Platforms { get; set; }
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
        [IgnoreDataMemberAttribute]
        public ObservableCollection<Setting> SettingsList { get; set; }
        public ObservableCollection<BuildTabItem> Tabs { get; set; }
        public int SelectedTabIndex { get; set; }

        public ObservableCollection<ViewModel.TreeSettings> TreeSettingsList { get; set; }

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
            set { if (value != _ProgressState) { _ProgressState = value; NotifyPropertyChanged("ProgressState"); } }
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
            Platforms = new StringCollection() { "Release", "Debug" };
            Executables = new ObservableCollection<string>();
            DistributionSourceMap = new Dictionary<string, string>();
            DistributionTargetMap = new Dictionary<string, string>();
            Tabs = new ObservableCollection<BuildTabItem>();
            var me = this;
            SettingsList = new ObservableCollection<Setting>();
            TreeSettingsList = new ObservableCollection<ViewModel.TreeSettings>();
            ViewModel.TreeSettings baseSettings = new ViewModel.TreeSettings { Name = Setting.Scopes.Base.ToString() };
            baseSettings.Members.Add(new TreeSetting { Key = Setting.Executables.BuildExe.ToString(), Value = @"C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild.exe" });
            baseSettings.Members.Add(new TreeSetting { Key = Setting.Executables.CopyExe.ToString(), Value = @"C:\Windows\System32\Robocopy.exe" });

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
            TreeSettingsList.CollectionChanged += new NotifyCollectionChangedEventHandler(SettingsListCollectionChangedMethod);
            foreach (Setting item in SettingsList)
            {
                var treeSettings = TreeSettingsList.FirstOrDefault(x => x.Name == item.Scope);
                if(treeSettings==null)
                {
                    treeSettings = new ViewModel.TreeSettings() { Name = item.Scope };
                    TreeSettingsList.Add(treeSettings);
                }
                treeSettings.Members.CollectionChanged += new NotifyCollectionChangedEventHandler(SettingsCollectionChangedMethod);
                treeSettings.Members.Add(new TreeSetting() { Key = item.Key, Value = item.Value });
                treeSettings.Members.Last().PropertyChanged += SettingPropertyChangedMethod;
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
            ViewModel.TreeSettings treeSettings = TreeSettingsList.First(x => x.Name == scope);
            if(treeSettings != null )
            {
                var treeSetting = treeSettings.Members.First(x => x.Key == key);
                if (treeSetting != null)
                    return treeSetting.Value;
            }
            return "";
        }
        public String GetSettingByScope(String key, Setting.Scopes scope = Setting.Scopes.Base)
        {
            return GetSetting(key, scope.ToString());
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
        private void SettingsListCollectionChangedMethod(object sender, NotifyCollectionChangedEventArgs e)
        {
            //different kind of changes that may have occurred in collection
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (ViewModel.TreeSettings settings in e.NewItems)
                {
                    settings.Members.CollectionChanged += SettingsCollectionChangedMethod;
                    if (settings.Name == Setting.Scopes.DistributionSource.ToString())
                    {
                        foreach(TreeSetting setting in settings.Members)
                        {
                            if (setting.Key == null)
                                continue;
                            DistributionSourceMap[setting.Key] = setting.Value;
                            NotifyPropertyChanged("DistributionSourceMap");
                        }
                    }
                    if (settings.Name == Setting.Scopes.DistributionTarget.ToString())
                    {
                        foreach(TreeSetting setting in settings.Members)
                        {
                            if (setting.Key != null)
                                continue;
                            DistributionList.Add(new DistributionItem() { Folder = setting.Key });
                            DistributionTargetMap[setting.Key] = setting.Value;
                            NotifyPropertyChanged("DistributionTargetMap");
                        }
                    }
                    if (settings.Name == Setting.Scopes.DistributionExe.ToString())
                    {
                        foreach(TreeSetting setting in settings.Members)
                        {
                            if (setting.Key != null)
                                continue;
                            if (!Executables.Contains(setting.Value))
                            {
                                Executables.Add(setting.Value);
                            }
                            var distribution = DistributionList.First(x => x.Folder == setting.Key);
                            if (distribution != null)
                            {
                                distribution.Executable = setting.Value;
                            }
                        }
                    }
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (ViewModel.TreeSettings settings in e.OldItems)
                {
                    settings.Members.CollectionChanged -= SettingsCollectionChangedMethod;
                    foreach (TreeSetting setting in settings.Members)
                        setting.PropertyChanged -= SettingPropertyChangedMethod;
                }
                foreach (ViewModel.TreeSettings settings in e.NewItems)
                {
                    settings.Members.CollectionChanged += SettingsCollectionChangedMethod;
                    foreach (TreeSetting setting in settings.Members)
                        setting.PropertyChanged += SettingPropertyChangedMethod;
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (ViewModel.TreeSettings settings in e.OldItems)
                {
                    settings.Members.CollectionChanged -= SettingsCollectionChangedMethod;
                    foreach (TreeSetting setting in settings.Members)
                    {
                        setting.PropertyChanged -= SettingPropertyChangedMethod;
                        if (setting.Scope == Setting.Scopes.DistributionSource.ToString() && setting.Key != null)
                            DistributionSourceMap.Remove(setting.Key);
                        if (setting.Scope == Setting.Scopes.DistributionTarget.ToString() && setting.Key != null)
                            DistributionTargetMap.Remove(setting.Key);
                        if (setting.Scope == Setting.Scopes.DistributionExe.ToString() && setting.Key != null)
                        {
                            if (Executables.Contains(setting.Value))
                            {
                                Executables.Remove(setting.Value);
                            }
                            var distribution = DistributionList.First(x => x.Folder == setting.Key);
                            if (distribution != null)
                            {
                                distribution.Executable = "";
                            }
                        }
                    }
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Move)
            {
                //your code
            }
        }
        private void SettingsCollectionChangedMethod(object sender, NotifyCollectionChangedEventArgs e)
        {
            //different kind of changes that may have occurred in collection
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (TreeSetting setting in e.NewItems)
                {
                    setting.PropertyChanged += SettingPropertyChangedMethod;
                    if (setting.Scope == Setting.Scopes.DistributionSource.ToString())
                    {
                        DistributionSourceMap[setting.Key] = setting.Value;
                        NotifyPropertyChanged("DistributionSourceMap");
                    }
                    if (setting.Scope == Setting.Scopes.DistributionTarget.ToString() && setting.Key != null)
                    {
                        DistributionList.Add(new DistributionItem() { Folder = setting.Key });
                        DistributionTargetMap[setting.Key] = setting.Value;
                        NotifyPropertyChanged("DistributionTargetMap");
                    }
                    if (setting.Scope == Setting.Scopes.DistributionExe.ToString() && setting.Key != null)
                    {
                        if (!Executables.Contains(setting.Value))
                        {
                            Executables.Add(setting.Value);
                        }
                        var distribution = DistributionList.First(x => x.Folder == setting.Key);
                        if (distribution != null)
                        {
                            distribution.Executable = setting.Value;
                        }
                    }
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (TreeSetting setting in e.OldItems)
                    setting.PropertyChanged -= SettingPropertyChangedMethod;
                foreach (TreeSetting setting in e.NewItems)
                    setting.PropertyChanged += SettingPropertyChangedMethod;
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (TreeSetting setting in e.OldItems)
                {
                    setting.PropertyChanged -= SettingPropertyChangedMethod;
                    if (setting.Scope == Setting.Scopes.DistributionSource.ToString() && setting.Key != null)
                        DistributionSourceMap.Remove(setting.Key);
                    if (setting.Scope == Setting.Scopes.DistributionTarget.ToString() && setting.Key != null)
                        DistributionTargetMap.Remove(setting.Key);
                    if (setting.Scope == Setting.Scopes.DistributionExe.ToString() && setting.Key != null)
                    {
                        if (Executables.Contains(setting.Value))
                        {
                            Executables.Remove(setting.Value);
                        }
                        var distribution = DistributionList.First(x => x.Folder == setting.Key);
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

        private void SettingPropertyChangedMethod(object sender, PropertyChangedEventArgs e)
        {
            TreeSetting setting = (TreeSetting)sender;
            foreach (var Tab in Tabs)
            {
                if (Tab.Header == setting.Scope)
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
                scopes.Add(tab.Header);
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
    }
}
