using EdhDeckBuilder.Model;
using EdhDeckBuilder.Service;
using Microsoft.Practices.Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace EdhDeckBuilder.ViewModel
{
    public class TagManagerViewModel : ViewModelBase
    {
        private string _title;
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private string _tagsSummary;
        public string TagsSummary
        {
            get { return _tagsSummary; }
            set { SetProperty(ref _tagsSummary, value); }
        }

        private ObservableCollection<TagSummaryViewModel> _tagSummaryVms;
        public ObservableCollection<TagSummaryViewModel> TagSummaryVms
        {
            get { return _tagSummaryVms; }
            set { SetProperty(ref _tagSummaryVms, value); }
        }

        private TagSummaryViewModel _selectedTagSummaryVm;
        public TagSummaryViewModel SelectedTagSummaryVm
        {
            get { return _selectedTagSummaryVm; }
            set { SetProperty(ref _selectedTagSummaryVm, value); }
        }

        private ObservableCollection<DeckRoleViewModel> _deckRoleVms;
        public ObservableCollection<DeckRoleViewModel> DeckRoleVms
        {
            get { return _deckRoleVms; }
            set { SetProperty(ref _deckRoleVms, value); }
        }

        private DeckRoleViewModel _selectedDeckRoleVm;
        public DeckRoleViewModel SelectedDeckRoleVm
        {
            get { return _selectedDeckRoleVm; }
            set
            {
                if (SetProperty(ref _selectedDeckRoleVm, value))
                {
                    RaisePropertyChanged(nameof(SelectedDeckRoleVm.Tags));
                }
            }
        }

        private string _selectedRoleTag;
        public string SelectedRoleTag
        {
            get { return _selectedRoleTag; }
            set { SetProperty(ref _selectedRoleTag, value); }
        }

        private bool _canRetrieve;
        public bool CanRetrieve
        {
            get { return _canRetrieve; }
            set { SetProperty(ref _canRetrieve, value); }
        }

        private string _status;
        public string Status
        {
            get { return _status; }
            set { SetProperty(ref _status, value); }
        }

        private string _errors;
        public string Errors
        {
            get { return _errors; }
            set { SetProperty(ref _errors, value); }
        }

        private string _filterInput;
        public string FilterInput
        {
            get { return _filterInput; }
            set
            {
                if (SetProperty(ref _filterInput, value))
                {
                    StartOrRestartFilterTags(value);
                }
            }
        }

        private List<string> _deckRoles;
        public List<string> DeckRoles
        {
            get { return _deckRoles; }
            set { SetProperty(ref _deckRoles, value); }
        }

        private bool _overrideExistingData;
        public bool OverrideExistingData
        {
            get { return _overrideExistingData; }
            set { SetProperty(ref _overrideExistingData, value); }
        }

        private string _filterCardsInput;
        public string FilterCardsInput
        {
            get { return _filterCardsInput; }
            set
            {
                if (SetProperty(ref _filterCardsInput, value))
                {
                    StartOrRestartFilterCards(value);
                    RaiseCanExecuteChanged(ResetCardsFilterCommand);
                }
            }
        }

        private bool _shouldFilterCardsByTag;
        public bool ShouldFilterCardsByTag
        {
            get { return _shouldFilterCardsByTag; }
            set { SetProperty(ref _shouldFilterCardsByTag, value); }
        }

        private ObservableCollection<string> _cardsToIgnore;
        public ObservableCollection<string> CardsToIgnore
        {
            get { return _cardsToIgnore; }
            set { SetProperty(ref _cardsToIgnore, value); }
        }

        private string _selectedIgnoreCard;
        public string SelectedIgnoreCard
        {
            get { return _selectedIgnoreCard; }
            set { SetProperty(ref _selectedIgnoreCard, value); }
        }

        private int _selectedIgnoreIndex;
        public int SelectedIgnoreIndex
        {
            get { return _selectedIgnoreIndex; }
            set { SetProperty(ref _selectedIgnoreIndex, value); }
        }

        private ObservableCollection<string> _cardsToUpdate;
        public ObservableCollection<string> CardsToUpdate
        {
            get { return _cardsToUpdate; }
            set { SetProperty(ref _cardsToUpdate, value); }
        }

        private string _selectedUpdateCard;
        public string SelectedUpdateCard
        {
            get { return _selectedUpdateCard; }
            set { SetProperty(ref _selectedUpdateCard, value); }
        }

        private int _selectedUpdateIndex;
        public int SelectedUpdateIndex
        {
            get { return _selectedUpdateIndex; }
            set { SetProperty(ref _selectedUpdateIndex, value); }
        }

        private FilterMode _cardsFilterMode;
        public FilterMode CardsFilterMode
        {
            get { return _cardsFilterMode; }
            set
            {
                if (SetProperty(ref _cardsFilterMode, value))
                {
                    RaisePropertyChanged(nameof(IsNameFilterChecked));
                    RaisePropertyChanged(nameof(IsTagFilterChecked));
                    RaisePropertyChanged(nameof(IsRoleFilterChecked));
                }
            }
        }

        public bool IsNameFilterChecked => CardsFilterMode == FilterMode.ByName;

        public bool IsTagFilterChecked => CardsFilterMode == FilterMode.ByTag;

        public bool IsRoleFilterChecked => CardsFilterMode == FilterMode.ByRole;

        public ICommand RetrieveCommand { get; set; }
        public ICommand ResetFilterCommand { get; set; }
        public ICommand RemoveTagFromRoleCommand { get; set; }
        public ICommand AddTagToRoleCommand { get; set; }
        public ICommand UpdateRolesInDeckCommand { get; set; }
        public ICommand ResetCardsFilterCommand { get; set; }
        public ICommand MoveSelectedFromIgnoreToUpdateCommand { get; set; }
        public ICommand MoveAllFromIgnoreToUpdateCommand { get; set; }
        public ICommand MoveAllFromUpdateToIgnoreCommand { get; set; }
        public ICommand MoveSelectedFromUpdateToIgnoreCommand { get; set; }
        public ICommand ChangeFilterModeCommand { get; set; }

        private List<TagSummaryViewModel> _fullTagsList;
        private List<string> _fullIgnoreList;
        private List<string> _fullUpdateList;
        private readonly DeckModel _deck;
        private readonly CardProvider _cardProvider;
        private readonly DeckBuilderViewModel _deckBuilderVm;
        private Task _filterTagsTask;
        private CancellationTokenSource _filterTagsCancel;
        private Task _filterCardsTask;
        private CancellationTokenSource _filterCardsCancel;

        public TagManagerViewModel(
            string title,
            DeckModel deck,
            CardProvider cardProvider,
            DeckBuilderViewModel deckBuilderVm)
        {
            Title = title;
            Status = "Idle";
            CanRetrieve = true;
            _deck = deck;
            _cardProvider = cardProvider;
            _deckBuilderVm = deckBuilderVm;
            _fullTagsList = new List<TagSummaryViewModel>();
            _fullIgnoreList = new List<string>();
            _fullUpdateList = new List<string>();
            TagSummaryVms = new ObservableCollection<TagSummaryViewModel>();
            _filterTagsCancel = new CancellationTokenSource();
            _filterCardsCancel = new CancellationTokenSource();

            RetrieveCommand = new DelegateCommand(async () => await Retrieve());
            ResetFilterCommand = new DelegateCommand(ResetFilter);
            AddTagToRoleCommand = new DelegateCommand(AddTagToRole);
            RemoveTagFromRoleCommand = new DelegateCommand(RemoveTagFromRole);
            UpdateRolesInDeckCommand = new DelegateCommand(UpdateRolesInDeck);
            ChangeFilterModeCommand = new DelegateCommand<string>(ChangeFilterMode);

            ResetCardsFilterCommand = new DelegateCommand(ResetCardsFilter,
                () => !string.IsNullOrEmpty(FilterCardsInput));
            MoveSelectedFromIgnoreToUpdateCommand = new DelegateCommand(MoveSelectedToUpdate,
                () => !string.IsNullOrEmpty(SelectedIgnoreCard));
            MoveSelectedFromUpdateToIgnoreCommand = new DelegateCommand(MoveSelectedToIgnore,
                () => !string.IsNullOrEmpty(SelectedUpdateCard));
            MoveAllFromIgnoreToUpdateCommand = new DelegateCommand(MoveAllToUpdate,
                () => CardsToIgnore.Any());
            MoveAllFromUpdateToIgnoreCommand = new DelegateCommand(MoveAllToIgnore,
                () => CardsToUpdate.Any());

            DeckRoleVms = new ObservableCollection<DeckRoleViewModel>(
                deck.RoleAndTagGroupings.Select(gModel => new DeckRoleViewModel(gModel)));
            CardsToUpdate = new ObservableCollection<string>(_deckBuilderVm.CardVms
                .Select(c => c.Name)
                .OrderBy(name => name));
            _fullUpdateList = CardsToUpdate.ToList();
            CardsToIgnore = new ObservableCollection<string>();
            CardsFilterMode = FilterMode.ByName;
        }

        public void ChangeFilterMode(string newMode)
        {
            var modeChanged = false;

            switch (newMode)
            {
                case "ByName":
                    modeChanged = CardsFilterMode != FilterMode.ByName;
                    CardsFilterMode = FilterMode.ByName;
                    break;
                case "ByTag":
                    modeChanged = CardsFilterMode != FilterMode.ByTag;
                    CardsFilterMode = FilterMode.ByTag;
                    break;
                case "ByRole":
                    modeChanged = CardsFilterMode != FilterMode.ByRole;
                    CardsFilterMode = FilterMode.ByRole;
                    break;
            }

            // Filter using new mode.
            if (modeChanged)
            {
                StartOrRestartFilterCards(FilterCardsInput);
            }
        }

        public async void UpdateRolesInDeck()
        {
            if (!_fullUpdateList.Any())
            {
                MessageBox.Show(
                    "The list of cards to update is empty. Please move one or more\n" +
                    "cards to the update list and try again.",
                    "Empty Update List",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                {
                    return;
                }
            }

            if (OverrideExistingData)
            {
                if (MessageBox.Show(
                    "The box to override existing data is checked.\n" +
                    "This will overwrite all role data " +
                    "entirely.\n\n" +
                    "Are you sure you want to continue?",
                    "Data Override Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    return;
                }
            }

            if (!string.IsNullOrEmpty(FilterCardsInput))
            {
                if (MessageBox.Show(
                    "There is an active filter on the card lists, hiding some cards\n" +
                    "in the lists of cards to ignore and update.\n\n" +
                    "These hidden cards will still be updated.\n\n" +
                    "Are you sure you want to continue?",
                    "Active Filter Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    return;
                }
            }

            Status = "Updating tags and roles...";

            var status = await _deckBuilderVm.UpdateRolesWithTags(DeckRoleVms.ToList(),
            _fullUpdateList, new CancellationTokenSource(), OverrideExistingData);

            Status = status;
        }

        public void AddTagToRole()
        {
            if (SelectedTagSummaryVm == null) return;
            if (SelectedDeckRoleVm == null) return;

            SelectedDeckRoleVm.AddTag(SelectedTagSummaryVm.Name);
        }

        public void RemoveTagFromRole()
        {
            if (SelectedDeckRoleVm == null) return;
            if (string.IsNullOrEmpty(SelectedRoleTag)) return;

            SelectedDeckRoleVm.RemoveTag(SelectedRoleTag);
        }

        public void ResetFilter()
        {
            FilterInput = string.Empty;
        }

        public void ResetCardsFilter()
        {
            FilterCardsInput = string.Empty;
        }

        public enum ListMoveDirection
        {
            ToIgnore,
            ToUpdate,
        }

        public void ConductHousekeepingForIgnoreListAndUpdateList(
            ListMoveDirection listMoveDirection,
            List<string> movedCards)
        {
            if (!movedCards.Any()) return;

            // Full lists are maintained independently from the UI
            // to facilitate functional filtering.
            List<string> fromList = listMoveDirection == ListMoveDirection.ToIgnore
                ? _fullUpdateList
                : _fullIgnoreList;
            List<string> toList = listMoveDirection == ListMoveDirection.ToIgnore
                ? _fullIgnoreList
                : _fullUpdateList;

            foreach (var cardName in movedCards)
            {
                toList.Add(cardName);
                fromList.Remove(cardName);
            }

            RaiseCanExecuteChanged(MoveAllFromIgnoreToUpdateCommand);
            RaiseCanExecuteChanged(MoveAllFromUpdateToIgnoreCommand);
            RaiseCanExecuteChanged(MoveSelectedFromIgnoreToUpdateCommand);
            RaiseCanExecuteChanged(MoveSelectedFromUpdateToIgnoreCommand);
        }

        public void MoveSelectedToUpdate()
        {
            if (string.IsNullOrEmpty(SelectedIgnoreCard))
            {
                return;
            }

            var currentSelectedIndex = SelectedIgnoreIndex;
            var nameOfCardToMove = SelectedIgnoreCard;
            var cardsToUpdateWasEmpty = !CardsToUpdate.Any();

            CardsToUpdate.Add(nameOfCardToMove);
            CardsToIgnore.Remove(nameOfCardToMove);

            if (CardsToIgnore.Any())
            {
                SelectedIgnoreIndex = Math.Min(currentSelectedIndex, CardsToIgnore.Count - 1);
            }

            if (cardsToUpdateWasEmpty) SelectedUpdateIndex = 0;

            SelectUpdateCard(nameOfCardToMove);

            ConductHousekeepingForIgnoreListAndUpdateList(
                ListMoveDirection.ToUpdate,
                new List<string> { nameOfCardToMove });
        }

        public void MoveAllToUpdate()
        {
            var movedCards = new List<string>();

            while (CardsToIgnore.Any())
            {
                var ignoreCard = CardsToIgnore.First();
                CardsToUpdate.Add(ignoreCard);
                CardsToIgnore.Remove(ignoreCard);
                movedCards.Add(ignoreCard);
            }

            ConductHousekeepingForIgnoreListAndUpdateList(ListMoveDirection.ToUpdate, movedCards);
        }

        public void MoveAllToIgnore()
        {
            var movedCards = new List<string>();

            while (CardsToUpdate.Any())
            {
                var updateCard = CardsToUpdate.First();
                CardsToIgnore.Add(updateCard);
                CardsToUpdate.Remove(updateCard);
                movedCards.Add(updateCard);
            }

            ConductHousekeepingForIgnoreListAndUpdateList(ListMoveDirection.ToIgnore, movedCards);
        }

        public void MoveSelectedToIgnore()
        {
            if (string.IsNullOrEmpty(SelectedUpdateCard))
            {
                return;
            }

            var currentSelectedIndex = SelectedUpdateIndex;
            var nameOfCardToMove = SelectedUpdateCard;
            var cardsToIgnoreWasEmpty = !CardsToIgnore.Any();

            CardsToIgnore.Add(nameOfCardToMove);
            CardsToUpdate.Remove(nameOfCardToMove);

            if (CardsToUpdate.Any())
            {
                SelectedUpdateIndex = Math.Min(currentSelectedIndex, CardsToUpdate.Count - 1);
            }

            if (cardsToIgnoreWasEmpty) SelectedIgnoreIndex = 0;

            SelectIgnoreCard(nameOfCardToMove);

            ConductHousekeepingForIgnoreListAndUpdateList(
                ListMoveDirection.ToIgnore,
                new List<string> { nameOfCardToMove });
        }

        public void SelectUpdateCard(string cardName)
        {
            if (!CardsToUpdate.Contains(cardName)) return;

            SelectedUpdateCard = cardName;
        }

        public void SelectIgnoreCard(string cardName)
        {
            if (!CardsToIgnore.Contains(cardName)) return;

            SelectedIgnoreCard = cardName;
        }

        public async void StartOrRestartFilterCards(string filterText)
        {
            if (_filterCardsTask != null && !_filterCardsTask.IsCompleted)
            {
                _filterCardsCancel.Cancel();
                await _filterCardsTask;
            }

            _filterCardsCancel = new CancellationTokenSource();
            _filterCardsTask = FilterCardsAsync(filterText, CardsFilterMode, _filterCardsCancel);
            await _filterCardsTask;
        }

        public enum FilterMode
        {
            ByName,
            ByTag,
            ByRole,
        }

        /// <summary>
        /// Filters both cards to ignore and cards to update lists
        /// based on provided filter text and checkbox value.
        /// </summary>
        /// <param name="filterText">The search term.</param>
        /// <param name="filterMode">An enum determining whether to filter by card name,
        /// card tag, or roles assigned to card.</param>
        /// <param name="cancellationTokenSource"></param>
        /// <returns>An awaitable Task for the function.</returns>
        public async Task FilterCardsAsync(
            string filterText,
            FilterMode filterMode,
            CancellationTokenSource cancellationTokenSource)
        {
            if (filterMode == FilterMode.ByTag &&
                !_deckBuilderVm.CardVms.Any((cardVm) => cardVm.HasScryfallTags))
            {
                // User has opted to filter by tags, but none
                // of the cards have tags yet.
                MessageBox.Show("None of the cards in the deck have any Scryfall tags to\n" +
                    "filter on. Retrieve tags and update roles, then try again.",
                    "No Tags Available",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var filteredIgnoreList = new List<string>();
            var filteredUpdateList = new List<string>();

            await Task.Run(() =>
            {
                switch (filterMode)
                {
                    case FilterMode.ByName:
                        filteredIgnoreList = _fullIgnoreList
                            .Where(cardName => cardName.ToLower().Contains(filterText.ToLower()))
                            .ToList();
                        filteredUpdateList = _fullUpdateList
                            .Where(cardName => cardName.ToLower().Contains(filterText.ToLower()))
                            .ToList();
                        break;
                    case FilterMode.ByTag:
                        if (string.IsNullOrEmpty(FilterCardsInput))
                        {
                            // If the filter field is empty, simply reset
                            // the lists to full.
                            filteredIgnoreList = _fullIgnoreList;
                            filteredUpdateList = _fullUpdateList;
                        }
                        else
                        {
                            // For each card in each list, check if it has any tags that match the
                            // filter text.
                            foreach (var cardName in _fullIgnoreList)
                            {
                                if (cancellationTokenSource.IsCancellationRequested)
                                {
                                    return;
                                }

                                var cardVm = _deckBuilderVm.CardVms.First(cVm => cVm.Name == cardName);

                                if (!cardVm.AllTags.Any()) continue;

                                if (cardVm.AllTags.Any(tag => tag.ToLower() == filterText.ToLower()))
                                    filteredIgnoreList.Add(cardName);
                            }

                            if (cancellationTokenSource.IsCancellationRequested)
                            {
                                return;
                            }

                            foreach (var cardName in _fullUpdateList)
                            {
                                if (cancellationTokenSource.IsCancellationRequested)
                                {
                                    return;
                                }

                                var cardVm = _deckBuilderVm.CardVms.First(cVm => cVm.Name == cardName);

                                if (!cardVm.AllTags.Any()) continue;

                                if (cardVm.AllTags.Any(tag => tag.ToLower() == filterText.ToLower()))
                                    filteredUpdateList.Add(cardName);
                            }
                        }
                        break;
                    case FilterMode.ByRole:
                        MessageBox.Show("This feature is Coming Soon!", "Coming Soon");
                        break;
                }
            });

            if (cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            CardsToIgnore = new ObservableCollection<string>(filteredIgnoreList);
            CardsToUpdate = new ObservableCollection<string>(filteredUpdateList);
        }

        public async void StartOrRestartFilterTags(string filterText)
        {
            if (_filterTagsTask != null && !_filterTagsTask.IsCompleted)
            {
                _filterTagsCancel.Cancel();
                await _filterTagsTask;
            }

            _filterTagsCancel = new CancellationTokenSource();
            _filterTagsTask = FilterTagsAsync(filterText, _filterTagsCancel);
            await _filterTagsTask;
        }

        public async Task FilterTagsAsync(string filterText, CancellationTokenSource cts)
        {
            var filteredList = new List<TagSummaryViewModel>();

            await Task.Run(() =>
            {
                foreach (var tagSummaryVm in _fullTagsList)
                {
                    if (cts.IsCancellationRequested)
                    {
                        break;
                    }

                    if (tagSummaryVm.Name.ToLower().Contains(filterText.ToLower()))
                    {
                        filteredList.Add(tagSummaryVm);
                    }
                }
            });

            if (cts.IsCancellationRequested)
            {
                return;
            }

            TagSummaryVms = new ObservableCollection<TagSummaryViewModel>(filteredList);
            RaisePropertyChanged(nameof(TagSummaryVms));
        }

        public async Task Retrieve()
        {
            CanRetrieve = false;

            if (_deck == null || _deck.Cards.Count == 0)
            {
                Status = "Deck invalid. Deck must contain at least one card.";
                return;
            }

            Status = "Deck valid. Retrieving tags...";
            var scryfallTagsResult = await _cardProvider.GetScryfallTagsForCardsAsync(
                _deck.Cards.Select((c) => c.Name).ToList(),
                new CancellationTokenSource());
            var tagsDictionary = scryfallTagsResult.Item1;
            var errors = scryfallTagsResult.Item2;
            Status = "Tags retrieved. Compiling summary...";
            Errors = $"Errors:\n{string.Join("\n", errors)}";
            TagSummaryVms = await Task.Run(() => CompileTagsSummary(tagsDictionary));
            _fullTagsList = TagSummaryVms.ToList();
            RaisePropertyChanged(nameof(TagSummaryVms));
            Status = $"Retrieval complete! Retrieved tags for {tagsDictionary.Keys.Count} card(s).";

            CanRetrieve = true;
        }

        private ObservableCollection<TagSummaryViewModel> CompileTagsSummary(Dictionary<string, List<string>> tagsDictionary)
        {
            var result = new List<TagSummaryViewModel>();

            // Create dictionary of tag counts keyed by tag name.
            var tagSummaries = new Queue<TagSummaryViewModel>();

            // Queue up the staple tags first.
            tagSummaries.Enqueue(new TagSummaryViewModel("ramp", 0));
            tagSummaries.Enqueue(new TagSummaryViewModel("card advantage", 0));
            tagSummaries.Enqueue(new TagSummaryViewModel("removal", 0));
            tagSummaries.Enqueue(new TagSummaryViewModel("sweeper", 0));
            tagSummaries.Enqueue(new TagSummaryViewModel("tapland", 0));

            foreach (var cardName in tagsDictionary.Keys)
            {
                var tags = tagsDictionary[cardName];

                foreach (var tag in tags)
                {
                    if (!tagSummaries.Any((tagSummary) => tagSummary.Name == tag))
                    {
                        var newTagSummary = new TagSummaryViewModel(tag);
                        newTagSummary.AddCard(cardName);
                        tagSummaries.Enqueue(newTagSummary);
                        continue;
                    }

                    var existingSummary = tagSummaries.First((tagSummary) => tagSummary.Name == tag);
                    existingSummary.AddCard(cardName);
                    existingSummary.IncrementCount();
                }
            }

            // Print staple counts first.
            for (var i = 0; i < 5; i++)
            {
                result.Add(tagSummaries.Dequeue());
            }

            // Sort the rest descending by count.
            tagSummaries = new Queue<TagSummaryViewModel>(
                tagSummaries.OrderByDescending((summary) => summary.Count));

            while (tagSummaries.Any())
            {
                result.Add(tagSummaries.Dequeue());
            }

            return new ObservableCollection<TagSummaryViewModel>(result);
        }

        public class TagSummaryViewModel : ViewModelBase
        {
            public string Name { get; }
            public int Count { get; private set; }

            private List<string> _cards;
            public List<string> Cards
            {
                get { return _cards; }
                set { SetProperty(ref _cards, value); }
            }

            public string Summary => ToString();

            public TagSummaryViewModel(string name, int count = 1)
            {
                Name = name;
                Count = count;
                Cards = new List<string>();
            }

            public void AddCard(string cardName)
            {
                Cards.Add(cardName);
                RaisePropertyChanged(nameof(Summary));
            }

            public void IncrementCount()
            {
                Count = ++Count;
                RaisePropertyChanged(nameof(Summary));
            }

            public override string ToString()
            {
                return $"{Name}: {Count}";
            }
        }
    }

    public class DeckRoleViewModel : ViewModelBase
    {
        private ObservableCollection<string> _tags;
        public ObservableCollection<string> Tags
        {
            get { return _tags; }
            set { SetProperty(ref _tags, value); }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        public string OriginalName { get; private set; }

        public DeckRoleViewModel()
        {

        }

        public DeckRoleViewModel(string name)
        {
            Name = name;
            Tags = new ObservableCollection<string>();
        }

        public DeckRoleViewModel(DeckRoleTagModel model)
        {
            OriginalName = model.RoleName;
            Name = model.RoleName;
            Tags = new ObservableCollection<string>(model.Tags);
        }

        public void AddTag(string tag)
        {
            if (Tags.Contains(tag))
            {
                return;
            }

            Tags.Add(tag);
        }

        public void RemoveTag(string tag)
        {
            if (!Tags.Contains(tag))
            {
                return;
            }

            Tags.Remove(tag);
        }

        public bool Renamed()
        {
            return Name != OriginalName;
        }

        public DeckRoleTagModel ToModel()
        {
            return new DeckRoleTagModel
            {
                RoleName = Name,
                Tags = Tags.ToList()
            };
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
