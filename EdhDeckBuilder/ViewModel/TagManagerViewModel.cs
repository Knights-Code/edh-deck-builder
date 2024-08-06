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
                    StartOrRestartFilter(value);
                }
            }
        }

        private List<string> _deckRoles;
        public List<string> DeckRoles
        {
            get { return _deckRoles; }
            set { SetProperty(ref _deckRoles, value); }
        }

        public ICommand RetrieveCommand { get; set; }
        public ICommand ResetFilterCommand { get; set; }
        public ICommand RemoveTagFromRoleCommand { get; set; }
        public ICommand AddTagToRoleCommand { get; set; }
        public ICommand UpdateRolesInDeckCommand { get; set; }

        private List<TagSummaryViewModel> _fullTagsList;
        private readonly DeckModel _deck;
        private readonly CardProvider _cardProvider;
        private readonly DeckBuilderViewModel _deckBuilderVm;
        private Task _filterTask;
        private CancellationTokenSource _filterCancel;

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
            TagSummaryVms = new ObservableCollection<TagSummaryViewModel>();
            _filterCancel = new CancellationTokenSource();

            RetrieveCommand = new DelegateCommand(async () => await Retrieve());
            ResetFilterCommand = new DelegateCommand(ResetFilter);
            AddTagToRoleCommand = new DelegateCommand(AddTagToRole);
            RemoveTagFromRoleCommand = new DelegateCommand(RemoveTagFromRole);
            UpdateRolesInDeckCommand = new DelegateCommand(UpdateRolesInDeck);

            DeckRoleVms = new ObservableCollection<DeckRoleViewModel>();
            var rampRoleVm = new DeckRoleViewModel("Ramp");
            DeckRoleVms.Add(rampRoleVm);
        }

        public void UpdateRolesInDeck()
        {
            _deckBuilderVm.UpdateRolesWithTags(DeckRoleVms.ToList(),
                new CancellationTokenSource());
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

        public async void StartOrRestartFilter(string filterText)
        {
            if (_filterTask != null && !_filterTask.IsCompleted)
            {
                _filterCancel.Cancel();
                await _filterTask;
            }

            _filterCancel = new CancellationTokenSource();
            _filterTask = FilterAsync(filterText, _filterCancel);
            await _filterTask;
        }

        public async Task FilterAsync(string filterText, CancellationTokenSource cts)
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

        public DeckRoleViewModel(string name)
        {
            Name = name;
            Tags = new ObservableCollection<string>();
        }

        public DeckRoleViewModel(DeckRoleTagModel model)
        {
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
