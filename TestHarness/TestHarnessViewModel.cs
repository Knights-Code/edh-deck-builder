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

namespace TestHarness
{
    public class TestHarnessViewModel : ViewModelBase
    {
        private CardProvider _cardProvider;

        private DeckBuilderViewModel _deckBuilderVm;
        public DeckBuilderViewModel DeckBuilderVm
        {
            get { return _deckBuilderVm; }
            set { SetProperty(ref _deckBuilderVm, value); }
        }


        private ObservableCollection<CardViewModel> _cards = new ObservableCollection<CardViewModel>();
        public ObservableCollection<CardViewModel> Cards
        {
            get { return _cards; }
            set { SetProperty(ref _cards, value); }
        }

        private CardViewModel _selectedCard;
        public CardViewModel SelectedCard
        {
            get { return _selectedCard; }
            set { SetProperty(ref _selectedCard, value); }
        }

        private string _newCardName;
        public string NewCardName
        {
            get { return _newCardName; }
            set { SetProperty(ref _newCardName, value); }
        }

        #region Menu Commands
        public ICommand NewDeckCommand { get; set; }
        public ICommand SaveDeckCommand { get; set; }
        public ICommand SaveDeckAsCommand { get; set; }
        public ICommand OpenDeckCommand { get; set; }
        #endregion

        public ICommand NewCardEnterCommand { get; set; }

        public TestHarnessViewModel()
        {
            NewCardEnterCommand = new DelegateCommand(AddNewCard, () => !string.IsNullOrEmpty(NewCardName));

            // Menu Commands //
            // TODO: New Deck
            NewDeckCommand = new DelegateCommand(NewDeck);
            SaveDeckCommand = new DelegateCommand(SaveDeck);
            // TODO: Save Deck As
            OpenDeckCommand = new DelegateCommand(OpenDeck);
            DeckBuilderVm = new DeckBuilderViewModel();

            _cardProvider = new CardProvider();
            _cardProvider.Initialise();
        }

        public void NewDeck()
        {
            DeckBuilderVm.NewDeck();
        }

        public void SaveDeck()
        {
            DeckBuilderVm.SaveDeck();
        }

        public void OpenDeck()
        {
            DeckBuilderVm.OpenDeck();
        }

        public void AddNewCard()
        {
            DeckBuilderVm.AddCard(NewCardName);
        }
    }
}
