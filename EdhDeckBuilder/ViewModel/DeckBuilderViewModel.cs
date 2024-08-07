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
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace EdhDeckBuilder.ViewModel
{
    public class DeckBuilderViewModel : ViewModelBase
    {
        private CardProvider _cardProvider;
        private DeckProvider _deckProvider;
        private RoleProvider _roleProvider;
        private List<DeckRoleViewModel> _roleAndTagGroupings = new List<DeckRoleViewModel>();
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

        public async void MaybeUpdateCardImagesAsync(CardViewModel cardVm)
        {
            if (cardVm.ImagesLoaded) return;

            var imageTuple = await _cardProvider.GetCardImagesAsync(cardVm.Name, new CancellationTokenSource());
            var frontImage = imageTuple.Item1;
            var backImage = imageTuple.Item2;

            if (frontImage == null || backImage == null)
            {
                return;
            }

            cardVm.FrontImage = frontImage;
            cardVm.BackImage = backImage;
            cardVm.ImagesLoaded = true;
            cardVm.RefreshCardImage();
        }

        public void ClearPreview()
        {
            HoveredCardVm = null;
        }

        public async Task TryPreviewAsync(string previewCardName)
        {
            var previewCard = await _cardProvider.TryGetCardModelAsync(previewCardName,
                new CancellationTokenSource());

            if (previewCard != null)
            {
                var previewVm = new CardViewModel(previewCard);
                MaybeUpdateCardImagesAsync(previewVm);
                HoveredCardVm = previewVm;
                HoveredCardVm.RefreshCardImage();
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

        public bool AddCard(CardModel cardModel, int numCopies = 1, List<RoleModel> cardRoleRankings = null, List<string> deckRoles = null, bool ignoreTags = false)
        {
            if (cardModel == null) return false;

            if (CardVms.Any(vm => vm.Name == cardModel.Name)) return false; // Don't add dupes.

            cardModel.NumCopies = numCopies;

            var deckBuilderVmCustomRoles = GetCustomRoles();

            if (deckRoles == null && deckBuilderVmCustomRoles.Any())
            {
                deckRoles = deckBuilderVmCustomRoles;
            }

            var cardVm = new CardViewModel(
                cardModel,
                deckRoles,
                cardRoleRankings,
                ignoreTags ? null : _roleAndTagGroupings.Select((grouping) => grouping.ToModel()).ToList());

            var cardBack = _cardProvider.GetCardBack();
            cardVm.FrontImage = cardBack;
            cardVm.BackImage = cardBack;
            cardVm.PropertyChanged += CardVm_PropertyChanged;
            cardVm.RoleUpdated += CardVm_RoleUpdated;
            CardVms.Add(cardVm);
            _lastNumCopiesForCard[cardVm.Name] = 0;
            UpdateRoleHeaders(cardVm);
            RaisePropertyChanged(nameof(TotalCards));

            return true;
        }

        /// <summary>
        /// Attempts to add a single card, including all its roles.
        /// If no roles are found, Scryfall tags will be loaded.
        /// </summary>
        /// <param name="cardName"></param>
        /// <param name="numCopies"></param>
        /// <param name="cardRoleRankings"></param>
        /// <param name="deckRoles"></param>
        /// <returns></returns>
        public async Task<bool> AddCardAsync(string cardName, int numCopies = 1, List<RoleModel> cardRoleRankings = null, List<string> deckRoles = null)
        {
            var cts = new CancellationTokenSource();
            var cardModel = await _cardProvider.TryGetCardModelAsync(cardName, cts);

            // Load roles from role DB.
            var cardRoles = _roleProvider.GetRolesForCard(cardModel.Name);

            if (cardRoles != null)
            {
                cardModel.Roles.AddRange(cardRoles);
            }
            else if (!cardModel.ScryfallTags.Any())
            {
                // Try to add roles using Scryfall tags.
                var scryfallTags = await _cardProvider.GetScryfallTagsForCardAsync(cardName, cts);
                cardModel.ScryfallTags = scryfallTags;
            }

            return AddCard(cardModel, numCopies, cardRoleRankings, deckRoles);
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

        public async Task ImportFromClipboardAsync()
        {
            var clipboardCardModels = UtilityFunctions.ParseCardsFromText(_clipboard.GetClipboardText());
            var cardsToAddToUi = await LoadCardsForDeckAsync(clipboardCardModels);

            // Update UI.
            if (cardsToAddToUi.Any())
            {
                foreach (var cardStub in cardsToAddToUi)
                {
                    // Load roles from role DB.
                    var cardRoles = _roleProvider.GetRolesForCard(cardStub.CardModel.Name);

                    if (cardRoles != null)
                    {
                        cardStub.CardModel.Roles.AddRange(cardRoles);
                    }

                    AddCard(cardStub.CardModel, cardStub.NumCopies, cardStub.Roles, null, true);
                }
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

        public async Task OpenDeck()
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
                await LoadDeck(openDialog.FileName);
            }
        }

        /// <summary>
        /// Loads deck data, then loads card data for each card in the deck,
        /// updates the UI, and then updates the deck file path in settings.
        /// </summary>
        /// <param name="deckPath"></param>
        public async Task LoadDeck(string deckPath)
        {
            var deckModel = _deckProvider.LoadDeck(deckPath);

            // TODO: Validate model before resetting and attempting to load cards.
            Reset();
            var manifest = deckModel.Cards.Select((c) => c.Name).ToList();

            _roleAndTagGroupings = deckModel.RoleAndTagGroupings.Select((grouping) =>
                new DeckRoleViewModel(grouping)).ToList();

            // Populate groupings with defaults, if some groupings are empty.
            var allRoles = TemplatesAndDefaults.DefaultRoleSet().Union(deckModel.CustomRoles);
            foreach (var role in allRoles)
            {
                if (_roleAndTagGroupings.Any(g => g.Name == role)) continue;

                _roleAndTagGroupings.Add(new DeckRoleViewModel
                {
                    Name = role,
                    Tags = new ObservableCollection<string>(
                        TemplatesAndDefaults.DefaultTagsForRole(role))
                });
            }

            // TODO: Set loading state here.
            var cardsToAddToUi = await LoadCardsForDeckAsync(deckModel.Cards);

            // Update UI.
            if (cardsToAddToUi.Any())
            {
                Name = deckModel.Name;
                foreach (var customRole in deckModel.CustomRoles)
                {
                    AddRoleHeader(customRole, 0, 100);
                }

                foreach (var cardStub in cardsToAddToUi)
                {
                    // Load roles from role DB.
                    var cardRoles = _roleProvider.GetRolesForCard(cardStub.CardModel.Name);

                    if (cardRoles != null)
                    {
                        cardStub.CardModel.Roles.AddRange(cardRoles);
                    }

                    AddCard(cardStub.CardModel, cardStub.NumCopies, cardStub.Roles, deckModel.CustomRoles, true);
                }

                SettingsProvider.UpdateDeckFilePath(deckPath);
            }
        }

        public async Task<List<CreateCardStub>> LoadCardsForDeckAsync(List<CardModel> manifest)
        {
            var result = new List<CreateCardStub>();

            // Get cards.
            var cardModels = await _cardProvider.TryGetCardModelsAsync(manifest.Select((c) => c.Name).ToList(),
                new CancellationTokenSource());

            foreach (var cardModel in cardModels)
            {
                var deckModelCard = manifest.First((deckCardModel) => deckCardModel.Name == cardModel.Name);
                var numCopies = deckModelCard.NumCopies;
                var roles = deckModelCard.Roles;

                result.Add(new CreateCardStub
                {
                    CardModel = cardModel,
                    NumCopies = numCopies,
                    Roles = roles,
                });
            }

            return result;
        }

        /// <summary>
        /// Represents a card to add to the card list in the UI.
        /// As card view models cannot be directly added to the UI from a background thread,
        /// these stubs have to be returned to the UI thread to be turned into view models.
        /// </summary>
        public class CreateCardStub
        {
            public CardModel CardModel { get; set; }
            public int NumCopies { get; set; }
            public List<RoleModel> Roles { get; set; }
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

            // Create grouping for new role.
            _roleAndTagGroupings.Add(new DeckRoleViewModel
            {
                Name = customRoleName,
                Tags = new ObservableCollection<string>()
            });

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

            _roleAndTagGroupings = new List<DeckRoleViewModel>();

            foreach (var role in TemplatesAndDefaults.DefaultRoleSet())
            {
                _roleAndTagGroupings.Add(new DeckRoleViewModel
                {
                    Name = role,
                    Tags = new ObservableCollection<string>(
                        TemplatesAndDefaults.DefaultTagsForRole(role))
                });
            }
        }

        private void AddRoleHeader(TemplateModel templateModel)
        {
            AddRoleHeader(templateModel.Role, templateModel.Minimum, templateModel.Maximum);
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

        public void ManageTags()
        {
            var tagManagerWindow = new TagManagerView();
            var tagManagerVm = new TagManagerViewModel(
                $"Manage Scryfall Tags for {Name}",
                ToModel(),
                _cardProvider,
                this);

            tagManagerWindow.DataContext = tagManagerVm;
            tagManagerWindow.Show();
        }

        /// <summary>
        /// Used by TagManagerViewModel to mass update card roles.
        /// </summary>
        /// <param name="rolesWithTags"></param>
        /// <param name="cts"></param>
        public async void UpdateRolesWithTags(
            List<DeckRoleViewModel> rolesWithTags,
            CancellationTokenSource cts)
        {
            // Update headers if role name changed.
            foreach (var renamedRole in rolesWithTags.Where(roleWithTags => roleWithTags.Renamed()))
            {
                var headerToUpdate = TemplateVms
                    .FirstOrDefault(templateVm => templateVm.Role == renamedRole.OriginalName);

                if (headerToUpdate != null)
                {
                    headerToUpdate.Role = renamedRole.Name;
                }
            }

            foreach (var cardVm in CardVms)
            {
                // Get card model with updated Scryfall tags.
                var modelWithUpdatedTags = await _cardProvider.TryGetCardModelAsync(cardVm.Name, cts);

                // Update card vm's Scryfall tags.
                cardVm.UpdateScryfallTags(modelWithUpdatedTags.ScryfallTags);

                var allTags = modelWithUpdatedTags.ScryfallTags
                    .Union(modelWithUpdatedTags.AllTypes
                    .Select((type) => _cardProvider.GetTagNameForType(type)));

                foreach (var roleWithTags in rolesWithTags)
                {
                    if (roleWithTags.Renamed())
                    {
                        cardVm.RenameRoleVm(roleWithTags);
                    }

                    if (!roleWithTags.Tags.Any(allTags.Contains)) continue;

                    // This role has one or more tags in common with allTags.
                    // Apply the role.
                    cardVm.ApplyRole(roleWithTags);
                    UpdateRoleHeaders(cardVm);
                }
            }

            _roleAndTagGroupings = rolesWithTags;
        }

        public void DecklistDiff()
        {
            if (!CardVms.Any()) return;

            var decklistDiffWindow = new DecklistDiffWindow();
            var decklistDiffVm = new DecklistDiffViewModel(
                $"Decklist Diff for {Name}",
                ToModel(),
                _clipboard);

            decklistDiffWindow.DataContext = decklistDiffVm;
            decklistDiffWindow.Show();
        }

        public void DecklistDiffFromFile()
        {
            // Get and load deck from file.
            var openDialog = new CommonOpenFileDialog
            {
                Title = "Open",
            };
            openDialog.Filters.Add(new CommonFileDialogFilter("Comma Separated Values", "*.csv"));

            if (openDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                try
                {
                    var diffDeck = _deckProvider.LoadDeck(openDialog.FileName);

                    // Create decklist diff vm and set diff deck to deck string from file.
                    var decklistDiffWindow = new DecklistDiffWindow();
                    var decklistDiffVm = new DecklistDiffViewModel(
                        $"Decklist Diff for {Name}",
                        ToModel(),
                        _clipboard);

                    decklistDiffWindow.DataContext = decklistDiffVm;
                    decklistDiffVm.DiffDeck = UtilityFunctions.CardsToClipboardFormat(diffDeck.Cards);
                    decklistDiffVm.Diff();
                    decklistDiffWindow.Show();
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Couldn't load deck for diff. {e.Message}");
                }
            }

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

        public bool CanSortByRole()
        {
            return GetHighlightedRole() != null;
        }

        public void SortByRole()
        {
            // Only sort if exactly 1 role is highlighted.
            if (!CanSortByRole()) return;

            // Put all cards with role at top of list, then order them by
            // role ranking in ascending order. Put all other cards after.
            var list = new List<CardViewModel>(CardVms);

            CardVms = new ObservableCollection<CardViewModel>(list.OrderBy(card => card.RoleVms.FirstOrDefault(role => role.Name == GetHighlightedRole() && role.Applies)?.Value ?? 100));
        }

        public DeckModel ToModel()
        {
            var deckName = string.IsNullOrEmpty(_name) ? _defaultDeckName : _name;
            var result = new DeckModel(deckName);

            result.AddCards(CardVms.Select(cardVm => cardVm.ToModel()));
            result.CustomRoles = GetCustomRoles();
            result.RoleAndTagGroupings = _roleAndTagGroupings.Select((grouping) => grouping.ToModel()).ToList();

            return result;
        }
    }
}
