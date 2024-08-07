using EdhDeckBuilder.Model;
using EdhDeckBuilder.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
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

        public Image CardImage
        {
            get { return Keyboard.IsKeyDown(Key.LeftShift) ? BackImage : FrontImage; }
        }

        public Image FrontImage { get; set; }

        public Image BackImage { get; set; }

        public bool ImagesLoaded { get; set; } = false;

        private ObservableCollection<string> _scryfallTags = new ObservableCollection<string>();
        public ObservableCollection<string> ScryfallTags
        {
            get { return _scryfallTags; }
            set { SetProperty(ref _scryfallTags, value); }
        }

        public bool HasScryfallTags => ScryfallTags.Any();

        private int _numCopies;
        public int NumCopies
        {
            get { return _numCopies; }
            set { SetProperty(ref _numCopies, value); }
        }

        public SolidColorBrush BackgroundColour
        {
            get { return Hovered ? new SolidColorBrush(Colors.LightGray) : Highlighted ? new SolidColorBrush(Colors.LightBlue) : new SolidColorBrush(Colors.Transparent); }
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

        private bool _highlighted;
        public bool Highlighted
        {
            get { return _highlighted; }
            set
            {
                SetProperty(ref _highlighted, value);
                RaisePropertyChanged(nameof(BackgroundColour));
            }
        }

        public int NumRoles => RoleVms.Count;

        public void RefreshCardImage()
        {
            RaisePropertyChanged(nameof(CardImage));
        }

        private ObservableCollection<RoleViewModel> _roleVms;
        public ObservableCollection<RoleViewModel> RoleVms
        {
            get { return _roleVms; }
            set { SetProperty(ref _roleVms, value); }
        }

        public CardViewModel(
            CardModel model,
            List<string> customRoles = null,
            List<RoleModel> roleRankings = null,
            List<DeckRoleTagModel> deckRoleTagModels = null)
        {
            _name = model.Name;
            FrontImage = model.CardImage;
            BackImage = model.BackImage;
            ScryfallTags = new ObservableCollection<string>(model.ScryfallTags);
            _numCopies = model.NumCopies > 0 ? model.NumCopies : 1;
            _roleVms = new ObservableCollection<RoleViewModel>();

            CreateDefaultRoleVms();

            if (customRoles != null)
            {
                foreach (var customRole in customRoles)
                {
                    AddRole(customRole);
                }
            }

            foreach (var roleModel in model.Roles)
            {
                var roleVm = _roleVms.FirstOrDefault(vm => vm.Name == roleModel.Name);

                if (roleVm == null) continue; // This can happen if the role model is a custom role not used by this deck.

                if (roleRankings != null)
                {
                    var rankingForRole = roleRankings.FirstOrDefault(rr => rr.Name == roleVm.Name);
                    var roleTagsIncluded = false;

                    if (rankingForRole != null) roleVm.UpdateValueSilently(rankingForRole.Value);
                }

                if (!roleModel.Applies) continue;

                roleVm.ApplySilently();
            }

            if (deckRoleTagModels == null) return;

            foreach (var roleAndTagGrouping in deckRoleTagModels)
            {
                // Check if tags for role include card tags.
                var allTags = model.ScryfallTags
                    .Union(model.AllTypes.Select((type) => $"type:{type}"));

                if (!roleAndTagGrouping.Tags.Any(allTags.Contains)) continue;

                var roleVm = _roleVms.FirstOrDefault(vm => vm.Name == roleAndTagGrouping.RoleName);

                if (roleVm == null) continue;

                roleVm.UpdateValueSilently(1);
                roleVm.ApplySilently();
            }
        }

        public void UpdateScryfallTags(List<string> newTags)
        {
            ScryfallTags = new ObservableCollection<string>(newTags);
            RaisePropertyChanged(nameof(HasScryfallTags));
        }

        private void CreateDefaultRoleVms()
        {
            foreach (var defaultRole in TemplatesAndDefaults.DefaultRoleSet())
            {
                AddRole(defaultRole);
            }
        }

        public void AddRole(string roleName)
        {
            if (_roleVms.Any(vm => vm.Name == roleName)) return;
            var roleVm = new RoleViewModel(roleName);
            roleVm.PropertyChanged += RoleVm_PropertyChanged;
            RoleVms.Add(roleVm);
            RaisePropertyChanged(nameof(NumRoles));
        }

        private void RoleVm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RoleUpdated.Invoke(new RoleUpdatedSenders { CardVm = this, RoleVm = sender as RoleViewModel }, e);
        }

        public void ApplyRole(DeckRoleViewModel deckRoleViewModel)
        {
            var roleVm = RoleVms.FirstOrDefault((rVm) => rVm.Name == deckRoleViewModel.Name);

            if (roleVm == null) return;

            roleVm.Applies = true;
        }

        public void RenameRoleVm(DeckRoleViewModel deckRoleViewModel)
        {
            var roleVmToRename = RoleVms.FirstOrDefault((rVm) => rVm.Name == deckRoleViewModel.OriginalName);

            if (roleVmToRename != null)
            {
                roleVmToRename.RenameSilently(deckRoleViewModel.Name);
            }
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
