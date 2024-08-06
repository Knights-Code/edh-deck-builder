using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Model
{
    public class DeckModel
    {
        public string Name { get; private set; }
        public List<CardModel> Cards { get; private set; }
        public List<string> CustomRoles { get; set; }
        public List<DeckRoleTagModel> RoleAndTagGroupings { get; set; } = new List<DeckRoleTagModel>();
        public decimal FileVersion { get; set; }

        public DeckModel(string name)
        {
            Name = name;
            Cards = new List<CardModel>();
            CustomRoles = new List<string>();
        }

        public void AddCards(IEnumerable<CardModel> cards)
        {
            Cards.AddRange(cards);
        }
    }
}
