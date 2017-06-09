using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using SolutionBuilder.ViewModel;

namespace SolutionBuilder
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<DistributionItem> DistributionList { get; set; }
        public StringCollection Folders { get; set; }
        public StringCollection Platforms { get; set; }
        /// <summary>
        /// 
        /// </summary>
        private Model _Model;
        public ObservableCollection<Setting> SettingsList { get; set; }
        public ObservableCollection<TabItem> Tabs { get; set; }
        public int SelectedTabIndex { get; set; }
        [IgnoreDataMemberAttribute]
        public StringCollection AllSolutionsForSelectedTab
        {
            get
            {
                if (SelectedTabIndex >= 0 && SelectedTabIndex < Tabs.Count)
                    return Tabs[SelectedTabIndex].AllSolutions;
                else
                    return new StringCollection();
            }
        }
        private String _Log;
        [IgnoreDataMemberAttribute]
        public String Log
        {
            get { return _Log; }
            set
            {
                if (value != _Log) {
                    _Log = value;
                    NotifyPropertyChanged("Log");
                }
            }
        }
        public String CompleteLog
        {
            get
            {
                StringBuilder log = new StringBuilder();
                foreach (var tab in Tabs) {
                    foreach (SolutionObjectView solution in tab.Solutions) {
                        log.AppendLine(solution.BuildLog);
                    }
                }
                return log.ToString();
            }
        }

        // Constructor
        public MainViewModel()
        {
            DistributionList = new ObservableCollection<DistributionItem>();
            Platforms = new StringCollection() { "Release", "Debug" };
            Folders = new StringCollection() { "SR01", "SR02", "LS01", "LS02" };
            DistributionList.Add(new DistributionItem() { Folder = "SR01", Platform = "Debug", Copy = true, Start = true });
            DistributionList.Add(new DistributionItem() { Folder = "SR02", Platform = "Debug", Copy = true, Start = true });
            DistributionList.Add(new DistributionItem() { Folder = "LS01", Platform = "Debug", Copy = true, Start = true });
            DistributionList.Add(new DistributionItem() { Folder = "LS02", Platform = "Debug", Copy = true, Start = true });
            Tabs = new ObservableCollection<TabItem>();
            var me = this;
            SettingsList = new ObservableCollection<Setting>
            {
                new Setting { Scope = "Base", Key = "BuildExe", Value = @"C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild.exe" },
                new Setting { Scope = "Base", Key = "CopyExe", Value= @"robocopy.exe" }
            };
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
                item.PropertyChanged += Setting_PropertyChanged;
        }
        public String GetSetting( String key, String scope="Base" )
        {
            foreach (Setting setting in SettingsList) {
                if (setting.Scope == scope && setting.Key == key) {
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
            FileStream stream = new FileStream(file.Name, FileMode.Open);
            XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(stream, new XmlDictionaryReaderQuotas());
            DataContractSerializer serializer = new DataContractSerializer(typeof(MainViewModel));
            MainViewModel model = (MainViewModel)serializer.ReadObject(reader, true);
            reader.Close();
            model.Init();
            return model;
        }

        public void BindToModel( ref Model Model )
        {
            Model.PropertyChanged += new PropertyChangedEventHandler(Model_PropertyChanged);
            _Model = Model;
            var me = this;
            foreach( var tab in Tabs) 
            {
                tab.BindToModel(ref _Model, ref me);
            }
        }
        private void Model_PropertyChanged( object sender, PropertyChangedEventArgs e )
        {
        }
        public void UpdateLog( SolutionObjectView solution )
        {
            StringBuilder logBuilder= new StringBuilder();
            logBuilder.Append(solution.BuildLog);
            Log = logBuilder.ToString();
        }
        private void SettingsChangedMethod(object sender, NotifyCollectionChangedEventArgs e)
        {
            //different kind of changes that may have occurred in collection
            if (e.Action == NotifyCollectionChangedAction.Add) {
                foreach( Setting item in e.NewItems )
                    item.PropertyChanged += Setting_PropertyChanged;
            }
            if (e.Action == NotifyCollectionChangedAction.Replace) {
                foreach (Setting item in e.OldItems)
                    item.PropertyChanged -= Setting_PropertyChanged;
                foreach (Setting item in e.NewItems)
                    item.PropertyChanged += Setting_PropertyChanged;
            }
            if (e.Action == NotifyCollectionChangedAction.Remove) {
                foreach (Setting item in e.OldItems) {
                    item.PropertyChanged -= Setting_PropertyChanged;
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Move) {
                //your code
            }
        }
        private void Setting_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Setting setting = (Setting)sender;
            foreach ( var Tab in Tabs)
            {
                if (Tab.Header == setting.Scope)
                {
                    Tab.UpdateAvailableSolutions();
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
