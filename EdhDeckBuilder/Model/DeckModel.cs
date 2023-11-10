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
        public List<string> CustomRoles { get; private set; }

    }
}
