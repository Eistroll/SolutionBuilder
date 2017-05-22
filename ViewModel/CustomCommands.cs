using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BuildWG.ViewModel
{
    public static class CustomCommands
    {
        public static readonly RoutedUICommand NewSolution = new RoutedUICommand
        (
            "New Solution",
            "New Solution",
            typeof(CustomCommands),
            null
        );
    }
}
