﻿using EdhDeckBuilder.Model;
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

        private AppliedBySource _appliedBySourceOverride;

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

        private List<string> _allTypes = new List<string>();

        public IEnumerable<string> AllTags => _allTypes.Union(_scryfallTags.ToList());

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
            _allTypes = model.AllTypes;
            _numCopies = model.NumCopies > 0 ? model.NumCopies : 1;
            _roleVms = new ObservableCollection<RoleViewModel>();
            _appliedBySourceOverride = AppliedBySource.NotApplied;

            CreateDefaultRoleVms();

            if (customRoles != null)
            {
                foreach (var customRole in customRoles)
                {
                    AddRoleVm(customRole);
                }
            }

            if (model.Roles.Any())
            {
                foreach (var roleModel in model.Roles)
                {
                    var roleVm = _roleVms.FirstOrDefault(vm => vm.Name == roleModel.Name);

                    if (roleVm == null) continue; // This can happen if the role model is a custom role not used by this deck.

                    if (roleRankings != null)
                    {
                        var rankingForRole = roleRankings.FirstOrDefault(rr => rr.Name == roleVm.Name);

                        if (rankingForRole != null) roleVm.UpdateValueSilently(rankingForRole.Value);
                    }

                    if (roleModel.Applies) roleVm.ApplySilently(AppliedBySource.RoleDb);
                    else roleVm.UnapplySilently(AppliedBySource.RoleDb);
                }
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
                roleVm.ApplySilently(AppliedBySource.RoleDb);
            }
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

        public bool UpdateScryfallTags(List<string> newTags, RolesAndTagsReport statusReport = null)
        {
            if (newTags.All(ScryfallTags.Contains) &&
                ScryfallTags.All(newTags.Contains))
            {
                // New tag list is identical to current tag list.
                return false;
            }

            if (statusReport != null)
            {
                // Update status report, if one is provided.
                statusReport.AddScryfallTagsUpdateEvent(
                    BuildScryfallTagsUpdateEvent(newTags));
            }

            ScryfallTags = new ObservableCollection<string>(newTags);
            RaisePropertyChanged(nameof(HasScryfallTags));
            return true;
        }

        private string BuildScryfallTagsUpdateEvent(List<string> newTags)
        {
            var addedTags = newTags.Except(ScryfallTags);
            var removedTags = ScryfallTags.Except(newTags);

            if (!addedTags.Any() && !removedTags.Any())
            {
                // Should probably throw an exception here, because this
                // should only be called when newTags is different to
                // ScryfallTags in some way.
                return string.Empty;
            }

            var addedMessage = addedTags.Any()
                ? $"Added {string.Join(", ", addedTags)} tag(s) to {Name}."
                : string.Empty;
            var removedMessage = removedTags.Any()
                ? $"Removed {string.Join(", ", removedTags)} tag(s) from {Name}."
                : string.Empty;

            return string.Join(" ", new[] { addedMessage, removedMessage });
        }

        private void CreateDefaultRoleVms()
        {
            foreach (var defaultRole in TemplatesAndDefaults.DefaultRoleSet())
            {
                AddRoleVm(defaultRole);
            }
        }

        public void AddRoleVm(string roleName)
        {
            if (_roleVms.Any(vm => vm.Name == roleName)) return;
            var roleVm = new RoleViewModel(roleName);
            roleVm.PropertyChanged += RoleVm_PropertyChanged;
            RoleVms.Add(roleVm);
            RaisePropertyChanged(nameof(NumRoles));
        }

        private void RoleVm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var roleVm = sender as RoleViewModel;

            if (e.PropertyName == nameof(roleVm.Applies))
            {
                // This property change event is raised by both user interactions
                // and programmatically when applying Scryfall tags. If
                // the former, the override will be "NotApplied", and if
                // the latter, the override will be an actual value.
                if (_appliedBySourceOverride == AppliedBySource.NotApplied)
                {
                    roleVm.SetAppliedBySource(AppliedBySource.User);
                }
                else
                {
                    roleVm.SetAppliedBySource(_appliedBySourceOverride);
                    _appliedBySourceOverride = AppliedBySource.NotApplied;
                }
            }

            RoleUpdated.Invoke(new RoleUpdatedSenders { CardVm = this, RoleVm = sender as RoleViewModel }, e);
        }

        public bool ApplyRole(
            DeckRoleViewModel deckRoleViewModel,
            AppliedBySource source,
            RolesAndTagsReport statusReport = null)
        {
            var roleVm = RoleVms.FirstOrDefault((rVm) => rVm.Name == deckRoleViewModel.Name);

            if (roleVm == null || roleVm.Applies) return false;

            _appliedBySourceOverride = source;
            roleVm.Applies = true;

            if (statusReport != null)
                statusReport.AddRoleUpdateEvent($"Added {roleVm.Name} role to {Name}.");

            return true;
        }

        public bool UnapplyRole(
            DeckRoleViewModel deckRoleViewModel,
            AppliedBySource source,
            RolesAndTagsReport statusReport = null)
        {
            var roleVm = RoleVms.FirstOrDefault((rVm) => rVm.Name == deckRoleViewModel.Name);

            if (roleVm == null || !roleVm.Applies) return false;

            _appliedBySourceOverride = source;
            roleVm.Applies = false;

            if (statusReport != null)
                statusReport.AddRoleUpdateEvent($"Removed {roleVm.Name} from {Name}.");

            return true;
        }

        public bool CanUseTagsToUpdateRole(string roleName)
        {
            var roleVm = RoleVms.FirstOrDefault((rVm) => rVm.Name == roleName);

            if (roleVm == null) return false;

            switch (roleVm.AppliedBySource)
            {
                case AppliedBySource.NotApplied:
                case AppliedBySource.ScryfallTag:
                    return true;
                case AppliedBySource.RoleDb:
                case AppliedBySource.User:
                    return false;
                default:
                    return true;
            }
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
    }

    public class RoleUpdatedSenders
    {
        public CardViewModel CardVm { get; set; }
        public RoleViewModel RoleVm { get; set; }
    }
}
