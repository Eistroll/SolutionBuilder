using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using SolutionBuilder.Converters;

namespace SolutionBuilder.ViewModel
{
    public class DistributionItem : INotifyPropertyChanged, ICloneable
    {
        [IgnoreDataMemberAttribute]
        public bool IsSelected { get; set; }
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
        private string _Configuration;
        public string Configuration
        {
            get { return _Configuration; }
            set { if (_Configuration != value) { _Configuration = value; NotifyPropertyChanged("Configuration"); } }
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
        public ObservableCollection<string> ExecArguments { get; set; }
        private string _SelectedExecArgument;
        public string SelectedExecArgument
        {
            get { return _SelectedExecArgument; }
            set
            {
                _SelectedExecArgument = value;
                NotifyPropertyChanged("SelectedExecArgument");
            }
        }
        [IgnoreDataMemberAttribute]
        public string NewExecArgument
        {
            set
            {
                if (SelectedExecArgument != null)
                {
                    return;
                }
                if (!string.IsNullOrEmpty(value))
                {
                    ExecArguments?.Add(value);
                    SelectedExecArgument = value;
                }
            }
        }
        [IgnoreDataMemberAttribute]
        public int PID { get; set; }
        [IgnoreDataMemberAttribute]
        public Process Proc;
        public DistributionItem()
        {
            ExecArguments = new ObservableCollection<string>();
        }
        public object Clone()
        {
            return this.MemberwiseClone();
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
            ApplyToAllEventArgs e = new ApplyToAllEventArgs() { PropertyName = parameter as string };
            EventHandler<ApplyToAllEventArgs> handler = ApplyToAll;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public event EventHandler<ApplyToAllEventArgs> ApplyToAll;
        public class ApplyToAllEventArgs : EventArgs
        {
            public string PropertyName;
        }
        private CommandHandler _ApplyToSelectedCmd;
        public CommandHandler ApplyToSelectedCmd
        {
            get { return _ApplyToSelectedCmd ?? (_ApplyToSelectedCmd = new CommandHandler(param => OnApplyToSelected(param), param => ApplyToSelected_CanExecute(param))); }
        }
        public bool ApplyToSelected_CanExecute(object parameter)
        {
            return parameter != null;
        }
        protected virtual void OnApplyToSelected(object parameter)
        {
            if (parameter is SelectedDistributionItemsConverter.Selection selection)
            {
                ApplyToSelectedEventArgs e = new ApplyToSelectedEventArgs() { PropertyName = selection.Name, SelectedItems = selection.SelectedItems };
                ApplyToSelected?.Invoke(this, e);
            }
        }
        public event EventHandler<ApplyToSelectedEventArgs> ApplyToSelected;
        public class ApplyToSelectedEventArgs : EventArgs
        {
            public string PropertyName;
            public IList SelectedItems;
        }
        private CommandHandler _RemoveExecTargetCmd;
        public CommandHandler RemoveExecTargetCmd
        {
            get { return _RemoveExecTargetCmd ?? (_RemoveExecTargetCmd = new CommandHandler(param => OnRemoveExecTarget(param), param => RemoveExecTarget_CanExecute(param))); }
        }
        public bool RemoveExecTarget_CanExecute(object parameter)
        {
            return true;
        }
        protected virtual void OnRemoveExecTarget(object parameter)
        {
            ExecArguments.Remove(SelectedExecArgument);
            if (ExecArguments.Count > 0)
                SelectedExecArgument = ExecArguments.ElementAt(0);
        }
        public object this[string propertyName]
        {
            get { return this.GetType().GetProperty(propertyName).GetValue(this, null); }
            set { this.GetType().GetProperty(propertyName).SetValue(this, value, null); }
        }
    }
}