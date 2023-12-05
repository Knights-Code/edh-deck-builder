using Microsoft.Practices.Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace EdhDeckBuilder.ViewModel
{
    public class RoleRankingsViewModel : ViewModelBase
    {
        private ObservableCollection<CardViewModel> _cards;
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

        private int _selectedIndex;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set { SetProperty(ref _selectedIndex, value); }
        }


        private string _role;
        public string Role
        {
            get { return _role; }
            set { SetProperty(ref _role, value); }
        }

        public ICommand MoveUpCommand { get; set; }
        public ICommand MoveDownCommand { get; set; }
        public ICommand SaveAndCloseCommand { get; set; }

        public RoleRankingsViewModel(List<CardViewModel> cards, string role)
        {

            Cards = new ObservableCollection<CardViewModel>(cards);
            Role = role;

            if (Cards.Any()) SelectedCard = Cards.First();

            MoveUpCommand = new DelegateCommand(MoveSelectedUp, CanMoveSelectedUp);
            MoveDownCommand = new DelegateCommand(MoveSelectedDown, CanMoveSelectedDown);
            SaveAndCloseCommand = new DelegateCommand<Window>(SaveAndClose);
        }

        public bool CanMoveSelectedUp()
        {
            return Cards.IndexOf(SelectedCard) > 0;
        }
        public void MoveSelectedUp()
        {
            var currentIndex = SelectedIndex;
            var card = Cards[currentIndex];
            Cards.RemoveAt(currentIndex);
            Cards.Insert(--currentIndex, card);
            SelectedIndex = currentIndex;
        }

        public bool CanMoveSelectedDown()
        {
            // Collection is zero-indexed, so index of last card will be
            // the number of items in the collection minus one.
            return Cards.IndexOf(SelectedCard) < Cards.Count - 1;
        }
        public void MoveSelectedDown()
        {
            var currentIndex = SelectedIndex;
            var card = Cards[currentIndex];
            Cards.RemoveAt(currentIndex);
            Cards.Insert(++currentIndex, card);
            SelectedIndex = currentIndex;
        }

        public void SaveAndClose(Window window)
        {
            var rankingValue = 1;

            foreach (var card in Cards)
            {
                var roleVm = card.RoleVms.FirstOrDefault(r => r.Name == Role);

                if (roleVm == null) throw new InvalidOperationException($"Attempted to update {Role} role ranking for a card without {Role} role.");

                roleVm.Value = rankingValue++;
            }

            if (window != null) window.Close();
        }
    }
}
