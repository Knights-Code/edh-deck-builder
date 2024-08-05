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

        public ICommand RetrieveCommand { get; set; }

        private readonly DeckModel _deck;
        private readonly CardProvider _cardProvider;

        public TagManagerViewModel(string title, DeckModel deck, CardProvider cardProvider)
        {
            Title = title;
            Status = "Idle";
            CanRetrieve = true;
            _deck = deck;
            _cardProvider = cardProvider;
            RetrieveCommand = new DelegateCommand(async () => await Retrieve());
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
            TagsSummary = await Task.Run(() => CompileTagsSummary(tagsDictionary, errors));
            Status = $"Retrieval complete! Retrieved tags for {tagsDictionary.Keys.Count} card(s).";

            CanRetrieve = true;
        }

        private string CompileTagsSummary(Dictionary<string, List<string>> tagsDictionary,
            List<string> errors)
        {
            var result = "";

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

            // Frontload errors.
            if (errors.Any())
            {
                result += "Errors:\n";
                result += $"{string.Join("\n", errors)}";
                result += "\n\n";
            }

            // Print staple counts second.
            for (var i=0; i < 5; i++)
            {
                result += tagSummaries.Dequeue().ToString() + "\n";
            }

            // Sort the rest descending by count.
            tagSummaries = new Queue<TagSummary>(
                tagSummaries.OrderByDescending((summary) => summary.Count));

            while (tagSummaries.Any())
            {
                result += tagSummaries.Dequeue().ToString() + "\n";
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
                return $"{Name}: {Count} ({string.Join(", ", Cards)})";
            }
        }
    }
}
