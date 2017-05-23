using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.ComponentModel;

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
        public string Name { get { return _SolutionObject?.Name; } set{ _SolutionObject.Name = value; } }

        private string _Options;
        public string Options
        {
            get { return _Options; }
            set { _Options = value; OnPropertyChanged("Options"); }
        }
        public bool Selected { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
