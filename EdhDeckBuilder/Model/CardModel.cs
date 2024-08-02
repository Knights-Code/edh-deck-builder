using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Model
{
    public class CardModel
    {
        public string Name { get; set; }
        public string MultiverseId { get; set; }
        public string ScryfallId { get; set; }
        public string CollectorNumber { get; set; }
        public string SetCode { get; set; }
        public List<string> AllTypes { get; set; }
        public Image CardImage { get; set; } = null;
        public Image BackImage { get; set; } = null;
        public int NumCopies { get; set; }
        public List<RoleModel> Roles { get; set; }
        public bool HasDownloadableImage => !string.IsNullOrEmpty(ScryfallId) || !string.IsNullOrEmpty(MultiverseId);

        public CardModel()
        {
            AllTypes = new List<string>();
            Roles = new List<RoleModel>();
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

        public string BuildScryfallTaggerUrl()
        {
            if (string.IsNullOrEmpty(SetCode) || string.IsNullOrEmpty(CollectorNumber)) return string.Empty;

            return $"https://tagger.scryfall.com/card/{SetCode}/{CollectorNumber}";
        }

        public string ClipboardFormat()
        {
            return $"{NumCopies} {Name}";
        }
    }
}
