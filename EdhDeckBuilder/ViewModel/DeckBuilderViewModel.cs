using EdhDeckBuilder.Model;
using EdhDeckBuilder.Service;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace EdhDeckBuilder.ViewModel
{
    public class DeckBuilderViewModel : ViewModelBase
    {
        private CardProvider _cardProvider;
        private DeckProvider _deckProvider;
        private Dictionary<string, int> _lastNumCopiesForCard = new Dictionary<string, int>();

        private string _defaultDeckName = "Untitled Deck";

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

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

        public DeckBuilderViewModel()
        {
            _cardProvider = new CardProvider();
            _deckProvider = new DeckProvider();

            // TODO: If available, load templates from CSV database instead.
            SetUpDefaultTemplateAndRoles();
        }

        private int CalculateTotalCards()
        {
            return CardVms.Sum(vm => vm.NumCopies);
        }

        public void AddCard(string cardName, int numCopies = 1)
        {
            if (CardVms.Any(vm => vm.Name == cardName)) return; // Don't add dupes.

            var cardModel = _cardProvider.TryGetCard(cardName);

            if (cardModel == null) return;

            cardModel.NumCopies = numCopies;
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

        public void NewDeck()
        {
            // TODO: Check for unsaved changes.
            // Clear card list.
            CardVms.Clear();
            // Reset deck name.
            Name = _defaultDeckName;
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

        public void SaveDeck()
        {
            // Check if we have a file path set already.
            // If not, prompt user for one.
            if (string.IsNullOrEmpty(SettingsProvider.DeckFilePath()))
            {
                PromptUserForSaveDestination();
                
                // If we still don't have a file path, the user canceled
                // the op. Bail out.
                if (string.IsNullOrEmpty(SettingsProvider.DeckFilePath()))
                {
                    return;
                }
            }

            // We have a file path at this point. Convert the vm and save the file.
            var deckModel = ToModel();
            _deckProvider.SaveDeck(deckModel, SettingsProvider.DeckFilePath());
        }

        public void OpenDeck()
        {
            // TODO: Check for unsaved changes.
            // Prompt for csv file.
            var openDialog = new CommonOpenFileDialog
            {
                Title = "Open",
            };
            openDialog.Filters.Add(new CommonFileDialogFilter("Comma Separated Values", "*.csv"));

            if (openDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var deckModel = _deckProvider.LoadDeck(openDialog.FileName);

                // Set name.
                Name = deckModel.Name;

                // Add cards.
                foreach (var cardModel in deckModel.Cards)
                {
                    AddCard(cardModel.Name, cardModel.NumCopies);
                }

                // Update deck file path for saving.
                SettingsProvider.UpdateDeckFilePath(openDialog.FileName);
            }
        }

        private void PromptUserForSaveDestination()
        {
            var fileDialog = new CommonSaveFileDialog
            {
                Title = "Save As",
                DefaultExtension = ".csv",
                AlwaysAppendDefaultExtension = true
            };
            fileDialog.Filters.Add(new CommonFileDialogFilter("Comma Separated Values", "*.csv"));

            if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                SettingsProvider.UpdateDeckFilePath(fileDialog.FileName);
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

        public DeckModel ToModel()
        {
            var deckName = string.IsNullOrEmpty(_name) ? _defaultDeckName : _name;
            var result = new DeckModel(deckName);

            result.AddCards(CardVms.Select(cardVm => cardVm.ToModel()));

            return result;
        }
    }
}
