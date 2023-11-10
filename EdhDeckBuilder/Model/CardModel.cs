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
        public Image CardImage { get; set; } = null;
        public int NumCopies { get; set; }
        public List<RoleModel> Roles { get; set; }

        public CardModel()
        {
            Roles = new List<RoleModel>();
        }

        public string BuildGathererUrl()
        {
            return $"https://gatherer.wizards.com/Handlers/Image.ashx?multiverseid={MultiverseId}&type=card";
        }
    }
}
