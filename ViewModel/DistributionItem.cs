using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SolutionBuilder.ViewModel
{
    public class DistributionItem
    {
        public bool Checked { get; set; }
        public string Folder { get; set; }
        public string Platform { get; set; }
        public string Executable { get; set; }
        public bool Copy { get; set; }
        public bool Start { get; set; }
        [IgnoreDataMemberAttribute]
        public int PID { get; set; }
        [IgnoreDataMemberAttribute]
        public Process Proc;
    }
}
