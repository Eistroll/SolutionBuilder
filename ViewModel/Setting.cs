using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace SolutionBuilder
{
    public partial class Setting : INotifyPropertyChanged
    {
        public enum Scopes { Base, DistributionExe, DistributionSource, DistributionTarget }
        public enum Executables { CopyExe, BuildExe }
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
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            Setting objAsPart = obj as Setting;
            if (objAsPart == null) return false;
            else return Equals(objAsPart);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public bool Equals(Setting other)
        {
            if (other == null) return false;
            if (this.Scope == other.Scope && this.Key == other.Key)
                return true;
            return false;
        }
    }
}
