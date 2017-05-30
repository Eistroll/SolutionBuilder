using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Xml.Serialization;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Windows.Input;

namespace SolutionBuilder
{
    [DataContract]
    public class TabItem
    {
        [DataMember]
        public string Header { get; set; }
        [DataMember]
        public String SelectedPath { get; set; }
        [DataMember]
        public String BaseOptions { get; set; }
        public ObservableCollection<SolutionObjectView> Solutions { get; set; }
        [DataMember]
        public StringCollection SelectedSolutions { get; set; }
        [DataMember]
        public StringCollection Paths { get; set; }
        [DataMember]
        public StringCollection Platforms { get; set; }

        private String _SelectedPlatform;
        [DataMember]
        public String SelectedPlatform
        {
            get { return _SelectedPlatform; }
            set
            {
                _SelectedPlatform = value;
                for (int i = 0; i < Solutions.Count; ++i) {
                    Solutions[i].Options = _Model.SolutionObjects[i].Options[_SelectedPlatform];
                }
            }
        }
        [DataMember]
        private int _SelectedSolutionIndex;
        [IgnoreDataMemberAttribute]
        public int SelectedSolutionIndex
        {
            get { return _SelectedSolutionIndex; }
            set
            {
                _SelectedSolutionIndex = value;
                if(_SelectedSolutionIndex != -1)
                    _ViewModel.UpdateLog(Solutions[_SelectedSolutionIndex]);
            }
        }
        public StringCollection AllSolutions { get; set; }
        private MainViewModel _ViewModel;
        [IgnoreDataMemberAttribute]
        private Model _Model;

        public TabItem()
        {
            AllSolutions = new StringCollection();
            SelectedSolutions = new StringCollection();
            Solutions = new ObservableCollection<SolutionObjectView>();
            Paths = new StringCollection();
            Platforms = new StringCollection();
            Paths.Add("WG1");
            Paths.Add("WG2");
            Platforms.Add("Release");
            Platforms.Add("Debug");
            _SelectedPlatform = "Debug";
            SelectedPath = "WG1";
            SelectedSolutionIndex = -1;
        }
        private void UpdateAvailableSolutions()
        {
            String BaseDir = _ViewModel.GetSetting("BaseDir", Header);
            if (BaseDir.Length == 0)
                return;
            System.IO.DirectoryInfo BaseDirInfo = new System.IO.DirectoryInfo(BaseDir);
            if (BaseDirInfo.Exists) {
                var solutionPaths = Directory.GetFiles(BaseDir, @"*.sln", SearchOption.AllDirectories);
                foreach (var path in solutionPaths) {
                    String newPath = path.Replace(BaseDir, "");
                    AllSolutions.Add(newPath);
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
            if (!Model.Scope2SolutionObjects.ContainsKey(Header))
                return;
            foreach (SolutionObject solution in Model.Scope2SolutionObjects[Header]) {
                SolutionObject tmp = solution;
                SolutionObjectView solutionView = new SolutionObjectView(ref tmp, SelectedPlatform);
                if (SelectedSolutions.Contains(tmp.Name))
                    solutionView.Selected = true;
                solutionView.PropertyChanged += new PropertyChangedEventHandler(SolutionView_PropertyChanged);
                Solutions.Add(solutionView);
            }
        }
        private void SolutionView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Options") {
                SolutionObjectView solView = (SolutionObjectView)sender;
                if (solView == null)
                    return;
                if (solView.SolutionObject != null) {
                    solView.SolutionObject.Options[SelectedPlatform] = solView.Options;
                }
            }
            if (e.PropertyName == "Selected") {
                SolutionObjectView solView = (SolutionObjectView)sender;
                if (solView == null)
                    return;
                if (!solView.Selected)
                    SelectedSolutions.Remove(solView.Name);
                else
                    SelectedSolutions.Add(solView.Name);
            }
        }
        private ICommand _AddSolutionCmd;
        public ICommand AddSolutionCmd
        {
            get { return _AddSolutionCmd ?? (_AddSolutionCmd = new CommandHandler(() => AddSolution(), true)); }
        }
        public void AddSolution()
        {
            SolutionObject solution = new SolutionObject();
            if (!_Model.Scope2SolutionObjects.ContainsKey(Header))
            {
                _Model.Scope2SolutionObjects[Header] = new ObservableCollection<SolutionObject>();
            }
            _Model.Scope2SolutionObjects[Header].Add(solution);
            Solutions.Add(new SolutionObjectView(ref solution, SelectedPlatform));
        }
    }
    public class MainViewModel : INotifyPropertyChanged
    {
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
        public MainViewModel()
        {
            Tabs = new ObservableCollection<TabItem>();
            var me = this;
            SettingsList = new ObservableCollection<Setting>();
            SettingsList.Add(new Setting { Scope="Base", Key = "BuildExe", Value = @"C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild.exe" });
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
        public void Save()
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(MainViewModel));
            FileStream writer = new FileStream("DataViewModel.xml", FileMode.Create);
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
            return model;
        }

        private Model _Model;
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
        public ObservableCollection<Setting> SettingsList { get; set; }
        public String CompleteLog
        {
            get
            {
                StringBuilder log = new StringBuilder();
                foreach ( var tab in Tabs)
                {
                    foreach ( SolutionObjectView solution in tab.Solutions )
                    {
                        log.AppendLine(solution.BuildLog);
                    }
                }
                return log.ToString();
            }
        }
        private String _Log;
        [IgnoreDataMemberAttribute]
        public String Log
        {
            get { return _Log; }
            set
            {
                if (value != _Log) 
                {
                    _Log = value;
                    NotifyPropertyChanged("Log");
                }
            }
        }
        public void UpdateLog( SolutionObjectView solution )
        {
            StringBuilder logBuilder= new StringBuilder();
            logBuilder.Append(solution.BuildLog);
            Log = logBuilder.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
