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
        private Action<object> _execute;
        private Predicate<object> _canExecute;
        public CommandHandler(Action<object> execute)
            : this(execute, null)
        {
        }
        public CommandHandler(Action<object> execute, Predicate<object> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute==null? true:_canExecute(parameter);
        }

        public event EventHandler CanExecuteChanged;
        public void Execute(object parameter)
        {
            _execute(parameter);
        }
        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
                CanExecuteChanged(this, EventArgs.Empty);
        }
    }
}
