using EdhDeckBuilder.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.ViewModel
{
    public class RoleViewModel : ViewModelBase
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private bool _applies;
        public bool Applies
        {
            get { return _applies; }
            set { SetProperty(ref _applies, value); }
        }

        private int _value;
        public int Value
        {
            get { return _value; }
            set { SetProperty(ref _value, value); }
        }

        public RoleViewModel(RoleModel model)
        {
            _name = model.Name;
            _applies = model.Applies;
            _value = model.Value;
        }

        public RoleViewModel(string name)
        {
            _name = name;
            _applies = false;
            _value = 1;
        }

        public void ApplySilently()
        {
            _applies = true;
        }

        public RoleModel ToModel()
        {
            return new RoleModel(_name, Value, Applies);
        }

        public void UpdateValueSilently(int value)
        {
            _value = value;
        }

        public void RenameSilently(string newName)
        {
            _name = newName;
        }
    }
}
