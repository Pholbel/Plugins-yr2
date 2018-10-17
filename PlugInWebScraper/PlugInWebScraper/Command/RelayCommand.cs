using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PlugInWebScraper.Command
{
    public class RelayCommand : ICommand
    {
        private bool showHiddenColumns;

        public RelayCommand(Action _function)
        {
            Function = _function;
        }

        public RelayCommand(bool showHiddenColumns)
        {
            this.showHiddenColumns = showHiddenColumns;
        }

        public Action Function
        { get; set; }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (Function != null)
            {
                return true;
            }

            return false;
        }

        public void Execute(object parameter)
        {
            if (Function != null)
            {

                Function();

            }

        }


    }


}
