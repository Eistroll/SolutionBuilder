using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace SolutionBuilder
{
    public class Setting : INotifyPropertyChanged
    {
        private string _Scope;
        public string Scope { get { return _Scope; } set { if (value != _Scope) { _Scope = value; NotifyPropertyChanged("Scope"); } } }
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
