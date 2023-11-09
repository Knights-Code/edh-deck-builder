using Microsoft.Practices.Prism;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EdhDeckBuilder.ViewModel
{
    public class ViewModelBase : NotificationObject, IActiveAware
    {
        private bool _isActive;

        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (SetProperty(ref _isActive, value))
                {
                    if (IsActive && !IsInitialised)
                    {
                        Initialise();
                    }
                    OnIsActiveChanged();
                }
            }
        }

        public virtual void Initialise()
        {
            IsInitialised = true;
        }

        private bool _isInitialised;

        public bool IsInitialised
        {
            get { return _isInitialised; }
            set { SetProperty(ref _isInitialised, value); }
        }

        public event EventHandler IsActiveChanged;

        protected virtual void OnIsActiveChanged()
        {
            IsActiveChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual bool SetProperty<T>(ref T storage, T value, Action onChanged, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;

            storage = value;
            onChanged?.Invoke();
            RaisePropertyChanged(propertyName);

            return true;
        }

        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;

            storage = value;
            RaisePropertyChanged(propertyName);

            return true;
        }

        protected void RaiseCanExecuteChanged(ICommand command)
        {
            var delegateCommand = command as DelegateCommand;

            if (delegateCommand == null)
            {
                throw new ArgumentException("Cannot raise can execute changed on non-DelegateCommand ICommand.");
            }

            delegateCommand.RaiseCanExecuteChanged();
        }
    }
}
