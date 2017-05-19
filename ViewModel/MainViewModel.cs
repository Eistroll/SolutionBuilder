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
            FileStream stream = new FileStream("DataViewModel.xml", FileMode.Open);
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
                Solutions.Add(new SolutionObjectView( ref tmp ) { Options = solution.Options[SelectedPlatform] });
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
        public String SelectedPath { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
