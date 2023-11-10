using EdhDeckBuilder.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.ViewModel
{
    public class DeckBuilderViewModel : ViewModelBase
    {
        private CardProvider _cardProvider;
        private Dictionary<string, int> _lastNumCopiesForCard = new Dictionary<string, int>();

        private ObservableCollection<CardViewModel> _cardVms = new ObservableCollection<CardViewModel>();
        public ObservableCollection<CardViewModel> CardVms
        {
            get { return _cardVms; }
            set { SetProperty(ref _cardVms, value); }
        }

        private ObservableCollection<TemplateViewModel> _templateVms = new ObservableCollection<TemplateViewModel>();
        public ObservableCollection<TemplateViewModel> TemplateVms
        {
            get { return _templateVms; }
            set { SetProperty(ref _templateVms, value); }
        }

        public int NumRoles => TemplateVms.Count;

        public int TotalCards => CalculateTotalCards();

        private int CalculateTotalCards()
        {
            return CardVms.Sum(vm => vm.NumCopies);
        }

        public void AddCard(string cardName)
        {
            if (CardVms.Any(vm => vm.Name == cardName)) return; // Don't add dupes.

            var cardModel = _cardProvider.TryGetCard(cardName);

            if (cardModel == null) return;

            var cardVm = new CardViewModel(cardModel);

            // TODO: Temporarily disabled while working on other things.
            //if (cardVm.CardImage == null)
            //{
            //    cardVm.CardImage = _cardProvider.DownloadImageForCard(cardModel);
            //}

            cardVm.PropertyChanged += CardVm_PropertyChanged;
            cardVm.RoleUpdated += CardVm_RoleUpdated;
            CardVms.Add(cardVm);
            _lastNumCopiesForCard[cardVm.Name] = 0;
            UpdateRoleHeaders(cardVm);
            RaisePropertyChanged(nameof(TotalCards));
        }

        private void CardVm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CardViewModel.NumCopies):
                    RaisePropertyChanged(nameof(TotalCards));

                    // If role applies, update total count for role.
                    var cardVm = sender as CardViewModel;
                    UpdateRoleHeaders(cardVm);
                    break;
            }
        }

        private void UpdateRoleHeaders(CardViewModel cardVm)
        {
            var applicableRoleNames = cardVm.RoleVms.Where(role => role.Applies).Select(role => role.Name);
            var dCopies = cardVm.NumCopies - _lastNumCopiesForCard[cardVm.Name];
            foreach (var roleHeader in TemplateVms.Where(rHeader => applicableRoleNames.Contains(rHeader.Role)))
            {
                roleHeader.Current += dCopies;
            }
            _lastNumCopiesForCard[cardVm.Name] = cardVm.NumCopies;
        }

        private void CardVm_RoleUpdated(object senders, EventArgs e)
        {
            var roleUpdatedSenders = senders as RoleUpdatedSenders;
            var cardVm = roleUpdatedSenders.CardVm;
            var roleVm = roleUpdatedSenders.RoleVm;
            var relevantRoleHeader = TemplateVms.First(vm => vm.Role == roleVm.Name);

            if (roleVm.Applies) relevantRoleHeader.Current += cardVm.NumCopies;
            else relevantRoleHeader.Current -= cardVm.NumCopies;
            // NOTE TO SELF: This seemed like a good alternative to recalculating all totals whenever anything changed,
            // but what happens when the user changes NumCopies while a role is checked, unchecks the role, and then
            // changes the NumCopies back?
            // Let's see:
            // NumCopies: 1, Role: checked, Total: 1. -> Role change detected, use logic above.
            // NumCopies: 2, Role: checked, Total: 2. -> NumCopies change detected, role checked, so update total.
            // NumCopies: 2, Role: unchecked, Total: 0. -> Role change detected, use logic above. 
            // NumCopies: 1, Role: unchecked, Total: 0. -> NumCopies change detected, role unchecked, leave total as is.
            // As long as the logic responding to the NumCopies change only updates the total when the role is checked,
            // it should be fine. ... I think.
        }

        public DeckBuilderViewModel()
        {
            _cardProvider = new CardProvider();

            // TODO: If available, load templates from CSV database instead.
            SetUpDefaultTemplateAndRoles();
        }

        private void SetUpDefaultTemplateAndRoles()
        {
            foreach (var templateModel in TemplatesAndDefaults.DefaultTemplates())
            {
                TemplateVms.Add(new TemplateViewModel(templateModel));
            }
        }

        /// <summary>
        /// Counts each card view model's roles and updates the column headers accordingly.
        /// </summary>
        private void UpdateRoleCounts()
        {
            foreach (var cardVm in CardVms)
            {
                foreach (var roleVm in cardVm.RoleVms)
                {
                    if (!roleVm.Applies) continue;

                    TemplateVms.First(vm => vm.Role == roleVm.Name).Current += 1;
                }
            }
        }
    }
}
