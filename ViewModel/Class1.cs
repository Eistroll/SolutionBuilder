using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.ObjectModel;

namespace SolutionBuilder.ViewModel
{
    public class TreeSettings
    {
        public TreeSettings()
        {
            this.Members = new ObservableCollection<TreeSetting>();
        }

        public string Name { get; set; }

        public ObservableCollection<TreeSetting> Members { get; set; }
    }

    public class TreeSetting
    {
        public string Key { get; set; }

        public string Value { get; set; }
    }
}
