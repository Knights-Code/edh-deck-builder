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
        public string Name { get; set; } // Might not need to store this...

        public string MultiverseId { get; set; }

        public Image CardImage { get; set; } = null;

        public string BuildGathererUrl()
        {
            return $"https://gatherer.wizards.com/Handlers/Image.ashx?multiverseid={MultiverseId}&type=card";
        }
    }
}
