using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;

namespace SolutionBuilder
{
    public class SolutionObjectView : INotifyPropertyChanged
    {
        public SolutionObjectView(ref SolutionObject SolutionObject, String selectedPlatform)
        {
            _SolutionObject = SolutionObject;
            Options = _SolutionObject.Options[selectedPlatform];
        }
        public SolutionObjectView()
        {
            _SolutionObject = new SolutionObject();
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
                this._Options == toCompareWith._Options &&
                this.Selected == toCompareWith.Selected;
        }
        private SolutionObject _SolutionObject;
        public SolutionObject SolutionObject { get { return _SolutionObject; } }
        public string Name { get { return _SolutionObject?.Name; } set{ _SolutionObject.Name = value; } }

        private string _Options;
        public string Options
        {
            get { return _Options; }
            set { if (_Options != value) { _Options = value; NotifyPropertyChanged("Options"); } }
        }
        private bool _Selected = false;
        public bool Selected
        {
            get { return _Selected; }
            set { if (_Selected != value) { _Selected = value; NotifyPropertyChanged("Selected"); } }
        }
        private BitmapImage _BuildState;
        public BitmapImage BuildState
        {
            get { return _BuildState; }
            set { if (_BuildState != value) { _BuildState = value; NotifyPropertyChanged("BuildState"); } }
        }

        [IgnoreDataMemberAttribute]
        public String BuildLog { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
