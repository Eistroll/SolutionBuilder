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
    public class MainViewModel : INotifyPropertyChanged
    {
        public MainViewModel()
        {
            Solutions = new ObservableCollection<SolutionObjectView>();
            Paths = new StringCollection();
            Platforms = new StringCollection();
            Paths.Add("WG1");
            Paths.Add("WG2");
            Platforms.Add("Release");
            Platforms.Add("Debug");
            _SelectedPlatform = "Debug";
            SelectedPath = "WG1";
            AllSolutions = new StringCollection();
            BaseDir = @"C:\Users\thomas.roller\Documents\work\git\win\wg\";
            var solutionPaths = Directory.GetFiles(BaseDir, @"*.sln", SearchOption.AllDirectories);
            foreach ( var path in solutionPaths )
            {
                String newPath = path.Replace(BaseDir, "");
                AllSolutions.Add( newPath);
            }
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
            return this.Paths == toCompareWith.Paths &&
                this.Platforms == toCompareWith.Platforms &&
                this._SelectedPlatform == toCompareWith._SelectedPlatform &&
                this.SelectedPath == toCompareWith.SelectedPath;
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
            //Binding binding = new Binding { Source = Model, Path = new PropertyPath("SolutionObjects") };
            Model.PropertyChanged += new PropertyChangedEventHandler(Model_PropertyChanged);
            _Model = Model;
            UpdateFromModel(ref Model);
        }

        private void UpdateFromModel(ref Model Model)
        {
            Solutions.Clear();
            foreach (SolutionObject solution in Model.SolutionObjects) {
                SolutionObject tmp = solution;
                Solutions.Add(new SolutionObjectView( ref tmp, SelectedPlatform ));
            }
        }

        private void Model_PropertyChanged( object sender, PropertyChangedEventArgs e )
        {
            if (e.PropertyName == "SolutionObjects")
            {
                Model model = (Model)sender;
                if (model == null)
                    return;
                UpdateFromModel(ref model);
            }
        }
        public ObservableCollection<SolutionObjectView> Solutions { get; set; }
        public StringCollection Paths { get; set; }
        public StringCollection Platforms { get; set; }
        private String _SelectedPlatform;
        public String SelectedPlatform {
            get { return _SelectedPlatform; }
            set {
                _SelectedPlatform = value;
                for( int i=0; i<Solutions.Count; ++i ) {
                    Solutions[i].Options = _Model.SolutionObjects[i].Options[_SelectedPlatform];
                }
            }
        }
        [XmlIgnoreAttribute]
        public StringCollection AllSolutions { get; set; }
        [XmlIgnoreAttribute]
        public String CompleteLog
        {
            get
            {
                StringBuilder log = new StringBuilder();
                foreach ( SolutionObjectView solution in Solutions )
                {
                    log.AppendLine(solution.BuildLog);
                }
                return log.ToString();
            }
        }
        public String SelectedPath { get; set; }
        public String BaseDir { get; set; }
        public String BaseOptions{ get; set; }
        [XmlIgnoreAttribute]
        public String Log { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private ICommand _AddSolutionCmd;
        public ICommand AddSolutionCmd
        {
            get { return _AddSolutionCmd ?? (_AddSolutionCmd = new CommandHandler(() => AddSolution(), true)); }
        }
        public void AddSolution()
        {
            SolutionObject solution = new SolutionObject();
            _Model.SolutionObjects.Add( solution );
            Solutions.Add(new SolutionObjectView(ref solution, SelectedPlatform));
        }
    }
}
