using System;
using System.Windows.Input;

namespace PhotoSorter
{
    public class ActionCommand : ICommand
    {
        private readonly Action<object> _action;
        private readonly Predicate<object> _predicate;
 
        public ActionCommand(Action<object> action) : this (action, null)
        { }

        public ActionCommand(Action<object> action, Predicate<object> predicate)
        {
            if (action == null)
                throw new ArgumentNullException("action", "Action can't be null");
            _action = action;
            _predicate = predicate;
        }

        public bool CanExecute(object parameter)
        {
            return _predicate == null || _predicate(parameter);
        }


        public bool CanExecute()
        {
            return CanExecute(null);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            _action(parameter);
        }

        public void Execute()
        {
            Execute(null);
        }
    }
}
