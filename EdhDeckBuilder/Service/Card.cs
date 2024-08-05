using EdhDeckBuilder.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Service
{
    public class Card
    {
        public string Name { get; set; }
        public string Uuid { get; set; }
        public string MultiverseId { get; set; }
        public string ScryfallId { get; set; }
        public string CollectorNumber { get; set; }
        public string SetCode { get; set; }
        public List<string> AllTypes { get; set; }
        public Image CardImage { get; set; } = null;
        public Image BackImage { get; set; } = null;
        public int NumCopies { get; set; }
        public List<Role> Roles { get; set; } = new List<Role>();
        public bool HasDownloadableImage => !string.IsNullOrEmpty(ScryfallId) || !string.IsNullOrEmpty(MultiverseId);

        public Card()
        {

        }

        public Card(CardModel cardModel)
        {
            Name = cardModel.Name;
            MultiverseId = cardModel.MultiverseId;
            ScryfallId = cardModel.ScryfallId;
            CollectorNumber = cardModel.CollectorNumber;
            SetCode = cardModel.SetCode;
            AllTypes = new List<string>(cardModel.AllTypes);
            CardImage = cardModel.CardImage;
            BackImage = cardModel.BackImage;
            NumCopies = cardModel.NumCopies;
            Roles = cardModel.Roles.Select((roleModel) => new Role(roleModel)).ToList();
        }

        public CardModel ToModel()
        {
            return new CardModel
            {
                Name = Name,
                MultiverseId = MultiverseId,
                ScryfallId = ScryfallId,
                CollectorNumber = CollectorNumber,
                SetCode = SetCode,
                AllTypes = new List<string>(AllTypes),
                CardImage = CardImage,
                BackImage = BackImage,
                NumCopies = NumCopies,
                Roles = Roles.Select((role) => role.ToModel()).ToList()
            };
        }

        public string BuildGathererUrl()
        {
            if (string.IsNullOrEmpty(MultiverseId)) return string.Empty;

            return $"https://gatherer.wizards.com/Handlers/Image.ashx?multiverseid={MultiverseId}&type=card";
        }

        public string BuildScryfallUrl(bool back = false)
        {
            if (string.IsNullOrEmpty(ScryfallId)) return string.Empty;

            var firstChar = ScryfallId[0];
            var secondChar = ScryfallId[1];
            return $"https://cards.scryfall.io/large/{(back ? "back" : "front")}/{firstChar}/{secondChar}/{ScryfallId}.jpg";
        }
    }
}
