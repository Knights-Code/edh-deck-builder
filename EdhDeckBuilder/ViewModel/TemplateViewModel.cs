using EdhDeckBuilder.Model;
using Microsoft.Practices.Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace EdhDeckBuilder.ViewModel
{
    public class TemplateViewModel : ViewModelBase
    {
        public EventHandler HighlightButtonClicked;

        private int _minimum;
        public int Minimum
        {
            get { return _minimum; }
            set
            {
                SetProperty(ref _minimum, value);
                RaisePropertyChanged(nameof(ToolTipText));
            }
        }

        private int _maximum;
        public int Maximum
        {
            get { return _maximum; }
            set
            {
                SetProperty(ref _maximum, value);
                RaisePropertyChanged(nameof(ToolTipText));
            }
        }

        private int _current;
        public int Current
        {
            get { return _current; }
            set
            {
                SetProperty(ref _current, value);
                RaisePropertyChanged(nameof(ForegroundColour));
                RaisePropertyChanged(nameof(BackgroundColour));
            }
        }

        private string _role;
        public string Role
        {
            get { return _role; }
            set { SetProperty(ref _role, value); }
        }

        public string ToolTipText
        {
            get { return $"Min: {Minimum}, Max: {Maximum}"; }
        }

        private bool _highlighted;
        public bool Highlighted
        {
            get { return _highlighted; }
            set
            {
                SetProperty(ref _highlighted, value);
                RaisePropertyChanged(nameof(ButtonTextColour));
            }
        }

        public SolidColorBrush ButtonTextColour
        {
            get
            {
                return Highlighted ? new SolidColorBrush(Colors.DarkBlue) : new SolidColorBrush(Colors.Black);
            }
        }

        public ICommand HighlightCommand { get; set; }

        public SolidColorBrush ForegroundColour
        {
            get
            {
                if (Current > Maximum) return new SolidColorBrush(Colors.White);
                return new SolidColorBrush(Colors.Black);
            }
        }

        public SolidColorBrush BackgroundColour
        {
            get
            {
                if (Current < Minimum) return new SolidColorBrush(Colors.LightCoral);
                if (Current > Maximum) return new SolidColorBrush(Colors.DarkGreen);
                return new SolidColorBrush(Colors.LightGreen);
            }
        }


        public TemplateViewModel(TemplateModel model)
        {
            _minimum = model.Minimum;
            _maximum = model.Maximum;
            _role = model.Role;

            HighlightCommand = new DelegateCommand(Highlight);
        }

        private void Highlight()
        {
            Highlighted = !Highlighted;
            HighlightButtonClicked.Invoke(this, null);
        }
    }
}
