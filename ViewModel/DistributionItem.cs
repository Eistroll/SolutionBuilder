using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace SolutionBuilder.ViewModel
{
    public class DistributionItem : INotifyPropertyChanged
    {
        public bool Checked { get; set; }
        private string _Source;
        public string Source
        {
            get { return _Source; }
            set { if (_Source != value) { _Source = value; NotifyPropertyChanged("Source"); } }
        }
        private string _Folder;
        public string Folder
        {
            get { return _Folder; }
            set { if (_Folder != value) { _Folder = value; NotifyPropertyChanged("Folder"); } }
        }
        private string _Platform;
        public string Platform
        {
            get { return _Platform; }
            set { if (_Platform != value) { _Platform = value; NotifyPropertyChanged("Platform"); } }
        }
        private string _Executable;
        public string Executable
        {
            get { return _Executable; }
            set { if (_Executable != value) { _Executable = value; NotifyPropertyChanged("Executable"); } }
        }
        private bool _Copy;
        public bool Copy
        {
            get { return _Copy; }
            set { if (_Copy != value) { _Copy = value; NotifyPropertyChanged("Copy"); } }
        }
        private bool _Start;
        public bool Start
        {
            get { return _Start; }
            set { if (_Start != value) { _Start = value; NotifyPropertyChanged("Start"); } }
        }
        [IgnoreDataMemberAttribute]
        public int PID { get; set; }
        [IgnoreDataMemberAttribute]
        public Process Proc;
        private string _ApplyToAllProperty;
        [IgnoreDataMemberAttribute]
        public string ApplyToAllProperty
        {
            get { return _ApplyToAllProperty; }
            set { _ApplyToAllProperty = value; NotifyPropertyChanged("ApplyToAllProperty"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        private CommandHandler _ApplyToAllCmd;
        public CommandHandler ApplyToAllCmd
        {
            get { return _ApplyToAllCmd ?? (_ApplyToAllCmd = new CommandHandler(param => OnApplyToAll(param), param => ApplyToAll_CanExecute(param))); }
        }
        public bool ApplyToAll_CanExecute(object parameter)
        {
            return parameter != null;
        }
        protected virtual void OnApplyToAll(object parameter)
        {
            if (parameter == null)
                return;
            ApplyToAllProperty = parameter as string;
            ApplyToAllEventArgs e = new ApplyToAllEventArgs() { Property = ApplyToAllProperty };
            EventHandler<ApplyToAllEventArgs> handler = ApplyToAll;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public event EventHandler<ApplyToAllEventArgs> ApplyToAll;
        public class ApplyToAllEventArgs : EventArgs
        {
            public string Property;
        }
        public object this[string propertyName]
        {
            get { return this.GetType().GetProperty(propertyName).GetValue(this, null); }
            set { this.GetType().GetProperty(propertyName).SetValue(this, value, null); }
        }
    }
}