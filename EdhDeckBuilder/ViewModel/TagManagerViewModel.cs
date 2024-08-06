using EdhDeckBuilder.Model;
using EdhDeckBuilder.Service;
using Microsoft.Practices.Prism.Commands;
using System;
using System.Collections.Generic;
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

        private List<string> _tagSummaries;
        public List<string> TagSummaries
        {
            get { return _tagSummaries; }
            set { SetProperty(ref _tagSummaries, value); }
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

        public ICommand RetrieveCommand { get; set; }
        public ICommand ResetFilterCommand { get; set; }

        private List<string> _fullTagsList;
        private readonly DeckModel _deck;
        private readonly CardProvider _cardProvider;
        private Task _filterTask;
        private CancellationTokenSource _filterCancel;

        public TagManagerViewModel(string title, DeckModel deck, CardProvider cardProvider)
        {
            Title = title;
            Status = "Idle";
            CanRetrieve = true;
            _deck = deck;
            _cardProvider = cardProvider;
            _fullTagsList = new List<string>();
            TagSummaries = new List<string>();
            _filterCancel = new CancellationTokenSource();
            RetrieveCommand = new DelegateCommand(async () => await Retrieve());
            ResetFilterCommand = new DelegateCommand(ResetFilter);
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
            var filteredList = new List<string>();

            await Task.Run(() =>
            {
                foreach (var tagSummary in _fullTagsList)
                {
                    if (cts.IsCancellationRequested)
                    {
                        break;
                    }

                    if (tagSummary.ToLower().Contains(filterText.ToLower()))
                    {
                        filteredList.Add(tagSummary);
                    }
                }
            });

            if (cts.IsCancellationRequested)
            {
                return;
            }

            TagSummaries = filteredList;
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
            TagSummaries = await Task.Run(() => CompileTagsSummary(tagsDictionary));
            _fullTagsList = TagSummaries;
            Status = $"Retrieval complete! Retrieved tags for {tagsDictionary.Keys.Count} card(s).";

            CanRetrieve = true;
        }

        private List<string> CompileTagsSummary(Dictionary<string, List<string>> tagsDictionary)
        {
            var result = new List<string>();

            // Create dictionary of tag counts keyed by tag name.
            var tagSummaries = new Queue<TagSummary>();

            // Queue up the staple tags first.
            tagSummaries.Enqueue(new TagSummary("ramp", 0));
            tagSummaries.Enqueue(new TagSummary("card advantage", 0));
            tagSummaries.Enqueue(new TagSummary("removal", 0));
            tagSummaries.Enqueue(new TagSummary("sweeper", 0));
            tagSummaries.Enqueue(new TagSummary("tapland", 0));

            foreach (var cardName in tagsDictionary.Keys)
            {
                var tags = tagsDictionary[cardName];

                foreach (var tag in tags)
                {
                    if (!tagSummaries.Any((tagSummary) => tagSummary.Name == tag))
                    {
                        var newTagSummary = new TagSummary(tag);
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
            for (var i=0; i < 5; i++)
            {
                result.Add(tagSummaries.Dequeue().ToString() + "\n");
            }

            // Sort the rest descending by count.
            tagSummaries = new Queue<TagSummary>(
                tagSummaries.OrderByDescending((summary) => summary.Count));

            while (tagSummaries.Any())
            {
                result.Add(tagSummaries.Dequeue().ToString() + "\n");
            }

            return result;
        }

        class TagSummary
        {
            public string Name { get; }
            public int Count { get; private set; }
            public List<string> Cards {get; private set; }
            public TagSummary(string name, int count = 1)
            {
                Name = name;
                Count = count;
                Cards = new List<string>();
            }

            public void AddCard(string cardName)
            {
                Cards.Add(cardName);
            }

            public void IncrementCount()
            {
                Count = ++Count;
            }

            public override string ToString()
            {
                return $"{Name}: {Count}";
                return $"{Name}: {Count} ({string.Join(", ", Cards)})";
            }
        }
    }
}
