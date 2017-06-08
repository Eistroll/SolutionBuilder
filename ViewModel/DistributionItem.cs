using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionBuilder.ViewModel
{
    public class DistributionItem
    {
        public bool Selected { get; set; }
        public string Folder { get; set; }
        public string Platform { get; set; }
        public bool Copy { get; set; }
        public bool Start { get; set; }
    }
}
