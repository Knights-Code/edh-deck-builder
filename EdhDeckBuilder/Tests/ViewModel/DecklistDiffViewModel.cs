using EdhDeckBuilder.Model;
using EdhDeckBuilder.Service;
using EdhDeckBuilder.ViewModel;
using Microsoft.Practices.Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EdhDeckBuilder.Tests.ViewModel
{
    public class DecklistDiffViewModel : ViewModelBase
    {
        private string _title;
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private string _diffDeck;
        public string DiffDeck
        {
            get { return _diffDeck; }
            set { SetProperty(ref _diffDeck, value); }
        }

        private string _addsHeader;
        public string AddsHeader
        {
            get { return _addsHeader; }
            set { SetProperty(ref _addsHeader, value); }
        }

        private ObservableCollection<string> _cardsToAdd;
        public ObservableCollection<string> CardsToAdd
        {
            get { return _cardsToAdd; }
            set { SetProperty(ref _cardsToAdd, value); }
        }

        private string _cutsHeader;
        public string CutsHeader
        {
            get { return _cutsHeader; }
            set { SetProperty(ref _cutsHeader, value); }
        }

        private ObservableCollection<string> _cardsToCut;
        public ObservableCollection<string> CardsToCut
        {
            get { return _cardsToCut; }
            set { SetProperty(ref _cardsToCut, value); }
        }

        private DeckModel _deckBuilderDeck;

        public ICommand DiffCommand { get; set; }

        public DecklistDiffViewModel(string title, DeckModel deckBuilderDeck)
        {
            Title = title;
            _deckBuilderDeck = deckBuilderDeck;

            DiffCommand = new DelegateCommand(Diff);
        }

        public void Diff()
        {
            var diffDeckCards = new Stack<CardModel>(UtilityFunctions.ParseCardsFromText(DiffDeck));
            var cardsToAdd = new List<string>();
            var cardsToCut = new List<string>();

            // Firstly, go through all the cards in the diff deck list, and determine if any cuts
            // need to be made.
            foreach (var card in diffDeckCards)
            {
                var newDeckCard = _deckBuilderDeck.Cards.FirstOrDefault(c => c.Name == card.Name);

                if (newDeckCard == null)
                {
                    // Card should be cut entirely.
                    cardsToCut.Add($"-{card.NumCopies} {card.Name}");
                    continue;
                }

                // Card is in both decks. Check copies.
                var copiesDelta = newDeckCard.NumCopies - card.NumCopies;

                if (copiesDelta < 0)
                {
                    // Diff deck has more copies than new deck. Need to cut some copies.
                    cardsToCut.Add($"{copiesDelta} {card.Name}");
                }

                if (copiesDelta > 0)
                {
                    // Diff deck has fewer copies than new deck. Need to add more copies.
                    cardsToAdd.Add($"+{copiesDelta} {card.Name}");
                }

                // Remove from new deck so we don't re-process it later.
                _deckBuilderDeck.Cards.Remove(newDeckCard);
            }

            // At this point, the only cards left to process in the new deck are cards to add.
            cardsToAdd.AddRange(_deckBuilderDeck.Cards.Select(c => $"+{c.NumCopies} {c.Name}"));

            // Update UI.
            CardsToAdd = new ObservableCollection<string>(cardsToAdd);
            AddsHeader = $"Adds ({cardsToAdd.Count} new cards)";
            CardsToCut = new ObservableCollection<string>(cardsToCut);
            CutsHeader = $"Cuts ({cardsToCut.Count} cards no longer in deck)";
        }
    }
}
