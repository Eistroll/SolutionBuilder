﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace SolutionBuilder
{
    public class SolutionObjectView : INotifyPropertyChanged, ICloneable
    {
        private SolutionObject _SolutionObject;
        public SolutionObject SolutionObject { get { return _SolutionObject; } }
        public string Name { get { return _SolutionObject?.Name; } set { _SolutionObject.Name = value; } }

        public ObservableCollection<string> PublishToList { get; set; }
        private string _SelectedPublishTo;
        public string SelectedPublishTo
        {
	        get { return _SelectedPublishTo; }
	        set
	        {
		        _SelectedPublishTo = value;
		        NotifyPropertyChanged("SelectedPublishTo");
	        }
        }
        [IgnoreDataMember]
        public string NewPublishTo
        {
	        set
	        {
		        if (SelectedPublishTo != null)
		        {
			        return;
		        }
		        if (!string.IsNullOrEmpty(value))
		        {
			        PublishToList?.Add(value);
			        SelectedPublishTo = value;
		        }
	        }
        }
        private string _Options;
        public string Options
        {
            get { return _Options; }
            set { if (_Options != value) { _Options = value; NotifyPropertyChanged("Options"); } }
        }
        private string _PostBuildStep;
        public string PostBuildStep
        {
            get { return _PostBuildStep; }
            set { if (_PostBuildStep != value) { _PostBuildStep = value; NotifyPropertyChanged("PostBuildStep"); } }
        }
        public string _TimeChecked;
        public string TimeChecked
        {
            get { return _TimeChecked; }
            set { if (_TimeChecked != value) { _TimeChecked = value; NotifyPropertyChanged("TimeChecked"); } }
        }
        private bool _Checked = false;
        public bool Checked
        {
            get { return _Checked; }
            set { if (_Checked != value) { _Checked = value; if (_Checked) TimeChecked = DateTime.Now.ToLongTimeString(); NotifyPropertyChanged("Checked"); } }
        }
        private View.State _BuildState;
        public View.State BuildState
        {
            get { return _BuildState; }
            set { if (_BuildState != value) { _BuildState = value; NotifyPropertyChanged("BuildState"); } }
        }
        private bool _SuccessFlag;
        public bool SuccessFlag
        {
            get { return _SuccessFlag; }
            set { if (_SuccessFlag != value) { _SuccessFlag = value; NotifyPropertyChanged("SuccessFlag"); } }
        }

        [IgnoreDataMember]
        public bool IsSelected { get; set; }
        public String BuildLog { get; set; }
        public string LastOutFolder { get; set; }

        public SolutionObjectView(ref SolutionObject SolutionObject, String selectedConfiguration)
        {
            _SolutionObject = SolutionObject;
            Options = _SolutionObject.Options[selectedConfiguration];
            PublishToList = _SolutionObject.PublishToLists[selectedConfiguration];
            SelectedPublishTo = _SolutionObject.PublishTos[selectedConfiguration];
            _SolutionObject.PostBuildSteps.TryGetValue(selectedConfiguration, out var postBuildStep);
            if (!string.IsNullOrEmpty(postBuildStep))
                PostBuildStep = postBuildStep;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            var toCompareWith = obj as SolutionObjectView;
            if (toCompareWith == null)
                return false;
            return this.Name == toCompareWith.Name &&
                this.PostBuildStep == toCompareWith.PostBuildStep &&
                this._Options == toCompareWith._Options &&
                this.Checked == toCompareWith.Checked;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void NotifyAllPropertiesChanged()
        {
            NotifyPropertyChanged(nameof(Name));
            NotifyPropertyChanged(nameof(Options));
        }
        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
