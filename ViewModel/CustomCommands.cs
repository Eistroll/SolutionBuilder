using System;
using System.Windows.Input;

namespace SolutionBuilder
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

    public class CommandHandler : ICommand
    {
        private Action _action;
        private bool _canExecute;
        public CommandHandler(Action action, bool canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _action();
        }
    }
}
