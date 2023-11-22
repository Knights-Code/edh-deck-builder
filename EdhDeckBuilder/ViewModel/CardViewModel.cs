using EdhDeckBuilder.Model;
using EdhDeckBuilder.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace EdhDeckBuilder.ViewModel
{
    public class CardViewModel : ViewModelBase
    {
        public EventHandler RoleUpdated;

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private Image _cardImage;
        public Image CardImage
        {
            get { return _cardImage; }
            set { SetProperty(ref _cardImage, value); }
        }

        private int _numCopies;
        public int NumCopies
        {
            get { return _numCopies; }
            set { SetProperty(ref _numCopies, value); }
        }

        public SolidColorBrush BackgroundColour
        {
            get { return Hovered ? new SolidColorBrush(Colors.LightGray) : new SolidColorBrush(Colors.Transparent); }
        }

        private bool _hovered;
        public bool Hovered
        {
            get { return _hovered; }
            set
            {
                SetProperty(ref _hovered, value);
                RaisePropertyChanged(nameof(BackgroundColour));
            }
        }


        public int NumRoles => RoleVms.Count;

        private ObservableCollection<RoleViewModel> _roleVms;
        public ObservableCollection<RoleViewModel> RoleVms
        {
            get { return _roleVms; }
            set { SetProperty(ref _roleVms, value); }
        }

        public CardViewModel(CardModel model)
        {
            _name = model.Name;
            _cardImage = model.CardImage;
            _numCopies = model.NumCopies > 0 ? model.NumCopies : 1;
            _roleVms = new ObservableCollection<RoleViewModel>();

            CreateDefaultRoleVms();

            foreach (var roleModel in model.Roles)
            {
                var roleVm = _roleVms.First(vm => vm.Name == roleModel.Name);

                if (!roleModel.Applies) continue;

                roleVm.ApplySilently();
            }
        }

        private void CreateDefaultRoleVms()
        {
            foreach (var defaultRole in TemplatesAndDefaults.DefaultRoleSet())
            {
                AddRole(defaultRole);
            }

            // TODO: Get custom roles here.
        }

        private void AddRole(string roleName)
        {
            if (_roleVms.Any(vm => vm.Name == roleName)) return;
            var roleVm = new RoleViewModel(roleName);
            roleVm.PropertyChanged += RoleVm_PropertyChanged;
            RoleVms.Add(roleVm);
        }

        private void RoleVm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RoleUpdated.Invoke(new RoleUpdatedSenders { CardVm = this, RoleVm = sender as RoleViewModel }, e);
        }

        public override string ToString()
        {
            return Name;
        }

        public CardModel ToModel()
        {
            return new CardModel
            {
                Name = _name,
                NumCopies = _numCopies,
                Roles = _roleVms.Select(roleVm => roleVm.ToModel()).ToList(),
            };
        }
    }

    public class RoleUpdatedSenders
    {
        public CardViewModel CardVm { get; set; }
        public RoleViewModel RoleVm { get; set; }
    }
}
