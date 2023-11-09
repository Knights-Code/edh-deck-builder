using EdhDeckBuilder.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.ViewModel
{
    public class CardViewModel : ViewModelBase
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private Image _cardImage;
        public Image CardImage
        {
            get { return _cardImage; }
            set { SetProperty(ref _cardImage, value); }
        }

        public CardViewModel(CardModel model)
        {
            _name = model.Name;
            _cardImage = model.CardImage;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
