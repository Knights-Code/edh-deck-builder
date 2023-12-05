using EdhDeckBuilder.Model;
using EdhDeckBuilder.Service;
using EdhDeckBuilder.Service.Clipboard;
using EdhDeckBuilder.Tests.ViewModel;
using EdhDeckBuilder.View;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace EdhDeckBuilder.ViewModel
{
    public class DeckBuilderViewModel : ViewModelBase
    {
        private CardProvider _cardProvider;
        private DeckProvider _deckProvider;
        private RoleProvider _roleProvider;
        private Dictionary<string, int> _lastNumCopiesForCard = new Dictionary<string, int>();
        private IClipboard _clipboard;

        private string _defaultDeckName = "Untitled Deck";

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private CardViewModel _hoveredCardVm;
        public CardViewModel HoveredCardVm
        {
            get { return _hoveredCardVm; }
            set
            {
                SetProperty(ref _hoveredCardVm, value);
            }
        }

        public void ClearPreview()
        {
            HoveredCardVm = null;
        }

        public void TryPreview(string previewCardName)
        {
            var previewCard = _cardProvider.TryGetCard(previewCardName);

            if (previewCard != null)
            {
                var previewVm = new CardViewModel(previewCard);
                AddImagesToCard(previewVm);
                HoveredCardVm = previewVm;
            }
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

        public DeckBuilderViewModel(IClipboard clipboard = null)
        {
            _cardProvider = new CardProvider();
            _deckProvider = new DeckProvider();
            _roleProvider = new RoleProvider();

            if (clipboard != null) _clipboard = clipboard;
            else _clipboard = new SimpleClipboard();

            // TODO: If available, load templates from CSV database instead.
            SetUpDefaultTemplateAndRoles();
        }

        public void SortCards()
        {
            // Quite possibly the laziest sorting solution imaginable. Should probably
            // use Live Shaping with a CollectionViewSource.
            CardVms = new ObservableCollection<CardViewModel>(CardVms.OrderBy(card => card.Name));
        }

        public void CleanUp()
        {
            Stack<CardViewModel> toRemove = new Stack<CardViewModel>();

            foreach (var removableCard in CardVms.Where(card => card.NumCopies == 0))
            {
                toRemove.Push(removableCard);
            }

            while (toRemove.Any())
            {
                RemoveCard(toRemove.Pop());
            }
        }

        private void RemoveCard(CardViewModel cardVm)
        {
            cardVm.PropertyChanged -= CardVm_PropertyChanged;
            cardVm.RoleUpdated -= CardVm_RoleUpdated;
            CardVms.Remove(cardVm);
        }

        private int CalculateTotalCards()
        {
            return CardVms.Sum(vm => vm.NumCopies);
        }

        public bool AddCard(string cardName, int numCopies = 1, List<RoleModel> cardRoleRankings = null, List<string> deckRoles = null)
        {
            var cardModel = _cardProvider.TryGetCard(cardName);

            if (cardModel == null) return false;

            if (CardVms.Any(vm => vm.Name == cardModel.Name)) return false; // Don't add dupes.

            // Load roles from role DB.
            var cardRoles = _roleProvider.GetRolesForCard(cardModel.Name);

            if (cardRoles != null)
            {
                cardModel.Roles.AddRange(cardRoles);
            }

            cardModel.NumCopies = numCopies;

            var deckBuilderVmCustomRoles = GetCustomRoles();

            if (deckRoles == null && deckBuilderVmCustomRoles.Any())
            {
                deckRoles = deckBuilderVmCustomRoles;
            }

            var cardVm = new CardViewModel(cardModel, deckRoles, cardRoleRankings);

            AddImagesToCard(cardVm);
            cardVm.PropertyChanged += CardVm_PropertyChanged;
            cardVm.RoleUpdated += CardVm_RoleUpdated;
            CardVms.Add(cardVm);
            _lastNumCopiesForCard[cardVm.Name] = 0;
            UpdateRoleHeaders(cardVm);
            RaisePropertyChanged(nameof(TotalCards));

            return true;
        }

        private void AddImagesToCard(CardViewModel cardVm)
        {
            if (cardVm.FrontImage == null)
            {
                cardVm.FrontImage = _cardProvider.GetCardImage(cardVm.Name);
            }

            if (cardVm.BackImage == null)
            {
                cardVm.BackImage = _cardProvider.GetCardImage(cardVm.Name, true);
            }
        }

        public void SaveDeckAs()
        {
            if (!RolesDataSourceIsSet()) return;

            PromptUserForSaveDestination();
            SaveDeck();
        }

        public void NewDeck()
        {
            // TODO: Check for unsaved changes.
            Reset();
            // Reset deck file path so as not to overwrite existing deck
            // with new one.
            SettingsProvider.UpdateDeckFilePath(string.Empty);
        }

        private void Reset()
        {
            foreach (var card in CardVms)
            {
                card.PropertyChanged -= CardVm_PropertyChanged;
                card.RoleUpdated -= CardVm_RoleUpdated;
            }

            CardVms.Clear();
            TemplateVms.Clear();

            SetUpDefaultTemplateAndRoles();
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

        public void ImportFromClipboard()
        {
            var cardsToAdd = UtilityFunctions.ParseCardsFromText(_clipboard.GetClipboardText());
            var failures = new List<string>();

            foreach (var cardModel in cardsToAdd)
            {
                if (!AddCard(cardModel.Name, cardModel.NumCopies))
                {
                    failures.Add(cardModel.Name);
                }
            }

            if (failures.Any())
            {
                MessageBox.Show($"Failed to add the following cards:\n{string.Join(Environment.NewLine, failures)}");
            }
        }

        public void ExportToClipboard()
        {
            var clipboardText = UtilityFunctions.CardsToClipboardFormat(CardVms.Select(cardVm => cardVm.ToModel()).ToList());
            _clipboard.SetClipboardText(clipboardText);
        }

        private bool RolesDataSourceIsSet()
        {
            // Check if we have a roles path set already.
            // If not, prompt user for one.
            if (string.IsNullOrEmpty(SettingsProvider.RolesFilePath()))
            {
                MessageBox.Show("Before saving the deck, choose a file in which to save role data.");
                PromptUserForRolesFile();

                // If we don't have a role file, we can't save. Abort.
                if (string.IsNullOrEmpty(SettingsProvider.RolesFilePath()))
                {
                    return false;
                }
            }

            return true;
        }

        public void SaveDeck()
        {
            if (!RolesDataSourceIsSet()) return;

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
            _roleProvider.SaveRoles(deckModel, SettingsProvider.RolesFilePath());
        }

        private void PromptUserForRolesFile()
        {
            var fileDialog = new CommonSaveFileDialog
            {
                Title = "Set Role File",
                DefaultExtension = ".csv",
                AlwaysAppendDefaultExtension = true
            };
            fileDialog.Filters.Add(new CommonFileDialogFilter("Comma Separated Values", "*.csv"));

            if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                SettingsProvider.UpdateRolesFilePath(fileDialog.FileName);
            }
        }

        public void OpenDeck()
        {
            // TODO: Check for unsaved changes.
            Reset();

            // Prompt for csv file.
            var openDialog = new CommonOpenFileDialog
            {
                Title = "Open",
            };
            openDialog.Filters.Add(new CommonFileDialogFilter("Comma Separated Values", "*.csv"));

            if (openDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                LoadDeck(openDialog.FileName);

                // Update deck file path for saving.
                SettingsProvider.UpdateDeckFilePath(openDialog.FileName);
            }
        }

        public void LoadDeck(string deckPath)
        {
            // TODO: Check for unsaved changes.
            Reset();

            var deckModel = _deckProvider.LoadDeck(deckPath);

            // Set name.
            Name = deckModel.Name;

            // Add custom role columns.
            foreach (var customRole in deckModel.CustomRoles)
            {
                AddRoleHeader(customRole, 0, 100);
            }

            // Add cards.
            foreach (var cardModel in deckModel.Cards)
            {
                AddCard(cardModel.Name, cardModel.NumCopies, cardModel.Roles, deckModel.CustomRoles);
            }
        }

        private void AddRoleHeader(string name, int min, int max)
        {
            var newVm = new TemplateViewModel(new TemplateModel(name, min, max));
            newVm.HighlightButtonClicked += RoleHeader_OnHighlightButtonClicked;
            TemplateVms.Add(newVm);
            RaisePropertyChanged(nameof(NumRoles));
        }

        private readonly Dictionary<string, bool> _highlightedRoles = new Dictionary<string, bool>();

        private void RoleHeader_OnHighlightButtonClicked(object sender, EventArgs eventArgs)
        {
            var roleheaderVm = sender as TemplateViewModel;
            var roleName = roleheaderVm.Role;

            if (_highlightedRoles.ContainsKey(roleName)) _highlightedRoles[roleName] = !_highlightedRoles[roleName]; // Toggle highlighting for role.
            else _highlightedRoles[roleName] = true;

            // For any cards to which this role applies, check if it should be highlighted.
            foreach (var card in CardVms.Where(c => c.RoleVms.Any(r => r.Name == roleName && r.Applies)))
            {
                // If any roles apply to the card that should be highlighted, highlight the card.
                card.Highlighted = card.RoleVms.Any(r => r.Applies && _highlightedRoles.ContainsKey(r.Name) && _highlightedRoles[r.Name]);
            }
        }

        public void AddCustomRole()
        {
            var numDefaultRoles = TemplatesAndDefaults.DefaultRoleSet().Count;
            var numCustomRoles = TemplateVms.Count - numDefaultRoles;

            // TODO: Prompt user for custom role name.
            var customRoleName = $"Custom {++numCustomRoles}";

            // Create role header.
            AddRoleHeader(customRoleName, 0, 100);

            // Create new role view model for cards.
            foreach (var card in CardVms)
            {
                card.AddRole(customRoleName);
            }

            RaisePropertyChanged(nameof(NumRoles));
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

            // Update card highlighting.
            cardVm.Highlighted = cardVm.RoleVms.Any(r => r.Applies && _highlightedRoles.ContainsKey(r.Name) && _highlightedRoles[r.Name]);
        }

        private void SetUpDefaultTemplateAndRoles()
        {
            foreach (var templateModel in TemplatesAndDefaults.DefaultTemplates())
            {
                AddRoleHeader(templateModel);
            }
        }

        private void AddRoleHeader(TemplateModel templateModel)
        {
            AddRoleHeader(templateModel.Role, templateModel.Minimum, templateModel.Maximum);
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

        private List<string> GetCustomRoles()
        {
            var result = new List<string>();

            var defaultRoleSet = TemplatesAndDefaults.DefaultRoleSet();
            foreach (var role in TemplateVms.Select(r => r.Role))
            {
                if (defaultRoleSet.Contains(role)) continue;

                result.Add(role);
            }

            return result;
        }

        public void DecklistDiff()
        {
            var decklistDiffWindow = new DecklistDiffWindow();
            var decklistDiffVm = new DecklistDiffViewModel($"Decklist Diff for {Name}", ToModel());

            decklistDiffWindow.DataContext = decklistDiffVm;
            decklistDiffWindow.Show();
        }

        private string GetHighlightedRole()
        {
            if (TemplateVms.Count(vm => vm.Highlighted) != 1) return null;

            return TemplateVms.First(vm => vm.Highlighted).Role;
        }

        public void RoleRankings()
        {
            // Get relevant role.
            var highlightedRole = GetHighlightedRole();
            if (string.IsNullOrEmpty(highlightedRole)) return;

            // Get applicable cards.
            var cardsWithRole = CardVms.Where(card => card.RoleVms.Any(role => role.Name == highlightedRole && role.Applies))
                .OrderBy(card => card.RoleVms.First(role => role.Name == highlightedRole).Value)
                .ToList();

            if (!cardsWithRole.Any()) return;

            var roleRankingsWindow = new RoleRankingWindow();
            roleRankingsWindow.Closing += RoleRankingsWindow_Closing;
            var roleRankingsVm = new RoleRankingsViewModel(cardsWithRole, highlightedRole);

            roleRankingsWindow.DataContext = roleRankingsVm;
            roleRankingsWindow.Show();
        }

        private void RoleRankingsWindow_Closing(object sender, CancelEventArgs e)
        {
            var roleRankingsVm = (sender as RoleRankingWindow).DataContext as RoleRankingsViewModel;

            // Update rankings.
            foreach (var roleRankedCard in roleRankingsVm.Cards)
            {
                var cardVm = CardVms.FirstOrDefault(card => card.Name == roleRankedCard.Name);

                if (cardVm == null) return;

                var rankedRole = roleRankedCard.RoleVms.First(role => role.Name == roleRankingsVm.Role);
                var roleToUpdate = cardVm.RoleVms.First(role => role.Name == roleRankingsVm.Role);

                roleToUpdate.Value = rankedRole.Value;
            }
        }

        public DeckModel ToModel()
        {
            var deckName = string.IsNullOrEmpty(_name) ? _defaultDeckName : _name;
            var result = new DeckModel(deckName);

            result.AddCards(CardVms.Select(cardVm => cardVm.ToModel()));
            result.CustomRoles = GetCustomRoles();

            return result;
        }
    }
}
