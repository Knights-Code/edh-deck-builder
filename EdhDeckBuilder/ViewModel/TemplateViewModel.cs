using EdhDeckBuilder.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.ViewModel
{
    public class TemplateViewModel : ViewModelBase
    {
        private int _minimum;
        public int Minimum
        {
            get { return _minimum; }
            set { SetProperty(ref _minimum, value); }
        }

        private int _maximum;
        public int Maximum
        {
            get { return _maximum; }
            set { SetProperty(ref _maximum, value); }
        }

        private int _current;
        public int Current
        {
            get { return _current; }
            set { SetProperty(ref _current, value); }
        }

        private string _role;
        public string Role
        {
            get { return _role; }
            set { SetProperty(ref _role, value); }
        }

        public TemplateViewModel(TemplateModel model)
        {
            _minimum = model.Minimum;
            _maximum = model.Maximum;
            _role = model.Role;
        }
    }
}
