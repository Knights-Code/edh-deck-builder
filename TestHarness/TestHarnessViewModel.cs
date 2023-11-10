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

        public ICommand NewCardEnterCommand { get; set; }

        public TestHarnessViewModel()
        {
            NewCardEnterCommand = new DelegateCommand(AddNewCard, () => !string.IsNullOrEmpty(NewCardName));
            DeckBuilderVm = new DeckBuilderViewModel();

            _cardProvider = new CardProvider();
            _cardProvider.Initialise();
        }

        public void AddNewCard()
        {
            DeckBuilderVm.AddCard(NewCardName);
            // var cardModel = _cardProvider.TryGetCard(NewCardName);
            // 
            // if (cardModel == null) return;
            // 
            // var cardVm = new CardViewModel(cardModel);
            // 
            // if (cardVm.CardImage == null)
            // {
            //     cardVm.CardImage = _cardProvider.DownloadImageForCard(cardModel);
            // }
            // 
            // Cards.Add(cardVm);
        }
    }
}
