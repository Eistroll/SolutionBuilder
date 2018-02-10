using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SolutionBuilder.ViewModel
{
    public class TreeSettings : INotifyPropertyChanged
    {
        public TreeSettings()
        {
            this.Members = new ObservableCollection<TreeSetting>();
            Members.CollectionChanged += Members_CollectionChanged;
        }

        private void Members_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (TreeSetting setting in e.NewItems)
                    setting.Scope = Name;
            }
        }

        public string Name { get; set; }

        public ObservableCollection<TreeSetting> Members { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class TreeSetting : INotifyPropertyChanged
    {
        public string Scope;
        private string _Key;
        public string Key { get { return _Key; } set { if (value != _Key) { _Key = value; NotifyPropertyChanged("Key"); } } }
        private string _Value;
        public string Value { get { return _Value; } set { if (value != _Value) { _Value = value; NotifyPropertyChanged("Value"); } } }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
